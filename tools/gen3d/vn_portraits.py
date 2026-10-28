"""Portraits du visual novel (Mara), via ComfyUI + Animagine XL 4.0 + ControlNet Union SDXL (pose).

Méthode (docs/PRODUCTION.md, 2026-10-28) :
- la pose vient d'un squelette OpenPose dessiné ici, aux proportions de Mara mesurées sur son modèle 3D
  (1,78 m, épaules à 1,45 m, hanches à 0,92 m), cadré des genoux au-dessus de la tête ;
- une seule passe texte → image par portrait ; les expressions gardent la même graine, la même pose et
  la même description, seuls les mots d'expression changent. Aucune image n'est dérivée d'une autre
  (pas d'img2img, pas d'inpainting) ni d'un concept validé (règle de l'auteur) ;
- fond noir uni, pour le détourage (tools/gen3d/vn_cutout.py, BiRefNet).

    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/vn_portraits.py --stage T0 --seeds 8501 8502 8503 --only front:moqueuse
    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/vn_portraits.py --stage T0 --seeds 8502

ComfyUI doit tourner. Sorties brutes : ArtSource/VN/Mara/<stade>/ (avec un .json par image : prompt, graine, réglages).
"""
import argparse
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
import concept  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "ArtSource" / "VN" / "Mara"
SIZE = (832, 1216)
CONTROLNET = "controlnet-union-sdxl-1.0-promax.safetensors"

# Mara commune à toutes les tenues (docs/CHARACTERS.md), reprise du prompt du concept validé.
MARA = ("1girl, solo, adult, mature female, 28 years old, mature face, tall, voluptuous, large breasts, narrow waist, "
        "wide hips, very long hair, black hair, red streaks, streaked hair, multicolored hair, blunt bangs, red eyes")

# Tenues (docs/CHARACTERS.md, « Progression des tenues ») : seule M-T0 est produite avant validation.
OUTFITS = {
    "T0": ("black bodysuit, latex bodysuit, shiny clothes, high collar, turtleneck, red straps, crossed straps, corset, "
           "buckles, long skirt, side slit, thigh boots, fully covered"),
}

POSES = {
    "front": "cowboy shot, standing, hand on own hip, holding sword over shoulder, sword resting on shoulder, "
             "glowing red energy sword, head tilt, contrapposto, front view, facing viewer, looking at viewer",
    "back": "cowboy shot, standing, from behind, back, arched back, looking back, looking over shoulder, "
            "hand on own hip, holding sword, glowing red energy sword, sword pointing down",
}

EXPRESSIONS = {
    "moqueuse": "smirk, smug, teasing, one eyebrow raised, half-closed eyes",
    "meprisante": "disdainful, contempt, looking down on viewer, cold eyes, frown, closed mouth",
    "amusee": "smile, amused, light laugh, closed eyes smile, happy",
    "troublee": "flustered, blush, surprised, parted lips, wide eyes, looking to the side",
    "colere": "angry, glaring, furrowed brows, v-shaped eyebrows, clenched teeth, shouting",
}

STYLE = ("anime coloring, cel shading, clean lineart, detailed face, dark fantasy, dramatic rim light, "
         "simple background, black background, safe")
NEGATIVE = ("nsfw, nude, nipples, explicit, cleavage cutout, child, loli, young, petite, flat chest, aged down, "
            "baby face, male, t-pose, spread arms, arms outstretched, full body, feet, multiple views, logo, text, "
            "letters, watermark, signature, lowres, bad anatomy, bad hands, extra fingers, missing fingers, "
            "extra limbs, extra arms, extra swords, cropped head, out of frame, chibi, big head, worst quality, "
            "low quality, low score, bad score, average score, blurry, white background, grey background, "
            "busy background, scenery")

# Squelettes OpenPose (ordre COCO 18), en pixels de 832 × 1216 ; None = point caché.
# Échelle : 852 px par mètre, genoux à y = 1130, haut de la tête à y = 40.
SKELETONS = {
    "front": [
        (424, 178),  # nez (tête légèrement penchée)
        (416, 321),  # cou
        (263, 330), (236, 560), (300, 410),   # épaule, coude, poignet droits : main près de l'épaule (lame)
        (569, 330), (680, 560), (578, 762),   # épaule, coude, poignet gauches : main sur la hanche
        (345, 785), (332, 1130), None,        # hanche, genou, cheville droits
        (490, 762), (505, 1124), None,        # hanche, genou, cheville gauches
        (397, 150), (451, 152), (371, 162), (478, 166),  # yeux, oreilles
    ],
    "back": [
        (352, 188),  # nez : tête tournée par-dessus l'épaule gauche
        (416, 325),
        (569, 338), (612, 585), (636, 800),   # bras droit le long du corps (côté droit de l'image, de dos)
        (263, 334), (186, 560), (300, 762),   # bras gauche, main sur la hanche
        (482, 782), (500, 1130), None,
        (350, 770), (338, 1130), None,
        None, (372, 158), (466, 166), None,   # œil gauche visible, oreille droite visible
    ],
}
LIMBS = [(1, 2), (1, 5), (2, 3), (3, 4), (5, 6), (6, 7), (1, 8), (8, 9), (9, 10), (1, 11), (11, 12), (12, 13),
         (1, 0), (0, 14), (14, 16), (0, 15), (15, 17)]
COLORS = [(255, 0, 0), (255, 85, 0), (255, 170, 0), (255, 255, 0), (170, 255, 0), (85, 255, 0), (0, 255, 0),
          (0, 255, 85), (0, 255, 170), (0, 255, 255), (0, 170, 255), (0, 85, 255), (0, 0, 255), (85, 0, 255),
          (170, 0, 255), (255, 0, 255), (255, 0, 170), (255, 0, 85)]


def draw_openpose(points, path):
    """Squelette au format OpenPose (couleurs standard), sur fond noir."""
    image = Image.new("RGB", SIZE, (0, 0, 0))
    draw = ImageDraw.Draw(image)

    for i, (a, b) in enumerate(LIMBS):
        if points[a] is None or points[b] is None:
            continue
        color = tuple(int(c * 0.6) for c in COLORS[i])
        draw.line([points[a], points[b]], fill=color, width=10)

    for i, p in enumerate(points):
        if p is not None:
            draw.ellipse([p[0] - 6, p[1] - 6, p[0] + 6, p[1] + 6], fill=COLORS[i])

    image.save(path)
    return path


def workflow(cfg, prompt, negative, seed, pose_image, strength=0.9, end=0.8):
    width, height = SIZE
    return {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": cfg["checkpoint"]}},
        "2": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["1", 1], "text": prompt}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["1", 1], "text": negative}},
        "4": {"class_type": "EmptyLatentImage", "inputs": {"width": width, "height": height, "batch_size": 1}},
        "10": {"class_type": "ControlNetLoader", "inputs": {"control_net_name": CONTROLNET}},
        "11": {"class_type": "SetUnionControlNetType", "inputs": {"control_net": ["10", 0], "type": "openpose"}},
        "12": {"class_type": "LoadImage", "inputs": {"image": pose_image}},
        "13": {"class_type": "ControlNetApplyAdvanced", "inputs": {
            "positive": ["2", 0], "negative": ["3", 0], "control_net": ["11", 0], "image": ["12", 0],
            "strength": strength, "start_percent": 0.0, "end_percent": end, "vae": ["1", 2]}},
        "5": {"class_type": "KSampler", "inputs": {
            "model": ["1", 0], "positive": ["13", 0], "negative": ["13", 1], "latent_image": ["4", 0],
            "seed": seed, "steps": cfg.get("steps", 28), "cfg": cfg.get("cfg", 5.0),
            "sampler_name": cfg.get("sampler", "euler_ancestral"), "scheduler": cfg.get("scheduler", "normal"),
            "denoise": 1.0}},
        "6": {"class_type": "VAEDecode", "inputs": {"samples": ["5", 0], "vae": ["1", 2]}},
        "7": {"class_type": "SaveImage", "inputs": {"images": ["6", 0], "filename_prefix": "redmet_vn"}},
    }


def run(cfg, prompt, seed, pose_path, out_path):
    """Envoie le graphe et copie l'image (même attente que concept.generate)."""
    import shutil
    import time
    import urllib.parse
    import urllib.request
    import uuid

    base = cfg["url"].rstrip("/")
    pose_name = concept.upload_image(base, pose_path)
    graph = workflow(cfg, prompt, NEGATIVE, seed, pose_name)
    prompt_id = json.loads(concept._request(f"{base}/prompt", {"prompt": graph, "client_id": str(uuid.uuid4())}))["prompt_id"]
    start = time.time()

    while True:
        history = json.loads(concept._request(f"{base}/history/{prompt_id}"))
        entry = history.get(prompt_id)
        if entry:
            if entry.get("status", {}).get("status_str") == "error":
                raise RuntimeError(json.dumps(entry["status"])[:600])
            images = [img for node in entry.get("outputs", {}).values() for img in node.get("images", [])]
            if images:
                break
        if time.time() - start > 900:
            raise TimeoutError("ComfyUI n'a pas rendu d'image à temps")
        time.sleep(1.0)

    image = images[0]
    query = urllib.parse.urlencode({"filename": image["filename"], "subfolder": image["subfolder"], "type": image["type"]})
    with urllib.request.urlopen(f"{base}/view?{query}", timeout=60) as resp, open(out_path, "wb") as f:
        shutil.copyfileobj(resp, f)

    info = {"tool": "ComfyUI", "checkpoint": cfg["checkpoint"], "controlnet": CONTROLNET + " (openpose, 0.9, fin 0.8)",
            "pose": Path(pose_path).name, "prompt": prompt, "negative": NEGATIVE, "seed": seed,
            "steps": cfg.get("steps", 28), "cfg": cfg.get("cfg", 5.0), "size": SIZE, "duration_s": round(time.time() - start, 1)}
    Path(out_path).with_suffix(".json").write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
    return info


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--stage", default="T0", choices=sorted(OUTFITS))
    parser.add_argument("--seeds", type=int, nargs="+", required=True)
    parser.add_argument("--only", nargs="*", help="vue:expression (ex. front:moqueuse back:moqueuse)")
    args = parser.parse_args()

    cfg = json.loads((Path(__file__).parent / "backends.json").read_text(encoding="utf-8"))["concept"]["comfyui_animagine"]
    out = RAW / args.stage
    out.mkdir(parents=True, exist_ok=True)
    jobs = [(view, expr) for view in POSES for expr in EXPRESSIONS]
    # De dos, le visage se voit à peine : une seule expression (la scène de fin est moqueuse, docs/GDD.md).
    jobs = [(v, e) for v, e in jobs if v == "front" or e == "moqueuse"]

    if args.only:
        wanted = {tuple(item.split(":")) for item in args.only}
        jobs = [job for job in jobs if job in wanted]

    for view in {v for v, _ in jobs}:
        draw_openpose(SKELETONS[view], out / f"pose_{view}.png")

    for seed in args.seeds:
        for view, expr in jobs:
            prompt = ", ".join([MARA, OUTFITS[args.stage], POSES[view], EXPRESSIONS[expr], STYLE]) + cfg.get("prompt_suffix", "")
            target = out / f"Mara_{args.stage}_{view}_{expr}_s{seed}.png"
            info = run(cfg, prompt, seed, out / f"pose_{view}.png", target)
            print(f"{target.name} ({info['duration_s']} s)", flush=True)


if __name__ == "__main__":
    main()
