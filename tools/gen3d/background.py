"""Couches peintes du décor de l'arène de la lune rouge, via ComfyUI + Animagine XL 4.0.

Pour chaque couche : génération (graine fixe), raccord sans couture pour les couches qui se répètent
(fondu enchaîné entre les deux bords, qui garantit la continuité, puis la bande fondue, ramenée au centre,
est repeinte par inpainting partiel pour changer la double image en formes cohérentes), détourage des couches en parallaxe, puis
réglage de valeur : plus une couche est lointaine, plus elle est sombre, désaturée et noyée dans la brume
rouge, pour que les personnages ressortent toujours du fond.

Détourage : les couches sont peintes en silhouettes à contre-jour sur un ciel rouge vif uni, puis le ciel
est retiré par sa luminosité. Un fond vert ne marche pas avec Animagine : il en fait une scène éclairée en vert.

    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/background.py            # tout
    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/background.py ruins sky  # quelques couches

ComfyUI doit tourner. Un original déjà généré (ArtSource/Backgrounds/RedMoon/<couche>_raw.png) est réutilisé :
supprimez-le pour en générer un nouveau. Bruts et métadonnées : ArtSource/Backgrounds/RedMoon/ ; couches finales :
Assets/_Game/Art/Backgrounds/RedMoonPainted/<couche>.png (lues par Editor/RedMoonDecor.cs).
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import concept  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "ArtSource" / "Backgrounds" / "RedMoon"
OUT = ROOT / "Assets" / "_Game" / "Art" / "Backgrounds" / "RedMoonPainted"

STYLE = "no humans, scenery, dark fantasy, anime style, highly detailed background art, painterly, dramatic lighting"
BACKLIT = ("dark silhouette against a bright glowing red sky, backlit, flat bright red background, simple background, "
           "uniform red sky")
NEGATIVE = ("1girl, 1boy, person, people, character, text, watermark, signature, logo, frame, border, "
            "lowres, blurry, jpeg artifacts, worst quality, low quality, cropped")
NEGATIVE_KEYED = NEGATIVE + ", clouds, moon, sun, stars, fog, mist, water, reflection, dark sky, night sky, green"
NEGATIVE_SKY = NEGATIVE + ", moon, planet, sun, ground, land, mountains, landscape, horizon line, buildings, trees"
NEGATIVE_MOON = NEGATIVE + ", lens flare, light streak, horizon, ground, eclipse, dark side, crescent"
NEGATIVE_GROUND = NEGATIVE + ", sky, horizon, trees, cave, landscape, scenery, perspective, water, snow, blue"

# nom : prompt, taille, graine, détourage, répétition horizontale, profondeur (0 = plan du jeu, 1 = ciel).
LAYERS = {
    "sky": ("night sky, dark storm clouds lit by red light from below, blood red sky near the horizon, black sky above, "
            "no moon, cloudscape, only sky", (1344, 768), 5101, False, True, 1.0),
    "moon": ("huge full moon, fully lit bright crimson moon surface, detailed craters and maria, glowing red moon, "
             "centered, black background, simple background, space", (1024, 1024), 5112, False, False, 1.0),
    "clouds": ("thin wispy black clouds, long horizontal cloud streaks, smoke wisps, " + BACKLIT,
               (1536, 640), 5103, True, True, 0.95),
    "mountains": ("distant jagged mountain range silhouette, sharp rocky peaks, volcanic mountains, dark crimson haze, "
                  "side view, wide panorama, " + BACKLIT, (1536, 640), 5104, True, True, 0.85),
    "ruins": ("ruins of a gothic city, broken cathedral spires, collapsed arches, crumbling towers, burnt gothic buildings, "
              "silhouette against the light, side view, wide panorama, " + BACKLIT, (1536, 640), 5105, True, True, 0.6),
    "trees": ("twisted dead trees, gnarled bare branches, leafless black trees, battlefield with swords and spears stuck in "
              "the ground, torn war banners on poles, side view, wide panorama, " + BACKLIT, (1536, 640), 5106, True, True, 0.3),
    "battlefield": ("low ash covered ground with scattered battlefield debris, skulls and bones, rusty helmets, broken "
                    "shields, a few swords and spears stuck in the ground, torn banner on a pole, side view, ground level, "
                    "flat horizon, " + BACKLIT, (1536, 640), 5117, True, True, 0.12),
    "foreground": ("dark rocks and dead thorny branches, broken spear shafts, close-up foreground silhouette, black shapes, "
                   "side view, ground level, " + BACKLIT, (1536, 640), 5108, True, True, -0.3),
    "ground": ("texture, top-down view of the ground surface, dark grey ash and gravel, cracked black stone slabs, "
               "dead roots, pebbles, flat lighting, seamless pattern", (1024, 1024), 5119, False, True, 0.0),
}

HAZE = np.array([0.30, 0.05, 0.05])


def run_workflow(cfg, workflow, out_path, timeout_s=900):
    """Envoie un graphe à ComfyUI et enregistre la première image produite."""
    import time
    import urllib.parse
    import urllib.request
    import uuid

    base = cfg["url"].rstrip("/")
    prompt_id = json.loads(concept._request(f"{base}/prompt", {"prompt": workflow, "client_id": str(uuid.uuid4())}))["prompt_id"]
    start = time.time()

    while True:
        entry = json.loads(concept._request(f"{base}/history/{prompt_id}")).get(prompt_id)
        images = [img for node in (entry or {}).get("outputs", {}).values() for img in node.get("images", [])]

        if entry and entry.get("status", {}).get("status_str") == "error":
            raise RuntimeError(json.dumps(entry["status"])[:500])
        if images:
            break
        if time.time() - start > timeout_s:
            raise TimeoutError("ComfyUI n'a pas rendu d'image à temps")
        time.sleep(1.0)

    query = urllib.parse.urlencode({k: images[0][k] for k in ("filename", "subfolder", "type")})
    with urllib.request.urlopen(f"{base}/view?{query}", timeout=60) as resp:
        out_path.write_bytes(resp.read())
    return round(time.time() - start, 1)


def tile_seam(cfg, path, prompt, negative, seed, band=300, denoise=0.6):
    """Rend l'image répétable. Fondu enchaîné sur « band » pixels entre le début et la fin de l'image
    (largeur finale : W - band, continuité garantie à la jointure), puis l'image est décalée pour amener la
    bande fondue au centre et repeinte par inpainting partiel (masque flou), qui change la double image en
    formes cohérentes. L'image est enfin remise à sa largeur d'origine."""
    img = np.asarray(Image.open(path).convert("RGB")).astype(np.float32)
    h, w, _ = img.shape
    t = np.linspace(0, 1, band)[None, :, None]
    blended = img[:, :w - band].copy()
    blended[:, :band] = img[:, w - band:] * (1 - t) + img[:, :band] * t
    rolled = np.roll(blended, (w - band) // 2 - band // 2, axis=1)
    rolled_img = Image.fromarray(rolled.astype(np.uint8)).resize((w, h), Image.LANCZOS)
    rolled_path = path.with_name(path.stem + "_rolled.png")
    rolled_img.save(rolled_path)

    x = np.arange(w)
    ramp = np.clip(1 - np.abs(x - w / 2) / (band * 0.75), 0, 1)
    mask = np.repeat((np.clip(ramp * 1.6, 0, 1) * 255).astype(np.uint8)[None, :], h, axis=0)
    mask_path = path.with_name(path.stem + "_seam_mask.png")
    Image.fromarray(mask).save(mask_path)

    base = cfg["url"].rstrip("/")
    image_name = concept.upload_image(base, rolled_path)
    mask_name = concept.upload_image(base, mask_path)
    full = cfg.get("prompt_prefix", "") + prompt + cfg.get("prompt_suffix", "")
    workflow = {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": cfg["checkpoint"]}},
        "2": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["1", 1], "text": full}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["1", 1], "text": negative}},
        "8": {"class_type": "LoadImage", "inputs": {"image": image_name}},
        "10": {"class_type": "LoadImage", "inputs": {"image": mask_name}},
        "12": {"class_type": "ImageToMask", "inputs": {"image": ["10", 0], "channel": "red"}},
        "4": {"class_type": "VAEEncode", "inputs": {"pixels": ["8", 0], "vae": ["1", 2]}},
        "14": {"class_type": "SetLatentNoiseMask", "inputs": {"samples": ["4", 0], "mask": ["12", 0]}},
        "5": {"class_type": "KSampler", "inputs": {
            "model": ["1", 0], "positive": ["2", 0], "negative": ["3", 0], "latent_image": ["14", 0],
            "seed": seed + 1000, "steps": cfg.get("steps", 28), "cfg": cfg.get("cfg", 5.0),
            "sampler_name": cfg.get("sampler", "euler_ancestral"), "scheduler": cfg.get("scheduler", "normal"),
            "denoise": denoise}},
        "6": {"class_type": "VAEDecode", "inputs": {"samples": ["5", 0], "vae": ["1", 2]}},
        "13": {"class_type": "ImageCompositeMasked", "inputs": {"destination": ["8", 0], "source": ["6", 0],
                                                               "x": 0, "y": 0, "resize_source": False, "mask": ["12", 0]}},
        "7": {"class_type": "SaveImage", "inputs": {"images": ["13", 0], "filename_prefix": "redmet_decor"}},
    }
    out_path = path.with_name(path.stem + "_tiled.png")
    duration = run_workflow(cfg, workflow, out_path)
    with open(out_path.with_suffix(".json"), "w", encoding="utf-8") as f:
        json.dump({"source": path.name, "mode": "raccord : fondu enchaîné des bords, bande au centre, SetLatentNoiseMask, masque flou",
                   "band": band, "denoise": denoise, "seed": seed + 1000, "prompt": full, "negative": negative},
                  f, ensure_ascii=False, indent=2)
    return out_path, {"duration_s": duration}


def key_sky(img):
    """Alpha par contre-jour : le ciel rouge vif (valeur haute) disparaît, les silhouettes sombres restent.
    Le seuil suit la luminosité du ciel ligne par ligne (dégradé vertical du ciel)."""
    a = np.asarray(img.convert("RGB")).astype(np.float32) / 255.0
    value = a.max(axis=2)
    sky = np.percentile(value, 90, axis=1, keepdims=True)
    sky = np.maximum(sky, np.percentile(value, 90))
    alpha = np.clip((sky * 0.62 - value) / (sky * 0.22), 0, 1)
    alpha_img = Image.fromarray((alpha * 255).astype(np.uint8)).filter(ImageFilter.MedianFilter(3))
    return a, np.asarray(alpha_img).astype(np.float32) / 255.0


def grade(rgb, depth):
    """Lointain : plus sombre, désaturé, noyé dans la brume rouge. Premier plan : presque noir."""
    luminance = rgb @ np.array([0.299, 0.587, 0.114])
    gray = np.repeat(luminance[..., None], 3, axis=2)

    if depth < 0:
        return gray * 0.18 + HAZE * 0.05

    saturation = 0.75 - 0.35 * depth
    rgb = gray + (rgb - gray) * saturation
    rgb = rgb * (0.85 - 0.45 * depth)
    return rgb * (1 - 0.45 * depth) + HAZE * (0.45 * depth)


def process(name, path):
    _, _, _, keyed, _, depth = LAYERS[name]
    img = Image.open(path).convert("RGB")

    if name == "moon":
        # Disque de la lune : tout ce qui sort du fond noir.
        a = np.asarray(img).astype(np.float32) / 255.0
        alpha = np.clip((a.max(axis=2) - 0.08) / 0.12, 0, 1)
        rgba = np.dstack([a, alpha])
    elif keyed:
        rgb, alpha = key_sky(img)
        rgba = np.dstack([grade(rgb, depth), alpha])

        # Les branches et les flèches qui touchent le haut de l'image s'effacent en fondu, sinon le bord de
        # la couche dessine une ligne droite à l'écran.
        fade = np.clip(np.linspace(0, 1, rgba.shape[0]) / 0.18, 0, 1)[:, None]
        rgba[..., 3] *= fade

        if name == "clouds":
            # Les nuages flottent : le bas s'efface aussi.
            rgba[..., 3] *= fade[::-1]

        if name == "foreground":
            # Flou du premier plan : il passe devant les personnages sans attirer l'œil. Le haut de l'image
            # s'efface en fondu, sinon les branches coupées par le bord dessinent une ligne droite.
            blurred = Image.fromarray((np.clip(rgba, 0, 1) * 255).astype(np.uint8), "RGBA").filter(ImageFilter.GaussianBlur(6))
            rgba = np.asarray(blurred).astype(np.float32) / 255.0
            fade = np.clip(np.linspace(0, 1, rgba.shape[0]) / 0.45, 0, 1)[:, None]
            rgba[..., 3] *= fade * fade
    elif name == "sky":
        a = np.asarray(img).astype(np.float32) / 255.0
        rgba = np.dstack([grade(a, 0.6), np.ones(a.shape[:2])])
    else:
        # Sol : seule la moitié basse (pierres, racines) sert ; le haut de l'image est de la fumée.
        a = np.asarray(img).astype(np.float32) / 255.0
        a = a[int(a.shape[0] * 0.45):]
        rgba = np.dstack([grade(a, 0.35), np.ones(a.shape[:2])])

    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(rgba, 0, 1) * 255).astype(np.uint8), "RGBA").save(OUT / f"{name}.png")


def main():
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    with open(Path(__file__).resolve().parent / "backends.json", encoding="utf-8") as f:
        backends = json.load(f)
    cfg = backends["concept"][backends["default_concept"]]
    names = sys.argv[1:] or list(LAYERS)
    RAW.mkdir(parents=True, exist_ok=True)

    for name in names:
        prompt, size, seed, keyed, tiled, _ = LAYERS[name]
        full = f"{prompt}, {STYLE}"
        negative = {"sky": NEGATIVE_SKY, "moon": NEGATIVE_MOON, "ground": NEGATIVE_GROUND}.get(name, NEGATIVE_KEYED if keyed else NEGATIVE)
        path = RAW / f"{name}_raw.png"

        if path.exists():
            print(f"[décor] {name} : original réutilisé ({path.name})", flush=True)
        else:
            generated, info = concept.generate(cfg, full, negative, seed, RAW, name, size=size)
            generated.replace(path)
            generated.with_suffix(".json").replace(path.with_suffix(".json"))
            print(f"[décor] {name} : {path.name} ({info['duration_s']} s)", flush=True)

        if tiled:
            path, info = tile_seam(cfg, path, full, negative, seed)
            print(f"[décor] {name} raccordé : {path.name} ({info['duration_s']} s)", flush=True)

        process(name, path)
        print(f"[décor] {name} traité : {OUT / (name + '.png')}", flush=True)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
