#!/usr/bin/env bash
# Nathan R1 et R2 : stades intermédiaires entre R0 (le garçon invisible) et R4 (corps brisé).
# img2img depuis le concept R0 validé, pour garder le même visage, la même pose en T et le même cadrage.
# Plus le denoise est fort, plus le corps change : R1 (0,65) reste proche de R0, R2 (0,75) s'en éloigne.
#
#   bash tools/gen3d/batches/2026-10-25_concepts_r1_r2.sh [r1|r2|all]
set -euo pipefail
cd "$(dirname "$0")/../../.."
PY="${CONCEPT_PYTHON:-C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe}"
R0="Assets/_Game/Art/Characters/Nathan_R0/Concept/Nathan_R0_concept_legs_d05_s4303.png"

POSE="t-pose, spread arms, arms outstretched, straight arms, standing, full body, front view, facing viewer, looking at viewer"
STYLE="anime coloring, cel shading, clean lineart, simple background, plain grey background"
SAME_BOY="1boy, solo, male focus, adult, young adult man, 19 years old, tall, long legs, defined jawline, visible neck, brown hair, curly hair, messy hair, thick glasses, black-framed glasses, bifocals, brown eyes, open eyes"
# Malade et épuisé, jamais mort-vivant
NEG_UNDEAD="zombie, undead, corpse, grey skin, green skin, wounds, open wounds, blood, gore, empty eyes, blank eyes, white pupils, no pupils, glowing eyes, skeleton, visible ribs, horror, smile, grin, teeth"
NEG_BASE="2boys, multiple boys, character sheet, turnaround, reference sheet, multiple views, child, kid, shota, young boy, baby face, aged down, female, nsfw, nude, short legs, stubby legs, big head, chibi, drop shadow, floor shadow, shadow, logo, text, letters, emblem, watermark, signature, username, lowres, bad anatomy, bad hands, extra fingers, missing fingers, extra limbs, extra arms, cropped, out of frame, busy background, worst quality, low quality, low score, bad score, average score, error, blurry"

r1() {
  P="$SAME_BOY, slightly chubby, slimmer face, thinner cheeks, light stubble, acne, pimples, acne on cheeks, acne on forehead, dark circles under eyes, tired, nervous, anxious, dilated pupils, sweat, sweating forehead, tense, raised shoulders, clenched hands, plain black hoodie, oversized hoodie, loose hoodie, grey cargo pants, sneakers, $POSE, $STYLE, safe"
  N="obese, muscular, healthy glow, $NEG_UNDEAD, $NEG_BASE"
  "$PY" tools/gen3d/concept.py --character Nathan_R1 --seeds 9101 9102 9103 9104 --init-image "$R0" --denoise 0.65 --size 832 1216 --prompt "$P" --negative "$N"
}

r2() {
  P="$SAME_BOY, thin, skinny, gaunt face, hollow cheeks, visible cheekbones, thin neck, deep dark circles under eyes, eye bags, pale skin, dull skin, dry lips, chapped lips, stubble, unshaven, greasy hair, crooked glasses, scratched glasses, cracked lens, tired eyes, cold, shivering, hunched shoulders, red scratch marks on forearms, red marks on neck, sleeves pushed up, worn dark grey hoodie, oversized hoodie, faded t-shirt, worn black jeans, old sneakers, $POSE, $STYLE, safe"
  N="chubby, fat, belly, muscular, torn jeans, ripped jeans, black hoodie, $NEG_UNDEAD, $NEG_BASE"
  "$PY" tools/gen3d/concept.py --character Nathan_R2 --seeds 9201 9202 9203 9204 --init-image "$R0" --denoise 0.75 --size 832 1216 --prompt "$P" --negative "$N"
}

# R2, 2e passe : à denoise 0,75, le cargo beige de R0 restait. L'inpainting du pantalon (masque
# rectangulaire) a donné des raccords visibles et des mains en trop : abandonné. On monte le denoise
# et on pondère les vêtements.
r2b() {
  P="$SAME_BOY, thin, skinny, gaunt face, hollow cheeks, visible cheekbones, thin neck, deep dark circles under eyes, eye bags, pale skin, dull skin, dry lips, chapped lips, stubble, unshaven, greasy hair, crooked glasses, scratched glasses, cracked lens, tired eyes, cold, shivering, hunched shoulders, red scratch marks on forearms, red marks on neck, sleeves pushed up, (worn dark grey hoodie:1.2), oversized hoodie, faded t-shirt, (worn black jeans:1.4), straight leg jeans, old sneakers, $POSE, $STYLE, safe"
  N="(cargo pants:1.3), beige pants, khaki pants, extra hands, extra arms, hands on hips, skinny legs, chubby, fat, belly, muscular, torn jeans, ripped jeans, $NEG_UNDEAD, $NEG_BASE"
  "$PY" tools/gen3d/concept.py --character Nathan_R2 --name Nathan_R2_concept_b --seeds 9211 9212 9213 9214 --init-image "$R0" --denoise 0.8 --size 832 1216 --prompt "$P" --negative "$N"
}

# R2, 3e passe : r2b a retiré le pantalon (jambes nues, short). On part d'un R0 au pantalon noirci
# par r2_darken_pants.py (aucun modèle : pixels beiges ou plus sombres que le fond, dans la zone des
# jambes), puis img2img au réglage du 1er lot R2 (denoise 0,75).
r2c() {
  local init="Assets/_Game/Art/Characters/Nathan_R2/Concept/Nathan_R2_init_R0_jean_noir.png"
  "$PY" tools/gen3d/batches/r2_darken_pants.py "$R0" "$init"
  P="$SAME_BOY, thin, skinny, gaunt face, hollow cheeks, visible cheekbones, thin neck, deep dark circles under eyes, eye bags, pale skin, dull skin, dry lips, chapped lips, stubble, unshaven, greasy hair, crooked glasses, scratched glasses, cracked lens, tired eyes, cold, shivering, hunched shoulders, red scratch marks on forearms, red marks on neck, sleeves pushed up, worn dark grey hoodie, oversized hoodie, faded t-shirt, (worn black jeans:1.2), straight leg jeans, old sneakers, $POSE, $STYLE, safe"
  N="cargo pants, beige pants, extra hands, extra arms, hands on hips, chubby, fat, belly, muscular, torn jeans, ripped jeans, shorts, bare legs, $NEG_UNDEAD, $NEG_BASE"
  "$PY" tools/gen3d/concept.py --character Nathan_R2 --name Nathan_R2_concept_c --seeds 9221 9222 9223 9224 --init-image "$init" --denoise 0.75 --size 832 1216 --prompt "$P" --negative "$N"
}

case "${1:-all}" in
  r1) r1 ;; r2) r2 ;; r2b) r2b ;; r2c) r2c ;; all) r1; r2 ;;
  *) echo "usage: $0 [r1|r2|r2b|r2c|all]"; exit 1 ;;
esac
