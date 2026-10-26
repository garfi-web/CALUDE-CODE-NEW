"""Efface une ligne de sol fine d'un concept, sans toucher au personnage.

Un pixel est repeint de la couleur du fond seulement s'il diffère du fond ET que les pixels
situés `--gap` lignes au-dessus et au-dessous sont du fond : les chaussures, qui recouvrent la
ligne, sont donc conservées. Pas de génération, aucune retouche du personnage
(règle de l'auteur : ne pas dériver de nouvelles images d'un concept validé).

Usage : python remove_floor_line.py <concept.png> <sortie.png> --rows 928 940
"""
import argparse

import numpy as np
from PIL import Image


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("image")
    parser.add_argument("output")
    parser.add_argument("--rows", type=int, nargs=2, required=True, metavar=("Y0", "Y1"))
    parser.add_argument("--gap", type=int, default=4)
    parser.add_argument("--tolerance", type=int, default=40)
    args = parser.parse_args()

    im = np.asarray(Image.open(args.image).convert("RGB")).copy()
    data = im.astype(int)
    border = np.concatenate([data[0], data[-1], data[:, 0], data[:, -1]])
    bg = np.median(border, axis=0)
    is_bg = np.abs(data - bg).sum(axis=2) <= args.tolerance

    y0, y1 = args.rows
    changed = 0

    for y in range(y0, y1 + 1):
        above, below = y - args.gap, y + args.gap
        thin = ~is_bg[y] & is_bg[above] & is_bg[below]
        im[y][thin] = bg.astype(np.uint8)
        changed += int(thin.sum())

    Image.fromarray(im).save(args.output)
    print(f"fond {bg}, {changed} pixels repeints -> {args.output}")


if __name__ == "__main__":
    main()
