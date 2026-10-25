#!/usr/bin/env bash
# Le Chimiste (dealer de Red Met, personnage narratif non combattant). Visage entièrement généré :
# aucune photo ni personne réelle en référence. Description : docs/CHARACTERS.md.
set -euo pipefail
cd "$(dirname "$0")/../../.."
PY="${CONCEPT_PYTHON:-C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe}"

POSE="t-pose, spread arms, arms outstretched, straight arms, standing, full body, front view, facing viewer, looking at viewer"
STYLE="anime coloring, cel shading, clean lineart, simple background, plain grey background"
BODY="1boy, solo, male focus, adult, mature male, middle-aged man, 48 years old, dark-skinned male, black man, west african man, tall, long legs, mature face, light wrinkles, calm expression, calculating eyes, slight smile, gold tooth, short beard, well-groomed beard, heavy gold chain necklace, gold ring, wristwatch, dress pants, black leather shoes, elegant, wealthy, composed"
NEG="caricature, exaggerated features, cartoonish lips, mugshot, photo, photorealistic, realistic, street thug, gang, hoodie, tank top, sunglasses, tattoo, weapon, gun, 2boys, multiple boys, character sheet, turnaround, reference sheet, multiple views, child, kid, female, nsfw, nude, extra arms, extra hands, extra fingers, missing fingers, bad hands, bad anatomy, short legs, stubby legs, big head, chibi, drop shadow, floor shadow, shadow, logo, text, watermark, signature, username, lowres, cropped, out of frame, busy background, worst quality, low quality, low score, bad score, average score, error, blurry"

gen() { # seed, coiffure, chemise
  "$PY" tools/gen3d/concept.py --character Chimiste --seeds "$1" --prompt "$BODY, $2, $3, open collar, $POSE, $STYLE, safe" --negative "$NEG"
}

gen 10101 "bald, shaved head" "black dress shirt"
gen 10102 "very short hair, buzz cut, black hair" "burgundy dress shirt"
gen 10103 "short dreadlocks, tied hair, dreadlocks ponytail" "black dress shirt"
gen 10104 "very short hair, grey hair at temples" "burgundy dress shirt"
