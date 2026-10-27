#!/usr/bin/env bash
# Phase C : le Dealer, lieutenant de rue du Chimiste (ennemi 02), 2e série.
# La 1re série (14101 à 14104) a été rejetée par l'auteur : « trop gentils ».
# Demande de l'auteur : dealers africains à la peau noire foncée, cicatrices, tatouages, dent en or,
# chaîne en or, allure de caïd de cité, menaçant. Traits du visage réalistes, sans caricature physique.
# Tatouages sans texte (Animagine écrit du faux texte). Sans arme : le fusil à pompe sera modélisé
# dans Blender (nouvelle mécanique, docs/REPRISE.md). Pose en T pour la génération 3D.
# Visages entièrement générés, aucune personne réelle en référence. Taille prévue : 1,80 m.
set -euo pipefail
cd "$(dirname "$0")/../../.."
PY="${CONCEPT_PYTHON:-C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe}"

POSE="t-pose, spread arms, arms outstretched, straight arms, standing, full body, front view, facing viewer, looking at viewer"
STYLE="anime coloring, cel shading, clean lineart, simple background, plain grey background"
BODY="1boy, solo, male focus, adult man, african man, dark skin, very dark skin, black man, drug dealer, street gangster, menacing, intimidating, cold stare, sneer, showing teeth, gold tooth, scars, scar on face, tattoos, neck tattoo, arm tattoos, gold chain necklace, realistic body proportions, long legs"
NEG="weapon, gun, pistol, shotgun, knife, holding, money, cigarette, logo, brand logo, text, letters, words, print, emblem, badge, name tag, police, military, armor, helmet, supernatural, glowing eyes, caricature, exaggerated features, smile, friendly, light skin, pale skin, 2boys, multiple boys, character sheet, turnaround, reference sheet, multiple views, child, kid, female, nsfw, nude, extra arms, extra hands, extra fingers, missing fingers, bad hands, bad anatomy, short legs, stubby legs, big head, chibi, drop shadow, floor shadow, shadow, watermark, signature, username, lowres, cropped, out of frame, busy background, worst quality, low quality, low score, bad score, average score, error, blurry"

gen() { # seed, âge, morphologie et visage, tenue
  "$PY" tools/gen3d/concept.py --character Dealer --seeds "$1" --prompt "$BODY, $2, $3, $POSE, $STYLE, safe" --negative "$NEG"
}

gen 14201 "30 years old, lean muscular build, short twists hairstyle, scar across the eyebrow" "black tracksuit jacket open over a white tank top, thick gold chain, black jogging pants, white sneakers"
gen 14202 "35 years old, heavyset, broad shoulders, shaved head, long scar on the cheek, tattoo sleeves" "sleeveless black puffer vest over bare arms, thick gold chain, baggy dark jeans, tan work boots"
gen 14203 "25 years old, tall and wiry, cornrows, scar on the lip, hand tattoos" "grey hoodie under an open black leather jacket, several gold chains, black jogging pants, black sneakers"
gen 14204 "40 years old, muscular, bald head with scars on the scalp, short beard, several gold teeth" "open dark shirt showing chest tattoos, long black coat, gold chains, black jeans, black boots"
