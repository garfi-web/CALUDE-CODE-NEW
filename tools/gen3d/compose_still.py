"""Nathan M0 complet en image fixe (demande de l'auteur, 2026-10-30) : corps 3D rendu dans Blender au cadrage des
sprites (tools/blender/redmet_compose_v3.py, render_poses), tête A illustrée glissée sous le col, mains illustrées 2D
(tools/gen3d/hands2d.py) posées sur les poignets dans l'axe des avant-bras, avec le contour du corps.

    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/compose_still.py <dossier des rendus>

Sorties : Assets/_Game/Art/Characters/Nathan_M0/Stills/Nathan_M0_v3_<pose>.png (1024 px pour 2,6 m, pieds à 0,349 m
du bas, comme les sprites).
"""
import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets" / "_Game" / "Art" / "Characters"
CHARACTER = "Nathan_M0"
HEAD = ART / CHARACTER / "Heads" / f"{CHARACTER}_head_neutral.png"
OUT = ART / CHARACTER / "Stills"
OUTLINE_RGB = (51, 5, 8)
# Tête A : cadre de 1024 px pour 0,48 m, centre = milieu de l'os de la tête. Redressée de 18° (le regard montait,
# défaut relevé par l'auteur au palier 2 des têtes).
HEAD_FRAME_M = 0.48
HEAD_LEVEL_DEG = -30.0
# Décalage de la tête dans l'image (m, x vers la droite, y vers le haut) : le menton dégagé de la capuche baissée.
HEAD_OFFSET_M = (0.005, 0.062)
# Mains : (point du poignet dans l'image en fractions, direction poignet → doigts dans l'image, longueur en m).
HAND_GEOMETRY = {
    "fist_back": ((0.5, 0.97), (0.0, -1.0), 0.135),   # un peu plus gros que nature (style anime, adulte enrobé)
    "fist_side": ((0.04, 0.42), (1.0, 0.0), 0.14),
    "fist_front": ((0.5, 0.5), (0.0, -1.0), 0.09),
    "open_palm": ((0.5, 0.97), (0.0, -1.0), 0.17),
    "open_back": ((0.5, 0.97), (0.0, -1.0), 0.17),
}


def outlined(img, px, anchor):
    """Contour du corps (même couleur, même épaisseur) autour d'un dessin détouré ; renvoie aussi l'ancrage recalé
    dans l'image agrandie de la marge du contour."""
    w, h = img.size
    padded = Image.new("RGBA", (w + 2 * px, h + 2 * px), (0, 0, 0, 0))
    padded.paste(img, (px, px))
    img = padded
    anchor = ((anchor[0] * w + px) / img.width, (anchor[1] * h + px) / img.height)
    # Bord lissé avant d'épaissir : pas de dentelure autour des doigts.
    alpha = img.getchannel("A").filter(ImageFilter.GaussianBlur(1.2)).point(lambda v: 255 if v > 110 else 0)
    ring = alpha.filter(ImageFilter.MaxFilter(2 * px + 1)).filter(ImageFilter.GaussianBlur(0.8))
    base = Image.new("RGBA", img.size, OUTLINE_RGB + (0,))
    base.putalpha(ring)
    return Image.alpha_composite(base, img), anchor


def place(canvas, img, anchor_frac, target_xy, angle_deg):
    """Tourne img autour de son point d'ancrage et le pose sur target_xy."""
    w, h = img.size
    ax, ay = anchor_frac[0] * w, anchor_frac[1] * h
    pad = int(math.hypot(w, h))
    big = Image.new("RGBA", (2 * pad, 2 * pad), (0, 0, 0, 0))
    big.paste(img, (int(pad - ax), int(pad - ay)))
    big = big.rotate(angle_deg, resample=Image.BICUBIC, center=(pad, pad))
    canvas.alpha_composite(big, (int(target_xy[0] - pad), int(target_xy[1] - pad)))


def compose(renders):
    data = json.loads((Path(renders) / f"{CHARACTER}_poses.json").read_text(encoding="utf-8"))
    ppm = data["size"] / data["ortho"]
    outline_px = max(2, round(0.008 * ppm))
    OUT.mkdir(parents=True, exist_ok=True)
    head = Image.open(HEAD).convert("RGBA")
    head_px = int(round(HEAD_FRAME_M * ppm))
    head = head.resize((head_px, head_px), Image.LANCZOS)
    outputs = []

    for name, entry in data["poses"].items():
        body = Image.open(entry["image"]).convert("RGBA")
        canvas = Image.new("RGBA", body.size, (0, 0, 0, 0))
        # Inclinaison de l'os de la tête dans l'image (0 = vertical).
        hx, hy = entry["head_dir"]
        tilt = math.degrees(math.atan2(hx, -hy))
        cx, cy = entry["head_center"]
        place(canvas, head, (0.5, 0.5), (cx + HEAD_OFFSET_M[0] * ppm, cy - HEAD_OFFSET_M[1] * ppm), HEAD_LEVEL_DEG - tilt)
        canvas.alpha_composite(body)   # le col du sweat passe devant le cou de la tête

        # Main la plus éloignée de la caméra d'abord.
        for side, hand in sorted(entry["hands"].items(), key=lambda kv: kv[1]["depth"]):
            if hand["pose"] is None:
                continue

            img = Image.open(ART / CHARACTER / "Hands" / f"{CHARACTER}_hand_{hand['pose']}.png").convert("RGBA")
            anchor, axis, length = HAND_GEOMETRY[hand["pose"]]
            fx, fy = hand["wrist"][0] - hand["elbow"][0], hand["wrist"][1] - hand["elbow"][1]

            # Main de profil vers la gauche : retournée (pouce en haut) plutôt que tournée à 180°.
            if axis[0] != 0 and fx * axis[0] < 0:
                img = img.transpose(Image.FLIP_LEFT_RIGHT)
                anchor, axis = (1 - anchor[0], anchor[1]), (-axis[0], axis[1])

            if side == "l" and axis[0] == 0:   # autre main : retournement horizontal du même dessin
                img = img.transpose(Image.FLIP_LEFT_RIGHT)
                anchor = (1 - anchor[0], anchor[1])

            extent = img.height if axis[0] == 0 else img.width
            scale = length * ppm / extent
            img = img.resize((max(1, int(img.width * scale)), max(1, int(img.height * scale))), Image.LANCZOS)
            img, anchor = outlined(img, outline_px, anchor)
            angle = math.degrees(math.atan2(-fy, fx) - math.atan2(-axis[1], axis[0]))
            place(canvas, img, anchor, hand["wrist"], angle)

        path = OUT / f"{CHARACTER}_v3_{name}.png"
        canvas.save(path)
        outputs.append(path)
        print(f"[montage] {path.relative_to(ROOT)}", flush=True)

    return outputs


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    compose(sys.argv[1])
