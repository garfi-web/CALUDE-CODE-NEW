"""Fabrique un masque d'inpainting (blanc = à redessiner) pour concept.py --mask.

Le masque est l'union de rectangles, de polygones et de zones de couleur, moins les exclusions.
Les coordonnées sont celles de l'image d'entrée, en pixels.

python tools/gen3d/make_mask.py --input a.png --out a_mask.png \
    --color 180 170 155 --tol 40 --within 300 600 600 1150 --dilate 6 --preview a_mask_preview.png
"""
import argparse

import numpy as np
from PIL import Image, ImageDraw, ImageFilter


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--input", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--rect", type=int, nargs=4, action="append", default=[], metavar=("X0", "Y0", "X1", "Y1"))
    parser.add_argument("--poly", action="append", default=[], help='Polygone "x,y x,y x,y ..."')
    parser.add_argument("--color", type=int, nargs=3, action="append", default=[], metavar=("R", "G", "B"),
                        help="Couleur à masquer (distance max --tol), limitée aux zones --within")
    parser.add_argument("--tol", type=int, default=40)
    parser.add_argument("--within", type=int, nargs=4, action="append", default=[], metavar=("X0", "Y0", "X1", "Y1"))
    parser.add_argument("--exclude", type=int, nargs=4, action="append", default=[], metavar=("X0", "Y0", "X1", "Y1"))
    parser.add_argument("--dilate", type=int, default=0, help="Élargit le masque de N pixels")
    parser.add_argument("--preview", default=None, help="Image de contrôle : masque en rouge sur l'image")
    args = parser.parse_args()

    img = Image.open(args.input).convert("RGB")
    w, h = img.size
    mask = Image.new("L", (w, h), 0)
    draw = ImageDraw.Draw(mask)
    for x0, y0, x1, y1 in args.rect:
        draw.rectangle([x0, y0, x1, y1], fill=255)
    for poly in args.poly:
        points = [tuple(int(v) for v in p.split(",")) for p in poly.split()]
        draw.polygon(points, fill=255)

    m = np.asarray(mask).copy()
    if args.color:
        arr = np.asarray(img).astype(np.int16)
        region = np.zeros((h, w), bool)
        for x0, y0, x1, y1 in (args.within or [(0, 0, w, h)]):
            region[y0:y1, x0:x1] = True
        for color in args.color:
            near = np.abs(arr - np.array(color)).max(axis=2) <= args.tol
            m[near & region] = 255
    for x0, y0, x1, y1 in args.exclude:
        m[y0:y1, x0:x1] = 0

    mask = Image.fromarray(m)
    if args.dilate > 0:
        mask = mask.filter(ImageFilter.MaxFilter(args.dilate * 2 + 1))
        # referme les petits trous laissés par le seuil de couleur
        mask = mask.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3))
    mask.convert("RGB").save(args.out)

    if args.preview:
        overlay = img.copy()
        red = Image.new("RGB", (w, h), (255, 0, 0))
        overlay = Image.composite(Image.blend(img, red, 0.55), img, mask)
        overlay.save(args.preview)
    print(f"[mask] {args.out} : {round(np.asarray(mask).mean() / 255 * 100, 1)} % de l'image")


if __name__ == "__main__":
    main()
