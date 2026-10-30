"""Calques de visage anime 2D (demande de l'auteur du 2026-10-30), dessinés ici en traits et aplats, sans génération :
le même dessin pour chaque image d'animation, donc aucun scintillement. Vue de 3/4 tournée vers la droite (le sprite
est retourné par Unity quand le personnage regarde à gauche).

Quatre expressions par personnage, déclenchées par le jeu (FaceLayer25D) : neutre, effort (il frappe), douleur (il est
touché), rage. Nathan : lunettes à double foyer et acné. Voyou : visage plus clair sous la capuche, yeux qui accrochent
la lumière.

    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/faces.py

Repère : origine au point du visage suivi dans Blender (milieu du visage, à hauteur des yeux), x vers l'avant, y vers le
haut, en mètres. Image de 256 px pour 0,26 m (985 px/m), dessinée en 1024 px puis réduite (bords lisses).
Sorties : Assets/_Game/Art/Characters/<Perso>/Face/<Perso>_face_<expression>.png.
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
EXTENT = 0.26
FINAL = 256
SUPER = 4
SIZE = FINAL * SUPER
PPM = SIZE / EXTENT
EXPRESSIONS = ("neutral", "effort", "pain", "rage")

LINE = (24, 16, 18, 255)


def px(x, y):
    return (SIZE / 2 + x * PPM, SIZE / 2 - y * PPM)


def box(cx, cy, w, h):
    (x0, y0), (x1, y1) = px(cx - w / 2, cy + h / 2), px(cx + w / 2, cy - h / 2)
    return [x0, y0, x1, y1]


def stroke(width_m):
    return max(1, int(width_m * PPM))


def translucent(img):
    """Calque pour les traits semi-transparents : ImageDraw remplace les pixels au lieu de les mélanger."""
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    return layer, ImageDraw.Draw(layer)


def face_base(img, skin, center, radii, hood_shadow=0.0):
    """Ovale de peau au bord doux ; ombre de capuche en haut (Voyou)."""
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    d.ellipse(box(center[0], center[1], radii[0] * 2, radii[1] * 2), fill=(*skin, 255))
    alpha = layer.split()[3].filter(ImageFilter.GaussianBlur(0.006 * PPM))
    layer.putalpha(alpha)

    if hood_shadow > 0:
        shade = Image.new("RGBA", img.size, (0, 0, 0, 0))
        sd = ImageDraw.Draw(shade)
        top = px(0, center[1] + radii[1])[1]
        bottom = px(0, center[1] + radii[1] * 0.1)[1]

        for row in range(int(top), int(bottom)):
            t = (row - top) / max(1, bottom - top)
            sd.line([(0, row), (SIZE, row)], fill=(10, 6, 12, int(255 * hood_shadow * (1 - t) ** 1.5)))

        shade.putalpha(Image.composite(shade.split()[3], Image.new("L", img.size, 0), alpha))
        layer = Image.alpha_composite(layer, shade)

    img.alpha_composite(layer)


def eye(d, cx, cy, w, h, iris, expression, glow=False):
    """Œil anime : blanc, iris haut, pupille, reflets, paupière supérieure épaisse."""
    if expression == "pain":
        # Yeux plissés de douleur : chevrons.
        sign = 1 if cx < 0 else -1
        d.line([px(cx - w / 2, cy + h * 0.35), px(cx + sign * w * 0.15, cy), px(cx - w / 2, cy - h * 0.35)] if sign > 0 else
               [px(cx + w / 2, cy + h * 0.35), px(cx + sign * w * 0.15, cy), px(cx + w / 2, cy - h * 0.35)],
               fill=LINE, width=stroke(0.0032), joint="curve")
        return

    squint = {"neutral": 1.0, "effort": 0.55, "rage": 1.1}[expression]
    hh = h * squint
    sclera = (255, 250, 240, 255) if not glow else (255, 255, 235, 255)
    d.ellipse(box(cx, cy, w, hh), fill=sclera)
    iris_w = w * (0.42 if expression == "rage" else 0.55)
    iris_h = hh * (0.85 if expression == "rage" else 1.05)
    ix = cx + w * 0.08
    d.ellipse(box(ix, cy - hh * 0.05, iris_w, iris_h), fill=(*iris, 255))
    d.ellipse(box(ix, cy - hh * 0.08, iris_w * 0.5, iris_h * 0.55), fill=(20, 12, 10, 255))
    # Reflets : un grand en haut, un petit en bas ; plus forts sous la capuche (les yeux accrochent la lumière).
    highlight = (255, 255, 255, 255)
    d.ellipse(box(ix - iris_w * 0.18, cy + hh * 0.18, iris_w * (0.42 if glow else 0.34), iris_w * (0.42 if glow else 0.34)), fill=highlight)
    d.ellipse(box(ix + iris_w * 0.2, cy - hh * 0.25, iris_w * 0.16, iris_w * 0.16), fill=highlight)
    # Paupière supérieure épaisse, qui déborde un peu vers l'arrière ; trait inférieur fin.
    top = box(cx, cy, w * 1.12, hh * 1.25)
    d.arc(top, 195, 345, fill=LINE, width=stroke(0.0036))
    d.arc(box(cx, cy, w * 0.9, hh * 1.1), 30, 150, fill=LINE, width=stroke(0.0012))


def brows(d, eyes, expression, color, thick=0.004):
    for (cx, cy, w, _), near in zip(eyes, (True, False)):
        y = cy + 0.022
        inner = cx + w * 0.45 if near else cx - w * 0.45
        outer = cx - w * 0.55 if near else cx + w * 0.55
        tilt = {"neutral": 0.002, "effort": -0.006, "pain": 0.008, "rage": -0.011}[expression]
        d.line([px(outer, y + 0.002), px((inner + outer) / 2, y + 0.004), px(inner, y + tilt)],
               fill=(*color, 255), width=stroke(thick), joint="curve")


def mouth(d, x, y, expression, width=0.026):
    if expression == "neutral":
        d.line([px(x - width / 2, y), px(x + width / 2, y + 0.001)], fill=LINE, width=stroke(0.0018))
    elif expression == "effort":
        d.rectangle(box(x, y, width * 0.8, 0.008), fill=(250, 245, 235, 255), outline=LINE, width=stroke(0.0016))
        d.line([px(x - width * 0.4, y), px(x + width * 0.4, y)], fill=LINE, width=stroke(0.001))
    elif expression == "pain":
        d.ellipse(box(x, y - 0.002, width * 0.75, 0.013), fill=(70, 20, 25, 255), outline=LINE, width=stroke(0.0016))
    else:
        d.ellipse(box(x, y - 0.004, width * 1.05, 0.02), fill=(90, 18, 22, 255), outline=LINE, width=stroke(0.0018))
        d.rectangle(box(x, y + 0.004, width * 0.8, 0.004), fill=(250, 245, 235, 255))


def nathan(expression):
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    face_base(img, (236, 196, 172), (-0.006, -0.03), (0.057, 0.058))
    d = ImageDraw.Draw(img)
    eyes = [(-0.030, 0.0, 0.028, 0.016), (0.022, 0.002, 0.019, 0.015)]

    for cx, cy, w, h in eyes:
        eye(d, cx, cy, w, h, (110, 70, 40), expression)

    brows(d, eyes, expression, (58, 38, 28))
    # Nez de 3/4 : une ombre courte côté avant ; joues et acné.
    d.line([px(0.038, -0.018), px(0.043, -0.03), px(0.036, -0.032)], fill=(150, 95, 80, 255), width=stroke(0.0016))

    layer, ld = translucent(img)

    for x, y in ((-0.046, -0.026), (-0.04, -0.035), (-0.052, -0.016), (0.012, 0.02), (-0.012, 0.022), (0.028, -0.03), (-0.03, -0.042)):
        ld.ellipse(box(x, y, 0.0046, 0.0042), fill=(196, 84, 74, 210))

    img.alpha_composite(layer)
    d = ImageDraw.Draw(img)

    mouth(d, 0.008, -0.052, expression)

    # Lunettes à double foyer, monture noire épaisse, branche vers l'oreille.
    lenses = [(-0.030, 0.0, 0.044, 0.030), (0.022, 0.002, 0.031, 0.029)]

    layer, ld = translucent(img)

    for cx, cy, w, h in lenses:
        # Verre à peine teinté, ligne du double foyer, reflet en biais : les yeux restent visibles derrière.
        ld.rectangle(box(cx, cy, w, h), fill=(220, 235, 255, 40))
        ld.line([px(cx - w * 0.42, cy - h * 0.2), px(cx + w * 0.42, cy - h * 0.2)], fill=(40, 30, 30, 150), width=stroke(0.001))
        ld.line([px(cx - w * 0.32, cy + h * 0.4), px(cx - w * 0.08, cy + h * 0.12)], fill=(255, 255, 255, 140), width=stroke(0.0018))

    img.alpha_composite(layer)
    d = ImageDraw.Draw(img)

    for cx, cy, w, h in lenses:
        d.rounded_rectangle(box(cx, cy, w, h), radius=0.004 * PPM, outline=(14, 10, 12, 255), width=stroke(0.0034))

    d.line([px(-0.008, 0.004), px(0.0065, 0.005)], fill=(14, 10, 12, 255), width=stroke(0.003))
    d.line([px(-0.052, 0.006), px(-0.085, 0.01)], fill=(14, 10, 12, 255), width=stroke(0.003))
    return img


def voyou(expression):
    img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    face_base(img, (196, 150, 124), (-0.004, -0.024), (0.056, 0.062), hood_shadow=0.75)
    d = ImageDraw.Draw(img)
    eyes = [(-0.029, -0.002, 0.027, 0.013), (0.021, 0.0, 0.018, 0.012)]

    for cx, cy, w, h in eyes:
        eye(d, cx, cy, w, h, (196, 132, 50), expression, glow=True)

    brows(d, eyes, expression, (34, 22, 18), thick=0.005)
    d.line([px(0.038, -0.018), px(0.043, -0.03), px(0.035, -0.032)], fill=(110, 70, 58, 255), width=stroke(0.0018))

    # Barbe de quelques jours sur la mâchoire.
    layer, ld = translucent(img)

    for i in range(70):
        x = -0.05 + (i * 37 % 97) / 97 * 0.09
        y = -0.045 - (i * 53 % 89) / 89 * 0.035
        ld.ellipse(box(x, y, 0.0016, 0.0016), fill=(60, 40, 34, 140))

    img.alpha_composite(layer)
    d = ImageDraw.Draw(img)

    mouth(d, 0.008, -0.05, expression, width=0.024)
    return img


def main():
    for character, draw in (("Nathan_M0", nathan), ("Voyou", voyou)):
        folder = ROOT / "Assets" / "_Game" / "Art" / "Characters" / character / "Face"
        folder.mkdir(parents=True, exist_ok=True)

        for expression in EXPRESSIONS:
            image = draw(expression).resize((FINAL, FINAL), Image.LANCZOS)
            image.save(folder / f"{character}_face_{expression}.png")
            print(f"[visage] {character} {expression}")


if __name__ == "__main__":
    main()
