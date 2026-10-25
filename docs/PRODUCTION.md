# RED MET — Journal de production

Choix, prompts, seeds, nombre d'essais et problèmes rencontrés, phase par phase.
Pipeline visé : génération 3D locale → Blender (nettoyage, squelette, animation, rendu toon) → spritesheets PNG → Unity.

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
- Outil du dépôt : `tools/gen3d/` (`generate.py` en bibliothèque standard, `backends.json`, `backends/triposg_runner.py`, `concept.py` pour l'API ComfyUI). Nommé `gen3d` plutôt que `hunyuan` puisque le générateur a changé.
