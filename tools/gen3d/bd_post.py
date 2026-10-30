"""Essai « roman graphique » (2026-10-30) : finition des rendus BD de Blender (tools/blender/redmet_bd.py).
Grain de papier (bruit fin + fibres), palette ramenée vers des tons sourds et chauds ; le rouge vif reste réservé au
sang et à la Red Met (aucun rouge ajouté ici).

    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/bd_post.py <rendu.png> <sortie.png>
"""
import sys

import numpy as np
from PIL import Image, ImageFilter


def paper(src, dst, seed=7, grain=0.07, warmth=(1.03, 1.0, 0.92)):
    img = Image.open(src).convert("RGBA")
    a = np.asarray(img).astype(np.float32) / 255.0
    h, w = a.shape[:2]
    rng = np.random.default_rng(seed)
    fine = rng.normal(0, 1, (h, w))
    fibres = np.asarray(Image.fromarray((rng.random((h, w)) * 255).astype(np.uint8))
                        .filter(ImageFilter.GaussianBlur((3, 0.6)) if False else ImageFilter.BoxBlur(2))).astype(np.float32) / 255 - 0.5
    noise = (fine * 0.6 + fibres * 2.0) * grain
    rgb = a[..., :3]
    lum = rgb.mean(axis=2, keepdims=True)
    rgb = lum + (rgb - lum) * 0.8                      # palette sourde
    rgb = rgb * np.array(warmth, np.float32)           # papier chaud
    rgb = np.clip(rgb + noise[..., None] * (0.35 + lum), 0, 1)
    out = np.dstack([rgb, a[..., 3]])
    Image.fromarray((out * 255).astype(np.uint8), "RGBA").save(dst)
    return dst


if __name__ == "__main__":
    paper(sys.argv[1], sys.argv[2])
