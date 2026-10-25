import sys
import numpy as np
from PIL import Image, ImageFilter
src, out = sys.argv[1], sys.argv[2]
a = np.asarray(Image.open(src).convert("RGB")).astype(np.int16)
h, w, _ = a.shape
# zone des jambes du concept R0 retouché (832x1216) : sous l'ourlet, au-dessus des chaussures
region = np.zeros((h, w), bool); region[555:1085, 230:600] = True
r, g, b = a[..., 0], a[..., 1], a[..., 2]
warm = (r - b) > 4                       # le cargo est beige (chaud), le fond est gris bleuté (froid)
light = a.mean(axis=2) > 70
m = region & light & (warm | (a.mean(axis=2) < 150))
mask = Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.MinFilter(3))
m = np.asarray(mask) > 127
dark = a.copy()
lum = a.mean(axis=2, keepdims=True)
dark[m] = np.clip(lum[m] * 0.22 + np.array([8, 8, 12]), 0, 255)   # jean noir légèrement bleuté, plis conservés
Image.fromarray(dark.astype(np.uint8)).save(out)
print(f"{out} : {m.mean()*100:.1f} % des pixels teints")
