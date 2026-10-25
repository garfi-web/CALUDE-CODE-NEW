#!/usr/bin/env bash
# Série 2 de concepts : Nathan R3, Nathan M2 et M4, Mara M-T0 à M-T4.
# Animagine XL 4.0 dans ComfyUI (qui doit tourner sur 127.0.0.1:8188).
# Les images de docs/references ne servent que de références de style : aucune n'est passée en entrée.
#
#   bash tools/gen3d/batches/2026-10-25_concepts_serie2.sh [r3|m2|m4|mara|all]
set -euo pipefail
cd "$(dirname "$0")/../../.."
PY="${CONCEPT_PYTHON:-C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe}"

# Blocs communs
POSE="t-pose, spread arms, arms outstretched, straight arms, standing, full body, front view, facing viewer, looking at viewer"
STYLE="anime coloring, cel shading, clean lineart, simple background, plain grey background"
NEG_BASE="drop shadow, floor shadow, shadow, logo, text, letters, emblem, watermark, signature, username, lowres, bad anatomy, bad hands, extra fingers, missing fingers, extra limbs, extra arms, cropped, out of frame, multiple views, busy background, short legs, stubby legs, big head, chibi, worst quality, low quality, low score, bad score, average score, error, blurry"
NEG_MALE="child, kid, shota, young boy, baby face, aged down, female, nsfw, nude, genitals"
NEG_MARA="nsfw, nude, nipples, areolae, pussy, explicit, child, loli, young, petite, flat chest, aged down, baby face, round face, male"

run() { # personnage, seeds..., puis --prompt/--negative via variables P et N
  local character="$1"; shift
  "$PY" tools/gen3d/concept.py --character "$character" --seeds "$@" --prompt "$P" --negative "$N"
}

r3() {
  P="1boy, solo, male focus, adult, young adult man, 19 years old, tall, long legs, emaciated, skinny, gaunt, skin and bones, hollow cheeks, sunken cheeks, prominent collarbones, bony wrists, dark circles under eyes, eye bags, tired eyes, grey skin, pale skin, sickly, unhealthy, chapped lips, unkempt stubble, bad teeth, brown hair, curly hair, messy hair, greasy hair, thick glasses, black-framed glasses, bifocals, cracked glasses, crooked glasses, open eyes, brown eyes, red marks on forearms, red marks on neck, dark grey hoodie, oversized hoodie, stained clothes, dirty clothes, sleeves pushed up, worn t-shirt, black jeans, torn jeans, ripped jeans, knee holes, old leather boots, slight slouch, hunched shoulders, sad, exhausted, $POSE, $STYLE, safe"
  N="muscular, healthy, fit, handsome, chubby, fat, clean clothes, $NEG_MALE, $NEG_BASE"
  run Nathan_R3 6101 6202 6303 6404
}

M2_BODY="1boy, solo, male focus, adult, mature male, barbarian, giant, tall, long legs, huge muscles, muscular male, massive shoulders, broad shoulders, defined abs, pectorals, dominant, angry, furious, glaring, brown eyes, long hair, reddish brown hair, braided hair, partially braided hair, thick beard, full beard, scars, facial scar, scar on chest, fur cape, fur pauldrons, leather straps, leather belt, metal belt buckle, medallion, studded bracers, fur pants, fur boots, topless male, bare chest"

m2() {
  P="$M2_BODY, $POSE, $STYLE, safe"
  N="armor on torso, shirt, $NEG_MALE, $NEG_BASE"
  run Nathan_M2 7101 7202 7303 7404
}

m4() {
  P="$M2_BODY, grey skin, stone skin, cracked skin, glowing cracks, glowing red veins, red veins, glowing red eyes, red eyes, $POSE, $STYLE, safe"
  N="pink skin, tan skin, $NEG_MALE, $NEG_BASE"
  run Nathan_M4 7501 7602
}

MARA_BODY="1girl, solo, adult, mature female, 28 years old, mature face, cold expression, confident, serious, tall, long legs, voluptuous, large breasts, narrow waist, wide hips, thick thighs, very long hair, black hair, red streaks, streaked hair, multicolored hair, blunt bangs, red eyes, holding sword, glowing red energy sword, energy blade"

mara() {
  N="$NEG_MARA, $NEG_BASE"

  P="$MARA_BODY, black bodysuit, latex bodysuit, shiny clothes, high collar, turtleneck, red straps, crossed straps, corset, buckles, long skirt, side slit, thigh boots, fully covered, $POSE, $STYLE, safe"
  "$PY" tools/gen3d/concept.py --character Mara --name Mara_T0_concept --seeds 8001 8002 --prompt "$P" --negative "$N"

  P="$MARA_BODY, black bodysuit, latex bodysuit, shiny clothes, plunging neckline, v-neck, cleavage, red straps, corset, long skirt, high slit, side slit up to hip, thigh boots, $POSE, $STYLE, sensitive"
  "$PY" tools/gen3d/concept.py --character Mara --name Mara_T1_concept --seeds 8101 8102 --prompt "$P" --negative "$N"

  P="$MARA_BODY, black bustier, strapless, bare shoulders, midriff, navel, short skirt, side slit, garter belt, garter straps, thigh boots, red straps, $POSE, $STYLE, sensitive"
  "$PY" tools/gen3d/concept.py --character Mara --name Mara_T2_concept --seeds 8201 8202 --prompt "$P" --negative "$N"

  T3_OUTFIT="revealing armor, minimal armor, black armor, red armor pieces, red straps, high-cut top, deep neckline, thong, half skirt, side skirt, thigh boots"
  P="$MARA_BODY, $T3_OUTFIT, $POSE, $STYLE, sensitive"
  "$PY" tools/gen3d/concept.py --character Mara --name Mara_T3_concept --seeds 8301 8302 --prompt "$P" --negative "$N"

  # Vue de dos de M-T3 : même tenue, pose en T vue de derrière
  P="$MARA_BODY, $T3_OUTFIT, from behind, back view, facing away, ass, t-pose, spread arms, arms outstretched, straight arms, standing, full body, $STYLE, sensitive"
  "$PY" tools/gen3d/concept.py --character Mara --name Mara_T3_back_concept --seeds 8311 8312 --prompt "$P" --negative "$N, front view, facing viewer"

  P="$MARA_BODY, black armor, cracked armor, glowing red cracks, red veins on armor, spiked armor, armored dress, aggressive, intimidating, provocative, cleavage, thigh boots, dark aura, $POSE, $STYLE, sensitive"
  "$PY" tools/gen3d/concept.py --character Mara --name Mara_T4_concept --seeds 8401 8402 --prompt "$P" --negative "$N"
}

# Reprises après le premier passage : plusieurs personnages sur une image (6101, 6404, 8002, 8312),
# épée tenue à l'horizontale au lieu de la pose en T (8311), peau pas assez grise pour M4.
NEG_SHEET="2boys, 2girls, multiple boys, multiple girls, character sheet, turnaround, reference sheet, multiple views"

retakes() {
  P="1boy, solo, male focus, adult, young adult man, 19 years old, tall, long legs, emaciated, skinny, gaunt, skin and bones, hollow cheeks, sunken cheeks, prominent collarbones, bony wrists, dark circles under eyes, eye bags, tired eyes, grey skin, pale skin, sickly, unhealthy, chapped lips, unkempt stubble, bad teeth, brown hair, curly hair, messy hair, greasy hair, thick glasses, black-framed glasses, bifocals, cracked glasses, crooked glasses, open eyes, brown eyes, red marks on forearms, red marks on neck, dark grey hoodie, oversized hoodie, stained clothes, dirty clothes, sleeves pushed up, worn t-shirt, black jeans, torn jeans, ripped jeans, knee holes, old leather boots, slight slouch, hunched shoulders, sad, exhausted, $POSE, $STYLE, safe"
  N="$NEG_SHEET, smile, grin, muscular, healthy, fit, handsome, chubby, fat, clean clothes, dirt on ground, $NEG_MALE, $NEG_BASE"
  run Nathan_R3 6505 6606

  P="$M2_BODY, (grey skin:1.4), (stone skin:1.3), cracked skin, glowing cracks, (glowing red veins:1.2), red veins, glowing red eyes, red eyes, $POSE, $STYLE, safe"
  N="$NEG_SHEET, pink skin, tan skin, peach skin, $NEG_MALE, $NEG_BASE"
  "$PY" tools/gen3d/concept.py --character Nathan_M4 --name Nathan_M4_concept_grey --seeds 7501 7602 --prompt "$P" --negative "$N"

  N="$NEG_SHEET, $NEG_MARA, $NEG_BASE"
  P="$MARA_BODY, black bodysuit, latex bodysuit, shiny clothes, high collar, turtleneck, red straps, crossed straps, corset, buckles, long skirt, side slit, thigh boots, fully covered, $POSE, $STYLE, safe"
  "$PY" tools/gen3d/concept.py --character Mara --name Mara_T0_concept --seeds 8003 --prompt "$P" --negative "$N"

  # Dos : l'épée tenue pointe en bas, bras tendus à l'horizontale
  P="$MARA_BODY, revealing armor, minimal armor, black armor, red armor pieces, red straps, high-cut top, thong, half skirt, side skirt, thigh boots, sword pointing down, from behind, back view, facing away, ass, t-pose, spread arms, arms outstretched, straight arms, standing, full body, $STYLE, sensitive"
  "$PY" tools/gen3d/concept.py --character Mara --name Mara_T3_back_concept --seeds 8313 8314 --prompt "$P" --negative "$N, front view, facing viewer, looking at viewer, sword across body"
}

case "${1:-all}" in
  r3) r3 ;; m2) m2 ;; m4) m4 ;; mara) mara ;; retakes) retakes ;;
  all) r3; m2; m4; mara ;;
  *) echo "usage: $0 [r3|m2|m4|mara|retakes|all]"; exit 1 ;;
esac
