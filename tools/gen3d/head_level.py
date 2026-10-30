"""Tête A de Nathan M0 (s9104, choisie par l'auteur) redressée : regard horizontal vers l'adversaire, même visage,
sans bord coupé, avec un vrai cou (demande de l'auteur, 2026-10-30).

Le visage n'est pas régénéré : l'original est réduit et tourné (le menton redescend), puis Animagine ne dessine, en
inpainting, que ce qui manque : les cheveux coupés par le cadre, le fond, et le cou à la place du col du sweat
(ImageCompositeMasked recolle tout le reste à l'identique).

    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/head_level.py prepare
    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/head_level.py generate 9161 9162 9163
    "C:/RED MET/ai/BiRefNet/.venv/Scripts/python.exe" -I tools/gen3d/head_level.py cutout 9163_2

Retenue (2026-10-30) : s9163_2 (inclinaison -20°) ; écartées : 9161 (deuxième visage), 9162 et 9163 (inclinaison -14°,
regard encore montant), 9164 (masse de cheveux en double).
cutout : détourage BiRefNet, tout ce qui est sous la base du cou est retiré (le col et le corps 3D prennent le relais) ;
sortie Assets/_Game/Art/Characters/Nathan_M0/Heads/Nathan_M0_head_A_level.png, avec le point de la base du cou
(pivot) dans Nathan_M0_head_A_level.json.

Sorties : ArtSource/Blender/Heads/level/Nathan_M0_head_A_level_s<graine>.png (+ .json).
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "ArtSource" / "Blender" / "Heads" / "raw" / "Nathan_M0_head_v1_neutral_s9104.png"
OUT = ROOT / "ArtSource" / "Blender" / "Heads" / "level"
TILT = -20.0          # degrés (sens des aiguilles d'une montre) : le menton redescend, le regard devient horizontal
SCALE = 0.6
OFFSET = (-30, -60)   # position de la tête réduite dans le cadre de 1024 px (place pour le cou en bas)


def prepare():
    OUT.mkdir(parents=True, exist_ok=True)
    image = Image.open(SOURCE).convert("RGB")
    a = np.asarray(image).astype(np.float32)
    background = tuple(int(c) for c in np.median(a[:40, -40:].reshape(-1, 3), axis=0))
    # Col du sweat (noir, en bas de l'original) : à redessiner en cou nu.
    collar = (a.max(axis=2) < 70) & (np.arange(a.shape[0])[:, None] > 780)
    collar_img = Image.fromarray((collar * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(21))
    inside = Image.new("L", image.size, 255)

    def place(img, fill):
        img = img.rotate(TILT, resample=Image.BICUBIC, expand=True, fillcolor=fill)
        img = img.resize((int(img.width * SCALE), int(img.height * SCALE)), Image.LANCZOS)
        canvas = Image.new(img.mode, (1024, 1024), fill)
        canvas.paste(img, ((1024 - img.width) // 2 + OFFSET[0], (1024 - img.height) // 2 + OFFSET[1]))
        return canvas

    placed = place(image, background)
    known = np.asarray(place(inside, 0)) > 250
    # Bords coupés de l'original : une bande de 24 px à l'intérieur est aussi redessinée (raccord des cheveux).
    known = np.asarray(Image.fromarray((known * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(49))) > 0
    collar_placed = np.asarray(place(collar_img, 0)) > 128
    mask = (~known) | collar_placed
    mask_img = Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(4)).point(lambda v: 255 if v > 100 else 0)
    placed.save(OUT / "Nathan_M0_head_A_level_init.png")
    mask_img.convert("RGB").save(OUT / "Nathan_M0_head_A_level_mask.png")
    print("[tête A] init et masque prêts", flush=True)


def generate(seeds):
    import concept
    import heads
    import skies

    cfg = skies.config()
    description = heads.CHARACTERS["Nathan_M0"][0]
    prompt = (f"{description}, (looking ahead:1.3), (looking to the side:1.2), level head, (bare neck:1.3), thick neck, "
              f"no collar, no clothes, whole head, (complete curly hair:1.2), {heads.STYLE}")
    negative = heads.NEGATIVE + ", looking up, head tilted back, cropped hair, cut off, frame, border"

    for seed in seeds:
        path, info = concept.generate(cfg, prompt, negative, seed, OUT, f"Nathan_M0_head_A_level_s{seed}",
                                      init_image=str(OUT / "Nathan_M0_head_A_level_init.png"),
                                      mask_image=str(OUT / "Nathan_M0_head_A_level_mask.png"), denoise=1.0,
                                      size=[1024, 1024])
        info["source"] = SOURCE.name
        info["tilt_deg"], info["scale"] = TILT, SCALE
        path.with_suffix(".json").write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
        print(f"[tête A] {path.name}", flush=True)


# Base du cou dans l'image retenue (px, cadre de 1024) et ligne de coupe : au-dessus, tête et cou ; au-dessous, retiré.
NECK_BASE = (455, 795)


def keep_above(x, y):
    if x < 300:
        return y < 745
    if x > 600:
        return y < 700 + (x < 680) * 20
    if 320 < x < 600:   # le cou descend sous le col du sweat
        return y < NECK_BASE[1] + 60
    return y < NECK_BASE[1] - 20


def cutout(name):
    import vn_cutout

    src = OUT / f"Nathan_M0_head_A_level_s{name}.png"
    image = Image.open(src).convert("RGB")
    model = vn_cutout.load_model()
    alpha = vn_cutout.matte(model, image)
    h, w = alpha.shape
    yy, xx = np.mgrid[0:h, 0:w]
    keep = np.vectorize(keep_above)(xx, yy)
    alpha = np.where(keep, alpha, 0.0)
    rgb = np.asarray(image).astype(np.float32) / 255.0
    # Reste du fond vert pâle de l'original (gardé par l'inpainting, que BiRefNet prend pour la tête) : retiré.
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    old_bg = (g > r + 0.03) & (g >= b) & (rgb.mean(axis=2) > 0.68)
    old_bg = np.asarray(Image.fromarray((old_bg * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3))) > 0
    alpha = np.where(old_bg, 0.0, alpha)
    alpha[alpha < 0.05] = 0.0
    out = Image.fromarray((np.dstack([rgb, alpha]) * 255).astype(np.uint8), "RGBA")
    dest = ROOT / "Assets" / "_Game" / "Art" / "Characters" / "Nathan_M0" / "Heads" / "Nathan_M0_head_A_level.png"
    out.save(dest)
    dest.with_suffix(".json").write_text(json.dumps({"source": src.name, "size": [w, h], "neck_base_px": NECK_BASE},
                                                    indent=2), encoding="utf-8")
    print(f"[tête A] {dest.relative_to(ROOT)}", flush=True)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    if sys.argv[1] == "prepare":
        prepare()
    elif sys.argv[1] == "cutout":
        cutout(sys.argv[2])
    else:
        generate([int(a) for a in sys.argv[2:]] or [9161, 9162, 9163])
