"""Ciels des 10 chapitres du monde parallèle, via ComfyUI + Animagine XL 4.0 (demande de l'auteur du 2026-10-29).

Règle : la lune rouge est réservée aux ch. 1 et 10 (décor validé de l'arène, inchangé) ; les ch. 2 à 9 ont
chacun leur ciel. L'identité Red Met passe par les fissures rouges lumineuses du sol, jamais par le ciel.
Les images de référence de l'auteur (Skyrim, inFAMOUS 2) ne servent que d'inspiration : aucune n'est une entrée.

Produit :
- jeu : ciel peint de chaque chapitre (Art/Backgrounds/Skies/chNN_sky.png) et versions neutres (grises) des
  couches de paysage de l'arène, colorées en jeu par la lumière du chapitre ;
- visual novel : nouveaux décors des ch. 2 à 9 (Art/Backgrounds/VN/chNN.png). Quand l'architecture du décor
  précédent était bonne (ch. 2, 3, 8), ses contours guident la nouvelle image (ControlNet Union, canny) ;
  le ciel, la lune et les couleurs sont refaits. Sinon, nouvelle image complète.

    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/skies.py               # tout
    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/skies.py sky ch05      # un ciel du jeu
    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/skies.py vn ch03       # un décor du visual novel
    "C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/skies.py neutral       # couches neutres (sans ComfyUI)

ComfyUI doit tourner (sauf pour « neutral »). Un original déjà généré (ArtSource/Backgrounds/Skies/) est
réutilisé : supprimez-le pour en refaire un.
"""
import json
import sys
import uuid
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import background  # noqa: E402
import concept  # noqa: E402
import vn_backgrounds  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "ArtSource" / "Backgrounds" / "Skies"
OUT = ROOT / "Assets" / "_Game" / "Art" / "Backgrounds" / "Skies"
LANDSCAPE = ROOT / "Assets" / "_Game" / "Art" / "Backgrounds" / "RedMoonPainted"
VN_RAW = ROOT / "ArtSource" / "VN" / "Backgrounds"
VN_OUT = ROOT / "Assets" / "_Game" / "Art" / "Backgrounds" / "VN"
CONTROLNET = "controlnet-union-sdxl-1.0-promax.safetensors"

STYLE = "no humans, scenery, anime style, highly detailed background art, painterly, dramatic lighting"
NEGATIVE = ("1girl, 1boy, person, people, character, text, watermark, signature, logo, frame, border, "
            "lowres, blurry, jpeg artifacts, worst quality, low quality")
# Ni lune rouge ni rouge sang dans le ciel des ch. 2 à 9.
NOT_RED = ", red moon, blood moon, crimson sky, blood red sky, red clouds"
NEGATIVE_SKY = NEGATIVE + ", ground, land, mountains, landscape, buildings, trees, horizon line, moon, planet" + NOT_RED

# Ciel de chaque chapitre (texte de l'auteur) : prompt du ciel seul, graine.
SKIES = {
    "ch02": ("heavy overcast sky, thick low clouds, sickly grey green clouds, drizzle, light rain, gloomy, oppressive, "
             "muted grey green color palette, cloudscape, only sky", 7102),
    "ch03": ("warm hazy sunset sky, orange and pink clouds, golden haze, humid air, glowing horizon, low sun behind thin "
             "clouds, southern city sunset, cloudscape, only sky", 7103),
    "ch04": ("clear bright blue sky, big bright sun glow, a few white fluffy cumulus clouds, sunny day, vivid azure sky, "
             "cloudscape, only sky", 7104),
    "ch05": ("night sky, aurora borealis, green and blue aurora curtains, many stars, deep blue night sky, cloudscape, "
             "only sky", 7105),
    "ch06": ("violent thunderstorm sky, dark churning storm clouds, purple and steel blue clouds, clouds lit from inside, "
             "heavy rain, cloudscape, only sky", 7106),
    "ch07": ("pale dawn sky, washed out cold morning light, thick fog, mist layers, bleached pale grey and faint yellow, "
             "overcast, cloudscape, only sky", 7107),
    "ch08": ("toxic violet dusk sky, purple and magenta clouds, sickly green tinge, neon glow from a city below lighting "
             "the clouds, cloudscape, only sky", 7108),
    "ch09": ("sky on fire, wildfire glow, orange and black smoke columns, falling ash, burning sky, embers in the air, "
             "cloudscape, only sky", 7109),
}

# Soleil et lunes pâles : calculés par Editor/RedMoonDecor.cs (les astres peints par Animagine sortaient en
# disque mécanique et en grappe de lunes, essai du 2026-10-29 rejeté).

# Décors du visual novel, ch. 2 à 9 : lieu, ciel, graine, contours du décor précédent gardés ou non, et zones
# du précédent dont les contours sont effacés (lune, astre rouge), en fractions de l'image (x0, y0, x1, y1).
# Poids : 1re passe (2026-10-29), fissures rarement visibles.
CRACKS = "(glowing red cracks in the ground:1.3)"
VN = {
    "ch02": ("medieval city street, plague, boarded houses, plague marks on doors, burning braziers, ruined granaries, "
             "wet cobblestones, heavy overcast grey green sky, drizzle, sickly grey green light", 6202, True,
             [(0.7, 0.0, 1.0, 0.35)]),
    "ch03": ("approach to a black stone fortress on a cliff, huge gate, burning braziers, (warm hazy sunset:1.3), "
             "(orange and pink sky:1.3), golden haze", 6213, True, [(0.0, 0.0, 1.0, 0.45)]),
    "ch04": ("abandoned province, empty burnt village, broken carts, war banners, smoke columns, clear bright blue sky, "
             "big sun, white clouds, sunny day", 6204, False, []),
    "ch05": ("dark throne room of black stone, ash covered throne, tall gothic pillars, braziers, huge open arches "
             "showing the night sky, green and blue aurora borealis, two pale moons", 6205, False, []),
    "ch06": ("ruined hall on the fortress outskirts, many cracked mirrors, shards of glass on the floor, broken roof "
             "open to a violent thunderstorm, lightning, purple and steel blue light", 6206, False, []),
    "ch07": ("(ruined fairground:1.3), (broken carousel with horses:1.3), torn striped circus tents, dim lanterns, "
             "pale dawn, thick fog, cold washed out light", 6217, False, []),
    "ch08": ("modern city street at night, wet asphalt, cracks in the walls glowing with red veins, grey blue "
             "buildings, toxic violet dusk sky, purple and magenta clouds, neon signs", 6208, True,
             [(0.45, 0.05, 0.7, 0.45)]),
    "ch09": ("battlefield, broken siege towers, spears and banners, burning ruins, (sky on fire:1.4), (orange burning "
             "sky:1.3), black smoke columns, falling ash", 6219, False, []),
}
VN_STYLE = ("no humans, scenery, dark fantasy, anime style, highly detailed background art, painterly, dramatic "
            "lighting, wide shot, eye level")
VN_NEGATIVE = (NEGATIVE + ", silhouette of person" + NOT_RED)

# Couches de paysage de l'arène dont une version neutre est faite.
NEUTRAL_LAYERS = ["mountains", "ruins", "trees", "battlefield", "foreground"]


def config():
    return json.loads((Path(__file__).parent / "backends.json").read_text(encoding="utf-8"))["concept"]["comfyui_animagine"]


def generate(cfg, name, prompt, negative, seed, size):
    path = RAW / f"{name}_raw.png"

    if path.exists():
        print(f"[ciel] {name} : original réutilisé", flush=True)
        return path

    generated, info = concept.generate(cfg, prompt, negative, seed, RAW, f"{name}_raw", size=size)
    generated.with_suffix(".json").write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"[ciel] {name} : {generated.name} ({info['duration_s']} s)", flush=True)
    return generated


def save(rgba, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(rgba, 0, 1) * 255).astype(np.uint8), "RGBA").save(path)


def sky(cfg, scene):
    prompt, seed = SKIES[scene]
    raw = generate(cfg, f"{scene}_sky", f"{prompt}, {STYLE}", NEGATIVE_SKY, seed, (1344, 768))
    # Ciel un peu assombri et désaturé : les personnages et le paysage passent devant.
    a = np.asarray(Image.open(raw).convert("RGB")).astype(np.float32) / 255.0
    gray = (a @ np.array([0.299, 0.587, 0.114]))[..., None]
    a = (gray + (a - gray) * 0.85) * 0.8
    save(np.dstack([a, np.ones(a.shape[:2])]), OUT / f"{scene}_sky.png")


def neutral():
    """Couches de paysage en gris clair : la lumière globale 2D et la teinte du chapitre les colorent."""
    for name in NEUTRAL_LAYERS:
        a = np.asarray(Image.open(LANDSCAPE / f"{name}.png").convert("RGBA")).astype(np.float32) / 255.0
        lum = a[..., :3] @ np.array([0.299, 0.587, 0.114])
        # Les couches d'origine sont très sombres : la valeur est remontée pour que la teinte se voie.
        value = np.clip(lum * 3.2, 0, 1)[..., None]
        # Les voiles semi-transparents des originaux (haut des couches, détourage imparfait) disparaissent sur le
        # ciel rouge sombre mais forment une bande sur les ciels clairs : seules les silhouettes franches restent.
        alpha = a[..., 3]
        if name != "foreground":
            t = np.clip((alpha - 0.2) / 0.45, 0, 1)
            alpha = t * t * (3 - 2 * t)
        save(np.dstack([np.repeat(value, 3, axis=2), alpha]), OUT / "Neutral" / f"{name}.png")
        print(f"[ciel] paysage neutre : {name}", flush=True)


def edges(raw_path, erase, size):
    """Contours du décor précédent (filtre de Sobel), sans ceux des zones effacées (lune, astre)."""
    image = Image.open(raw_path).convert("L").resize(size, Image.LANCZOS).filter(ImageFilter.GaussianBlur(1.2))
    g = np.asarray(image).astype(np.float32) / 255.0
    gx = np.zeros_like(g)
    gy = np.zeros_like(g)
    gx[:, 1:-1] = g[:, 2:] - g[:, :-2]
    gy[1:-1, :] = g[2:, :] - g[:-2, :]
    magnitude = np.hypot(gx, gy)
    lines = (magnitude > np.percentile(magnitude, 88)).astype(np.float32)
    h, w = lines.shape

    for x0, y0, x1, y1 in erase:
        lines[int(y0 * h):int(y1 * h), int(x0 * w):int(x1 * w)] = 0

    path = VN_RAW / f"{raw_path.stem}_edges.png"
    Image.fromarray((lines * 255).astype(np.uint8)).convert("RGB").save(path)
    return path


def vn(cfg, scene):
    place, seed, keep, erase = VN[scene]
    prompt = f"{place}, {CRACKS}, {VN_STYLE}" + cfg.get("prompt_suffix", "")
    raw = VN_RAW / f"{scene}_v2_raw.png"
    size = vn_backgrounds.GEN_SIZE

    if raw.exists():
        print(f"[ciel] décor {scene} : original réutilisé", flush=True)
    elif keep:
        control = edges(VN_RAW / f"{scene}_raw.png", erase, size)
        duration = controlled(cfg, prompt, VN_NEGATIVE, seed, control, size, raw)
        info = {"tool": "ComfyUI", "checkpoint": cfg["checkpoint"],
                "controlnet": f"{CONTROLNET} (canny, 0.55, fin 0.6), contours de {scene}_raw.png sans {erase}",
                "prompt": prompt, "negative": VN_NEGATIVE, "seed": seed, "size": size, "duration_s": duration}
        raw.with_suffix(".json").write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
        print(f"[ciel] décor {scene} (architecture gardée) : {duration} s", flush=True)
    else:
        generated, info = concept.generate(cfg, f"{place}, {CRACKS}, {VN_STYLE}", VN_NEGATIVE, seed, VN_RAW,
                                           f"{scene}_v2_raw", size=size)
        generated.with_suffix(".json").write_text(json.dumps(info, indent=2, ensure_ascii=False), encoding="utf-8")
        raw = generated
        print(f"[ciel] décor {scene} : {info['duration_s']} s", flush=True)

    vn_backgrounds.finish(raw, VN_OUT / f"{scene}.png")


def controlled(cfg, prompt, negative, seed, control, size, out_path):
    base = cfg["url"].rstrip("/")
    name = concept.upload_image(base, control)
    width, height = size
    graph = {
        "1": {"class_type": "CheckpointLoaderSimple", "inputs": {"ckpt_name": cfg["checkpoint"]}},
        "2": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["1", 1], "text": prompt}},
        "3": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["1", 1], "text": negative}},
        "4": {"class_type": "EmptyLatentImage", "inputs": {"width": width, "height": height, "batch_size": 1}},
        "10": {"class_type": "ControlNetLoader", "inputs": {"control_net_name": CONTROLNET}},
        "11": {"class_type": "SetUnionControlNetType", "inputs": {"control_net": ["10", 0], "type": "canny/lineart/anime_lineart/mlsd"}},
        "12": {"class_type": "LoadImage", "inputs": {"image": name}},
        "13": {"class_type": "ControlNetApplyAdvanced", "inputs": {
            "positive": ["2", 0], "negative": ["3", 0], "control_net": ["11", 0], "image": ["12", 0],
            "strength": 0.55, "start_percent": 0.0, "end_percent": 0.6, "vae": ["1", 2]}},
        "5": {"class_type": "KSampler", "inputs": {
            "model": ["1", 0], "positive": ["13", 0], "negative": ["13", 1], "latent_image": ["4", 0],
            "seed": seed, "steps": cfg.get("steps", 28), "cfg": cfg.get("cfg", 5.0),
            "sampler_name": cfg.get("sampler", "euler_ancestral"), "scheduler": cfg.get("scheduler", "normal"),
            "denoise": 1.0}},
        "6": {"class_type": "VAEDecode", "inputs": {"samples": ["5", 0], "vae": ["1", 2]}},
        "7": {"class_type": "SaveImage", "inputs": {"images": ["6", 0], "filename_prefix": f"redmet_sky_{uuid.uuid4().hex[:6]}"}},
    }
    return background.run_workflow(cfg, graph, out_path)


def main():
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    args = sys.argv[1:]
    what = args[0] if args else "all"
    scenes = args[1:]
    RAW.mkdir(parents=True, exist_ok=True)

    if what in ("all", "neutral"):
        neutral()

    if what == "neutral":
        return 0

    cfg = config()

    if what in ("all", "sky"):
        for scene in scenes or SKIES:
            sky(cfg, scene)

    if what in ("all", "vn"):
        for scene in scenes or VN:
            vn(cfg, scene)

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
