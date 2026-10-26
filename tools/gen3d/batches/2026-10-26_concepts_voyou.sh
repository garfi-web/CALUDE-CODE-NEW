#!/usr/bin/env bash
# Tranche verticale : le Voyou classique (ennemi 01, docs/BESTIARY.md et docs/ENEMIES.md).
# 4 variantes de morphologie, de coiffure et de tenue ; l'auteur en choisit une pour TripoSG.
# Sans arme : couteau et barre seront modélisés à part dans Blender (comme l'épée de Mara).
# Pose en T pour la génération 3D. Visages entièrement générés, aucune personne réelle en référence.
set -euo pipefail
cd "$(dirname "$0")/../../.."
PY="${CONCEPT_PYTHON:-C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe}"

POSE="t-pose, spread arms, arms outstretched, straight arms, standing, full body, front view, facing viewer, looking at viewer"
STYLE="anime coloring, cel shading, clean lineart, simple background, plain grey background"
BODY="1boy, solo, male focus, adult, young adult man, 25 years old, street thug, delinquent, hostile expression, scowl, realistic body proportions, long legs, sneakers, worn clothes, urban streetwear"
NEG="weapon, knife, gun, bat, holding, military, armor, helmet, police, badge, logo, text, letters, print, emblem, supernatural, horns, glowing eyes, caricature, exaggerated features, 2boys, multiple boys, character sheet, turnaround, reference sheet, multiple views, child, kid, female, nsfw, nude, extra arms, extra hands, extra fingers, missing fingers, bad hands, bad anatomy, short legs, stubby legs, big head, chibi, drop shadow, floor shadow, shadow, watermark, signature, username, lowres, cropped, out of frame, busy background, worst quality, low quality, low score, bad score, average score, error, blurry"

gen() { # seed, morphologie et coiffure, tenue
  "$PY" tools/gen3d/concept.py --character Voyou --seeds "$1" --prompt "$BODY, $2, $3, $POSE, $STYLE, safe" --negative "$NEG"
}

gen 11101 "lean build, buzz cut, short black hair, stubble, scar on eyebrow" "black leather jacket, grey hoodie under jacket, black tracksuit pants, white sneakers"
gen 11102 "stocky build, broad shoulders, heavyset, shaved head, short beard" "dark puffer jacket, black hoodie, worn blue jeans, dirty sneakers"
gen 11103 "wiry build, thin, hood up, hoodie hood over head, sunken cheeks" "dark navy hoodie, hood up, grey tracksuit pants, black sneakers"
gen 11104 "athletic build, short curly hair, faded sides, thin mustache" "brown leather bomber jacket, black t-shirt, ripped dark jeans, high-top sneakers"
