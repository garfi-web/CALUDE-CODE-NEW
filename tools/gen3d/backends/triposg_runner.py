"""Runner TripoSG : image → .glb (forme seule). Lancé par generate.py dans le venv de TripoSG.

Licences : TripoSG (code et poids) est sous MIT. Le script d'origine de TripoSG détoure les
images avec RMBG-1.4 (BRIA), réservé à un usage non commercial : on ne le télécharge ni ne
l'utilise. Le détourage se fait ici sans modèle (fond uni relié aux bords de l'image), et le
runner refuse une image qu'il n'arrive pas à détourer plutôt que de retomber sur RMBG.

Sans le paquet `diso` (extension CUDA à compiler), on utilise le décodeur hiérarchique de
TripoSG, qui extrait la surface avec skimage.
"""
import argparse
import importlib.util
import json
import os
import sys
import time
import types

import cv2
import numpy as np
import torch
import trimesh
from huggingface_hub import snapshot_download

ALPHA_MIN_RATIO = 0.01  # même seuil que is_valid_alpha() dans TripoSG


def has_valid_alpha(alpha):
    total = alpha.size
    return (alpha <= 12).sum() >= total * ALPHA_MIN_RATIO and (alpha >= 243).sum() >= total * ALPHA_MIN_RATIO


def remove_plain_background(bgr, tolerance):
    """Rend transparent le fond uni relié aux bords de l'image."""
    border = np.concatenate([bgr[0], bgr[-1], bgr[:, 0], bgr[:, -1]]).astype(np.int16)
    background = np.median(border, axis=0)
    distance = np.abs(bgr.astype(np.int16) - background).max(axis=2)
    candidate = (distance <= tolerance).astype(np.uint8)

    _, labels = cv2.connectedComponents(candidate, connectivity=4)
    edge_labels = np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    edge_labels = edge_labels[edge_labels != 0]
    background_mask = np.isin(labels, edge_labels) & (candidate == 1)

    alpha = np.where(background_mask, 0, 255).astype(np.uint8)
    kernel = np.ones((3, 3), np.uint8)
    alpha = cv2.morphologyEx(alpha, cv2.MORPH_OPEN, kernel)
    return alpha, background.tolist()


def prepare_rgba(image_path, out_path, tolerance):
    img = cv2.imread(image_path, cv2.IMREAD_UNCHANGED)
    if img is None:
        raise SystemExit(f"Image illisible : {image_path}")
    if img.ndim == 2:
        img = cv2.cvtColor(img, cv2.COLOR_GRAY2BGR)

    info = {}
    if img.shape[2] == 4 and has_valid_alpha(img[:, :, 3]):
        rgba = img
        info["background"] = "alpha d'origine"
    else:
        bgr = img[:, :, :3]
        alpha, background = remove_plain_background(bgr, tolerance)
        rgba = np.dstack([bgr, alpha])
        info["background"] = f"fond uni retiré (couleur BGR {[round(c) for c in background]}, tolérance {tolerance})"

    if not has_valid_alpha(rgba[:, :, 3]):
        raise SystemExit("Détourage impossible : utilisez une image à fond uni ou déjà transparente.")

    cv2.imwrite(out_path, rgba)
    info["opaque_ratio"] = round(float((rgba[:, :, 3] > 127).mean()), 3)
    return info


def stub_diso_if_missing():
    if importlib.util.find_spec("diso") is not None:
        return True
    stub = types.ModuleType("diso")

    class DiffDMC:  # jamais appelé : on force use_flash_decoder=False
        def __init__(self, *args, **kwargs):
            raise RuntimeError("diso n'est pas installé")

    stub.DiffDMC = DiffDMC
    sys.modules["diso"] = stub
    return False


def simplify(mesh, faces):
    if faces <= 0 or len(mesh.faces) <= faces:
        return mesh
    import pymeshlab

    ms = pymeshlab.MeshSet()
    ms.add_mesh(pymeshlab.Mesh(vertex_matrix=mesh.vertices, face_matrix=mesh.faces))
    ms.meshing_merge_close_vertices()
    ms.meshing_decimation_quadric_edge_collapse(targetfacenum=faces, preservenormal=True)
    current = ms.current_mesh()
    return trimesh.Trimesh(vertices=current.vertex_matrix(), faces=current.face_matrix())


@torch.no_grad()
def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", required=True)
    parser.add_argument("--image", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--faces", type=int, default=40000)
    parser.add_argument("--steps", type=int, default=50)
    parser.add_argument("--guidance", type=float, default=7.0)
    parser.add_argument("--tolerance", type=int, default=24)
    args = parser.parse_args()

    sys.path.insert(0, args.repo)
    sys.path.insert(0, os.path.join(args.repo, "scripts"))
    has_diso = stub_diso_if_missing()

    from triposg.pipelines.pipeline_triposg import TripoSGPipeline
    from image_process import prepare_image

    preprocessed = os.path.splitext(args.out)[0] + "_input.png"
    background_info = prepare_rgba(args.image, preprocessed, args.tolerance)

    weights = os.path.join(args.repo, "pretrained_weights", "TripoSG")
    snapshot_download(repo_id="VAST-AI/TripoSG", local_dir=weights)

    device, dtype = "cuda", torch.float16
    torch.cuda.reset_peak_memory_stats()
    start = time.time()
    pipe = TripoSGPipeline.from_pretrained(weights).to(device, dtype)

    # rmbg_net=None : l'alpha est valide, TripoSG ne fait donc pas appel à RMBG
    image = prepare_image(preprocessed, bg_color=np.array([1.0, 1.0, 1.0]), rmbg_net=None)
    output = pipe(
        image=image,
        generator=torch.Generator(device=device).manual_seed(args.seed),
        num_inference_steps=args.steps,
        guidance_scale=args.guidance,
        use_flash_decoder=has_diso,
    ).samples[0]

    if output[0] is None:
        raise SystemExit("TripoSG n'a pas pu extraire de surface.")

    mesh = trimesh.Trimesh(output[0].astype(np.float32), np.ascontiguousarray(output[1]))
    raw_faces = len(mesh.faces)
    mesh = simplify(mesh, args.faces)
    mesh.export(args.out)

    result = {
        "faces_raw": raw_faces,
        "faces": int(len(mesh.faces)),
        "vertices": int(len(mesh.vertices)),
        "watertight": bool(mesh.is_watertight),
        "extents": [round(float(x), 4) for x in mesh.extents],
        "decoder": "flash (diso)" if has_diso else "hiérarchique (skimage)",
        "vram_peak_gb": round(torch.cuda.max_memory_allocated() / 1024**3, 2),
        "inference_s": round(time.time() - start, 1),
        "preprocessed_image": preprocessed,
        **background_info,
    }
    print("[result] " + json.dumps(result, ensure_ascii=False))


if __name__ == "__main__":
    main()
