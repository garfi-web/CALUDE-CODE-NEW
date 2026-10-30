"""Nathan M0 complet en image fixe (demande de l'auteur, 2026-10-30, assemblage corrigé) : corps 3D rendu dans Blender
au cadrage des sprites (tools/blender/redmet_compose_v3.py, render_poses) en deux couches, tête A redressée
(tools/gen3d/head_level.py) et mains illustrées 2D (tools/gen3d/hands2d.py).

Ordre des calques, pour que la tête et les poings soient « dans » le sweat :
1. couche « dessous » : tout le corps 3D, avec son cou et l'arrière de la capuche ;
2. ombre portée sous le menton, sur le cou 3D ;
3. tête A, pivot à la base du cou (os neck_01) ;
4. mains 2D au bout des poignets (os hand), dans l'axe des avant-bras, avec une ombre au raccord ;
5. couche « dessus » : le corps 3D percé par des volumes aux places de la tête et des mains ; ce qui est devant
   eux (col, avant de la capuche, revers des manches, autre bras) recouvre la tête et les mains.

    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/compose_still.py <dossier des rendus>

Sorties : Assets/_Game/Art/Characters/Nathan_M0/Stills/Nathan_M0_v3_<pose>.png (1024 px pour 2,6 m, pieds à 0,349 m
du bas, comme les sprites).
"""
import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets" / "_Game" / "Art" / "Characters"
CHARACTER = "Nathan_M0"
HEAD = ART / CHARACTER / "Heads" / f"{CHARACTER}_head_A_level.png"
OUT = ART / CHARACTER / "Stills"
OUTLINE_RGB = (51, 5, 8)
# Tête A redressée : cadre de 1024 px pour 0,40 m (base du cou → haut des boucles : 0,27 m) ; inclinaison de l'os du
# cou au repos dans l'image (la tête dessinée l'a déjà).
HEAD_FRAME_M = 0.40
REST_NECK_TILT_DEG = 20.4
# Mains : (articulation du poignet dans l'image en fractions, direction poignet → doigts, longueur poignet → bout en m).
# Le dessin garde un bout d'avant-bras derrière l'articulation : il glisse sous le revers de la manche.
HAND_GEOMETRY = {
    "fist_back": ((0.55, 0.78), (0.0, -1.0), 0.15),
    "fist_side": ((0.22, 0.45), (1.0, 0.0), 0.15),
    "fist_front": ((0.5, 0.5), (0.0, -1.0), 0.1),
    "open_palm": ((0.5, 0.82), (0.0, -1.0), 0.19),
    "open_back": ((0.5, 0.82), (0.0, -1.0), 0.19),
}


def trim_behind(img, anchor, axis, keep_px):
    """Retire le bout d'avant-bras dessiné au-delà de keep_px derrière le poignet (il sortait de la manche)."""
    w, h = img.size
    yy, xx = np.mgrid[0:h, 0:w]
    behind = (xx - anchor[0] * w) * -axis[0] + (yy - anchor[1] * h) * -axis[1]
    a = np.asarray(img).copy()
    a[..., 3] = np.where(behind > keep_px, 0, a[..., 3])
    return Image.fromarray(a, "RGBA")


def outlined(img, px, anchor, cut_below=None):
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

    if cut_below is not None:   # pas de contour sur la coupe du cou (elle passe sous le col)
        r = np.asarray(ring).copy()
        r[int(cut_below * img.height):] = np.minimum(r[int(cut_below * img.height):], np.asarray(alpha)[int(cut_below * img.height):])
        ring = Image.fromarray(r)

    base = Image.new("RGBA", img.size, OUTLINE_RGB + (0,))
    base.putalpha(ring)
    return Image.alpha_composite(base, img), anchor


def _shade(size, box, strength, blur, limit, rgb):
    """Ombre douce (ellipse floutée) limitée à l'alpha d'une image."""
    shade = Image.new("L", size, 0)
    ImageDraw.Draw(shade).ellipse(box, fill=strength)
    shade = shade.filter(ImageFilter.GaussianBlur(blur))
    dark = Image.new("RGBA", size, rgb + (0,))
    dark.putalpha(Image.fromarray(np.minimum(np.asarray(shade), np.asarray(limit))))
    return dark


def wrist_shadow(img, anchor, radius):
    """Ombre douce au raccord : le dessin s'assombrit autour du poignet, sous le revers de la manche."""
    ax, ay = anchor[0] * img.width, anchor[1] * img.height
    box = [ax - radius * 1.6, ay - radius, ax + radius * 1.6, ay + radius]
    return Image.alpha_composite(img, _shade(img.size, box, 110, radius * 0.5, img.getchannel("A"), (20, 6, 8)))


def chin_shadow(canvas, chin, ppm):
    """Ombre portée sous le menton, sur le cou 3D (seulement là où il y a déjà de la peau ou du tissu)."""
    w, h = 0.085 * ppm, 0.03 * ppm
    cx, cy = chin[0] + 0.01 * ppm, chin[1] + 0.012 * ppm
    box = [cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2]
    canvas.alpha_composite(_shade(canvas.size, box, 130, h * 0.35, canvas.getchannel("A"), (40, 10, 12)))


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
    meta = json.loads(HEAD.with_suffix(".json").read_text(encoding="utf-8"))
    ppm = data["size"] / data["ortho"]
    outline_px = max(2, round(0.008 * ppm))
    OUT.mkdir(parents=True, exist_ok=True)
    head = Image.open(HEAD).convert("RGBA")
    head_scale = HEAD_FRAME_M * ppm / head.width
    head = head.resize((round(head.width * head_scale), round(head.height * head_scale)), Image.LANCZOS)
    neck_anchor = (meta["neck_base_px"][0] / meta["size"][0], meta["neck_base_px"][1] / meta["size"][1])
    # La coupe sous le cou commence environ 6 cm au-dessus de la base du cou (côtés des épaules retirés).
    head, head_anchor = outlined(head, outline_px, neck_anchor, cut_below=neck_anchor[1] - 0.06 / HEAD_FRAME_M)
    outputs = []

    for name, entry in data["poses"].items():
        canvas = Image.open(entry["under"]).convert("RGBA")
        chin_shadow(canvas, entry["chin"], ppm)
        nx, ny = entry["neck_dir"]
        tilt = math.degrees(math.atan2(nx, -ny))
        place(canvas, head, head_anchor, entry["neck_base"], -(tilt - REST_NECK_TILT_DEG))

        # Main la plus éloignée de la caméra d'abord.
        for side, hand in sorted(entry["hands"].items(), key=lambda kv: kv[1]["depth"]):
            if hand["pose"] is None:
                continue

            # Fond de la manche (ouverture du revers vue de face) : ombre du tissu, sous la main.
            wx, wy = hand["wrist"]
            ex, ey = hand["elbow"]
            inner = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
            r = 0.04 * ppm
            cx, cy = wx + (ex - wx) * 0.12, wy + (ey - wy) * 0.12
            ImageDraw.Draw(inner).ellipse([cx - r, cy - r, cx + r, cy + r], fill=(14, 15, 22, 255))
            canvas.alpha_composite(inner)

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

            # Distance, dans le dessin, de l'articulation du poignet au bout des doigts.
            reach = img.height * anchor[1] if axis[0] == 0 else img.width * (anchor[0] if axis[0] < 0 else 1 - anchor[0])
            scale = length * ppm / reach
            img = img.resize((max(1, int(img.width * scale)), max(1, int(img.height * scale))), Image.LANCZOS)
            img = trim_behind(img, anchor, axis, 0.06 * ppm)   # remplit l'ouverture de la manche, caché ailleurs par le tissu
            img, anchor = outlined(img, outline_px, anchor)
            img = wrist_shadow(img, anchor, 0.025 * ppm)
            angle = math.degrees(math.atan2(-fy, fx) - math.atan2(-axis[1], axis[0]))
            place(canvas, img, anchor, hand["wrist"], angle)

        canvas.alpha_composite(Image.open(entry["over"]).convert("RGBA"))
        path = OUT / f"{CHARACTER}_v3_{name}.png"
        canvas.save(path)
        outputs.append(path)
        print(f"[montage] {path.relative_to(ROOT)}", flush=True)

    return outputs


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    compose(sys.argv[1])
