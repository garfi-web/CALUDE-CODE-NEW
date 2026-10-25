#!/usr/bin/env python3
"""Génère un modèle 3D de personnage (.glb) à partir d'une image ou d'un texte.

Sortie : Assets/_Game/Art/Characters/<Personnage>/Source/<Personnage>_<backend>_s<seed>.glb,
avec un .json à côté qui note l'entrée, la seed et les réglages.

Le script n'utilise que la bibliothèque standard : chaque générateur (« backend ») tourne
dans son propre environnement virtuel, décrit dans backends.json. Pour changer de générateur
(TRELLIS, etc.), il suffit d'ajouter une entrée et un script runner dans backends/.

Exemples :
  python tools/gen3d/generate.py --character Nathan_R0 --image concept.png --seed 42
  python tools/gen3d/generate.py --character Nathan_R0 --prompt "1boy, ..." --seed 42
"""
import argparse
import datetime
import hashlib
import json
import os
import subprocess
import sys
import time
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parent
REPO_ROOT = TOOLS_DIR.parents[1]
CHARACTERS_DIR = REPO_ROOT / "Assets" / "_Game" / "Art" / "Characters"


def load_backends():
    with open(TOOLS_DIR / "backends.json", encoding="utf-8") as f:
        return json.load(f)


def sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def relative(path):
    try:
        return Path(path).resolve().relative_to(REPO_ROOT).as_posix()
    except ValueError:
        return str(path)


def unique_path(path):
    # N'écrase jamais un essai précédent : ajoute _2, _3...
    if not path.exists():
        return path
    for i in range(2, 1000):
        candidate = path.with_name(f"{path.stem}_{i}{path.suffix}")
        if not candidate.exists():
            return candidate
    raise RuntimeError(f"Trop de fichiers existants pour {path}")


def main():
    # La console Windows est en cp1252 : sans cela, accents et flèches font planter print()
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--character", required=True, help="Dossier du personnage, ex. Nathan_M0")
    source = parser.add_mutually_exclusive_group(required=True)
    source.add_argument("--image", help="Image de concept (PNG, idéalement fond uni ou transparent)")
    source.add_argument("--prompt", help="Texte : passe d'abord par le générateur d'images de concept")
    parser.add_argument("--negative", default=None, help="Prompt négatif pour l'image de concept")
    parser.add_argument("--seed", type=int, default=42, help="Seed du générateur 3D")
    parser.add_argument("--concept-seed", type=int, default=None, help="Seed de l'image de concept (défaut : --seed)")
    parser.add_argument("--backend", default=None, help="Générateur 3D (défaut : celui de backends.json)")
    parser.add_argument("--faces", type=int, default=40000, help="Nombre de faces maximum, -1 pour ne pas réduire")
    parser.add_argument("--steps", type=int, default=50)
    parser.add_argument("--guidance", type=float, default=7.0)
    parser.add_argument("--note", default="", help="Remarque libre, recopiée dans le .json")
    args = parser.parse_args()

    backends = load_backends()
    backend_name = args.backend or backends["default_3d"]
    backend = backends["3d"][backend_name]

    out_dir = CHARACTERS_DIR / args.character / "Source"
    out_dir.mkdir(parents=True, exist_ok=True)
    glb_path = unique_path(out_dir / f"{args.character}_{backend_name}_s{args.seed}.glb")

    record = {
        "character": args.character,
        "created": datetime.datetime.now().isoformat(timespec="seconds"),
        "backend": backend_name,
        "seed": args.seed,
        "params": {"faces": args.faces, "steps": args.steps, "guidance": args.guidance},
        "note": args.note,
    }

    if args.prompt:
        # Le générateur 3D n'accepte qu'une image : on produit d'abord le concept
        sys.path.insert(0, str(TOOLS_DIR))
        import concept

        concept_seed = args.concept_seed if args.concept_seed is not None else args.seed
        image_path, concept_info = concept.generate(
            backends["concept"][backends["default_concept"]],
            prompt=args.prompt,
            negative=args.negative,
            seed=concept_seed,
            out_dir=CHARACTERS_DIR / args.character / "Concept",
            name=f"{args.character}_concept_s{concept_seed}",
        )
        record["input"] = {"type": "text", "prompt": args.prompt, "concept": concept_info}
    else:
        image_path = Path(args.image).resolve()
        record["input"] = {"type": "image"}

    record["input"]["image"] = relative(image_path)
    record["input"]["image_sha256"] = sha256(image_path)

    command = [
        backend["python"], str(TOOLS_DIR / backend["runner"]),
        "--repo", backend["repo"],
        "--image", str(image_path),
        "--out", str(glb_path),
        "--seed", str(args.seed),
        "--faces", str(args.faces),
        "--steps", str(args.steps),
        "--guidance", str(args.guidance),
    ]
    print(f"[gen3d] {backend_name} → {relative(glb_path)}", flush=True)
    start = time.time()
    # Sous Windows, la sortie d'un sous-processus Python est en cp1252 sans cette variable
    env = {**os.environ, "PYTHONIOENCODING": "utf-8"}
    proc = subprocess.run(command, cwd=backend["repo"], env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
    record["duration_s"] = round(time.time() - start, 1)
    sys.stdout.write(proc.stdout)
    sys.stderr.write(proc.stderr)

    if proc.returncode != 0:
        print(f"[gen3d] Échec du backend (code {proc.returncode})", file=sys.stderr)
        return proc.returncode

    # Le runner écrit sa dernière ligne au format JSON : statistiques du maillage, VRAM, etc.
    result_lines = [line for line in proc.stdout.splitlines() if line.startswith("[result] ")]
    if result_lines:
        record["result"] = json.loads(result_lines[-1][len("[result] "):])
        if "preprocessed_image" in record["result"]:
            record["result"]["preprocessed_image"] = relative(record["result"]["preprocessed_image"])
    record["output"] = relative(glb_path)

    json_path = glb_path.with_suffix(".json")
    with open(json_path, "w", encoding="utf-8") as f:
        json.dump(record, f, ensure_ascii=False, indent=2)
    print(f"[gen3d] OK en {record['duration_s']} s : {relative(glb_path)} + {json_path.name}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
