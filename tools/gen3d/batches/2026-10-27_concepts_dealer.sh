#!/usr/bin/env bash
# Phase C : le Dealer, lieutenant de rue du Chimiste (ennemi 02, docs/BESTIARY.md et docs/ENEMIES.md).
# 4 variantes d'âge, de morphologie et de tenue ostentatoire ; l'auteur en choisit une pour TripoSG.
# Tenue voyante, bijoux, lunettes teintées, assurance tranquille. Aucune marque, aucun logo ni texte
# (Animagine écrit du faux texte). Ni origine ni couleur de peau imposées : on évite le cliché du dealer.
# Sans arme : le pistolet sera celui du Flic, modélisé dans Blender (redmet_props.build_pistol).
# Pose en T pour la génération 3D. Visages entièrement générés, aucune personne réelle en référence.
# Taille prévue : 1,80 m (docs/ENEMIES.md, « Tailles »).
set -euo pipefail
cd "$(dirname "$0")/../../.."
PY="${CONCEPT_PYTHON:-C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe}"

POSE="t-pose, spread arms, arms outstretched, straight arms, standing, full body, front view, facing viewer, looking at viewer"
STYLE="anime coloring, cel shading, clean lineart, simple background, plain grey background"
BODY="1boy, solo, male focus, adult man, drug dealer, street boss, flashy expensive clothes, gold jewelry, tinted sunglasses, confident smirk, calm arrogant expression, realistic body proportions, long legs"
NEG="weapon, gun, pistol, knife, holding, money, cigarette, logo, brand logo, text, letters, words, print, emblem, badge, name tag, police, military, armor, helmet, supernatural, glowing eyes, caricature, exaggerated features, 2boys, multiple boys, character sheet, turnaround, reference sheet, multiple views, child, kid, female, nsfw, nude, extra arms, extra hands, extra fingers, missing fingers, bad hands, bad anatomy, short legs, stubby legs, big head, chibi, drop shadow, floor shadow, shadow, watermark, signature, username, lowres, cropped, out of frame, busy background, worst quality, low quality, low score, bad score, average score, error, blurry"

gen() { # seed, morphologie et visage, tenue
  "$PY" tools/gen3d/concept.py --character Dealer --seeds "$1" --prompt "$BODY, $2, $3, $POSE, $STYLE, safe" --negative "$NEG"
}

gen 14101 "32 years old, slim build, slicked back dark hair, thin goatee, round tinted sunglasses" "long burgundy leather coat open, black turtleneck, thick gold chain necklace, black slacks, pointed leather shoes"
gen 14102 "27 years old, athletic build, undercut with bleached tips, aviator sunglasses with yellow lenses" "white luxury tracksuit with gold trim, several thick gold chains, gold wristwatch, white sneakers"
gen 14103 "40 years old, heavyset, bald head, trimmed beard, rectangular tinted glasses, gold rings" "cream suit jacket open over a patterned silk shirt with open collar, gold chains, cream trousers, brown loafers"
gen 14104 "35 years old, lean build, long hair tied in a ponytail, sharp cheekbones, purple tinted sunglasses" "long black coat with fur collar over a purple velvet tracksuit, gold chain, gold rings, black sneakers"
