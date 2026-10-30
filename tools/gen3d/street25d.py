"""Décor de l'arène 2,5D (étude beat'em up, 2026-10-30), via ComfyUI + Animagine XL 4.0 + ControlNet Union (canny).

Rue en ruines du monde parallèle. Le ciel est celui du ch. 3 (coucher de soleil brumeux, sans lune rouge), déjà peint
(Art/Backgrounds/Skies/ch03_sky.png). Une génération libre donne des paysages en perspective inutilisables (1er essai,
2026-10-30) : chaque élément est donc dessiné ici en traits (guide ControlNet) et le même dessin sert de masque de
détourage. Les entrées des ennemis sont à des places connues, reprises par Editor/Arena25DBuilder.cs :

- facade : 16 × 8 m (96 px/m), vue de face, base au bas de l'image. Porte à double battant x 8,1 à 10,1 m,
  fenêtre brisée de l'étage x 3,0 à 4,2 m (appui à 2,4 m), ruelle x 5,0 à 6,6 m. Ciel transparent au-dessus des toits ;
- floor : chaussée pavée, lignes fuyantes vers le haut, tassée ensuite en bande par Unity ;
- foreground_rubble, foreground_post : débris et lampadaire tordu du premier plan, plus sombres ;
- crate, bar, food : caisse cassable, barre de fer ramassable, nourriture ;
- shadow : ombre ovale des personnages (dessinée ici, sans génération : disque plein au bord doux).

    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/street25d.py            # tout
    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/street25d.py facade_a crate

ComfyUI doit tourner. Un original déjà généré (ArtSource/Street25D/<id>_raw.png) est réutilisé : supprimez-le
pour en générer un nouveau. Images finales : Assets/_Game/Art/Backgrounds/Street25D/<id>.png.
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import concept  # noqa: E402
import skies  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "ArtSource" / "Street25D"
OUT = ROOT / "Assets" / "_Game" / "Art" / "Backgrounds" / "Street25D"
PPM = 96

STYLE = ("no humans, anime style, highly detailed background art, painterly, dark fantasy, ruined, abandoned, "
         "warm sunset light from the left, long shadows, faint glowing red cracks")
NEGATIVE = ("1girl, 1boy, person, people, character, text, watermark, signature, logo, frame, border, lowres, blurry, "
            "jpeg artifacts, worst quality, low quality, moon, red moon, night, perspective, fisheye")


def _m(x, y, height_m=8.0):
    """Mètres (origine en bas à gauche) vers pixels."""
    return int(x * PPM), int((height_m - y) * PPM)


def facade_layout():
    """Traits de la façade (guide) et masque des bâtiments (le ciel est hors masque)."""
    w, h = 16 * PPM, 8 * PPM
    lines = Image.new("L", (w, h), 0)
    mask = Image.new("L", (w, h), 0)
    d, m = ImageDraw.Draw(lines), ImageDraw.Draw(mask)

    # Toits brisés, en mètres : (x, hauteur) de gauche à droite.
    roof = [(0, 4.1), (1.2, 4.3), (2.0, 3.9), (2.6, 4.2), (5.0, 4.0), (5.0, 3.2), (6.6, 3.2), (6.6, 4.5), (8.5, 4.6),
            (9.4, 4.1), (10.2, 4.4), (11.6, 4.3), (11.6, 3.7), (13.0, 3.9), (13.8, 3.5), (16, 3.8)]
    outline = [_m(x, y) for x, y in roof] + [_m(16, 0), _m(0, 0)]
    m.polygon(outline, fill=255)
    d.line([_m(x, y) for x, y in roof], fill=255, width=3)

    def rect(x0, y0, x1, y1, width=3):
        d.rectangle([_m(x0, y1), _m(x1, y0)], outline=255, width=width)

    # Murs mitoyens, corniches, soubassement.
    for x in (5.0, 6.6, 11.6):
        d.line([_m(x, 0), _m(x, 4.6)], fill=255, width=3)
    for y in (0.5, 2.6):
        d.line([_m(0, y), _m(5.0, y)], fill=255, width=2)
        d.line([_m(6.6, y), _m(16, y)], fill=255, width=2)

    # Bâtiment A : fenêtre condamnée au rez-de-chaussée, fenêtre brisée à l'étage (entrée des ennemis).
    rect(1.0, 1.0, 2.2, 2.2)
    for i in range(4):
        d.line([_m(1.0, 1.15 + i * 0.3), _m(2.2, 1.05 + i * 0.3)], fill=255, width=2)
    rect(3.0, 2.4, 4.2, 3.6, 4)
    d.line([_m(3.2, 3.6), _m(3.5, 3.0), _m(3.3, 2.7)], fill=255, width=2)
    d.line([_m(4.0, 2.4), _m(3.8, 2.9), _m(4.2, 3.1)], fill=255, width=2)

    # Ruelle : mur du fond, plus bas et en retrait.
    d.line([_m(5.3, 0), _m(5.3, 3.0)], fill=255, width=2)
    d.line([_m(6.3, 0), _m(6.3, 3.0)], fill=255, width=2)
    d.line([_m(5.3, 3.0), _m(6.3, 3.0)], fill=255, width=2)

    # Bâtiment B : porte à double battant (entrée), fenêtres de l'étage.
    rect(8.1, 0, 10.1, 2.4, 4)
    d.line([_m(9.1, 0), _m(9.1, 2.4)], fill=255, width=3)
    d.arc([_m(8.1, 2.9), _m(10.1, 1.9)], 180, 360, fill=255, width=3)
    rect(7.0, 2.8, 8.0, 3.8)
    rect(10.4, 2.8, 11.2, 3.8)

    # Bâtiment C : devanture au rideau arraché.
    rect(12.2, 0.8, 15.4, 2.3)
    for i in range(6):
        d.line([_m(12.2, 2.1 - i * 0.22), _m(14.2 - i * 0.2, 2.1 - i * 0.22)], fill=255, width=1)

    # Briques suggérées, sans régularité parfaite.
    rng = np.random.default_rng(7)
    for _ in range(140):
        x, y = rng.uniform(0, 16), rng.uniform(0.6, 3.4)
        if 5.0 < x < 6.6 or (8.1 < x < 10.1 and y < 2.4):
            continue
        d.line([_m(x, y), _m(x + rng.uniform(0.2, 0.4), y)], fill=255, width=1)

    return lines, mask


def floor_layout():
    """Pavés en perspective : rangées qui s'écartent vers le bas, joints qui fuient vers un point haut."""
    w, h = 1536, 640
    lines = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(lines)
    d.line([(0, 18), (w, 18)], fill=255, width=4)  # bord du trottoir
    y, step = 30.0, 14.0
    rows = []
    while y < h:
        rows.append(y)
        d.line([(0, int(y)), (w, int(y))], fill=255, width=2)
        y += step
        step *= 1.14
    vanishing = (w / 2, -900)
    for x in np.linspace(-1600, w + 1600, 70):
        d.line([vanishing, (x, h)], fill=255, width=1)
    # Fissures.
    rng = np.random.default_rng(11)
    for _ in range(9):
        x, y = rng.uniform(0, w), rng.uniform(60, h)
        pts = [(x, y)]
        for _ in range(6):
            x += rng.uniform(-60, 60)
            y += rng.uniform(-25, 25)
            pts.append((x, y))
        d.line(pts, fill=255, width=2)
    return lines, Image.new("L", (w, h), 255)


def shape_layout(size, polygons, extra_lines=(), scale=1):
    """Silhouette pleine (masque) et ses traits (guide). scale : les petits objets sont dessinés sur 512 px puis
    générés en 1024 px, SDXL ne produisant que du bruit en 512 px (1er essai, 2026-10-30)."""
    polygons = [[(x * scale, y * scale) for x, y in poly] for poly in polygons]
    extra_lines = [[(x * scale, y * scale) for x, y in seg] for seg in extra_lines]
    w, h = size[0] * scale, size[1] * scale
    mask = Image.new("L", (w, h), 0)
    lines = Image.new("L", (w, h), 0)
    m, d = ImageDraw.Draw(mask), ImageDraw.Draw(lines)
    for poly in polygons:
        m.polygon(poly, fill=255)
        d.line(poly + [poly[0]], fill=255, width=3)
    for seg in extra_lines:
        d.line(seg, fill=255, width=2)
    return lines, mask


def rubble_layout():
    poly = [(30, 500), (90, 360), (170, 330), (230, 250), (330, 230), (400, 140), (470, 170), (520, 110), (610, 150),
            (660, 230), (760, 260), (840, 330), (930, 380), (1000, 500)]
    rebar = [[(400, 140), (380, 40), (420, 10)], [(520, 110), (560, 30)], [(610, 150), (680, 60), (720, 70)]]
    blocks = [[(150, 420), (300, 380)], [(330, 300), (520, 330)], [(560, 260), (700, 300)], [(700, 380), (900, 420)]]
    return shape_layout((1024, 512), [poly], rebar + blocks)


def post_layout():
    pole = [(230, 1024), (230, 230), (250, 160), (300, 110), (370, 90), (380, 120), (310, 140), (275, 190), (270, 1024)]
    lamp = [(360, 100), (420, 100), (430, 150), (350, 150)]
    base = [(170, 1024), (190, 960), (310, 960), (330, 1024)]
    return shape_layout((512, 1024), [pole, lamp, base])


def crate_layout():
    poly = [(96, 160), (416, 160), (416, 420), (96, 420)]
    planks = [[(96, 240), (416, 240)], [(96, 330), (416, 330)], [(96, 160), (416, 420)], [(120, 160), (120, 420)],
              [(392, 160), (392, 420)]]
    return shape_layout((512, 512), [poly], planks, scale=2)


def bar_layout():
    poly = [(40, 270), (470, 225), (474, 250), (44, 295)]
    return shape_layout((512, 512), [poly], [[(60, 282), (90, 279)], [(420, 233), (460, 229)]], scale=2)


def food_layout():
    bread = [(80, 330), (110, 260), (190, 230), (300, 240), (360, 290), (350, 350), (250, 370), (130, 365)]
    apple = [(370, 300), (400, 270), (440, 270), (470, 300), (470, 350), (440, 380), (400, 380), (370, 350)]
    return shape_layout((512, 512), [bread, apple], [[(170, 260), (190, 330)], [(230, 250), (250, 345)],
                                                    [(420, 270), (425, 245)]], scale=2)


# id : (dessin, prompt, graine, assombrissement)
LAYERS = {
    "facade_a": (facade_layout, "dark grimy dilapidated row of old grey stone and brick building facades seen straight from the front, flat front "
                 "elevation, orthographic, ground floor and one upper floor, broken roofs, large wooden double door, "
                 "broken upper window, boarded window, narrow dark alley between buildings, shop with torn shutter, "
                 "rubble at the foot of the walls, soot stains, cracked plaster, pitch black alley", 25321, 1.0),
    "facade_b": (facade_layout, "dark grimy dilapidated row of old grey stone and brick building facades seen straight from the front, flat front "
                 "elevation, orthographic, ground floor and one upper floor, broken roofs, large iron double door, "
                 "broken upper window, boarded window, narrow dark alley between buildings, burnt shop front, "
                 "rubble at the foot of the walls, soot stains, cracked plaster, pitch black alley", 25322, 1.0),
    "floor": (floor_layout, "cracked cobblestone street pavement, rows of stones, seen from a high angle, scattered "
              "broken bricks, small puddles reflecting orange light, thin red glowing cracks, texture, evenly lit",
              25313, 1.0),
    "foreground_rubble": (rubble_layout, "pile of broken concrete blocks and bricks with twisted rusty rebar, "
                          "isolated, black background, close foreground, backlit", 25314, 0.5),
    "foreground_post": (post_layout, "bent rusty iron street lamp post, broken lantern, isolated, black background, "
                        "close foreground, backlit", 25315, 0.5),
    "crate": (crate_layout, "old weathered wooden crate with nailed planks, painted illustration, isolated object, "
              "black background", 25326, 1.0),
    "bar": (bar_layout, "rusty iron bar, steel pipe weapon, isolated object, black background, game item", 25317, 1.0),
    "food": (food_layout, "loaf of crusty bread and a red apple, painted illustration, isolated object, "
             "black background", 25328, 1.0),
}


def generate(cfg, name, control):
    _, prompt, seed, _ = LAYERS[name]
    path = RAW / f"{name}_raw.png"

    if path.exists():
        print(f"[rue] {name} : original réutilisé", flush=True)
        return path

    full = f"{prompt}, {STYLE}"
    duration = skies.controlled(cfg, full, NEGATIVE, seed, control, Image.open(control).size, path)
    info = {"tool": "ComfyUI", "checkpoint": cfg["checkpoint"], "controlnet": f"{skies.CONTROLNET} (canny, 0.55, fin 0.6)",
            "control": control.name, "prompt": full, "negative": NEGATIVE, "seed": seed, "duration_s": duration}
    path.with_suffix(".json").write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"[rue] {name} : {duration} s", flush=True)
    return path


def ring(size=256):
    """Repère au sol du joueur : anneau fin au bord doux (Nathan se retrouve dans la mêlée)."""
    y, x = np.mgrid[0:size, 0:size]
    r = np.hypot(x - (size - 1) / 2, y - (size - 1) / 2) / (size / 2)
    alpha = np.clip(1.0 - np.abs(r - 0.86) / 0.07, 0, 1) ** 1.5
    image = np.full((size, size, 4), 255, np.uint8)
    image[..., 3] = (alpha * 255).astype(np.uint8)
    Image.fromarray(image, "RGBA").save(OUT / "ring.png")
    print("[rue] -> ring.png", flush=True)


def shadow(size=128):
    """Ombre au sol : alpha plein au centre, bord doux sur le dernier tiers (le halo Dot.png était trop diffus)."""
    y, x = np.mgrid[0:size, 0:size]
    r = np.hypot(x - (size - 1) / 2, y - (size - 1) / 2) / (size / 2)
    alpha = np.clip((1.0 - r) / 0.35, 0, 1) ** 1.2
    image = np.zeros((size, size, 4), np.uint8)
    image[..., 3] = (alpha * 255).astype(np.uint8)
    Image.fromarray(image, "RGBA").save(OUT / "shadow.png")
    print("[rue] -> shadow.png", flush=True)


def main():
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    RAW.mkdir(parents=True, exist_ok=True)
    OUT.mkdir(parents=True, exist_ok=True)
    names = sys.argv[1:] or [*LAYERS, "shadow"]

    if "shadow" in names:
        shadow()
        ring()
        names.remove("shadow")

    cfg = skies.config() if names else None

    for name in names:
        layout, _, _, shade = LAYERS[name]
        lines, mask = layout()
        control = RAW / f"{name}_guide.png"
        lines.convert("RGB").save(control)
        raw = generate(cfg, name, control)

        rgb = np.asarray(Image.open(raw).convert("RGB").resize(mask.size, Image.LANCZOS)).astype(np.float32) / 255.0
        alpha = np.asarray(mask.filter(ImageFilter.GaussianBlur(1.0))).astype(np.float32) / 255.0
        rgb = rgb * shade

        if name.startswith("facade"):
            # Ruelle dans l'ombre (x 5,0 à 6,6 m), plus noire vers le fond ; façades assombries vers le haut.
            x0, x1 = 5.0 * PPM, 6.6 * PPM
            rgb[:, int(x0):int(x1)] *= 0.22
            rows = np.linspace(0.7, 1.0, rgb.shape[0])[:, None, None]
            rgb *= rows

        rgba = np.dstack([rgb, alpha])
        out = OUT / f"{name}.png"
        Image.fromarray((np.clip(rgba, 0, 1) * 255).astype(np.uint8), "RGBA").save(out)
        print(f"[rue] -> {out.relative_to(ROOT)}", flush=True)


if __name__ == "__main__":
    main()
