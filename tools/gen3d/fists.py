"""Méthode C des poings (2026-10-30) : poing 2D illustré, posé sur les sprites comme les têtes.

La forme vient du poing 3D de la méthode B (main CC0 des Human Base Meshes repliée en poing, traits d'encre) :
ControlNet Union (type « lineart ») sur ses traits, une image par vue (face, 3/4, profil), rendue dans Blender
(tools/blender/redmet_hand_hbm.py). Animagine XL 4.0 dessine le poing dans le style anime.

    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/fists.py <dossier des rendus B> [graines...]

ComfyUI doit tourner. Sorties : ArtSource/Blender/Hands/raw/poing_<vue>_s<graine>.png (+ .json).
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import background  # noqa: E402
import concept  # noqa: E402
import heads  # noqa: E402
import skies  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
HANDS = ROOT / "ArtSource" / "Blender" / "Hands"
RAW = HANDS / "raw"
VIEWS = ("face", "34", "profil")

PROMPT = ("no humans, (clenched fist:1.4), (single hand:1.3), close-up, male hand, adult hand, (four fingers curled:1.2), "
          "(thumb across fingers:1.2), knuckles, wrist, fair skin, anime coloring, cel shading, clean lineart, "
          "simple background, plain grey background, highres, masterpiece, high score, great score, absurdres")
NEGATIVE = ("(extra fingers:1.4), (missing fingers:1.3), six fingers, fused fingers, extra hands, two hands, arm, sleeve, "
            "glove, boxing glove, open hand, pointing, face, person, body, text, logo, watermark, signature, "
            "bad anatomy, bad hands, lowres, blurry, worst quality, low quality, low score, bad score, average score")


def guides(renders):
    """Traits blancs sur fond noir tirés des rendus B (contour + traits d'encre)."""
    from PIL import Image, ImageFilter
    import numpy as np

    out = {}

    for view in VIEWS:
        rgba = Image.open(Path(renders) / f"B_{view}.png").convert("RGBA").resize((1024, 1024), Image.LANCZOS)
        a = np.asarray(rgba).astype(np.float32)
        dark = (a[..., :3].max(axis=2) < 90) & (a[..., 3] > 128)        # encre et contour
        lines = Image.fromarray((dark * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3))
        path = HANDS / f"poing_lineart_{view}.png"
        lines.convert("RGB").save(path)
        out[view] = path

    return out


def graph(cfg, prompt, negative, seed, image_name):
    g = heads.graph(cfg, prompt, negative, seed, image_name, 0.75, 0.8)
    g["11"]["inputs"]["type"] = "canny/lineart/anime_lineart/mlsd"
    return g


def main(renders, seeds=(9501, 9502)):
    paths = guides(renders)
    cfg = skies.config()
    base = cfg["url"].rstrip("/")
    RAW.mkdir(parents=True, exist_ok=True)

    for view in VIEWS:
        uploaded = concept.upload_image(base, paths[view])

        for seed in seeds:
            path = RAW / f"poing_{view}_s{seed}.png"

            if path.exists():
                continue

            duration = background.run_workflow(cfg, graph(cfg, PROMPT, NEGATIVE, seed, uploaded), path)
            info = {"tool": "ComfyUI", "checkpoint": cfg["checkpoint"], "controlnet": "union promax, lineart, 0.75, fin 0.8",
                    "guide": paths[view].name, "prompt": PROMPT, "negative": NEGATIVE, "seed": seed,
                    "duration_s": duration}
            path.with_suffix(".json").write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
            print(f"[poing] {path.name} ({duration} s)", flush=True)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    main(sys.argv[1], tuple(int(a) for a in sys.argv[2:]) or (9501, 9502))
