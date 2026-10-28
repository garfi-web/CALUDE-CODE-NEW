"""Décors plein écran du visual novel, un par chapitre, via ComfyUI + Animagine XL 4.0.

Même style que le décor validé de l'arène de la lune rouge (tools/gen3d/background.py) : peinture
dark fantasy, palette noir et rouge. Chaque décor est agrandi en 1920 × 1080, légèrement flouté et
assombri sur les bords, pour que le portrait de Mara ressorte au premier plan.

    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/vn_backgrounds.py          # tous
    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/vn_backgrounds.py ch01 ch05

ComfyUI doit tourner. Un original déjà généré (ArtSource/VN/Backgrounds/<id>_raw.png) est réutilisé :
supprimez-le pour en générer un nouveau. Décors finaux : Assets/_Game/Art/Backgrounds/VN/<id>.png.
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import concept  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "ArtSource" / "VN" / "Backgrounds"
OUT = ROOT / "Assets" / "_Game" / "Art" / "Backgrounds" / "VN"

STYLE = ("no humans, scenery, dark fantasy, anime style, highly detailed background art, painterly, dramatic lighting, "
         "black and red color palette, crimson light, wide shot, eye level")
NEGATIVE = ("1girl, 1boy, person, people, character, silhouette of person, text, watermark, signature, logo, frame, "
            "border, lowres, blurry, jpeg artifacts, worst quality, low quality, green, blue sky, daylight")

# Décor de chaque chapitre (lieux de docs/SCENARIO.md et de docs/CHARACTERS.md, « Niveaux »).
SCENES = {
    "ch01": ("vast ash plain under a huge blood red moon, scattered ruins and dead trees, ember particles, red mist", 6101),
    "ch02": ("medieval city street at night, plague, boarded houses, red plague marks on doors, burning braziers, "
             "red smoke, ruined granaries", 6102),
    "ch03": ("approach to a black stone fortress on a cliff, huge gate, burning braziers, ash storm, red moon behind", 6103),
    "ch04": ("abandoned province, empty burnt village, broken carts, war banners, smoke columns, red sky", 6104),
    "ch05": ("dark throne room of black stone, ash covered throne, tall gothic pillars, braziers, red light through "
             "broken stained glass", 6105),
    "ch06": ("fortress outskirts at night, cracked mirrors in ruined hall, shards of glass on the floor, red reflections",
             6106),
    "ch07": ("ruined fairground of a dark kingdom, broken carousel with plastic horses, torn colorful tents, "
             "dim red lanterns, eerie", 6107),
    "ch08": ("modern city street at night, wet asphalt, neon signs off, cracks in the walls glowing with red veins, "
             "dark fantasy invasion of the real world, grey blue buildings with red light", 6108),
    "ch09": ("battlefield at night under a giant red moon, broken siege towers, spears and banners, smoke, burning "
             "ruins on the horizon", 6109),
    "ch10": ("torn world, ash plain fused with a ruined modern city, floating debris, giant red moon cracked, "
             "red light rays, apocalyptic", 6110),
}
GEN_SIZE = (1344, 768)
FINAL = (1920, 1080)


def finish(raw_path, out_path):
    """Agrandi en 1920 × 1080, flou léger, vignette sombre."""
    image = Image.open(raw_path).convert("RGB").resize(FINAL, Image.LANCZOS)
    image = image.filter(ImageFilter.GaussianBlur(3.0))
    rgb = np.asarray(image).astype(np.float32) / 255.0
    h, w = rgb.shape[:2]
    y, x = np.mgrid[0:h, 0:w]
    d = np.sqrt(((x - w / 2) / (w / 2)) ** 2 + ((y - h / 2) / (h / 2)) ** 2)
    vignette = np.clip(1.0 - 0.45 * np.clip(d - 0.35, 0, None), 0.4, 1.0)
    rgb *= (0.85 * vignette)[..., None]
    Image.fromarray((rgb * 255).round().astype(np.uint8)).save(out_path)


def main():
    cfg = json.loads((Path(__file__).parent / "backends.json").read_text(encoding="utf-8"))["concept"]["comfyui_animagine"]
    wanted = sys.argv[1:] or list(SCENES)
    RAW.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)

    for scene in wanted:
        prompt, seed = SCENES[scene]
        raw = RAW / f"{scene}_raw.png"

        if not raw.exists():
            path, info = concept.generate(cfg, f"{prompt}, {STYLE}", NEGATIVE, seed, RAW, f"{scene}_raw", size=GEN_SIZE)
            Path(path).with_suffix(".json").write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")

        finish(raw, OUT / f"{scene}.png")
        print(f"{scene}.png", flush=True)


if __name__ == "__main__":
    main()
