#!/usr/bin/env bash
# Phase C : le Flic corrompu (ennemi 03, docs/BESTIARY.md et docs/ENEMIES.md).
# 4 variantes de morphologie, d'âge et de tenue ; l'auteur en choisit une pour TripoSG.
# Uniforme fictif : aucun insigne, logo, écusson ni texte (le texte générique « POLICE » autorisé par le
# bestiaire n'est pas demandé, Animagine écrit du faux texte). La corruption tient à l'attitude et à la tenue
# négligée, sans caricature des forces de l'ordre.
# Sans arme : pistolet et matraque seront modélisés à part dans Blender (comme l'épée de Mara).
# Pose en T pour la génération 3D. Visages entièrement générés, aucune personne réelle en référence.
set -euo pipefail
cd "$(dirname "$0")/../../.."
PY="${CONCEPT_PYTHON:-C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe}"

POSE="t-pose, spread arms, arms outstretched, straight arms, standing, full body, front view, facing viewer, looking at viewer"
STYLE="anime coloring, cel shading, clean lineart, simple background, plain grey background"
BODY="1boy, solo, male focus, adult man, police officer, dark navy blue police uniform, black bulletproof vest, duty belt with pouches, black boots, cold expression, arrogant look, realistic body proportions, long legs"
NEG="weapon, gun, pistol, rifle, baton, holding, badge, police badge, patch, emblem, logo, text, letters, words, print, name tag, insignia, flag, military, camouflage, helmet, riot shield, supernatural, glowing eyes, caricature, exaggerated features, 2boys, multiple boys, character sheet, turnaround, reference sheet, multiple views, child, kid, female, nsfw, nude, extra arms, extra hands, extra fingers, missing fingers, bad hands, bad anatomy, short legs, stubby legs, big head, chibi, drop shadow, floor shadow, shadow, watermark, signature, username, lowres, cropped, out of frame, busy background, worst quality, low quality, low score, bad score, average score, error, blurry"

gen() { # seed, morphologie et visage, tenue
  "$PY" tools/gen3d/concept.py --character Flic --seeds "$1" --prompt "$BODY, $2, $3, $POSE, $STYLE, safe" --negative "$NEG"
}

gen 13101 "middle-aged, 45 years old, heavyset, thick neck, short grey hair, thick mustache" "peaked police cap, long sleeve uniform shirt, vest strapped tight"
gen 13102 "35 years old, tall and lean, slicked back black hair, sharp jaw, narrow eyes" "uniform jacket zipped up, black gloves, vest over jacket, no hat"
gen 13103 "40 years old, bulky muscular build, buzz cut, square jaw, scar on cheek" "short sleeve uniform shirt, muscular arms, vest, no hat"
gen 13104 "50 years old, sloppy, receding hairline, stubble, tired bags under eyes, slight belly" "rolled up sleeves, open collar, loosened tie, vest unstrapped hanging open, no hat"
