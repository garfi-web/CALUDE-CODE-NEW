"""Recadre une zone d'un concept (ex. un seul personnage d'une planche) et la recentre sur un fond uni.

python tools/gen3d/crop_center.py --input a.png --out b.png --box 0 0 522 1024 [--size 1024 1024]
"""
import argparse

import numpy as np
from PIL import Image


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--input", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--box", type=int, nargs=4, required=True, metavar=("X0", "Y0", "X1", "Y1"))
    parser.add_argument("--size", type=int, nargs=2, default=None, metavar=("W", "H"), help="Défaut : taille d'origine")
    parser.add_argument("--at", type=int, nargs=2, default=None, metavar=("X", "Y"),
                        help="Position du coin haut-gauche de la découpe (défaut : centrée)")
    args = parser.parse_args()

    img = Image.open(args.input).convert("RGB")
    arr = np.asarray(img)
    border = np.concatenate([arr[0], arr[-1], arr[:, 0], arr[:, -1]])
    background = tuple(int(c) for c in np.median(border, axis=0))
    crop = img.crop(tuple(args.box))
    w, h = args.size or img.size
    canvas = Image.new("RGB", (w, h), background)
    canvas.paste(crop, tuple(args.at) if args.at else ((w - crop.width) // 2, (h - crop.height) // 2))
    canvas.save(args.out)
    print(f"[crop] {args.input} {args.box} -> {args.out} ({w}x{h}), fond {background}")


if __name__ == "__main__":
    main()
