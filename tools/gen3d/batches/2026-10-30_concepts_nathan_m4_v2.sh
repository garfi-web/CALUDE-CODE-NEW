#!/usr/bin/env bash
# Nathan M4 refait (demande de l'auteur du 2026-10-30) : beaucoup plus massif et bestial que M2, torse, dos, épaules et
# bras nus (plus de peau de bête), peau de pierre fissurée, veines rouges incandescentes, yeux rouges, cheveux et barbe
# sauvages, air de famille avec Nathan (mâchoire carrée, cheveux bruns bouclés). Planche de référence face + dos dans
# une seule image, pour que les deux vues montrent le même personnage. Génération à partir du texte seul : aucun
# concept validé n'est repris (règle de l'auteur).
cd "$(dirname "$0")/../../.."
"C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/concept.py --character Nathan_M4 --name Nathan_M4_v2_sheet \
  --size 1536 1024 --seeds 7701 7702 7703 7704 7705 7706 7707 7708 7709 \
  --prompt "1boy, solo, male focus, adult, mature male, giant, hulking, gigantic muscles, extremely muscular, (massive trapezius:1.3), (huge shoulders:1.3), (thick neck:1.2), wide back, v-taper, (huge arms:1.2), huge forearms, (huge hands:1.1), hunched posture, predatory stance, head between shoulders, square jaw, topless male, bare back, bare shoulders, bare arms, bare chest, detailed muscles, bulging veins, grey skin, (stone skin:1.2), (cracked skin:1.2), (glowing red veins:1.3), glowing red cracks, (glowing red eyes:1.2), wild hair, long messy curly dark brown hair, thick wild beard, leather belt, leather straps, bracers, torn pants, ripped trousers, boots, standing, full body, (reference sheet:1.2), multiple views, front view, back view, from behind, anime coloring, cel shading, clean lineart, simple background, plain grey background, safe, masterpiece, high score, great score, absurdres" \
  --negative "fur, fur cape, pelt, animal skin, cape, cloak, shoulder armor, pauldrons, chest armor, shirt, pink skin, tan skin, female, child, shota, aged down, nsfw, nude, genitals, text, logo, watermark, signature, lowres, bad anatomy, bad hands, extra fingers, missing fingers, fused fingers, extra limbs, extra arms, third arm, missing arm, cropped, out of frame, chibi, big head, worst quality, low quality, low score, bad score, average score, error, blurry"
