#!/usr/bin/env bash
# Modèles TripoSG des concepts retenus par l'auteur (Nathan M2, Mara M-T0 à M-T4).
# ComfyUI doit être arrêté avant (VRAM). Mara est générée sans épée : la lame sera modélisée à part.
set -euo pipefail
cd "$(dirname "$0")/../../.."
PY="${TRIPOSG_PYTHON:-C:/RED MET/ai/TripoSG/.venv/Scripts/python.exe}"
A="Assets/_Game/Art/Characters"

gen() { "$PY" tools/gen3d/generate.py --seed 42 --faces 40000 "$@"; }

# M2 : ombre au sol retirée par inpainting, reste un léger halo clair sous les bottes -> tolérance 45
gen --character Nathan_M2 --image "$A/Nathan_M2/Concept/Nathan_M2_noshadow_s7202.png" --bg-tolerance 45 \
    --note "Concept 7202 retenu, ombre au sol retirée (inpainting)"

gen --character Mara --name Mara_T0 --image "$A/Mara/Concept/Mara_T0_clean_s8002.png" \
    --note "8002 retenu : Mara de gauche (la droite avait 3 bras), recadrée, pointe d'épée retirée"
gen --character Mara --name Mara_T1 --image "$A/Mara/Concept/Mara_T1_nosword_s8102.png" \
    --note "8102 retenu, deux épées retirées (inpainting, bras exclus du masque)"
gen --character Mara --name Mara_T2 --image "$A/Mara/Concept/Mara_T2_nosword_clean_s8201.png" \
    --note "8201 retenu, épée retirée en deux passes"
gen --character Mara --name Mara_T3 --image "$A/Mara/Concept/Mara_T3_front_from8312_s8312.png" \
    --note "8312 retenu (planche dos + face) : Mara de face isolée, recentrée, bras et mains complétés, lame retirée"
gen --character Mara --name Mara_T4 --image "$A/Mara/Concept/Mara_T4_nosword_s8402.png" \
    --note "8402 retenu, épée retirée (main exclue du masque)"
