"""Mains illustrées 2D posées sur les sprites (demande de l'auteur, 2026-10-30).

Base : la rangée « DESSIN 2D » de la planche de référence générée par IA par l'auteur pour RED MET
(docs/references/planche_mains_redmet/planche_mains_redmet.png, usage autorisé : docs/LICENCES_TIERS.md).
1. cut   : les 5 mains de la première rangée découpées (BiRefNet), agrandies 3 fois, rangées dans
           Assets/_Game/Art/Characters/Hands/Hand_<pose>.png ;
2. adapt : une version par personnage sans changer le dessin (teinte de peau, proportions, veines ou pierre),
           dans Assets/_Game/Art/Characters/<Perso>/Hands/<Perso>_hand_<pose>.png. Le contour du corps est ajouté
           au montage, à l'échelle finale (même épaisseur que le corps).

    "C:/RED MET/ai/BiRefNet/.venv/Scripts/python.exe" -I tools/gen3d/hands2d.py cut
    "C:/RED MET/ai/BiRefNet/.venv/Scripts/python.exe" -I tools/gen3d/hands2d.py adapt
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
SHEET = ROOT / "docs" / "references" / "planche_mains_redmet" / "planche_mains_redmet.png"
ART = ROOT / "Assets" / "_Game" / "Art" / "Characters"
BASE = ART / "Hands"

# Rangée « DESSIN 2D » (planche de 1312 × 1199) : cases sans titres, légendes ni logo.
ROW = (92, 362)
CELLS = {"open_palm": (45, 305), "open_back": (300, 555), "fist_front": (560, 800),
         "fist_back": (815, 1050), "fist_side": (1060, 1300)}
UPSCALE = 3

# Personnage : (peau RVB moyenne visée, échelle (largeur, hauteur), traitement)
CHARACTERS = {
    "Nathan_M0": ((237, 194, 168), (1.1, 0.9), None),     # adulte enrobé : main potelée et plus courte
    "Nathan_M2": ((214, 160, 128), (1.15, 1.05), "veins"),  # plus grosse, veinée
    "Nathan_M4": ((128, 128, 132), (1.25, 1.1), "stone"),   # peau grise fissurée, veines rouges
}


def cut():
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    import vn_cutout

    model = vn_cutout.load_model()
    sheet = Image.open(SHEET).convert("RGB")
    BASE.mkdir(parents=True, exist_ok=True)

    for pose, (x0, x1) in CELLS.items():
        crop = sheet.crop((x0, ROW[0], x1, ROW[1]))
        crop = crop.resize((crop.width * UPSCALE, crop.height * UPSCALE), Image.LANCZOS)
        alpha = vn_cutout.matte(model, crop)
        alpha[alpha < 0.04] = 0.0
        rgb = np.asarray(crop).astype(np.float32) / 255.0
        # Fond quasi noir : couleur « dé-mélangée » du noir sur les bords doux (pas de halo sombre).
        safe = np.maximum(alpha, 1e-3)[..., None]
        rgb = np.where(alpha[..., None] > 0.02, np.clip(rgb / safe, 0, 1), 0)
        out = np.dstack([rgb, alpha])
        img = Image.fromarray((out * 255).astype(np.uint8), "RGBA")
        img = img.crop(img.getchannel("A").point(lambda v: 255 if v > 20 else 0).getbbox())
        path = BASE / f"Hand_{pose}.png"
        img.save(path)
        print(f"[main] {path.relative_to(ROOT)} {img.size}", flush=True)


def _skin_mask(a):
    """Peau : tout l'intérieur de la main ; la lueur rouge (liseré) n'est gardée que sur la bordure, pas dans les
    ombres rougeâtres de la peau."""
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    solid = Image.fromarray(((a[..., 3] > 0.5) * 255).astype(np.uint8))
    interior = np.asarray(solid.filter(ImageFilter.MinFilter(31))) > 0      # à plus de 15 px du bord
    red_rim = (r > 0.35) & (r > g * 1.9) & (r > b * 1.9) & ~interior
    return (a[..., 3] > 0.5) & ~red_rim, red_rim


def adapt_one(src, skin, scale, treatment, seed=7):
    a = np.asarray(src).astype(np.float32) / 255.0
    mask, rim = _skin_mask(a)
    lum = (0.3 * a[..., 0] + 0.59 * a[..., 1] + 0.11 * a[..., 2])
    ref = np.median(lum[mask & (lum > 0.25)])
    target = np.array(skin, np.float32) / 255.0
    # Même dessin : la luminosité relative (traits, ombres, lumières) est gardée, seule la teinte change.
    shade = np.clip(lum / ref, 0, 1.6)[..., None]
    rgb = np.where(mask[..., None], np.clip(target * shade, 0, 1), a[..., :3])
    out = Image.fromarray((np.dstack([rgb, a[..., 3]]) * 255).astype(np.uint8), "RGBA")
    rng = np.random.default_rng(seed)

    if treatment == "veins":
        out = _strokes(out, rng, (120, 60, 70, 150), count=5, width=3)
    elif treatment == "stone":
        out = _strokes(out, rng, (40, 38, 40, 220), count=14, width=2, jagged=True)   # fissures
        out = _strokes(out, rng, (235, 40, 30, 230), count=6, width=3, glow=True)      # veines rouges

    w, h = out.size
    return out.resize((int(w * scale[0]), int(h * scale[1])), Image.LANCZOS)


def _strokes(img, rng, color, count, width, jagged=False, glow=False):
    """Traits peints à l'intérieur de la main (veines, fissures), coupés par sa silhouette."""
    w, h = img.size
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    alpha = np.asarray(img.getchannel("A"))

    for _ in range(count):
        for _try in range(50):
            x, y = rng.uniform(0.2, 0.8) * w, rng.uniform(0.25, 0.85) * h
            if alpha[int(y), int(x)] > 200:
                break
        pts = [(x, y)]
        angle = rng.uniform(-2.2, -0.9)

        for _seg in range(rng.integers(3, 7)):
            angle += rng.uniform(-0.6, 0.6) if jagged else rng.uniform(-0.25, 0.25)
            step = rng.uniform(0.04, 0.09) * h
            x, y = x + np.cos(angle) * step, y + np.sin(angle) * step
            pts.append((x, y))

        d.line(pts, fill=color, width=width, joint="curve")

    if glow:
        halo = layer.filter(ImageFilter.GaussianBlur(4))
        layer = Image.alpha_composite(halo, layer)

    inside = Image.fromarray(alpha).point(lambda v: 255 if v > 200 else 0).filter(ImageFilter.MinFilter(5))
    layer.putalpha(Image.fromarray(np.minimum(np.asarray(layer.getchannel("A")), np.asarray(inside))))
    return Image.alpha_composite(img, layer)


def adapt():
    for character, (skin, scale, treatment) in CHARACTERS.items():
        folder = ART / character / "Hands"
        folder.mkdir(parents=True, exist_ok=True)

        for pose in CELLS:
            out = adapt_one(Image.open(BASE / f"Hand_{pose}.png").convert("RGBA"), skin, scale, treatment)
            path = folder / f"{character}_hand_{pose}.png"
            out.save(path)
            print(f"[main] {path.relative_to(ROOT)}", flush=True)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    {"cut": cut, "adapt": adapt}[sys.argv[1]]()
