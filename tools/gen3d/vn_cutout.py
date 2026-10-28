"""Détourage des portraits du visual novel avec BiRefNet (MIT), sans halo.

Les portraits sont générés sur fond noir (vn_portraits.py). BiRefNet donne un masque doux (cheveux fins),
complété par la luminosité pour la lame d'énergie et ses lueurs, qu'il écarte comme du fond ;
la couleur des pixels semi-transparents est ensuite « dé-mélangée » du noir (couleur / alpha), ce qui
supprime le liseré sombre autour des mèches. Le cadre reste 832 × 1216 pour que les expressions d'une
même vue se superposent exactement dans le jeu.

    "C:/RED MET/ai/BiRefNet/.venv/Scripts/python.exe" -I tools/gen3d/vn_cutout.py ArtSource/VN/Mara/T0/Mara_T0_front_moqueuse_s8502.png ...

Sorties : Assets/_Game/Art/Characters/Mara/VN/<Mara_T0_front_moqueuse>.png (sans le suffixe de graine).
Environnement : C:/RED MET/ai/BiRefNet/.venv (lock : requirements-birefnet.lock.txt, à côté du modèle).
"""
import re
import sys
from pathlib import Path

import numpy as np
import torch
from PIL import Image
from torchvision import transforms

MODEL_DIR = Path("C:/RED MET/ai/BiRefNet")
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "_Game" / "Art" / "Characters" / "Mara" / "VN"


def load_model():
    sys.path.insert(0, str(MODEL_DIR))
    from transformers import AutoModelForImageSegmentation

    model = AutoModelForImageSegmentation.from_pretrained(str(MODEL_DIR), trust_remote_code=True)
    model.to("cuda").eval().half()
    return model


def matte(model, image):
    prep = transforms.Compose([
        transforms.Resize((1024, 1024)),
        transforms.ToTensor(),
        transforms.Normalize([0.485, 0.456, 0.406], [0.229, 0.224, 0.225]),
    ])
    with torch.no_grad():
        pred = model(prep(image).unsqueeze(0).to("cuda").half())[-1].sigmoid().cpu()[0, 0]
    mask = transforms.functional.to_pil_image(pred.float()).resize(image.size, Image.LANCZOS)
    return np.asarray(mask).astype(np.float32) / 255.0


def cutout(model, path):
    image = Image.open(path).convert("RGB")
    alpha = matte(model, image)
    rgb = np.asarray(image).astype(np.float32) / 255.0

    # La lame d'énergie et ses flammes sont de la lumière sur fond noir : BiRefNet les prend pour du
    # fond. Sur du noir, une lueur se détoure par son intensité (alpha = canal le plus fort).
    glow = np.clip((rgb.max(axis=2) - 0.22) / 0.45, 0.0, 1.0)  # seuil : la faible lueur de contour reste dehors
    alpha = np.maximum(alpha, glow)
    alpha[alpha < 0.02] = 0.0

    # Fond noir : observé = alpha × couleur ; on retrouve la couleur en divisant par alpha.
    safe = np.maximum(alpha, 1e-3)[..., None]
    fg = np.where(alpha[..., None] > 0.05, np.clip(rgb / safe, 0, 1), rgb)
    rgba = np.dstack([fg, alpha])
    name = re.sub(r"_s\d+$", "", Path(path).stem)
    OUT.mkdir(parents=True, exist_ok=True)
    target = OUT / f"{name}.png"
    Image.fromarray((rgba * 255).round().astype(np.uint8), "RGBA").save(target)
    return target, float(alpha.mean())


def medallion(source, center, size, target):
    """Visage pour la carte du personnage : carré autour de la tête, découpé dans le portrait détouré."""
    image = Image.open(source)
    x, y = center
    box = (x - size // 2, y - size // 2, x + size // 2, y + size // 2)
    image.crop(box).resize((256, 256), Image.LANCZOS).save(target)
    return target


if __name__ == "__main__":
    model = load_model()
    for arg in sys.argv[1:]:
        target, coverage = cutout(model, arg)
        print(f"{target.name} : couverture {coverage:.0%}", flush=True)
