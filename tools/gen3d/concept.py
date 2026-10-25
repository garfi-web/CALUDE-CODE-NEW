"""Images de concept via l'API locale de ComfyUI (bibliothèque standard uniquement).

ComfyUI doit tourner (python main.py, port 8188 par défaut). On envoie un graphe SDXL
minimal : checkpoint → prompts → KSampler → VAE → image PNG, puis on récupère le fichier.
"""
import json
import shutil
import time
import urllib.parse
import urllib.request
import uuid
from pathlib import Path

DEFAULT_NEGATIVE = (
    "child, kid, chibi, nsfw, lowres, bad anatomy, bad hands, extra fingers, missing fingers, "
    "extra limbs, cropped, out of frame, multiple views, text, watermark, signature, "
    "busy background, shadow on background, worst quality, low quality, low score, bad score, "
    "average score, username, error, blurry"
)


def _request(url, data=None):
    body = json.dumps(data).encode("utf-8") if data is not None else None
    req = urllib.request.Request(url, data=body, headers={"Content-Type": "application/json"} if body else {})
    with urllib.request.urlopen(req, timeout=30) as resp:
        return resp.read()


def build_workflow(cfg, prompt, negative, seed):
    width, height = cfg.get("size", [1024, 1024])
    return {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": cfg["checkpoint"]}},
        "2": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["1", 1], "text": prompt}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["1", 1], "text": negative}},
        "4": {"class_type": "EmptyLatentImage", "inputs": {"width": width, "height": height, "batch_size": 1}},
        "5": {
            "class_type": "KSampler",
            "inputs": {
                "model": ["1", 0], "positive": ["2", 0], "negative": ["3", 0], "latent_image": ["4", 0],
                "seed": seed, "steps": cfg.get("steps", 28), "cfg": cfg.get("cfg", 5.0),
                "sampler_name": cfg.get("sampler", "euler_ancestral"), "scheduler": cfg.get("scheduler", "normal"),
                "denoise": 1.0,
            },
        },
        "6": {"class_type": "VAEDecode", "inputs": {"samples": ["5", 0], "vae": ["1", 2]}},
        "7": {"class_type": "SaveImage", "inputs": {"images": ["6", 0], "filename_prefix": "redmet_concept"}},
    }


def generate(cfg, prompt, negative, seed, out_dir, name, timeout_s=900):
    """Génère une image, la copie dans out_dir/<name>.png et renvoie (chemin, infos)."""
    base = cfg["url"].rstrip("/")
    negative = negative or DEFAULT_NEGATIVE
    full_prompt = cfg.get("prompt_prefix", "") + prompt + cfg.get("prompt_suffix", "")
    workflow = build_workflow(cfg, full_prompt, negative, seed)

    prompt_id = json.loads(_request(f"{base}/prompt", {"prompt": workflow, "client_id": str(uuid.uuid4())}))["prompt_id"]
    start = time.time()
    while True:
        history = json.loads(_request(f"{base}/history/{prompt_id}"))
        if prompt_id in history:
            entry = history[prompt_id]
            status = entry.get("status", {})
            if status.get("status_str") == "error":
                raise RuntimeError(f"ComfyUI a échoué : {json.dumps(status)[:500]}")
            images = [img for node in entry.get("outputs", {}).values() for img in node.get("images", [])]
            if images:
                break
        if time.time() - start > timeout_s:
            raise TimeoutError("ComfyUI n'a pas rendu d'image à temps")
        time.sleep(1.0)

    image = images[0]
    query = urllib.parse.urlencode({"filename": image["filename"], "subfolder": image["subfolder"], "type": image["type"]})
    out_dir = Path(out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    out_path = out_dir / f"{name}.png"
    i = 2
    while out_path.exists():
        out_path = out_dir / f"{name}_{i}.png"
        i += 1
    with urllib.request.urlopen(f"{base}/view?{query}", timeout=60) as resp, open(out_path, "wb") as f:
        shutil.copyfileobj(resp, f)

    info = {
        "tool": "ComfyUI",
        "checkpoint": cfg["checkpoint"],
        "prompt": full_prompt,
        "negative": negative,
        "seed": seed,
        "steps": workflow["5"]["inputs"]["steps"],
        "cfg": workflow["5"]["inputs"]["cfg"],
        "sampler": workflow["5"]["inputs"]["sampler_name"],
        "size": cfg.get("size", [1024, 1024]),
        "duration_s": round(time.time() - start, 1),
    }
    with open(out_path.with_suffix(".json"), "w", encoding="utf-8") as f:
        json.dump(info, f, ensure_ascii=False, indent=2)
    return out_path, info


def main():
    """Génère seulement des concepts (une image par seed), pour choisir avant la 3D.

    python tools/gen3d/concept.py --character Nathan_R0 --prompt "..." --seeds 101 202 303
    """
    import argparse
    import sys

    tools_dir = Path(__file__).resolve().parent
    characters_dir = tools_dir.parents[1] / "Assets" / "_Game" / "Art" / "Characters"

    parser = argparse.ArgumentParser(description=main.__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--character", required=True)
    parser.add_argument("--prompt", required=True)
    parser.add_argument("--negative", default=None)
    parser.add_argument("--seeds", type=int, nargs="+", required=True)
    parser.add_argument("--backend", default=None)
    args = parser.parse_args()

    with open(tools_dir / "backends.json", encoding="utf-8") as f:
        backends = json.load(f)
    cfg = backends["concept"][args.backend or backends["default_concept"]]

    for seed in args.seeds:
        path, info = generate(cfg, args.prompt, args.negative, seed,
                              characters_dir / args.character / "Concept", f"{args.character}_concept_s{seed}")
        print(f"[concept] seed {seed} : {path} ({info['duration_s']} s)", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
