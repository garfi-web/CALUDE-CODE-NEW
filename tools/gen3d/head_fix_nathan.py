"""Corrections de la tête choisie par l'auteur pour Nathan M0 (proposition B, s9107, palier 1 du 2026-10-30).

Demandes : redresser un peu la tête, retirer le col du sweat, joues plus rondes et acné bien visible (comme la A).
Le visage choisi n'est pas régénéré :
1. retournement vers la droite (sens des sprites) et rotation qui relève le menton ;
2. réduction dans une image plus grande : le menton et le cou, coupés par le bord dans B, ont de la place ;
3. inpainting Animagine limité au masque (col du sweat et bas du visage) : menton rond, double menton, haut du cou ;
   les yeux, les lunettes et les cheveux restent ceux de B (ImageCompositeMasked recolle tout le reste) ;
4. joues gonflées par une déformation locale, acné peinte (boutons rouges au bord doux, comme dans la A).

    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/head_fix_nathan.py [graine]

Sortie : ArtSource/Blender/Heads/fixed/Nathan_M0_head_neutral_B_s<graine>.png (avant détourage).
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import concept  # noqa: E402
import heads  # noqa: E402
import skies  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "ArtSource" / "Blender" / "Heads" / "raw" / "Nathan_M0_head_v1_neutral_s9107.png"
OUT = ROOT / "ArtSource" / "Blender" / "Heads" / "fixed"
TILT = 20.0     # degrés, sens inverse des aiguilles : le menton remonte
SCALE = 0.74
OFFSET = (60, -120)
# Ligne au-dessus de la bouche dans B (retourné), en pixels : tout ce qui est dessous est redessiné.
MOUTH_LINE = 925


def prepare():
    image = Image.open(SOURCE).convert("RGB").transpose(Image.FLIP_LEFT_RIGHT)
    a = np.asarray(image).astype(np.float32)
    background = tuple(int(c) for c in np.median(a[:40, :40].reshape(-1, 3), axis=0))

    # Masque dans le repère de B : le bas du visage et le col (pixels sombres et peu saturés sous les cheveux).
    mask = Image.new("L", image.size, 0)
    d = ImageDraw.Draw(mask)
    d.rectangle([0, MOUTH_LINE, image.width, image.height], fill=255)
    value = a.max(axis=2) / 255.0
    sat = (a.max(axis=2) - a.min(axis=2)) / np.maximum(a.max(axis=2), 1)
    collar = (value < 0.32) & (sat < 0.7)
    # Le col est en bas à gauche du visage (B retourné) ; ailleurs, les pixels sombres sont les lunettes et les traits.
    collar[:700] = False
    collar[:880, 420:] = False
    mask = Image.fromarray(np.maximum(np.asarray(mask), (collar * 255).astype(np.uint8)))
    mask = mask.filter(ImageFilter.MaxFilter(15))

    def place(img, fill):
        img = img.rotate(TILT, resample=Image.BICUBIC, expand=False, fillcolor=fill)
        img = img.resize((int(img.width * SCALE), int(img.height * SCALE)), Image.LANCZOS)
        canvas = Image.new(img.mode, (1024, 1024), fill)
        canvas.paste(img, ((1024 - img.width) // 2 + OFFSET[0], (1024 - img.height) // 2 + OFFSET[1]))
        return canvas

    placed = place(image, background)
    placed_mask = place(mask, 0)
    # Tout ce qui est hors de l'image de B (le bas ajouté) est aussi à dessiner.
    inside = place(Image.new("L", image.size, 255), 0)
    # Le bas ajouté sous B : seulement une bande sous le menton (le reste du fond reste uni, rien n'y est inventé).
    outside = 255 - np.asarray(inside)
    band = np.zeros_like(outside)
    band[620:, 420:900] = 255
    full_mask = np.maximum(np.asarray(placed_mask), np.minimum(outside, band))
    full_mask = Image.fromarray(full_mask).filter(ImageFilter.GaussianBlur(3)).point(lambda v: 255 if v > 60 else 0)
    return placed, full_mask.convert("RGB")


def puff_cheeks(image, center, radius, strength):
    """Joue gonflée : les pixels proches du centre sont tirés vers l'extérieur (déformation radiale douce)."""
    a = np.asarray(image)
    h, w = a.shape[:2]
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    dx, dy = x - center[0], y - center[1]
    r = np.hypot(dx, dy)
    k = np.clip(1.0 - r / radius, 0, 1) ** 2 * strength
    sx = np.clip(center[0] + dx * (1 - k), 0, w - 1).astype(np.int32)
    sy = np.clip(center[1] + dy * (1 - k), 0, h - 1).astype(np.int32)
    return Image.fromarray(a[sy, sx])


def acne(image, spots, rng):
    layer = Image.new("RGBA", image.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)

    for x, y in spots:
        r = rng.uniform(4, 7)
        d.ellipse([x - r * 1.6, y - r * 1.6, x + r * 1.6, y + r * 1.6], fill=(214, 120, 110, 90))
        d.ellipse([x - r, y - r, x + r, y + r], fill=(196, 82, 74, 210))
        d.ellipse([x - r * 0.35, y - r * 0.55, x + r * 0.15, y - r * 0.1], fill=(255, 210, 200, 160))

    layer = layer.filter(ImageFilter.GaussianBlur(0.8))
    out = image.convert("RGBA")
    out.alpha_composite(layer)
    return out.convert("RGB")


def main(seed=9131):
    OUT.mkdir(parents=True, exist_ok=True)
    placed, mask = prepare()
    init = OUT / "Nathan_M0_head_B_init.png"
    mask_path = OUT / "Nathan_M0_head_B_mask.png"
    placed.save(init)
    mask.save(mask_path)

    description = CHARACTER.replace(", black hoodie collar", "")
    prompt = (f"{description}, (round chin:1.2), (double chin:1.2), (chubby round cheeks:1.3), bare neck, "
              f"no collar, no clothes, closed mouth, calm, {heads.STYLE}")
    cfg = skies.config()
    path, info = concept.generate(cfg, prompt, heads.NEGATIVE + ", collar, hoodie, shirt, clothes", seed, OUT,
                                  f"Nathan_M0_head_B_inpaint_s{seed}", init_image=str(init), mask_image=str(mask_path),
                                  denoise=1.0, size=[1024, 1024])
    path.with_suffix(".json").write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"[tête B] inpainting : {path.name}", flush=True)
    return path


CHARACTER = heads.CHARACTERS["Nathan_M0"][0]

if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    main(*(int(a) for a in sys.argv[1:]))
