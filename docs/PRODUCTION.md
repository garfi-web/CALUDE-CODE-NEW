# RED MET — Journal de production

Choix, prompts, seeds, nombre d'essais et problèmes rencontrés, phase par phase.
Pipeline visé : génération 3D locale → Blender (nettoyage, squelette, animation, rendu toon) → spritesheets PNG → Unity.

## État d'avancement (au 2026-10-26, fin de la phase Combat dynamique)

**Phase en cours** : deux chantiers en parallèle.
- **Prototype jouable, phase 1 (modèles 3D)** : modèles bruts générés, nettoyage dans Blender pas encore commencé.
- **Bestiaire** : phases A et B terminées. **Phase Combat dynamique terminée** (voir sa section plus bas).
- **Prochaine étape demandée par l'auteur (2026-10-26) : une TRANCHE VERTICALE jouable aux graphismes définitifs**, avant la phase C : 4 concepts du Voyou classique (l'auteur en choisit un) puis son modèle TripoSG ; Nathan M0 et ce Voyou riggés et animés (idle, course, saut, combo, lourd, lanceur, dash, esquive, coup reçu, mort), rendu toon en spritesheets, intégrés dans Unity ; décor final de l'arène de la lune rouge (parallaxe, sol texturé, éclairage URP 2D, post-traitement, cendres) ; Play, 60 FPS, captures, mode d'emploi. L'auteur valide le style sur cette tranche, puis la phase C reprend avec la même méthode.

**Décisions prises**

| Sujet | Décision | Raison |
|---|---|---|
| Générateur 3D | **TripoSG** (VAST-AI, MIT), local, `C:\RED MET\ai\TripoSG` | Hunyuan3D 2.1 : licence qui exclut l'UE (et ses sorties) et VRAM insuffisante (8 Go pour 10 demandés). **Hunyuan3D n'est pas installé.** |
| Détourage | Sans modèle, dans `triposg_runner.py` | RMBG-1.4, appelé par le script d'origine de TripoSG, est non commercial : jamais téléchargé |
| Images de concept | ComfyUI (GPL-3.0) + **Animagine XL 4.0** (Open RAIL++-M, usage commercial permis) | Choix de l'auteur, licence vérifiée et validée avant téléchargement |
| Morfx 3D | **Abandonné** | Introuvable ; l'auteur a confirmé que ce n'était pas le bon outil |
| Squelettes | **Rigify** dans Blender, **Mixamo en secours** | Décision de l'auteur |
| Concepts validés | **Ne plus en dériver de nouvelles images** ; les corrections se font dans Blender | Chaque itération déformait le personnage (R1/R2 rejetés) |
| Épée de Mara | Modélisée à part dans Blender, Mara générée sans arme | Demande de l'auteur |

**Terminé**
- Phase 0 : TripoSG + ComfyUI installés, outils `tools/gen3d/` (concepts, inpainting, masques, recadrage, génération 3D), journal.
- Concepts validés et modèles TripoSG (seed 42, 40 000 faces), tous dans `Assets/_Game/Art/Characters/<Perso>/` :

| Personnage | Concept retenu | Modèle |
|---|---|---|
| Nathan R0 | `Nathan_R0_concept_legs_d05_s4303` | `Nathan_R0_triposg_s42.glb` |
| Nathan R2 | `Nathan_R2_init_R0_jean_noir` (R0 au pantalon teint) | `Nathan_R2_triposg_s42.glb` (corps de R0, à amincir) |
| Nathan M2 | `Nathan_M2_concept_s7202` (ombre retirée) | `Nathan_M2_triposg_s42.glb` |
| Nathan M4 | `Nathan_M4_concept_s7602` (peau couleur chair) | `Nathan_M4_triposg_s42.glb` |
| Mara M-T0 à M-T4 | 8002 (Mara de gauche), 8102, 8201, 8312 (face isolée), 8402, nettoyés sans épée | `Mara_T0`…`T3_triposg_s42.glb`, `Mara_T4_triposg_s7_legsfix.glb` |
| Épée de Mara | — (modélisée) | `Mara_EnergyBlade.glb` |
| Le Chimiste | `Chimiste_concept_v2_s10103` | `Chimiste_triposg_s42.glb` |

- Fichier Blender de travail : `ArtSource/Blender/REDMET_personnages.blend` (une collection par personnage, mises à l'échelle : R0/R2 1,70 m, M2/M4 2,05 m, Mara 1,78 m, Chimiste 1,83 m).
- Bestiaire phase A : `docs/BESTIARY.md` remis en place, `docs/ENEMIES.md` créé, cohérence Dealer / Chimiste et uniforme fictif du Flic.
- Code : correction du recul sur coup mortel (`PlayerCombat`, `EnemyAI`).
- **Bestiaire phase B** : architecture du code des ennemis, arène de test avec 6 vagues (touches 1 à 6), esquive à 0,40 s. Détail dans la section « Bestiaire — Phase B » plus bas.

**Reste à faire**
1. **Tranche verticale** (voir ci-dessus), puis validation du style par l'auteur, puis **phase C** (reste de l'Acte I).
2. **Nettoyage des modèles dans Blender** (phase 1) : supprimer les fragments, fermer les trous, origine aux pieds, orientation, réduction, matériaux, vues isolées face/profil/3-4. **R1 et R2 à dériver du modèle R0** (amincissement, cernes, teint), **lunettes de Nathan à modéliser** (double foyer, verres fêlés pour R2).
3. Personnages sans concept validé : **Nathan M0** (le faible, M0 = corps de R0 sans lunettes d'après `CHARACTERS.md` : à dériver de R0 dans Blender), **Nathan R1**, **Nathan R3**.
4. Phase 1 point 5 : designs des deux ennemis du monde Red Met, à rapprocher du bestiaire.
5. Phases 2 à 6 du prototype : Rigify, animations calées sur le code, rendu toon et spritesheets, intégration Unity, décors et éclairage, performance et build.

## Phase 0 — Générateur 3D local

### 2026-10-25 — Évaluation de Hunyuan3D 2.1

Source : `C:\RED MET\Hunyuan3D-2.1 source code\Hunyuan3D-2.1-main` (README, LICENSE, demo.py, requirements.txt).

**Licence (Tencent Hunyuan 3D 2.1 Community License) : bloquante en France.**
- Ligne 3 : « THIS LICENSE AGREEMENT DOES NOT APPLY IN THE EUROPEAN UNION, UNITED KINGDOM AND SOUTH KOREA ».
- Définition du territoire : « the worldwide territory, excluding the territory of the European Union, United Kingdom and South Korea ».
- Section 5.c : interdiction d'utiliser, reproduire, modifier, distribuer ou afficher le modèle, **ses sorties ou ses résultats** hors du territoire.
- Usage commercial : autorisé dans le territoire, sauf au-delà de 1 million d'utilisateurs actifs mensuels (licence à demander à Tencent).
- Conséquence : depuis la France, ni le modèle ni les .glb qu'il produirait ne sont couverts par la licence. **Installation suspendue** en attendant la décision de l'auteur.

**Entrée :** image seulement (`shape_pipeline(image=...)`, `paint_pipeline(..., image_path=...)`). Pas de texte vers 3D.

**Besoins annoncés par le README :** 10 Go de VRAM pour la forme, 21 Go pour la texture, 29 Go pour les deux. Python 3.10, PyTorch 2.5.1 + CUDA 12.4. Compilation de `custom_rasterizer` et du mesh painter.

### Machine

| Élément | Valeur |
|---|---|
| GPU | NVIDIA GeForce RTX 3070 Laptop, **8 Go de VRAM** (1 Go déjà occupé, Unity et Blender ouverts) |
| Pilote / CUDA | 591.86 / CUDA 13.1 côté pilote. Pas de toolkit CUDA (`nvcc` absent) |
| CPU / RAM | Ryzen 7 5800H / 31,4 Go |
| Python | Aucun Python système. `uv` est installé et peut fournir Python 3.10 dans un environnement dédié |
| Outils de compilation | Pas de Visual Studio Build Tools (nécessaires pour les extensions CUDA compilées) |
| Disque | C: 77 Go libres, D: 26 Go libres |

Verdict pour Hunyuan3D 2.1 : forme seule en dessous du minimum (8 Go pour 10 Go demandés), forme + texture PBR hors de portée (21 Go).

### Alternatives examinées

| Outil | Licence | Entrée | VRAM min. | Sortie | Windows |
|---|---|---|---|---|---|
| Hunyuan3D 2.1 | Tencent Community, **exclut l'UE** | Image | 10 Go (forme) / 29 Go | Forme + PBR | Oui |
| TripoSG (VAST) | MIT (code et poids) | Image | **8 Go** | Forme seule | Non documenté |
| TRELLIS (Microsoft) | MIT (hors quelques sous-modules) | Image ou texte | 16 Go | Forme texturée (GLB) | Non testé officiellement |

### Morfx 3D

Introuvable : aucun programme installé, aucun dossier ni addon Blender, aucun dépôt local, rien dans la configuration Claude Code, pas de `gh` installé. La recherche web ne trouve qu'un « Morfx » sans rapport (outil MCP de refactorisation de code). Lien demandé à l'auteur.

### Décisions de l'auteur
1. **TripoSG** remplace Hunyuan3D 2.1, sous réserve de vérification de sa licence.
2. Images de concept : **ComfyUI + un modèle SDXL de style anime** dont la licence permet l'usage commercial. La licence doit être montrée avant le téléchargement.
3. Visual Studio Build Tools autorisés si une dépendance l'exige.
4. Morfx 3D abandonné. Squelettes : Rigify, Mixamo en secours.

### 2026-10-25 — Vérification des licences

| Composant | Licence | Territoire | Usage commercial | Décision |
|---|---|---|---|---|
| TripoSG, code (GitHub `VAST-AI-Research/TripoSG`, commit fc5c409) | MIT | Aucune restriction | Oui | Retenu |
| TripoSG, poids (`VAST-AI/TripoSG`, Hugging Face) | MIT (champ `license: mit`) | Aucune restriction | Oui | Retenu |
| DINOv2 (encodeur d'image inclus dans les poids TripoSG) | Distribué dans le dépôt MIT de TripoSG | — | — | Inclus d'office |
| **RMBG-1.4** (BRIA), détourage appelé par le script d'origine de TripoSG | `bria-rmbg-1.4`, « source-available model for non-commercial use » | — | **Non** | **Écarté** : jamais téléchargé ni chargé |
| ComfyUI (commit fe298e4e) | GPL-3.0 | Aucune restriction | Oui (outil local, non redistribué avec le jeu) | Retenu |
| Animagine XL 4.0 (`cagliostrolab/animagine-xl-4.0`) | CreativeML Open RAIL++-M, reprise « sans modification ni restriction supplémentaire » | Licence « worldwide » | Oui | **Retenu** (accord de l'auteur le 2026-10-25). Fichier `animagine-xl-4.0-opt.safetensors`, 6,9 Go, rangé avec sa licence et sa fiche dans `C:\RED MET\ai\ComfyUI\models\checkpoints` |

Points clés d'Open RAIL++-M :
- Section « The Output You Generate » : « Licensor claims no rights in the Output You generate using the Model ». L'auteur est responsable de l'usage des images.
- Annexe A, restrictions d'usage : rien d'illégal, ni exploitation ou atteinte aux mineurs, ni diffamation, désinformation, discrimination, conseil médical, etc.
- Obligations en cas de redistribution du **modèle** (pas des images) : joindre la licence, signaler les modifications.

Écartés pour des raisons de licence : Illustrious-XL v2.0 (CreativeML OpenRAIL-M, la version SD 1.x de la licence) et NoobAI-XL 1.1 (Fair AI Public License 1.0-SD, conditions sur les modèles dérivés). Ils restent possibles, mais Animagine XL 4.0 est le plus simple sur le plan juridique.

### Contournements techniques
- **Pas de RMBG-1.4** : `tools/gen3d/backends/triposg_runner.py` détoure lui-même le fond uni relié aux bords de l'image (sans modèle), puis passe à TripoSG une image RGBA. Avec un alpha valide, `load_image()` de TripoSG n'appelle jamais RMBG. En cas d'échec du détourage, le runner s'arrête au lieu de retomber sur RMBG.
- **Pas de `diso`** (extension CUDA, distribuée uniquement en sources, qui demanderait le toolkit CUDA et MSVC) : TripoSG garde un décodeur hiérarchique qui extrait la surface avec `skimage` (`use_flash_decoder=False`). Build Tools non installés tant que ce n'est pas nécessaire.

### Installation
- `C:\RED MET\ai\TripoSG` : venv Python 3.10 (uv), torch 2.5.1 + cu124, `requirements.txt` sans `diso`.
- `C:\RED MET\ai\ComfyUI` : venv Python 3.13 (uv), torch cu130 (recommandation du README de ComfyUI).
- TripoSG : `diffusers` récent incompatible avec torch 2.5.1 (`torch.accelerator` absent). Versions figées à celles d'avril 2025 : diffusers 0.33.1, transformers 4.51.3, peft 0.15.2, huggingface_hub 0.30.2, accelerate 1.6.0. Versions exactes : `tools/gen3d/requirements-triposg.lock.txt` (et `requirements-comfyui.lock.txt`).
- Poids TripoSG : 7,5 Go dans `C:\RED MET\ai\TripoSG\pretrained_weights\TripoSG`. RMBG-1.4 non téléchargé.

### 2026-10-25 — Test technique de TripoSG (image d'exemple du dépôt TripoSG, MIT)
- Entrée : `assets/example_data/hjswed.png` (dinosaure), seed 42, 50 étapes, guidance 7, réduction à 40 000 faces. 1 essai.
- Résultat : réussite. Maillage brut de 1 760 508 faces, réduit à 40 000. Grille 512³ (décodeur hiérarchique). Le maillage n'est pas étanche, à traiter au nettoyage dans Blender.
- **VRAM : pic à 5,4 Go pour TripoSG, 7,5 Go sur 8 au total, avec Unity et Blender ouverts.** Il n'a pas fallu fermer Unity. 131 s de calcul.
- Détourage sans modèle réussi sur un fond coloré uni (tolérance 24).
- Problème corrigé : accents de la sortie du runner mal encodés sous Windows (`PYTHONIOENCODING=utf-8`).
- Sortie gardée hors du dépôt (dossier temporaire) : ce n'est pas un asset du jeu.
### 2026-10-25 — Concept de test : Nathan R0 (lot 1, 3 essais)
- Outil : ComfyUI + Animagine XL 4.0, 1024×1024, Euler a, 28 étapes, CFG 5 (réglages recommandés par la fiche du modèle). 18 à 24 s par image.
- Commande : `"C:\RED MET\ai\ComfyUI\.venv\Scripts\python.exe" tools/gen3d/concept.py --character Nathan_R0 --seeds 1001 2002 3003 --prompt "..."`. Les scripts du dépôt n'utilisent que la bibliothèque standard, n'importe quel Python ≥ 3.10 convient.
- Prompt (tags Animagine, d'après le prompt Tripo R0 de `CHARACTERS.md`) : `1boy, solo, male focus, adult, young man, 18 years old, slightly chubby, plump, rounded shoulders, brown hair, curly hair, messy hair, thick glasses, bifocals, acne, acne on cheeks, acne on forehead, black hoodie, oversized hoodie, grey cargo pants, sneakers, worn sneakers, t-pose, spread arms, arms outstretched, standing, full body, front view, facing viewer, looking at viewer, neutral expression, simple background, grey background` + suffixe `safe, masterpiece, high score, great score, absurdres`.
- Négatif : `child, kid, chibi, nsfw, lowres, bad anatomy, bad hands, extra fingers, missing fingers, extra limbs, cropped, out of frame, multiple views, text, watermark, signature, busy background, shadow on background, worst quality, low quality, low score, bad score, average score, username, error, blurry`.
- Seeds 1001, 2002, 3003 → `Assets/_Game/Art/Characters/Nathan_R0/Concept/`. Aucune image de référence en entrée.
- Constats :
  - Les trois ont les yeux fermés ou masqués par le reflet des lunettes.
  - Acné à peine visible, double foyer non rendu.
  - Visage encore juvénile malgré « adult, 18 years old » : à surveiller (CHARACTERS.md : jeune adulte, pas collégien).
  - 1001 : ombre portée au sol, qui gênerait le détourage.
  - 2002 : le plus enrobé, mais un faux logo avec du texte sur le hoodie.
  - 3003 : le plus propre pour la 3D (T-pose nette, fond uni, sans ombre).
- VRAM : ComfyUI garde le modèle chargé (4,8 Go occupés au repos). **Arrêter ComfyUI avant TripoSG.**
- Verdict de l'auteur : nouveau lot. Garder la propreté de 3003 et la silhouette de 2002 ; visage nettement adulte, yeux ouverts, double foyer lisible, acné visible, hoodie uni.

### 2026-10-25 — Concept Nathan R0, lot 2 (4 essais)
- Mêmes réglages. Seeds 4101, 4202, 4303, 4404.
- Prompt : `1boy, solo, male focus, adult, mature male, young adult man, 20 years old, defined jawline, square jaw, light stubble, stubble, visible neck, adam's apple, small head, adult proportions, chubby, plump, belly, rounded shoulders, brown hair, curly hair, messy hair, open eyes, brown eyes, eyes visible through glasses, thick glasses, black-framed glasses, bifocals, acne, pimples, acne on cheeks, acne on forehead, plain black hoodie, oversized hoodie, grey cargo pants, sneakers, t-pose, spread arms, arms outstretched, straight arms, standing, full body, front view, facing viewer, looking at viewer, neutral expression, simple background, plain grey background` + suffixe de qualité.
- Négatif : `child, kid, shota, young boy, baby face, round face, big head, large head, chibi, aged down, closed eyes, squinting, glasses glare, opaque glasses, logo, text on clothes, print, letters, emblem, drop shadow, floor shadow, shadow,` + négatif de qualité du lot 1.
- Constats :
  - Visage adulte obtenu sur les 4 (barbe naissante, mâchoire, cou), yeux ouverts, lunettes épaisses à monture noire, silhouette enrobée.
  - **Double foyer toujours absent** sur les 4 : SDXL ne dessine pas la ligne du segment inférieur.
  - Acné présente mais discrète (petits points).
  - 4101 : très enrobé, mais bras levés paumes vers le haut, légère ombre au sol.
  - 4202 : bonne T-pose, légère ombre au sol.
  - 4303 : le plus propre (T-pose droite, fond uni sans ombre visible, hoodie uni).
  - 4404 : petite marque blanche sur le hoodie, bras en A.
- Verdict de l'auteur : 4303 retenu, mais **jambes trop courtes**. Le personnage doit mesurer 1,70 m (5'8"), « pas un nain ».

### 2026-10-25 — Concept Nathan R0, lot 3 : proportions d'adulte
- Mesure automatique sur 4303 (fond retiré, entrejambe = première ligne vide au centre sous la mi-hauteur) : **jambes 28 % de la hauteur, environ 3,9 têtes**. Cible d'un adulte de 1,70 m : jambes à 45-48 %, environ 7 têtes.
- Essai A, nouveaux tirages (seeds 4303, 5101, 5202, 5303), prompt du lot 2 + `tall, 170cm, long legs, realistic body proportions, 7 heads tall`, négatif + `short legs, stubby legs, dwarf, short stature, long torso`. Les jambes passent à environ 45-48 %, mais le modèle amincit la silhouette et change les cheveux (longs sur 5101 et 5202). Bras irréguliers sur 5202 et 5303. Moins fidèle au 4303 aimé.
- Essai B, **retouche du 4303** :
  1. `tools/gen3d/stretch_band.py` étire la bande ourlet → chevilles (lignes 700 à 900) de +448 px, sans toucher à la tête, au torse ni aux chaussures, puis complète le fond au format 832×1216.
  2. img2img SDXL (seed 4303, 832×1216, prompt du lot 2 + `tall, long legs, long pants`) à denoise 0,35 et 0,5 (`concept.py --init-image --denoise --size`).
  - Résultat : `Nathan_R0_concept_legs_d035_s4303.png` et `Nathan_R0_concept_legs_d05_s4303.png`. **Jambes à 47-48 %**, environ 5,8 têtes (la tête reste grande, style anime). Visage, silhouette et hoodie du 4303 conservés. d0,5 est plus propre : moins de traces, pas de goutte de sueur.
  - Petit défaut commun : une mèche sombre en tache sur le front, à gauche.
- **Verdict de l'auteur : `Nathan_R0_concept_legs_d05_s4303.png` retenu tel quel** (tête non réduite).
- Incident : premier lancement de l'essai B fait sans prompt (une erreur d'encodage de « → » sous Windows a interrompu la commande avant la définition du prompt). Les deux images ont été supprimées et relancées. Message corrigé en « -> ».
- Outil du dépôt : `tools/gen3d/` (`generate.py` en bibliothèque standard, `backends.json`, `backends/triposg_runner.py`, `concept.py` pour l'API ComfyUI). Nommé `gen3d` plutôt que `hunyuan` puisque le générateur a changé.

### 2026-10-25 — Série 2 de concepts : Nathan R3, M2, M4 et Mara M-T0 à M-T4
- Script reproductible : `tools/gen3d/batches/2026-10-25_concepts_serie2.sh` (prompts complets, négatifs et seeds ; `retakes` pour les reprises). Animagine XL 4.0, 1024×1024, Euler a, 28 étapes, CFG 5.
- Le tag de classement Animagine est sorti du suffixe de `backends.json` et placé dans chaque prompt : `safe` pour Nathan et Mara M-T0, `sensitive` pour Mara M-T1 à M-T4 (tenues révélatrices, non explicites). Négatif de Mara : `nsfw, nude, nipples, areolae, pussy, explicit, child, loli, young, petite, flat chest, aged down, baby face, round face, male`.
- Références : `docs/references/nathan-reel-amaigri.jpg` demandée mais **absente du dépôt**. R3 s'appuie sur la description et sur `nathan-reel-hallucination.webp`. Aucune référence en entrée du générateur.
- **29 images, 22 + 7 reprises.**

| Personnage | Seeds | Utilisables | Rejetées (raison) |
|---|---|---|---|
| Nathan R3 | 6101, 6202, 6303, 6404, reprises 6505, 6606 | 6202, 6303, 6606 | 6101, 6404, 6505 : plusieurs personnages sur l'image |
| Nathan M2 | 7101, 7202, 7303, 7404 | les 4 (7202 avec ombre au sol) | — |
| Nathan M4 | 7501, 7602, reprise `grey` 7501, 7602 | grey_7501, grey_7602 | 1er passage : peau restée rose. Reprise avec `(grey skin:1.4), (stone skin:1.3), (glowing red veins:1.2)` |
| Mara M-T0 | 8001, 8002, reprise 8003 | 8001, 8003 | 8002 : deux personnages |
| Mara M-T1 | 8101, 8102 | les 2 (8101 : jupe blanche) | — |
| Mara M-T2 | 8201, 8202 | les 2 (épée qui casse la pose en T) | — |
| Mara M-T3 face | 8301, 8302 | 8301 (8302 bras pliés) | — |
| Mara M-T3 dos | 8311, 8312, reprises 8313, 8314 | 8313, 8314 | 8311 : épée à l'horizontale, pas de pose en T. 8312 : face et dos sur la même image |
| Mara M-T4 | 8401, 8402 | les 2 | — |

- Négatif des reprises : + `2boys, 2girls, multiple boys, multiple girls, character sheet, turnaround, reference sheet, multiple views`. Pour R3 : + `smile, grin, dirt on ground`. Pour le dos de M-T3 : + `sword pointing down` dans le prompt et `sword across body` dans le négatif.
- Constats généraux :
  - **L'épée de Mara gêne la pose en T** et la génération 3D (lame lumineuse, transparente, plaquée contre le corps). Recommandation : modéliser la lame à part dans Blender, et pour TripoSG générer Mara sans arme.
  - R3 : la posture voûtée ne passe pas en pose en T, ce qui est logique. À rendre dans les animations. Le visage ressemble peu à R0 (style anime plus fin).
  - M2 : air de famille avec Nathan faible. Bras en « paumes vers le haut » plutôt qu'en vraie pose en T.

### 2026-10-25 — Nathan R0 : premier modèle TripoSG (1 essai)
- Entrée : `Nathan_R0_concept_legs_d05_s4303.png` (concept validé). Seed 42, 50 étapes, guidance 7, réduction à 40 000 faces.
- Commande : `"C:\RED MET\ai\TripoSG\.venv\Scripts\python.exe" tools/gen3d/generate.py --character Nathan_R0 --image <concept> --seed 42 --faces 40000`. ComfyUI arrêté avant, pour libérer sa VRAM. Unity et Blender restés ouverts.
- Résultat : `Assets/_Game/Art/Characters/Nathan_R0/Source/Nathan_R0_triposg_s42.glb` (+ `.json`, + `_input.png` détouré). 651 310 faces brutes réduites à 40 000. **Pic de VRAM 5,23 Go**, 90 s de calcul, 99 s au total.
- Dans Blender (collection `Nathan_R0`) : mis à l'échelle 1,70 m, pieds au sol (transformations seulement, maillage non modifié). Dimensions 1,28 × 0,47 × 1,70 m.
- Constats :
  - **Bon** : silhouette fidèle au concept (ventre, hoodie trop large, cargo à poches, baskets), proportions d'adulte, pose en T propre.
  - **À reprendre en phase 1** :
    - cheveux bruités, avec un trou dans le crâne et **22 fragments détachés** (le plus gros fait 144 sommets) ;
    - visage très simplifié ;
    - **lunettes réduites à un relief** (ni verres ni branches) : à modéliser à part, ce qui permettra aussi le double foyer et les verres fêlés de R3 ;
    - maillage non étanche.
  - Pistes : relancer avec d'autres seeds et sans réduire, ou réduire moins (`--faces 100000`), selon la règle « une autre seed plutôt qu'une retouche à la main ».
- Problème corrigé : `generate.py` et `concept.py` forcent maintenant leur sortie en UTF-8 (une « → » faisait planter `print()` sous Windows).

### 2026-10-25 — Choix de l'auteur sur la série 2, puis reclassement R3 → R4
- Retenus : `Nathan_R0_concept_s4202` (**à clarifier** : le modèle R0 a été fait sur le 4303 retouché, validé plus tôt), `Nathan_M2_concept_s7202`, `Nathan_M4_concept_s7602`, `Mara_T0_concept_s8002`, `Mara_T1_concept_s8102`, `Mara_T2_concept_s8201`, `Mara_T3_back_concept_s8312`, `Mara_T4_concept_s8402`.
- Consignes : Mara modélisée **sans épée**, épée modélisée à part. **Erreur signalée par l'auteur : trois bras sur certains concepts de Mara.** Vérifié en pleine résolution :
  - **8002** : deux Mara sur l'image, celle de droite a **trois bras** (deux tendus, un troisième qui tient l'épée). Celle de gauche est correcte.
  - **8312** : une Mara de dos et une de face, qui **partagent un même bras** au milieu de l'image, et chacune est coupée au bord.
  - Mes contrôles précédents n'avaient compté ces images que comme « plusieurs personnages ». Désormais, chaque concept est vérifié en pleine résolution, bras et mains comptés, avant d'être présenté.
- Les concepts R3 « font trop zombie » : renommés `Nathan_R4_concept_s*` et déplacés dans `Assets/_Game/Art/Characters/Nathan_R4/Concept/` (avec leurs `.meta`, `git mv`). Le script de la série 2 écrit désormais en R4.

### 2026-10-25 — Concepts Nathan R1 et R2 (`tools/gen3d/batches/2026-10-25_concepts_r1_r2.sh`)
- Principe : img2img depuis le concept R0 validé (`Nathan_R0_concept_legs_d05_s4303.png`, 832×1216), pour garder le même visage, la même pose en T et le même cadrage. Négatif anti-mort-vivant : `zombie, undead, corpse, grey skin, green skin, wounds, open wounds, blood, gore, empty eyes, blank eyes, white pupils, no pupils, glowing eyes, skeleton, visible ribs, horror, smile, grin, teeth`.
- **R1** (denoise 0,65, seeds 9101 à 9104) : 4 variantes réussies du premier coup. Même garçon, joues et ventre affinés, sueur, poings crispés (9104). 9102 : petit point blanc parasite au-dessus de la tête. Acné discrète.
- **R2**, 4 passes :
  1. `Nathan_R2_concept_s9201`-`9204` (denoise 0,75) : bons visages amaigris, mais **cargo beige de R0 conservé** (et hoodie noir sur 9202). Gardés pour mémoire.
  2. Inpainting du pantalon (masque rectangulaire via `make_mask.py`) : **échec**. Raccords visibles aux bords du masque, chaussures coupées, **deux mains en trop sur 9203**, jambes en bâton. Images supprimées.
  3. Denoise 0,8 + `(worn black jeans:1.4)` et `(cargo pants:1.3)` en négatif : **échec**, le modèle a retiré le pantalon (jambes nues, short) et ajouté du texte sur les hoodies. Images supprimées.
  4. **Retenue comme proposition** : on noircit d'abord le pantalon de R0 sans modèle (`tools/gen3d/batches/r2_darken_pants.py` : pixels beiges ou plus sombres que le fond dans la zone des jambes, 13,7 % de l'image) → `Nathan_R2_init_R0_jean_noir.png`, puis img2img à 0,75 (seeds 9221 à 9224, `(worn black jeans:1.2)`). Résultat : `Nathan_R2_concept_c_s9221`-`9224`, amaigris, hoodie gris foncé, pantalon sombre, même garçon. Limites : pantalon plus « large » que « jean », manches baissées (rougeurs des avant-bras invisibles), yeux mi-clos sur 9223.
- La posture voûtée de R2 (bras serrés) est incompatible avec la pose en T demandée pour la 3D : elle est reportée sur l'animation (idle de la phase 2).
- `docs/CHARACTERS.md` : tableau du monde réel réécrit (R0, R1 « Premières semaines », R2 « Le manque », R3, R4), avec la règle « malade, jamais mort-vivant » de R0 à R2. R3 n'a plus de concept.
- Outils ajoutés : mode inpainting de `concept.py` (`--mask`, zone hors masque recollée à l'identique), `make_mask.py` (rectangles, polygones, couleur limitée à une zone, aperçu).

### 2026-10-25 — Nettoyage des concepts retenus avant TripoSG
Outils : `make_mask.py` (masques), mode inpainting de `concept.py` (seule la zone masquée est redessinée, le reste est recollé à l'identique), `crop_center.py` (recadrage et recentrage, option `--at`). Chaque résultat a été vérifié en pleine résolution, bras et mains comptés.

| Concept retenu | Problème | Traitement | Essais | Résultat |
|---|---|---|---|---|
| Mara T0 8002 | 2 Mara, celle de droite a **3 bras** et l'épée | Mara de gauche recadrée (`--box 0 0 522 1024`), recentrée ; inpainting de la pointe d'épée restante et des bords de recadrage | 1 | `Mara_T0_clean_s8002.png`. 2 bras, sans épée. Reste un artefact « Aa » minuscule sur le fond. **Combinaison-pantalon, sans la jupe longue fendue de M-T0** |
| Mara T1 8102 | 2 épées flottantes | Inpainting de polygones autour des lames | 3 | v1 : halo rose et mèches coupées. v2 (masque élargi) : **manche coupée, main flottante**. v3 (bande des bras exclue du masque) : `Mara_T1_nosword_s8102.png`, 2 bras intacts, chevelure continue |
| Mara T2 8201 | Épée en travers derrière le bras et le torse | Inpainting en 3 segments, bras exclu, puis 2e passe sur les restes (garde, traits de lumière) | 2 | `Mara_T2_nosword_clean_s8201.png`, 2 bras, sans épée |
| Mara T3 8312 | Planche dos + face, **un bras partagé**, figures coupées au bord | Mara de face recadrée (`--box 380 0 1024 1024`) et décalée (`--at 82 0`) pour que les deux bras tiennent ; inpainting de la fin du bras droit, d'une main au bout du bras gauche et de la lame de l'autre Mara | 2 | `Mara_T3_front_from8312_s8312.png`, une seule Mara de face, 2 bras, 2 mains, pose en T. Gants dépareillés (main gauche nue). La vue de dos de 8312 reste la référence de l'arrière (demi-jupe) |
| Mara T4 8402 | Épée tenue dans la main gauche | Inpainting de la poignée, de la garde et de la lame, main exclue | 1 | `Mara_T4_nosword_s8402.png`, 2 bras. Main gauche un peu tronquée (petit artefact carré) |
| Nathan M2 7202 | Ombre bleue au sol | Masque par couleur (36, 73, 108, tolérance 30) limité à la bande du sol, inpainting | 1 | `Nathan_M2_noshadow_s7202.png`. Léger halo clair sous les bottes → TripoSG lancé avec `--bg-tolerance 45` |

### 2026-10-25 — Épée de Mara (modélisée à part dans Blender)
- Modélisation procédurale (script Python via le MCP Blender) : poignée gainée noire (0,22 m), pommeau en gemme rouge, garde anguleuse noire avec deux pointes rouges, lame à section en losange en **matériau émissif rouge** (force 12), coque de lueur translucide (alpha 0,25, émission 4). 86 faces, environ 1,3 m. Objet parent `Mara_EnergyBlade` (vide) au centre de la poignée, pour l'attacher à l'os de la main.
- Fichiers : `Assets/_Game/Art/Characters/Mara/Source/Mara_EnergyBlade.glb`, source `ArtSource/Blender/Mara_EnergyBlade.blend` (dossier `ArtSource/Blender/` à la racine du dépôt, hors de `Assets/` pour qu'Unity n'essaie pas d'importer les `.blend`).
- À reprendre en phase 3 : l'émission sature en rose dans l'aperçu matériau. Le rendu toon et le bloom donneront le rouge final.

### 2026-10-25 — Modèles TripoSG : Nathan M2 et Mara M-T0 à M-T4 (1 essai chacun)
- Script : `tools/gen3d/batches/2026-10-25_models_m2_mara.sh` (seed 42, 50 étapes, guidance 7, 40 000 faces). ComfyUI arrêté avant, Unity et Blender ouverts.

| Modèle | Faces brutes | Pic VRAM | Calcul | Constat |
|---|---|---|---|---|
| `Nathan_M2_triposg_s42` | 1 121 526 | 5,35 Go | 111 s | Colosse fidèle (cape de fourrure, brassards, bottes). Mis à 2,05 m dans Blender |
| `Mara_T0_triposg_s42` | 965 054 | 5,23 Go | 87 s | Bonne silhouette, chevelure en nappe à l'arrière |
| `Mara_T1_triposg_s42` | 828 698 | 5,24 Go | 87 s | Bonne silhouette |
| `Mara_T2_triposg_s42` | 790 824 | 5,22 Go | 85 s | Jupe effilochée, jambes écartées |
| `Mara_T3_triposg_s42` | 610 990 | 5,21 Go | 80 s | Mèches flottantes |
| `Mara_T4_triposg_s42` | 1 188 306 | 5,32 Go | 105 s | **Jambes fusionnées** (croisées sur le concept), cheveux et cape en grande nappe : à relancer ou à retoucher avant le squelette |

- Mara est mise à 1,78 m, R0 à 1,70 m (comparaison dans Blender, collections par personnage).
- Profils en partie masqués par les personnages voisins dans la file : vues isolées à refaire au nettoyage (phase 1).
- Restent à trancher par l'auteur : `Nathan_R0_concept_s4202` (alors que le modèle R0 a été fait sur le 4303 retouché) et `Nathan_M4_concept_s7602` (version du 1er passage, à peau rose, ou `Nathan_M4_concept_grey_s7602` à peau grise ?). Aucun modèle M4 ni nouveau R0 n'a été lancé.

### 2026-10-25 — Concepts du Chimiste (4 essais)
- Script : `tools/gen3d/batches/2026-10-25_concepts_chimiste.sh`. Animagine XL 4.0, 1024×1024, réglages habituels. Visage entièrement généré, aucune photo ni personne réelle en référence.
- Prompt commun : `1boy, solo, male focus, adult, mature male, middle-aged man, 48 years old, dark-skinned male, black man, west african man, tall, long legs, mature face, light wrinkles, calm expression, calculating eyes, slight smile, gold tooth, short beard, well-groomed beard, heavy gold chain necklace, gold ring, wristwatch, dress pants, black leather shoes, elegant, wealthy, composed` + coiffure + chemise + `open collar` + pose et style + `safe`.
- Négatif : celui de l'auteur (`caricature, exaggerated features, cartoonish lips, mugshot, photo`) + `photorealistic, realistic, street thug, gang, hoodie, tank top, sunglasses, tattoo, weapon, gun` + négatifs habituels (planches, enfants, mains, ombres).

| Seed | Coiffure / chemise demandées | Obtenu | Constats |
|---|---|---|---|
| 10101 | Crâne rasé / noire | Crâne rasé, barbe blanche, **chemise blanche** | Paternel et souriant, mais paraît plus de 60 ans, yeux fermés, peau plus claire que demandé |
| 10102 | Très courts / bordeaux | Coupe rase, **costume bordeaux complet** à revers noirs, chemise blanche | Le plus « prospère et élégant », barbe nette ; paraît 35-40 ans ; yeux plissés |
| 10103 | Locks attachées / noire | Locks attachées, **chemise blanche**, pantalon noir | Peau la plus foncée, sourire en coin calculateur ; paraît 35 ans, silhouette mince |
| 10104 | Courts, tempes grises / bordeaux | Cheveux et barbe gris, chemise noire sous veste bordeaux | Le regard le plus « calme et calculateur », yeux ouverts ; paraît 55-60 ans, peau plus claire |

- Défauts communs : **dent en or invisible** à cette échelle (à peindre sur la texture), « black dress shirt » rendu en chemise blanche sur 10101 et 10103. Aucun trait caricatural. Mains correctes (2 bras sur les 4, vérifié).

### 2026-10-25 — Retours de l'auteur : concepts R1/R2 rejetés, règle de non-dérive
- **R1 (9101-9104) et R2 (c_9221-c_9224) rejetés comme médiocres**. Certains Nathan ont pris des traits féminins (cheveux longs, visage adouci), et les proportions se déformaient à chaque passe img2img enchaînée. Le nouveau R0 4202 / 4303 n'est pas retenu non plus : **on garde ce qui a été validé dans le fil** (R0 = `Nathan_R0_concept_legs_d05_s4303.png` et son modèle `Nathan_R0_triposg_s42`).
- **Règle adoptée** : ne plus dériver de nouvelles images d'un concept validé. Les corrections (lunettes, acné, usure, proportions) se font dans Blender, pas par de nouvelles passes SDXL. Chaque image est vérifiée en pleine résolution (bras, mains, genre lisible) avant d'être montrée.
- M4 : la version **à peau couleur chair** (`Nathan_M4_concept_s7602`, 1er passage) est retenue.

### 2026-10-25 — Nathan M4 (TripoSG) et correction des jambes de Mara T4
- `Nathan_M4_triposg_s42.glb` : 115 s, 40 000 faces. Mis à 2,05 m dans Blender.
- Mara T4 : relancée avec les seeds 7 et 2026 (règle « une autre seed plutôt qu'une retouche »). **Échec** : chevilles toujours fusionnées sur les 3 seeds (coupe à 15 % de la hauteur = un seul bloc), car le concept validé montre les jambes croisées et TripoSG le reproduit. Le concept ne devant plus être retouché, la correction s'est faite **dans Blender**, sur une copie du modèle seed 7 (la seule qui séparait déjà les mollets) :
  - coupe verticale dans l'axe du corps (x de la médiane des hanches), seulement sous l'entrejambe (46 % de la hauteur), puis séparation des arêtes de la coupe ;
  - rotation de chaque jambe de 6° vers l'extérieur autour de sa hanche, avec un poids progressif (0 à l'entrejambe, 1 sous 12 % de la hauteur) pour raccorder le bassin sans cassure.
  - Résultat : 2 jambes distinctes jusqu'aux pieds (coupes à 5 %, 15 % et 25 % = 2 blocs), haut du corps intact. Fichier : `Mara/Source/Mara_T4_triposg_s7_legsfix.glb`. Les seeds 42, 7 et 2026 brutes restent dans `Source/` pour mémoire.

### 2026-10-25 — Chimiste v2 : peau noire foncée et traits africains (4 essais)
- Retour de l'auteur : peau trop claire. Il faut une peau noire foncée et des traits de visage africains (Côte d'Ivoire).
- Même script (`2026-10-25_concepts_chimiste.sh v2`), mêmes seeds et coiffures. Ajouts : `(very dark skin:1.4), black skin, dark-skinned male, west african facial features, ivorian man` ; négatif + `light skin, pale skin, tan skin, light brown skin, white man, asian` ; chemises pondérées `(black dress shirt:1.3)` et `(burgundy dress shirt:1.3)`. Négatif anti-caricature conservé.

| Seed | Obtenu | Constats |
|---|---|---|
| v2_10101 | Crâne rasé, barbe, long manteau gris, chemise blanche, **plusieurs grosses chaînes** | Peau foncée réussie, visage posé. Trop de bijoux (« bling »), chemise blanche au lieu de noire |
| v2_10102 | Coupe rase grisonnante, costume bordeaux, chemise rose, sourire | Élégant et prospère. Sourire large, chemise rose et non noire |
| v2_10103 | Locks attachées (anneau doré), chemise blanche, pantalon noir | Le plus jeune (30-35 ans), **chemise encore blanche**, légère ombre au sol, yeux très clairs |
| v2_10104 | Cheveux et barbe gris, chemise bordeaux, double chaîne en or | Correspond le mieux à la tenue demandée, posture posée. Paraît 55-60 ans, yeux très clairs (reflet) |

### 2026-10-25 — Choix de l'auteur : Chimiste v2_10103, R1 et R2
- **Chimiste : `Chimiste_concept_v2_s10103` retenu.** Copie technique pour la 3D (`Chimiste_concept_v2_s10103_input3d.png`) : les 1 879 pixels de l'ombre qui déborde des chaussures (bande y 993 à 1018, hors des chaussures) sont repeints à la couleur du fond, sans génération ni retouche du personnage. Modèle `Chimiste/Source/Chimiste_triposg_s42.glb` : 100 s, 40 000 faces, pas de socle sous les pieds. Mis à 1,83 m dans Blender.
- **R1 : aucun concept retenu** (tous trop éloignés du R0 validé). R1 sera fait dans Blender à partir du modèle R0 validé.
- **R2 : base `Nathan_R2_init_R0_jean_noir.png`** (le R0 validé au pantalon teint en noir, sans génération SDXL). Modèle `Nathan_R2/Source/Nathan_R2_triposg_s42.glb` : 90 s. Comme TripoSG ne produit que la forme, le maillage est quasiment celui de R0 (corps enrobé). **L'amaigrissement de R2 se fera dans Blender.**

## Bestiaire — Phase A : intégration au design (2026-10-25)
- Fichiers reçus mal rangés : `docs/BESTIARY.md` était un **dossier** contenant `Prépares le script pour Claude Code.txt`, et `docs/references/bestiaires-12-ennemis.webp` un dossier contenant un **PNG**. Ils avaient été embarqués tels quels dans le commit `c36e147` par un `git add -A docs`. Remis en place avec `git mv` : `docs/BESTIARY.md` (fichier ; seule la 1re ligne, un reste de la boîte d'enregistrement, a été retirée) et `docs/references/bestiaire-12-ennemis.png` (non converti, pour ne pas altérer le fichier). La planche sert uniquement de référence de style.
- Liens vers le bestiaire ajoutés dans `docs/GDD.md` (nouvelle section « Bestiaire ») et `CLAUDE.md`.
- **`docs/ENEMIES.md`** créé : règles communes (équilibrage, lisibilité, esquive, étourdissement), profils de Nathan sobre et Red Met, fiches complètes des ennemis 01 à 08 (PV, vitesse, détection, poids, attaques avec portée, armement, actif, récupération, dégâts et annonce, fenêtres de vulnérabilité, ce qu'il punit, contre-stratégie, coups pour tuer et coups encaissables sur les deux trajectoires), tableau indicatif des ennemis 09 à 12 avec leurs points d'extension, vagues de l'arène de test.
  - Profil Red Met (dose 2) : 200 PV, combo 60/75/110, tir à 20. Calé sur la règle du GDD « environ 4 fois plus d'actions offensives en sobre » (Voyou : 4 coups en sobre, 1 coup sous Red Met).
  - Vérification : les décomptes ont été recalculés après rédaction ; 9 valeurs mal arrondies ont été corrigées (définition unifiée : PV ÷ dégâts, arrondi au supérieur).
  - **Constat pour la phase B** : `dodgeInvulnerability` (0,25 s) est plus court que l'armement minimal (0,35 s). Proposition : le passer à 0,40 s.
- **Dealer (ennemi 02) ≠ Chimiste** : précisé dans `ENEMIES.md`, dans une note de `BESTIARY.md`, dans `CHARACTERS.md` (fiche du Chimiste, avec le concept retenu v2_10103) et dans `GDD.md`.
- **Flic corrompu** : uniforme fictif, sans insigne, logo ni nom d'une police réelle, texte générique « POLICE » seulement. Précisé dans `ENEMIES.md` et dans une note de `BESTIARY.md`.

## Bestiaire — Phase B : architecture du code des ennemis (2026-10-26)

**Architecture** (`Assets/_Game/Scripts/Enemies/`)
- `EnemyArchetype` (ScriptableObject) : PV, vitesses, détection, poids (léger, moyen, lourd), super-armure, étourdissement, silhouette provisoire, liste d'`EnemyAttack` (type, portée, armement, actif, récupération, recharge, utilisations, dégâts, recul, projectiles, zone, charge, ordre, renfort, couleur d'annonce, ligne de visée, feinte).
- `Enemy` (socle) : applique l'archétype au prefab générique, détection et alerte de groupe, déplacement (avec saut par-dessus les abris), cycle armement → actif → récupération, interruption pendant l'armement, super-armure, charge qui sonne contre un mur, ordre (armement −20 %, jamais sous 0,35 s), renfort, filtre de dégâts derrière un abri. Le poids passe par la masse du Rigidbody2D (recul complet, moitié, nul).
- `EnemyTelegraph` (couleur d'armement, ligne de visée, états), `AttackTokens` (2 au contact, 1 tireur), `Projectile` et `Grenade` (trajectoires testées par CircleCast, sans Rigidbody), `Cover`, `EnemyWaveManager`.
- Comportements : `MeleeAggressive`, `Commander`, `CoverShooter`, `Tactical`, `HeavyCharger`. Points d'extension : `Ambusher`, `Feinter`, `Trickster` (Acte II) et les interfaces de l'Acte III (`EnemyExtensions.cs`).
- `Health.TakeDamage(DamageInfo)` (corps à corps, tir, explosion, origine) avec des filtres `IDamageFilter` ; `TakeDamage(int)` reste valable. `EnemyAI` est supprimé.
- `PlayerDodge` : invulnérabilité à **0,40 s**, et Nathan traverse les ennemis pendant l'esquive (layers `Player` et `Enemy`).
- `ArenaBuilder` : layers `Player` et `Enemy` (collision Ennemi/Ennemi désactivée), prefabs `Enemy`, `Projectile`, `Grenade`, `Cover`, archétypes dans `Assets/_Game/Data/Enemies/` (créés seulement s'ils manquent), 6 vagues, HUD (PV cumulés de la vague).

**Tests en mode Play** (banc de test lancé par le MCP Unity, dégâts et positions relevés dans la console)

| Test | Résultat |
|---|---|
| Voyou : armement 0,35 s, coup à 10 | OK |
| Coup reçu pendant l'armement | Attaque interrompue (Voyou) ; super-armure du Mutant : l'armement continue |
| Ordre du Dealer | Couteau armé en 0,36 s au lieu de 0,45 ; le Dealer reste à 6-9 unités |
| Jetons | 2 Voyous occupés au plus en même temps (vague 6) |
| Flic | Gagne l'abri en x = 8 ; tir de face sur lui à l'abri : 5 dégâts sur 20 ; de flanc : 20 ; radio sous 50 % → un Voyou arrive (un premier appel interrompu par un coup, puis réussi) |
| Soldat | Rafales arrêtées par l'abri libre ; grenade lancée quand Nathan se cache, explosion à 30 |
| Mutant | Charge à 30, frappe au sol à 22 ; esquive tardive à travers la charge sans dégât ; charge dans le mur → sonné 2,49 s (couleur bleue) |

**Problèmes rencontrés**
- La grenade partait à hauteur de poitrine et heurtait l'abri derrière lequel le Soldat se poste. Corrigé : lancer par-dessus la tête, cloche de 2,5 unités au-dessus du point le plus haut.
- Une esquive simulée trop tôt laissait la charge toucher : c'est la règle « esquiver tard » de la fiche, pas un défaut.
- Avertissements d'API dépréciées d'Unity 6.6 (`ContactFilter2D.NoFilter`, `FindObjectsSortMode`) corrigés.
- Le relais MCP d'Unity se coupe après 3 minutes d'inactivité : une nouvelle requête le relance.
- La capture par l'outil `Camera_Capture` échoue sur ce projet ; `ScreenCapture.CaptureScreenshot` fonctionne.

Capture : `docs/captures/2026-10-26_phaseB_arene_groupe.png` (vague 6 : un Voyou en armement, le Flic qui vise avec sa ligne rouge).

## Phase Combat dynamique (2026-10-26)

Objectif de l'auteur : le punch, l'impact et la vitesse d'exécution de Dragon Ball FighterZ, sans ses super-pouvoirs (ni téléportation, ni rayons, ni transformations), avec les personnages provisoires et 60 FPS stables.

**Ce qui a été fait**
- **`AttackData`** (ScriptableObject) : démarrage, actif, récupération et fenêtre d'annulation en images à 60 FPS, dégâts, type (contondant, lame, balle, explosion), zone de frappe, élan, hitstop (0 à 12 images), tremblement, zoom, image d'impact, type de recul (léger, lourd, projection, rebond au mur, rebond au sol), étourdissement. Le pas physique passe à **1/60 s** (une image = un pas). 9 attaques pour Nathan (`Data/Attacks/Nathan`), 16 pour les ennemis de la phase B (`Data/Attacks/Enemies`) ; `EnemyAttack` ne garde que ce qui est propre à l'IA.
- **Impact** : `HitStop` fige les deux combattants sur leur propre horloge (Rigidbody et Animator), jamais `Time.timeScale`. `CameraFollow` : tremblement proportionnel et zoom bref vers l'impact. `CombatVfx` : image d'impact (une image blanche puis une sombre), lignes de vitesse, étincelles, ondes de choc. `HitReaction` : recul glissé avec poussière, flash blanc, pose de douleur, jonglage, au sol couché. Ralenti de 0,3 s (×0,25) sur le coup qui finit une vague (`GameTime`).
- **Vitesse** : mémoire des commandes de 6 images (gelée pendant le hitstop), annulations léger → moyen → lourd, coup lourd (K, rebond au mur), lanceur (S + K) annulable par un saut, 3 coups aériens et smash (rebond au sol), dash au sol et en l'air (L, LB ou double appui) annulable en attaque.
- **Sang et gore** : coups contondants = sueur, salive, étincelles ; lames = giclée orientée, gouttes qui tombent et laissent des taches au sol et sur les murs (collisions des particules), qui s'estompent ; démembrement possible après 4 coups de lame (garanti sur le coup final, 2 membres au plus par ennemi) ; plus de sang sur le coup final et sur les ennemis à terre ou en l'air ; couleur du sang par archétype (`bloodColor`, `bloodGlows` pour les ennemis surnaturels). Tout est en réserve : 5 systèmes de particules créés au chargement, 8 anneaux, 60 taches, 8 membres ; balles et grenades passent par `Pool`.
- **Options** (Échap) : gore désactivé / réduit / complet, tremblement de 0 à 100 %, images d'impact activées ou non. Enregistrées dans les PlayerPrefs.
- **Test** : mannequin d'entraînement (touche 7, présent au lancement, ne riposte pas, se soigne), debug F1 (frise des images, zone de frappe, phase des ennemis, images par seconde), arme de test F2 (lame), démo de combo F3.
- Supprimé : `Hitbox` (remplacé par une zone de frappe testée image par image).

**Tests en mode Play** (scripts lancés par le MCP Unity)

| Test | Résultat |
|---|---|
| Combo complet sur le mannequin | Jab → direct → lanceur → saut → aériens 1, 2, 3 → smash → jab → direct → crochet → coup lourd : 12 coups d'affilée |
| Rebond au mur | Coup lourd près du mur droit : rebond en x = 20 |
| Rebond au sol | Lanceur (mannequin à 3 m), smash : rebond, remonte à 1,3 m |
| Gore désactivé / complet | 0 goutte de sang / 57 gouttes sur un coup de lame |
| **60 FPS** (3 combos d'affilée à l'arme tranchante, gore complet) | 5 010 images ; thread principal **2,44 ms en moyenne, 15,4 ms au pire, aucune image au-dessus de 16,6 ms** ; jusqu'à 197 gouttes, 60 taches (plafond) et 2 membres à l'écran |
| Allocations | environ 9 Ko par image mesurés dans l'éditeur, qui alloue lui-même en mode Play : à mesurer sur un build |

**Problèmes rencontrés**
- La mémoire du saut (0,1 s en temps réel) expirait pendant le hitstop du lanceur (8 images) : le saut d'annulation ne partait pas. Corrigé : toutes les mémoires de commandes comptent en images de jeu et sont gelées pendant le hitstop.
- Le membre arraché prenait la couleur du corps en plein flash blanc. Corrigé : couleur de l'archétype.
- Démo F3 : la première version enchaînait les commandes avant le démarrage de l'attaque suivante. Elle attend maintenant chaque nouvelle attaque.
- L'outil MCP refuse `AssetDatabase.DeleteAsset` et `File.Delete` (« user interaction ») : les anciens archétypes ont été supprimés depuis le disque.
- Pendant les tests, l'auteur jouait dans la vue Game : ses touches se mêlaient aux scripts de test (vagues changées, scène relancée). Les tests ont été refaits en forçant la vague du mannequin.

Captures : `docs/captures/2026-10-26_combat_planche_combo.png` (12 images du combo complet), `..._combat_impact_lame.png`, `..._combat_coup_lourd.png`. Sur ces captures, le membre arraché est encore blanc (prises avant la correction).

## Tranche verticale (demandée le 2026-10-26)

Avant le reste de l'Acte I : une tranche jouable aux graphismes définitifs (Voyou classique, Nathan M0, arène de la lune rouge), sur laquelle l'auteur valide le style.

### 2026-10-26 — Concepts du Voyou classique (4 essais)
- Script : `tools/gen3d/batches/2026-10-26_concepts_voyou.sh`. Animagine XL 4.0, 1024×1024, réglages habituels. Sans arme (couteau et barre seront modélisés à part), pose en T, visages entièrement générés.
- Prompt commun : `1boy, solo, male focus, adult, young adult man, 25 years old, street thug, delinquent, hostile expression, scowl, realistic body proportions, long legs, sneakers, worn clothes, urban streetwear` + morphologie et tenue + pose et style + `safe`. Négatif : armes, militaire, police, logos et texte, surnaturel, caricature, planches, anatomie, ombres.

| Seed | Demandé | Obtenu | Constats (vérifiés en pleine résolution) |
|---|---|---|---|
| 11101 | Mince, crâne rasé, veste en cuir, sweat gris | Conforme | 2 bras, 2 mains, fond uni, sans ombre. Veste ouverte qui s'écarte en pans (à surveiller en 3D) |
| 11102 | Trapu, crâne rasé, barbe, doudoune sombre, jean | Conforme | 2 bras, 2 mains, fond uni, sans ombre. Silhouette la plus lisible, la plus « doudoune sombre » du bestiaire |
| 11103 | Filiforme, capuche relevée, sweat marine | Capuche, visage entièrement noir | **Faux texte sur le sweat** (malgré le négatif), ligne de sol, pas de visage : à écarter pour la 3D |
| 11104 | Athlétique, cheveux bouclés, blouson en cuir marron, jean déchiré | Conforme | 2 bras, 2 mains. **Ombre au sol** sous les pieds (à repeindre avant TripoSG, comme pour le Chimiste) |

En attente du choix de l'auteur.
