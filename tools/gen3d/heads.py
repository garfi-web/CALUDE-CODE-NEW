"""Têtes illustrées de l'arène 2,5D (demande de l'auteur du 2026-10-30, qui remplace le calque de visage refusé).

Pour chaque personnage, une tête complète (visage, cheveux, capuche ou lunettes, haut du cou) par expression, dans le
style anime des concepts validés, au même angle que les sprites (3/4 surélevé de 18°, tourné de 30°). La forme vient
du modèle 3D : ControlNet Union (type « normal ») sur la carte des normales de la tête rendue dans Blender
(tools/blender/redmet_quality.py, head_reference). Même graine et même guide pour toutes les expressions d'un visage :
seuls les mots d'expression changent. Aucune image n'est dérivée d'un concept validé.

    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/heads.py generate [Nathan_M0 Voyou]
    "C:/RED MET/ai/BiRefNet/.venv/Scripts/python.exe" -I tools/gen3d/heads.py finish [Nathan_M0 Voyou]

generate : ComfyUI doit tourner. Originaux dans ArtSource/Blender/Heads/raw/ (un .json par image).
finish : détourage BiRefNet, calage sur la silhouette de la tête 3D, puis même traitement que le corps : 4 aplats,
liseré côté lumière, contour de même épaisseur et de même couleur, peau recalée sur celle du corps.
Sorties : Assets/_Game/Art/Characters/<Perso>/Heads/<Perso>_head_v<n>_<expression>.png (1024 px, pivot au centre =
milieu de l'os de la tête).
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
REF = ROOT / "ArtSource" / "Blender" / "Heads"
RAW = REF / "raw"
OUT = ROOT / "Assets" / "_Game" / "Art" / "Characters"
SIZE = 1024
# Contour du corps : coque de 0,024 m ; le cadre de la tête fait 0,48 m pour 1024 px.
OUTLINE_PX = int(round(0.024 * SIZE / 0.48))

EXPRESSIONS = {
    "neutral": "calm, neutral expression, closed mouth",
    "effort": "determined, focused, furrowed brows, clenched teeth, gritted teeth",
    "pain": "pain, wincing, grimace, one eye closed, teeth showing, sweat",
    "rage": "angry, furious, shouting, open mouth, glaring, v-shaped eyebrows",
    "ko": "closed eyes, unconscious, dazed, mouth slightly open",
}

STYLE = ("anime coloring, cel shading, clean lineart, detailed face, detailed hair, highres, simple background, "
         "plain grey background, masterpiece, high score, great score, absurdres")
VIEW = ("portrait, head only, (whole head in frame:1.1), close-up, (three-quarter view:1.3), face toward viewer, looking to the side, "
        "slightly from above, neck")
NEGATIVE = ("from behind, back of head, bald, receding hairline, shaved head, face hidden, face covered, body, shoulders, chest, hands, arms, multiple views, extra eyes, asymmetrical eyes, deformed face, "
            "bad anatomy, cropped head, out of frame, text, logo, watermark, signature, female, child, chibi, "
            "lowres, blurry, worst quality, low quality, low score, bad score, average score, nsfw")

# Personnage : (description, [(variante, graine)], contour RGB, peau du corps RGB)
CHARACTERS = {
    "Nathan_M0": (
        "1boy, solo, male focus, young adult man, 19 years old, chubby face, round cheeks, double chin, curly brown "
        "hair, messy hair, (thick curly brown hair on top of head:1.3), bangs, (thick black rectangular glasses:1.3), bifocal glasses, (acne:1.2), pimples on cheeks, "
        "light stubble, brown eyes, pale skin, black hoodie collar",
        # 9101 écartée (3 têtes sur 5 presque chauves, le crâne lisse de la tête 3D) : 9102.
        [("", 9102)], (51, 5, 8), (237, 194, 168)),
    "Voyou": (
        "1boy, solo, male focus, adult man, 30 years old, hard face, tough, thug, (navy blue hoodie:1.2), "
        "(hood up:1.3), hood shadow on forehead only, (face fully visible:1.3), eyes visible, mouth visible",
        [("short beard, scar on cheek, light brown skin, dark eyes", 9201),
         # 9301 (capuche qui remplit le cadre, visage coupé) et 9401 (visage caché ou déformé) écartées.
         ("goatee, broken nose, pale skin, dark circles under eyes, grey eyes", 9302),
         ("stubble, dark skin, thick eyebrows, small earring", 9402)],
        (0, 0, 0), (184, 140, 115)),
}


# ------------------------------------------------------------------ Génération

def guide(character):
    """Carte des normales sur fond neutre (normale face caméra), en RVB."""
    normal = Image.open(REF / f"{character}_head_ref_normal.png").convert("RGBA")
    flat = Image.new("RGBA", normal.size, (128, 128, 255, 255))
    flat.alpha_composite(normal)
    path = REF / f"{character}_head_guide.png"
    flat.convert("RGB").save(path)
    return path


def graph(cfg, prompt, negative, seed, image_name, strength=0.75, end=0.8):
    return {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": cfg["checkpoint"]}},
        "2": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["1", 1], "text": prompt}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["1", 1], "text": negative}},
        "4": {"class_type": "EmptyLatentImage", "inputs": {"width": SIZE, "height": SIZE, "batch_size": 1}},
        "10": {"class_type": "ControlNetLoader", "inputs": {"control_net_name": "controlnet-union-sdxl-1.0-promax.safetensors"}},
        "11": {"class_type": "SetUnionControlNetType", "inputs": {"control_net": ["10", 0], "type": "normal"}},
        "12": {"class_type": "LoadImage", "inputs": {"image": image_name}},
        "13": {"class_type": "ControlNetApplyAdvanced", "inputs": {
            "positive": ["2", 0], "negative": ["3", 0], "control_net": ["11", 0], "image": ["12", 0],
            "strength": strength, "start_percent": 0.0, "end_percent": end, "vae": ["1", 2]}},
        "5": {"class_type": "KSampler", "inputs": {
            "model": ["1", 0], "positive": ["13", 0], "negative": ["13", 1], "latent_image": ["4", 0],
            "seed": seed, "steps": cfg.get("steps", 28), "cfg": cfg.get("cfg", 5.0),
            "sampler_name": cfg.get("sampler", "euler_ancestral"), "scheduler": cfg.get("scheduler", "normal"),
            "denoise": 1.0}},
        "6": {"class_type": "VAEDecode", "inputs": {"samples": ["5", 0], "vae": ["1", 2]}},
        "7": {"class_type": "SaveImage", "inputs": {"images": ["6", 0], "filename_prefix": "redmet_head"}},
    }


def generate(names, seeds_override=None):
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    import background
    import concept
    import skies

    cfg = skies.config()
    RAW.mkdir(parents=True, exist_ok=True)

    for character in names:
        description, variants, _, _ = CHARACTERS[character]
        uploaded = concept.upload_image(cfg["url"].rstrip("/"), guide(character))

        for index, (variant, seed) in enumerate(variants, start=1):
            seed = (seeds_override or {}).get((character, index), seed)

            for expression, words in EXPRESSIONS.items():
                path = RAW / f"{character}_head_v{index}_{expression}_s{seed}.png"

                if path.exists():
                    continue

                prompt = ", ".join(p for p in (description, variant, words, VIEW, STYLE) if p)
                duration = background.run_workflow(cfg, graph(cfg, prompt, NEGATIVE, seed, uploaded), path)
                info = {"tool": "ComfyUI", "checkpoint": cfg["checkpoint"], "controlnet": "union promax, normal, 0.75, fin 0.8",
                        "guide": f"{character}_head_guide.png", "prompt": prompt, "negative": NEGATIVE, "seed": seed,
                        "duration_s": duration}
                path.with_suffix(".json").write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
                print(f"[tête] {path.name} ({duration} s)", flush=True)


# ------------------------------------------------------------------- Finition

def dilate(mask, radius):
    """Dilatation d'un masque booléen par un disque (décalages successifs, sans dépendance)."""
    out = mask.copy()
    r = int(radius)
    ys, xs = np.mgrid[-r:r + 1, -r:r + 1]
    offsets = [(y, x) for y, x in zip(ys.ravel(), xs.ravel()) if y * y + x * x <= r * r and (y or x)]
    padded = np.pad(mask, r)
    h, w = mask.shape

    for y, x in offsets[::3] + offsets[1::3] + offsets[2::3]:
        out |= padded[r + y:r + y + h, r + x:r + x + w]

    return out


def fit_to_reference(rgba, character):
    """Cale la tête détourée sur la silhouette de la tête 3D (même boîte), pour que pivot et proportions collent."""
    ref = np.asarray(Image.open(REF / f"{character}_head_ref_toon.png").convert("RGBA"))[..., 3] > 32
    alpha = rgba[..., 3] > 0.2
    ry, rx = np.where(ref)
    gy, gx = np.where(alpha)
    scale = ((ry.max() - ry.min()) / max(1, gy.max() - gy.min()) + (rx.max() - rx.min()) / max(1, gx.max() - gx.min())) / 2
    img = Image.fromarray((rgba * 255).astype(np.uint8), "RGBA")
    img = img.resize((max(1, int(img.width * scale)), max(1, int(img.height * scale))), Image.LANCZOS)
    a = np.asarray(img)[..., 3] > 50
    gy, gx = np.where(a)
    canvas = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    canvas.paste(img, (int((rx.min() + rx.max()) / 2 - (gx.min() + gx.max()) / 2), int(ry.max() - gy.max())), img)
    return np.asarray(canvas).astype(np.float32) / 255.0


def toon(rgba, outline_rgb, skin_rgb):
    """Même traitement que le corps : 4 aplats, peau recalée, liseré côté lumière, contour de même épaisseur."""
    rgb = rgba[..., :3].copy()
    alpha = rgba[..., 3]
    inside = alpha > 0.5

    # Peau : pixels chauds, peu saturés et clairs, ramenés à la teinte de la peau du corps.
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx, mn = rgb.max(axis=2), rgb.min(axis=2)
    sat = (mx - mn) / np.maximum(mx, 1e-3)
    skin = inside & (r >= g) & (g >= b * 0.9) & (sat > 0.12) & (sat < 0.6) & (mx > 0.45)

    if skin.sum() > 200:
        target = np.array(skin_rgb, dtype=np.float32) / 255.0
        current = np.median(rgb[skin], axis=0)
        gain = np.clip(target / np.maximum(current, 1e-3), 0.6, 1.6)
        rgb[skin] = np.clip(rgb[skin] * gain, 0, 1)

    # 4 aplats : la valeur de chaque pixel tombe sur la moyenne de sa bande ; les traits noirs restent.
    value = rgb.max(axis=2)
    bands = np.digitize(value, [0.3, 0.52, 0.74])
    flat = rgb.copy()

    for band in range(4):
        sel = inside & (bands == band) & (value > 0.16)

        if sel.any():
            mean = value[sel].mean()
            flat[sel] = rgb[sel] * (mean / np.maximum(value[sel], 1e-3))[:, None]

    rgb = np.clip(flat, 0, 1)

    # Liseré côté lumière (en haut, vers l'avant : le soleil des sprites) : bande claire au bord de la silhouette.
    edge = inside & ~(~dilate(~inside, 7))
    gy, gx = np.gradient(alpha)
    facing = (-gy * -0.6 + -gx * 0.8) > 0.02
    rim = edge & facing
    rgb[rim] = rgb[rim] * 0.45 + np.array([1.0, 0.86, 0.72]) * 0.55

    # Contour : même épaisseur que la coque du corps, même couleur, derrière la tête.
    ring = dilate(inside, OUTLINE_PX) & ~inside
    out = np.zeros((SIZE, SIZE, 4), np.float32)
    out[ring, :3] = np.array(outline_rgb, dtype=np.float32) / 255.0
    out[ring, 3] = 1.0
    out[inside, :3] = rgb[inside]
    out[inside, 3] = 1.0
    soft = (alpha > 0.05) & ~inside & ~ring
    out[soft, :3] = rgb[soft]
    out[soft, 3] = alpha[soft]
    return out


def finish(names):
    import torch  # noqa: F401  (BiRefNet)
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    import vn_cutout

    model = vn_cutout.load_model()

    for character in names:
        _, variants, outline_rgb, skin_rgb = CHARACTERS[character]
        folder = OUT / character / "Heads"
        folder.mkdir(parents=True, exist_ok=True)

        for raw in sorted(RAW.glob(f"{character}_head_v*_*.png")):
            image = Image.open(raw).convert("RGB")
            alpha = vn_cutout.matte(model, image)
            alpha[alpha < 0.03] = 0.0
            rgba = np.dstack([np.asarray(image).astype(np.float32) / 255.0, alpha])
            rgba = fit_to_reference(rgba, character)
            result = toon(rgba, outline_rgb, skin_rgb)
            name = raw.stem.rsplit("_s", 1)[0]
            Image.fromarray((np.clip(result, 0, 1) * 255).astype(np.uint8), "RGBA").save(folder / f"{name}.png")
            print(f"[tête] {name}", flush=True)


def candidates(character, index, seeds):
    """Candidats supplémentaires pour un visage (autres graines) : on garde ensuite, par expression, la tête la plus
    de 3/4 face (choix noté dans PICKS)."""
    description, variants, _, _ = CHARACTERS[character]
    for seed in seeds:
        variants[index - 1] = (variants[index - 1][0], seed)
        generate([character])


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    action = sys.argv[1]

    if action == "candidates":
        candidates(sys.argv[2], int(sys.argv[3]), [int(x) for x in sys.argv[4:]])
    else:
        names = sys.argv[2:] or list(CHARACTERS)
        generate(names) if action == "generate" else finish(names)
