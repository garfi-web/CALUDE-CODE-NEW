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

v1() {
  gen 10101 "bald, shaved head" "black dress shirt"
  gen 10102 "very short hair, buzz cut, black hair" "burgundy dress shirt"
  gen 10103 "short dreadlocks, tied hair, dreadlocks ponytail" "black dress shirt"
  gen 10104 "very short hair, grey hair at temples" "burgundy dress shirt"
}

# v2 (retour de l'auteur) : peau trop claire. Peau noire foncée et traits africains (Côte d'Ivoire),
# sans caricature ; couleur des chemises pondérée (le noir sortait en blanc).
SKIN="(very dark skin:1.4), black skin, dark-skinned male, west african facial features, ivorian man"
NEG_SKIN="light skin, pale skin, tan skin, light brown skin, white man, asian"
gen2() { # seed, coiffure, chemise
  "$PY" tools/gen3d/concept.py --character Chimiste --name Chimiste_concept_v2 --seeds "$1" \
    --prompt "$BODY, $SKIN, $2, $3, open collar, $POSE, $STYLE, safe" --negative "$NEG_SKIN, $NEG"
}
v2() {
  gen2 10101 "bald, shaved head" "(black dress shirt:1.3)"
  gen2 10102 "very short hair, buzz cut, black hair" "(burgundy dress shirt:1.3)"
  gen2 10103 "short dreadlocks, tied hair, dreadlocks ponytail" "(black dress shirt:1.3)"
  gen2 10104 "very short hair, grey hair at temples" "(burgundy dress shirt:1.3)"
}

case "${1:-v2}" in v1) v1 ;; v2) v2 ;; *) echo "usage: $0 [v1|v2]"; exit 1 ;; esac
