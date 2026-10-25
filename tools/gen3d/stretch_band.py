"""Allonge une bande horizontale d'une image de concept (ex. les jambes), sans toucher au reste.

La bande [--from-y, --to-y[ est étirée verticalement de --add pixels. Le haut (tête, torse)
et le bas (chaussures) gardent leur taille. Le fond est ensuite complété avec sa couleur
pour revenir à un format SDXL (--ratio largeur/hauteur), avant un passage img2img qui
efface les traces d'étirement.

python tools/gen3d/stretch_band.py --input a.png --out b.png --from-y 700 --to-y 900 --add 448
"""
import argparse

import numpy as np
from PIL import Image


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--input", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--from-y", type=int, required=True)
    parser.add_argument("--to-y", type=int, required=True)
    parser.add_argument("--add", type=int, required=True)
    parser.add_argument("--ratio", type=float, default=832 / 1216, help="Largeur/hauteur finale")
    args = parser.parse_args()

    img = Image.open(args.input).convert("RGB")
    w, h = img.size
    top = img.crop((0, 0, w, args.from_y))
    band = img.crop((0, args.from_y, w, args.to_y))
    bottom = img.crop((0, args.to_y, w, h))
    band = band.resize((w, band.height + args.add), Image.LANCZOS)

    stretched = Image.new("RGB", (w, top.height + band.height + bottom.height))
    stretched.paste(top, (0, 0))
    stretched.paste(band, (0, top.height))
    stretched.paste(bottom, (0, top.height + band.height))

    # Complète avec la couleur du fond pour atteindre le ratio demandé, personnage centré
    arr = np.asarray(img)
    border = np.concatenate([arr[0], arr[-1], arr[:, 0], arr[:, -1]])
    background = tuple(int(c) for c in np.median(border, axis=0))
    sw, sh = stretched.size
    if sw / sh > args.ratio:
        canvas = Image.new("RGB", (sw, round(sw / args.ratio)), background)
    else:
        canvas = Image.new("RGB", (round(sh * args.ratio), sh), background)
    canvas.paste(stretched, ((canvas.width - sw) // 2, (canvas.height - sh) // 2))
    canvas.save(args.out)
    print(f"[stretch] {args.input} -> {args.out} : {w}x{h} -> {canvas.width}x{canvas.height}, bande {args.from_y}-{args.to_y} +{args.add} px")


if __name__ == "__main__":
    main()
