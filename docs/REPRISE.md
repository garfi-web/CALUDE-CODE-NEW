# RED MET — Reprendre le travail

Ce dépôt contient le projet Unity complet (`Assets/` avec les `.meta`, `ProjectSettings/`, `Packages/`), les fichiers Blender de travail (`ArtSource/Blender/`), les outils de génération (`tools/gen3d/`) et la documentation (`docs/`).
Ne sont **pas** dans le dépôt : `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `Builds/`, les fichiers d'IDE, et les **outils d'IA locaux avec leurs poids** (`C:\RED MET\ai`, environ 23 Go).

État d'avancement et prochaine étape : **`docs/PRODUCTION.md`, section « État d'avancement »**.

| Élément | Valeur |
|---|---|
| Unity | **6000.6.6f1** (Unity 6), via Unity Hub |
| Rendu | URP 2D : `Assets/_Game/Settings/URP2D_RPAsset.asset` + `Renderer2D.asset` |
| Scène de test | `Assets/_Game/Scenes/Arena.unity` |
| Blender | 5.2.2 LTS, addon « Interface: Blender MCP » |
| Fichier Blender de travail | `ArtSource/Blender/REDMET_personnages.blend` |
| Générateur 3D | TripoSG, dans `C:\RED MET\ai\TripoSG` |
| Images de concept | ComfyUI + Animagine XL 4.0, dans `C:\RED MET\ai\ComfyUI` |
| Branche de travail | `claude/jolly-wright-b7a83l` |

## 1. Récupérer le projet

Sur le PC habituel (`C:\Users\Home\Unity user templates\REDMET`), un `git pull` suffit. Sur une autre machine :

```bash
git clone -b claude/jolly-wright-b7a83l https://github.com/garfi-web/CALUDE-CODE-NEW.git REDMET
```

## 2. Ouvrir Unity

1. Unity Hub → **Add → Add project from disk** → le dossier `REDMET`, avec l'éditeur **6000.6.6f1**. La première ouverture reconstruit `Library/` (plusieurs minutes).
2. Ouvrir `Assets/_Game/Scenes/Arena.unity`, puis **Play**. Pour reconstruire la scène : **RED MET → Construire l'arène prototype**.
3. **N'ouvrir qu'un seul projet Unity à la fois** : le MCP Unity se connecte à un seul éditeur et peut basculer sur le mauvais après une recompilation.

## 3. Ouvrir Blender

1. Ouvrir `ArtSource/Blender/REDMET_personnages.blend` : une collection par personnage, déjà mise à l'échelle.
2. Démarrer le serveur MCP : panneau MCP de la vue 3D (touche N) → **Start MCP Server**.
3. L'addon est en retard d'une version (protocole 13 au lieu de 15). Il fonctionne, mais pour le mettre à jour : `uvx mcp-for-blender install-addon`, redémarrer Blender, puis **Start MCP Server**.
4. Les `.blend` restent dans `ArtSource/`, jamais dans `Assets/` : Unity tenterait de les importer.

## 4. Relancer les outils de génération

### Pourquoi pas Hunyuan3D 2.1

Les sources sont dans `C:\RED MET\Hunyuan3D-2.1 source code`, mais **Hunyuan3D n'est pas installé et ne doit pas l'être** : sa licence exclut l'Union européenne, y compris pour l'usage de ses sorties, et la carte graphique (8 Go) est sous son minimum (10 Go). Il est remplacé par **TripoSG** (licence MIT). Détails : `docs/PRODUCTION.md`, phase 0.

### TripoSG (image → modèle 3D)

- Environnement : `C:\RED MET\ai\TripoSG\.venv` (Python 3.10, torch 2.5.1 + CUDA 12.4). Versions exactes : `tools/gen3d/requirements-triposg.lock.txt`.
- Poids : `C:\RED MET\ai\TripoSG\pretrained_weights\TripoSG` (7,5 Go, retéléchargés automatiquement s'ils manquent).
- **Arrêter ComfyUI avant** : les deux ne tiennent pas ensemble dans 8 Go de VRAM. Unity et Blender peuvent rester ouverts (pic de 5,4 Go pour TripoSG).
- Commande, depuis la racine du dépôt :

```bash
"C:/RED MET/ai/TripoSG/.venv/Scripts/python.exe" tools/gen3d/generate.py --character Nathan_R0 --image "Assets/_Game/Art/Characters/Nathan_R0/Concept/<concept>.png" --seed 42 --faces 40000
```

Sortie : `Assets/_Game/Art/Characters/<Perso>/Source/<Perso>_triposg_s<seed>.glb`, avec un `.json` qui note l'entrée, la seed et les réglages. Options utiles : `--name` (préfixe), `--bg-tolerance` (fond à reflets).

### ComfyUI + Animagine XL 4.0 (images de concept)

- Environnement : `C:\RED MET\ai\ComfyUI\.venv` (Python 3.13, torch cu130). Modèle : `models/checkpoints/animagine-xl-4.0-opt.safetensors`, rangé avec sa licence.
- Démarrer le serveur :

```bash
cd "C:/RED MET/ai/ComfyUI" && .venv/Scripts/python.exe main.py --listen 127.0.0.1 --port 8188 --disable-auto-launch
```

- Générer : `"C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" tools/gen3d/concept.py --character <Perso> --seeds 1 2 3 --prompt "..."`. Les lots déjà faits sont reproductibles avec `tools/gen3d/batches/*.sh`.
- **Règle de l'auteur** : ne jamais dériver de nouvelles images d'un concept validé (pas d'img2img enchaîné) ; les corrections se font dans Blender.

### Réinstaller sur une nouvelle machine

```bash
git clone https://github.com/VAST-AI-Research/TripoSG.git "C:/RED MET/ai/TripoSG"
cd "C:/RED MET/ai/TripoSG" && uv venv .venv --python 3.10 && uv pip install --python .venv/Scripts/python.exe -r "<dépôt>/tools/gen3d/requirements-triposg.lock.txt" --index-url https://download.pytorch.org/whl/cu124 --extra-index-url https://pypi.org/simple
git clone https://github.com/comfyanonymous/ComfyUI.git "C:/RED MET/ai/ComfyUI"
cd "C:/RED MET/ai/ComfyUI" && uv venv .venv --python 3.13 && uv pip install --python .venv/Scripts/python.exe -r "<dépôt>/tools/gen3d/requirements-comfyui.lock.txt" --index-url https://download.pytorch.org/whl/cu130 --extra-index-url https://pypi.org/simple
```

Puis remettre `animagine-xl-4.0-opt.safetensors` dans `ComfyUI/models/checkpoints/` (Hugging Face : `cagliostrolab/animagine-xl-4.0`). Ne jamais télécharger RMBG-1.4 (non commercial).

## 5. Relancer les MCP

Les serveurs sont déclarés dans la configuration utilisateur de Claude Code (`~/.claude.json`), pas dans le dépôt.

| Serveur | Commande | Côté application |
|---|---|---|
| `unity-mcp` | `C:\Users\Home\.unity\relay\relay_win.exe --mcp` | Fourni par le paquet `com.unity.ai.assistant`, démarre avec l'éditeur |
| `blender` | `uvx mcp-for-blender` | Blender : **Start MCP Server** |

Sur une nouvelle machine :

```bash
claude mcp add --scope user unity-mcp -- "C:\Users\Home\.unity\relay\relay_win.exe" --mcp
claude mcp add --scope user blender -- uvx mcp-for-blender
```

Vérifier dans Claude Code : `/mcp` (état et reconnexion). Unity se déconnecte à chaque recompilation, c'est normal : attendre quelques secondes, puis reconnecter. Le relais s'arrête aussi seul après 3 minutes sans requête (« Auto-shutdown timer fired ») : la requête suivante le relance, il suffit parfois de la refaire. Le journal de l'éditeur est dans `Logs/Editor.log` (dans le projet). Si le mode Play ne progresse pas quand Unity est en arrière-plan, remettre la fenêtre Unity au premier plan.

## 6. Sauvegarder ce qui n'est pas sur GitHub

`tools/backup_local.ps1` copie vers un disque externe ou OneDrive la configuration de Claude Code (`.claude.json`), l'historique des conversations (`.claude`) et, avec `-WithModels`, les outils d'IA (`C:\RED MET\ai`, environ 23 Go). Le jeton de connexion n'est pas copié, sauf avec `-IncludeCredentials`.

```powershell
powershell -ExecutionPolicy Bypass -File tools\backup_local.ps1 -Destination E:\
powershell -ExecutionPolicy Bypass -File tools\backup_local.ps1 -Destination "$env:OneDrive\Sauvegardes" -WithModels
```

## 7. Reprendre la session Claude

```bash
cd "C:\Users\Home\Unity user templates\REDMET"
claude --continue    # reprend la dernière conversation de ce dossier
claude --resume      # ou choisir dans la liste
```

L'historique des conversations reste sur ce PC (`~/.claude/projects/`).

**Phrase à taper pour reprendre là où on s'est arrêté :**

> Lis CLAUDE.md (règle « valider avant de produire »), docs/REPRISE.md (sections 7 et 8) et la section « État d’avancement » de docs/PRODUCTION.md. Vérifie que le MCP Unity est connecté au projet REDMET et que la console compile, puis que le MCP Blender répond (fichier ArtSource/Blender/REDMET_personnages.blend ouvert). Reprends la PROCHAINE ÉTAPE PRIORITAIRE en tête de la section 8 de docs/REPRISE.md : comparer 3 méthodes de poing (A main CC0 Human Base Meshes, B poing stylisé anime, C poing 2D illustré), un exemple de chacune (face, 3/4, profil) à côté d'une photo de référence, et corriger la posture de repos ; je te donne mon choix ou mes corrections, et tu ne passes au palier suivant qu'après mon « OK ».

L'arène de test se lance avec **Play** (mannequin au lancement) ; les touches **1 à 8** choisissent la vague (8 : Nathan M4 et le fusil à pompe), F1 à F4 le debug, F5 à F9 l'état de la partie (détail dans le `README.md`).

## 8. Programme à venir (dans l'ordre)

### DÉCISIONS DE L'AUTEUR SUR L'ÉTUDE 2,5D (2026-10-30) et ordre de travail

- **Cadrage : 3/4 surélevé de 18° et tourné de 30°.** **Conversion : oui, l'Acte I d'abord, après avoir unifié le code du combat de Nathan.**
- **Avant de rendre toutes les animations : qualité des personnages**, sur Nathan M0 et un Voyou seulement, en 3/4 : projection des concepts validés sur les modèles (dépliage UV propre), calque de visage anime 2D par personnage qui suit la tête à chaque image (yeux avec reflet, sourcils, bouche ; lunettes et acné pour Nathan ; visage visible sous la capuche des Voyous), expressions déclenchées par le jeu (neutre, effort, douleur, rage), double résolution, personnages à environ 35 % de la hauteur de l'écran (caméra et zones ajustées), 4 aplats au lieu de 3, contours intérieurs plus fins, **jamais de passage image par image dans ComfyUI**. Montrer une comparaison avant/après (gros plan sur les visages et en jeu, 60 FPS) et **attendre la validation**.
- **Défauts à corriger** : distinguer Nathan des Voyous (tenue ou liseré, repère discret au sol), sang et morceaux qui tombent sur le sol en 2,5D, flash d'impact limité quand un tir tue plusieurs ennemis. Les 3 bruits manquants attendront la phase son.
- **Nathan M4 refait** (beaucoup plus massif et bestial, torse et dos nus, peau de pierre fissurée, veines rouges, voir `docs/CHARACTERS.md`) : 4 concepts Animagine, anatomie vérifiée, de face et de dos, **attendre le choix de l'auteur** avant le modèle 3D ; ensuite même méthode de qualité, en 3/4.
- **Ordre** : 1) qualité Nathan M0 + Voyou et défauts, concepts de M4 → validation ; 2) unifier le code du combat de Nathan, convertir l'Acte I en 2,5D ; 3) reprendre les tâches en pause (portraits de dos de Mara, visual novel, ciels).

### PROCHAINE ÉTAPE PRIORITAIRE (2026-10-30, 3e retour sur les mains) : 3 MÉTHODES DE POING À COMPARER, PUIS CHOIX DE L'AUTEUR

**Retour de l'auteur** sur `docs/captures/2026-10-30_nathan_m0_v3_vetements_v2.png` : les poings sont fermés et compacts mais pas naturels :
- effet « gants de boxe » (blocs ronds trop gros au bout des manches) ;
- doigts en saucisses (4 boudins identiques, sans phalanges ni articulations) ;
- pouce peu lisible ;
- orientation fausse (face avant vers le spectateur au lieu de l'axe de l'avant-bras) ;
- poignet absent (la main ne s'affine pas, raccord brutal) ;
- bras raides et écartés comme un mannequin.

**À garder** : ventre, manches, baskets (« vont dans le bon sens »).

**Demande** (3e essai sur les mains) : avant de retoucher quoi que ce soit, proposer 2 ou 3 méthodes avec qualité attendue, temps et risques :
- A) main CC0 de qualité (Human Base Meshes) avec vraie topologie de doigts, posée en poing d'après des photos ;
- B) poings stylisés anime façon Dragon Ball FighterZ (phalanges en traits nets, articulations saillantes, pouce dessiné en travers, proportions un peu exagérées mais lisibles) ;
- C) poings 2D illustrés posés sur les sprites, comme prévu pour les têtes.

Pour chacune : un exemple rapide d'un seul poing (face, 3/4, profil) à côté d'une photo de référence d'un vrai poing, ouvert à l'écran ; **attendre le choix de l'auteur**.

**Posture du repos à corriger aussi** : bras plus près du corps, épaules un peu rentrées, mains à moitié fermées (pas en poings) ; poings serrés seulement en garde et pendant les coups.

Historique :

### ÉTAPE PRÉCÉDENTE (2026-10-30, retour de l'auteur sur l'étape 2) : POING FERMÉ, AVANT L'ÉTAPE 3 (TÊTE)

**Refus de l'auteur (deuxième refus sur les mains)**, sur la planche `docs/captures/2026-10-30_nathan_m0_v3_vetements.png` :

> « Gros problème sur l'image des poings fermés : ce n'est pas du tout bon. Un poing doit être compact et solide : les 4 doigts repliés à plat contre la paume, les phalanges alignées en une ligne nette sur le dessus, le pouce replié en travers devant l'index et le majeur, sans creux au milieu, sans doigt qui dépasse, et le poignet dans l'axe de l'avant-bras. Il doit se lire comme un poing au premier coup d'œil, de face, de 3/4 et de profil. Montre-moi d'abord le poing seul en gros plan sous ces 3 angles, et attends mon OK. C'est le deuxième refus sur les mains : si tu ne peux pas atteindre ce résultat en déformant la main MPFB, propose-moi une autre méthode avant d'insister (par exemple une main en poing modélisée à part, ou tirée d'une ressource CC0, comme celles des Human Base Meshes). »

Précisions du même retour : « Le gros plan ne montre pas un poing : les doigts sont à moitié repliés comme une main qui attrape, le pouce pend vers le bas, le centre est creux ; sur le corps, les mains ressemblent à des griffes molles. »
- **N'essaie plus de plier la main MPFB** : modéliser une main en poing dédiée, ou partir d'une ressource CC0 (par exemple les mains des Human Base Meshes), réutilisable pour tous les humains ; **vérifier et montrer la licence avant tout téléchargement** (`docs/LICENCES_TIERS.md`).
- Taille de main cohérente avec un adulte enrobé (un peu plus grosse qu'aujourd'hui, style anime).
- **Palier 1** : le poing seul en gros plan, de face, de 3/4 et de profil, **à côté d'une photo ou d'un dessin de référence de poing fermé**, ouvert à l'écran (`Start-Process`) ; attendre le OK.

**Palier 1 fait (2026-10-30), en attente du OK** : poing modélisé à part, sans téléchargement (`tools/blender/redmet_fist_v3.py`, `build_fist()` : paume, 4 doigts repliés à plat, articulations alignées, pouce en travers devant l'index et le majeur, poignet dans l'axe ; pièces fondues par remaillage en voxels, environ 9 000 faces ; `render_views()` pour les 3 vues). Objet `RM_Poing_v3` (collection `RM_Poing_v3`, x = 60) dans `REDMET_personnages.blend`. Planche : `docs/captures/2026-10-30_poing_v3_palier1.png` (photo de référence « Clenched fist », Genusfotografen / Wikimedia Sverige, CC BY-SA 4.0, comparaison seulement). Palier suivant après le OK : poser le poing sur Nathan (main MPFB masquée, poing attaché à l'os de la main, raccord au poignet sous la manche), main gauche en miroir.

**Poing validé par l'auteur (« oui », 2026-10-30).** Fait ensuite, montré, **en attente du OK avant l'étape 3** : planche `docs/captures/2026-10-30_nathan_m0_v3_vetements_v2.png`.
- Poing posé sur Nathan : `attach_fists("Nathan_M0")` (`redmet_fist_v3.py`) masque la main MPFB (groupe `Main_MPFB`, modificateur « Sans main ») et attache une copie du poing à l'os `hand_l` / `hand_r` (miroir pour l'autre main), contour à 4,2 mm comme les mains.
- `redmet_clothes_v3.py` : ventre qui pousse le sweat et bord-côte rentré dessous ; manches lisses (rayon continu, lissage des bras) ; épaules plus tombantes ; baskets refaites (enveloppe convexe du pied remaillée, sans orteils, bout blanc, semelle blanche à part `make_sole`), bas du pantalon à 10 cm.
- Ordre pour tout recréer : `stance(rig)`, `dress`, `skin`, `attach_fists`, `paint`, `still(..., close=("hand_l", "poing"))`.
- Points faibles signalés à l'auteur : baskets encore trop basses et lisses (lacets peu visibles, limite du bout blanc en escalier) ; ventre encore moins rond que sur le concept.

Rappel du retour, **ensuite, autres écarts avec le concept R0 à corriger** (même planche) :
- Ventre : le ventre rond pousse le sweat et tombe un peu sur la ceinture, comme sur le concept (aujourd'hui le sweat descend droit).
- Manches : plus d'effet de boudins segmentés (« Bibendum ») ; manches amples mais lisses, avec quelques plis.
- Épaules : plus tombantes et arrondies, moins carrées et moins larges.
- Baskets : de vraies baskets montantes avec lacets et une semelle nette, comme sur le concept (pas des chaussons ni des sabots).
- Puis montrer la planche corrigée (concept, face, 3/4, poing) ouverte à l'écran et **attendre le OK avant l'étape 3 (tête)**.

### ÉTAPE 2 (2026-10-30) : NATHAN M0 EN 3D PROPRE, ÉTAPE 2 (VÊTEMENTS) MONTRÉE, REFUSÉE (VOIR CI-DESSUS)

**Où reprendre exactement** : étape 1 (corps nu) validée par l'auteur (« silhouette validée »). Étape 2 faite et montrée : planche `docs/captures/2026-10-30_nathan_m0_v3_vetements.png` (concept R0 s4303, face, 3/4 du jeu, gros plan du poing). **En attente du OK ou des corrections de l'auteur.**
- Tout se recrée par script dans Blender : `exec` de `tools/blender/redmet_characters_v3.py` puis de `tools/blender/redmet_clothes_v3.py` ; `stance(rig)` (bras baissés, poings), `dress("Nathan_M0")` (hoodie, capuche baissée, cordons, cargo à poches, baskets, chacun copié du corps au repos, donc avec ses poids), `skin("Nathan_M0")`, `paint("Nathan_M0", "ArtSource/Blender/Textures/Nathan_M0_v3")` (plis peints en texture : poche kangourou, bord-côte, coudes, aisselles, entrejambe, genoux, chevilles, lacets), `still("Nathan_M0", <dossier>, 1400, close=("middle_02_l", "poing"))`.
- Matériaux : aplats à 4 tons nets (`flat_material`), soleil seul sans ombres portées, rendu « Standard » ; contour en coque inversée, aminci à 35 % sur les mains (groupe `Outline_Poids`).
- Point faible signalé à l'auteur : le poing (doigts serrés, pouce replié sur l'index et le majeur) se lit de côté mais reste mou de 3/4 (creux visible, petit doigt qui dépasse) : les mains de MPFB plient mal en poing. Pistes proposées : modéliser une main-poing dédiée (maillage fermé, échangé avec la main ouverte) ou corriger la forme par une cible de forme sculptée.
- Tête MPFB provisoire sur la planche ; elle est remplacée à l'étape 3 (tête 3D + visage A en texture).

Historique de l'étape 1 :

### ÉTAPE 1 (2026-10-30) : NATHAN M0 EN 3D PROPRE, ÉTAPE 1 (CORPS NU) MONTRÉE, EN ATTENTE DU OK

**Où reprendre exactement** : l'auteur a validé MPFB 2 (ressources de base CC0 seulement ; tête stylisée de Human Base Meshes, CC0, seulement si celle de MPFB est trop réaliste). MPFB 2.0.18 est installé dans Blender (copie du paquet : `C:\RED METi\downloads\mpfb`, empreinte dans `docs/LICENCES_TIERS.md`). Étape 1 faite : corps nu de Nathan M0 (`Nathan_M0_v3_Corps`, collection `Nathan_M0_v3`, x = 50, dans `ArtSource/Blender/REDMET_personnages.blend`), recréable par `tools/blender/redmet_characters_v3.py` (`create_body("Nathan_M0")` : homme, 19 ans, poids 1,0, muscle 0,25, ventre 0,35, 1,70 m). Planche ouverte à l'auteur : `docs/captures/2026-10-30_nathan_m0_v3_corps_nu.png`. **En attente de son OK ou de ses corrections.**
Étapes suivantes, chacune montrée (ouverte à l'écran, `Start-Process`) et validée avant la suivante : 2) vêtements modélisés (sweat noir, cargo gris, baskets d'après le concept R0), plis peints dans la texture, vraies mains (poings fermés nets) ; 3) tête 3D et visage en texture anime d'après la tête A (s9104), une texture par expression ; 4) image fixe de Nathan M0 (face, 3/4, en jeu à côté d'un Voyou). **Rien au-delà de l'image fixe sans l'accord de l'auteur.** Sauvegarde (Blender, Unity, docs, commit, push) à la fin de chaque étape et avant chaque tâche longue. Le prototype jouable reste tel quel en parallèle.

Décision précédente :

**Décision de l'auteur** : refaire les personnages en 3D propre, rendu anime cel-shading de haute qualité proche de Dragon Ball FighterZ. Conditions : un seul corps de base humain réutilisable pour tous les humains (Nathan à tous ses stades, Voyous, Flic, Soldat, Dealer, Chimiste), licence montrée avant téléchargement (CC0 ou compatible usage commercial) ; vraies mains (doigts, poings fermés nets), vêtements modélisés, plis peints dans la texture ; tête 3D avec visage en texture anime (chaque expression ne change que la texture), visage A (s9104) comme modèle ; stades de Nathan (enrobé, amaigri, colosse M4) = variantes du même corps ; d'abord Nathan M0 en image fixe (face, 3/4, en jeu à côté d'un Voyou), puis attendre le OK avant d'animer ; estimations de temps par personnage. Le prototype jouable reste tel quel en parallèle. Plus tard, peut-être un illustrateur pour les portraits du visual novel.
Licences vérifiées le 2026-10-30, **rien n'est téléchargé** : Blender Studio « Human Base Meshes » v1.4.1 (CC0, blender.org/download/demo-files) ; MPFB2 / MakeHuman (code GPL v3, ressources de base CC0 : maillage, cibles, peaux ; les ressources de tiers ont leur propre licence). En attente du choix et de l'accord de l'auteur.


**Retour de l'auteur sur le palier 2 (pas de OK pour le palier 3)** : régler le décalage tête/corps MAINTENANT, sur une seule image fixe. Tête : garder le visage A mais regard horizontal vers l'adversaire (régénérer A, même visage, coiffure, lunettes), tête abaissée avec un vrai cou, proportions d'adulte un peu enrobé, un seul contour continu autour de tout le personnage. Corps : abandonner la texture projetée floue ; aplats francs dans la palette du concept, bords et ombres nets, plis du sweat en contours intérieurs fins ; poings nets et lisibles ; même trait, liseré et peau partout. Puis comparaison avant/après (en jeu 1920×1080 et gros plan). **Si le corps ne peut pas atteindre ce niveau avec la méthode actuelle : le dire et proposer une alternative avant de continuer.**
Réponse de Claude (2026-10-30) : évaluation franche et options en attente du choix de l'auteur (voir la conversation) : la tête et l'assemblage sont faisables ; les poings nets et les plis dessinés ne le sont pas avec le maillage TripoSG (mains en moufles, surface bosselée).


**Palier 1 validé : l'auteur a choisi A (s9104)** (« c'est le mieux »). Palier 2 fait : tête A nettoyée (col effacé, bords coupés des cheveux arrondis), calée sur la tête 3D et traitée comme le corps (`tools/gen3d/heads.py`, `Assets/_Game/Art/Characters/Nathan_M0/Heads/Nathan_M0_head_neutral.png`), posée sur l'image 0 du repos par la piste de l'os de la tête, à côté d'un Voyou (encore avec sa tête 3D, image fixe `Voyou/Stills/`). Outil : **RED MET → Capturer l'essai de tête (palier 2)** (`Editor/HeadStillCapture25D.cs`, ne modifie pas la scène). Captures : `docs/captures/2026-10-30_tete_nathan_palier2_jeu.png` (1920×1080) et `..._grosplan.png`. Palier 3 (autres expressions de Nathan) seulement après le « OK » de l'auteur.

Historique du palier 1 :

**Règle de l'auteur (2026-10-30, aussi dans CLAUDE.md) : VALIDER AVANT DE PRODUIRE.** Petits paliers, un « OK » explicite à chacun, temps et effort du palier suivant annoncés ; deux refus de suite : arrêter et proposer une autre méthode (par exemple un illustrateur).

**Retour de l'auteur (2026-10-30)** : le calque de visage 2D est rejeté (« ovales plats avec des traits simplistes collés sur la tête, sans volume, sans cheveux intégrés, sans rapport avec le style anime des concepts validés ») ; à supprimer. Nouvelle méthode : **têtes illustrées complètes** (visage, cheveux, capuche ou lunettes, haut du cou), Animagine XL + ControlNet Union sur un rendu de la tête 3D au même angle que les sprites (3/4 surélevé de 18°, tourné de 30°), au moins 512 px par tête, détourées avec BiRefNet ; elles remplacent toute la tête du sprite, placées et inclinées à chaque image par l'os de la tête, avec le même traitement que le corps (aplats, liseré côté lumière, contour de même épaisseur, même peau) et un raccord du cou sans cassure. Expressions : neutre, effort, douleur, rage, yeux fermés (K.O.). Nathan : 18-19 ans, cheveux bruns bouclés, lunettes à double foyer, acné, joues rondes. Voyou : visage adulte dur bien visible sous la capuche, 3 variantes.

**Paliers (demande de l'auteur)** : 1) Nathan seul, 3 propositions de tête neutre, en gros plan, sans assemblage ; 2) la tête choisie sur UNE image fixe de Nathan (idle) dans l'arène 2,5D à 1920×1080, à côté d'un Voyou, capture à taille réelle + gros plan ; 3) les autres expressions de Nathan en image fixe ; 4) toute l'animation de Nathan (idle, course, frappe) ; 5) le Voyou (3 visages), en recommençant au palier 1.

**Où on en est : palier 1, 2e essai montré, en attente du choix de l'auteur.** 1er essai : l'auteur a choisi B (s9107) avec corrections (tête redressée, sans col, joues plus rondes et acné visible comme la A) ; B avait le menton hors du cadre, l'inpainting du bas du visage a été raté 3 fois (nouveau visage de face et 2e paire de lunettes, rejets dans `ArtSource/Blender/Heads/fixed/rejet/`), abandonné. Puis l'auteur a écrit « Aucune ne me convient » (1er refus de la méthode ; un 2e refus = arrêter et proposer une autre méthode). 2e essai, corrections intégrées au prompt (`heads.py`) et guidage ControlNet allégé (0,6, fin 0,7 : à 0,75 la tête regardait vers le haut) : 20 têtes, 3 proposées, D s9157, E s9155 (reste de col gris, reflet sur la joue), F s9159, planche `docs/captures/2026-10-30_tete_nathan_essai2.png` ; aussi correctes mais non montrées : s9141, s9148, s9156, s9158. Planche du 1er essai : Planche : `docs/captures/2026-10-30_tetes_nathan_palier1.png` (A s9104, B s9107 retournée vers la droite, C s9111 ; détourées dans `ArtSource/Blender/Heads/propositions/`). 11 têtes neutres générées, 8 écartées : s9102 (visage tourné vers l'arrière, lunettes sous les yeux), s9103 (tête déformée), s9105 (à l'envers), s9106 (yeux masqués), s9108 (lunettes sur la bouche), s9109 (tête couchée), s9110 (lunettes sous l'œil), s9112 (belle mais visage mince et cadrage coupé). Le guide par carte des normales tient mal l'orientation (environ 1 tête sur 4 utilisable) : à signaler si l'auteur refuse.
- Outils : `tools/gen3d/heads.py` (generate / candidates / finish), `tools/blender/redmet_quality.py` (head_zone, set_head_holdout, head_reference, track_heads). Rendus de référence : `ArtSource/Blender/Heads/` (pose de repos, cadre 0,48 m).
- **État laissé, à connaître** : les feuilles `Sprites25D` de Nathan M0 et du Voyou ont été refaites **sans tête** (tête en réserve) avec `heads.json` ; le calque de visage (`FaceLayer25D`, `tools/gen3d/faces.py`, dossiers `Face/`) est encore dans le projet : la scène 2,5D montre donc des corps sans tête avec l'ancien calque, jusqu'au palier 2 (le supprimer et poser la tête). Le fichier Blender enregistré garde les faces de la tête en réserve (matériau 2) ; `set_head_holdout(perso, False)` les remet.
- Déjà produit avant la règle, à NE PAS utiliser avant son palier : autres expressions de Nathan (s9102 à s9104, `ArtSource/Blender/Heads/raw/`) ; têtes du Voyou (`raw/en_attente_palier5/`, visage 1 s9201 réussi, visages 2 et 3 incertains) ; rejets dans `raw/rejet/`.
- Aussi en attente : traitement « comme le corps » (`heads.py finish`) pas encore appliqué ; build 2,5D lourd (1,5 Go, images à recadrer en atlas avant la conversion).

**Nathan M4 : choix de l'auteur = `Nathan_M4_v2_sheet_s7705`** (2026-10-30). Modèle 3D après la validation des têtes (l'auteur a demandé de ne toucher à aucun autre personnage avant).

**Ordre ensuite** : paliers des têtes (Nathan puis Voyou) → M4 s7705 en 3D avec la méthode validée → unifier le code du combat de Nathan, convertir l'Acte I en 2,5D → tâches en pause (portraits de dos de Mara, visual novel, ciels).

### PRIORITÉ ACTUELLE (2026-10-30) : étude et prototype beat'em up 2,5D

Demande de l'auteur du 2026-10-30 : étude et prototype 2,5D (nouvelle scène `Assets/_Game/Scenes/Arena25D.unity`, composants nouveaux, rien de l'existant cassé), puis `docs/ETUDE_25D.md`, commit et push, et **attente de la décision de l'auteur** avant toute autre conversion. Une fois cette étude validée ou tranchée par l'auteur, reprendre les tâches en pause ci-dessous, dans l'ordre et là où elles ont été laissées.

**État (2026-10-30) : fait, EN ATTENTE DE LA DÉCISION DE L'AUTEUR.** Tout est dans `docs/ETUDE_25D.md` (recommandation, mesures, ce que coûterait la conversion). Deux décisions attendues :
- cadrage des sprites (planche `docs/captures/2026-10-30_25d_comparaison_profil_3q.png` : profil, profil surélevé de 18°, ou 3/4 surélevé de 18° et tourné de 30°, conseillé). **Aucune animation n'est rendue avant ce choix** ;
- convertir ou non (conseil : oui, l'Acte I d'abord, après unification du code de combat).

Pour rejouer : **RED MET → Construire l'arène 2,5D (étude)**, puis Play dans `Arena25D.unity` ; ou **RED MET → Build Windows (arène 2,5D)** puis `Builds/Windows25D/REDMET25D.exe` (arguments `-redmet-benchmark-25d`, `-redmet-capture-25d`, `-redmet-showcase-25d`). En arrière-plan, l'éditeur Unity reste figé en mode Play (image 1) : tester avec la fenêtre au premier plan ou par le build. Bruits restant à générer (limite par minute du forfait gratuit d'ElevenLabs) : toit, réception, caisse (`SFX_Toit`, `SFX_Reception`, `SFX_Caisse`).

### TÂCHES EN PAUSE (mises en pause le 2026-10-30, à reprendre dans cet ordre)

1. **Portraits de dos de Mara M-T0** : rejeter s8531 (trois bras) et s8534 (de face), proposer 3 variantes à partir de la pose de s8533 avec vérification de l'anatomie, puis attendre le choix de l'auteur. Laissé ici :
   - **s8551 à s8556 vérifiées le 2026-10-30 : les 6 écartées.** Toutes ont 2 bras, mais aucune n'a les cheveux noirs à mèches rouges : s8551 et s8555 ont les cheveux gris, s8556 bruns, et s8553 et s8554 sont noires en haut mais blanc-lavande sur toute la moitié basse. s8552 a aussi la main droite fondue dans la flamme de l'épée. Chez s8553, la lame est tenue par le tranchant, sans poignée, et la main sur la hanche n'a pas de doigts visibles. Chez s8554, la main droite et la poignée ne forment qu'un bloc rouge illisible ;
   - **s8561 à s8566 générées le 2026-10-30 (mêmes réglages, graines nouvelles), PAS ENCORE VÉRIFIÉES** : les vérifier en premier ;
   - piste si les cheveux restent clairs : le bas des cheveux est éclairci par le contre-jour (`dramatic rim light` dans `STYLE`) et par le guidage canny de s8533, dont le bas des cheveux est déjà rose-rouge. Essayer de retirer le liseré pour la vue `back2` et d'effacer aussi la zone basse des cheveux dans `GUIDES["back2"]` ;
   - nombre d'images écartées à annoncer à l'auteur : 6 (s8551 à s8556), plus celles de s8561 à s8566 qui échoueront ;
   - ensuite : détourer la face (8521 proposée) et le dos choisis, puis refaire le médaillon.
2. **Suite de la refonte de l'écran visual novel** : portraits M-T1 à M-T4 (même méthode, même vérification), interface, décors branchés par scène, test, captures, 60 FPS, commit et push, puis validation de l'auteur (détail plus bas).
3. **Refonte des ciels des 10 niveaux** (lune rouge aux ch. 1 et 10 seulement) : faite (commit `7a53375`), **en attente de la validation de l'auteur** (détail au point 3 ci-dessous).

### PROCHAINE ÉTAPE (sauvegarde du 2026-10-29) : tâches en attente, dans l'ordre

1. **Portraits de dos de Mara M-T0** (retour de l'auteur du 2026-10-29, texte complet ci-dessous) :
   - rejetées : s8531 (trois bras), s8534 (de face) ; s8532 rejetée aussi, car le visage ne se voit presque pas ;
   - base retenue par l'auteur : **s8533**. Vérifiée en pleine résolution : deux bras et deux mains (gauche sur la hanche, droite sur la poignée), doigts gantés et repliés, donc non comptables. Défauts : cadre rouge rectangulaire autour de l'épée, lame sombre au lieu de la lame d'énergie rouge, bas des cheveux rose-rouge sous le contre-jour ;
   - `vn_portraits.py`, vue `back2` : squelette relevé sur s8533 (`SKELETONS["back2"]`), cheveux noirs pondérés et négatif violet, bleu ou rose, plus un guidage canny par la silhouette de s8533 sans la zone de l'épée (`GUIDES["back2"]`, force 0,5, fin 0,5) ;
   - 1er essai sans guidage (s8541 à s8546) : 4 écartées (s8541, s8544 et s8545 de face ; s8543 avec une 2e épée qui flotte). s8542 et s8546 sont de dos mais l'épée est levée, loin de la pose de s8533 ; anatomie non vérifiée en détail ;
   - 2e essai avec guidage (**s8551 à s8556**) : généré, **PAS ENCORE VÉRIFIÉ**. À faire : contrôler chaque image en pleine résolution (2 bras, 2 mains, 5 doigts, aucun membre en trop ou fusionné, aucun objet qui traverse le corps, vraie vue de dos tête tournée, cheveux noirs à mèches rouges), relancer avec d'autres graines celles qui échouent, montrer **3 variantes** avec le nombre d'images écartées, puis attendre le choix de l'auteur ;
   - ensuite : détourer (`vn_cutout.py`) la face choisie (8521 proposée) et le dos choisi, refaire le médaillon.
2. **Suite de la refonte de l'écran visual novel** (demande du 2026-10-28, texte complet plus bas) : portraits M-T1 à M-T4 avec la même méthode et la même vérification d'anatomie, interface, décors branchés par scène, test, captures, 60 FPS, commit et push, puis attente de la validation.
3. **Refonte des ciels des 10 niveaux** (lune rouge aux ch. 1 et 10 seulement, un ciel propre à chaque chapitre de 2 à 9) : **faite le 2026-10-29** (commit `7a53375`) et **en attente de la validation de l'auteur**. Planches : `docs/captures/2026-10-29_*`. Retouches possibles : fissures rouges sur les décors du visual novel des ch. 2, 4, 5, 6 et 8 ; bande douce en haut des couches de paysage de l'arène ; ciel qui suit le chapitre de la partie.

**Règle de l'auteur (2026-10-29)** : avant de montrer un portrait, vérifier soi-même l'anatomie (2 bras, 2 mains, 5 doigts par main, aucun membre en trop ou fusionné, aucun objet qui traverse le corps), relancer avec une autre graine toute image qui échoue, et dire combien ont été écartées.

**Requête de l'auteur sur les portraits de dos (2026-10-29, texte complet)** :
> Retour sur la planche des portraits de dos (planche_dos_v2.png) :
> - Image 1 (s8531) : Mara a TROIS bras (un bras ganté rouge sur la poitrine, un sur la hanche, un qui tient l'épée). À rejeter.
> - Image 4 (s8534) : elle est de face, pas de dos. À rejeter pour la scène de fin.
> - Image 2 (s8532) : on ne voit presque pas son visage, alors que la scène demande qu'elle regarde Nathan par-dessus l'épaule.
> - Image 3 (s8533) : c'est la meilleure base (de dos, tête tournée, sourire moqueur, corset lacé, épée). Vérifie qu'elle a bien deux bras et deux mains complètes.
> Les cheveux tirent parfois sur le violet (images 1 et 4) : garde le noir avec les mèches rouges du concept validé.
>
> À partir de maintenant, avant de me montrer un portrait, vérifie toi-même l'anatomie : 2 bras, 2 mains, 5 doigts par main, aucun membre en trop ou fusionné, aucun objet qui traverse le corps. Relance avec une autre seed toute image qui échoue, et indique-moi combien ont été écartées.
> Propose-moi 3 nouvelles variantes de dos à partir de la pose de la 3 (s8533), puis attends mon choix.

### Refonte de l'écran visual novel (demandée le 2026-10-28)

**Ordre de travail** : 1) l'auteur valide les portraits de Mara M-T0 ; 2) portraits M-T1 à M-T4, même méthode ; 3) interface ; 4) décors branchés par scène ; 5) test, captures, 60 FPS, commit et push, puis attente de la validation.

**Décisions déjà prises par l'auteur** :
- **Modèles** : ControlNet Union SDXL de xinsir (Apache 2.0) et BiRefNet (MIT). Les deux sont téléchargés et figés dans `C:\RED MET\ai`, hors dépôt.
- **Test des choix** : dans un nœud séparé `test_choix`, aux options marquées PROVISOIRE, joué après `ch01_mara_ouverture`. Jamais de choix ajouté dans les scènes de l'auteur.

**Où on en est** (`docs/PRODUCTION.md`, dernière entrée, 2026-10-29) :
- Portraits M-T0 : 1re série montrée à l'auteur, qui a demandé de corriger les sangles, la mèche blanche et la lame qui flotte, et de relancer le dos. **2e série faite, en attente de son choix** : face graine 8521 conseillée, mais elle ajoute une cape et un col rouges (`planche_expressions_v2_s8521.png`) ; dos 8531 ou 8532 (`planche_dos_v2.png`). Détourer ensuite les graines choisies (`vn_cutout.py`) et refaire le médaillon.
- Méthode : squelette OpenPose et ControlNet ; le rendu du maillage 3D est inutilisable (cheveux fusionnés aux bras).
- **Ciels refaits sur demande de l'auteur (2026-10-29), en attente de validation** : lune rouge aux ch. 1 et 10 seulement, un ciel par chapitre de 2 à 9 (`SkyProfile`, `Data/Sky/`, F12 dans l'arène), décors du visual novel ch. 2 à 9 repeints, 60 FPS mesurés sur l'orage et les aurores. Planches : `docs/captures/2026-10-29_*`. Consigne complète : ci-dessous.
- La police M PLUS Rounded 1c (OFL) est installée.
- L'interface n'est pas commencée.

**Outils** (ComfyUI à démarrer d'abord, section 4) :
```bash
"C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/vn_portraits.py --stage T0 --seeds 8502
"C:/RED MET/ai/BiRefNet/.venv/Scripts/python.exe" -I tools/gen3d/vn_cutout.py ArtSource/VN/Mara/T0/<brut>.png
"C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/vn_backgrounds.py ch01
"C:/RED MET/ai/ComfyUI/.venv/Scripts/python.exe" -I tools/gen3d/skies.py sky ch05   # ciel du jeu (aussi : vn ch03, neutral)
Builds/Windows/REDMET.exe -redmet-benchmark-sky                                  # 60 FPS orage et aurores (benchmark_sky.txt)
```
Libérer la mémoire vidéo de ComfyUI avant BiRefNet : `curl -X POST http://127.0.0.1:8188/free -d '{"unload_models":true,"free_memory":true}'`.

**Requête de l'auteur sur les ciels (2026-10-29, texte complet ; faite, en attente de validation)** :
> Refonte des ciels des 10 niveaux du monde parallèle. Problème : avec une lune rouge partout, les joueurs croiront que c'est toujours le même décor avec seulement des bâtiments différents. Ce n'est pas l'effet recherché.
>
> 1. Règle
> - La lune rouge est réservée au ch. 1 (première incursion) et au ch. 10 (dernier niveau, que Nathan soit devenu vampire en ayant pris la Red Met à chaque fois, ou qu'il se batte encore avec ses poings et ses pieds). Ces deux niveaux doivent se répondre.
> - Les ch. 2 à 9 ont chacun leur propre ciel, sans lune rouge : ch. 2 : ciel nuageux lourd, gris-vert maladif, bruine (la peste) ; ch. 3 : coucher de soleil chaud et brumeux, orange et rose, dans l'esprit de la Nouvelle-Orléans d'inFAMOUS 2 ; ch. 4 : ciel dégagé, bleu lumineux, grand soleil, quelques nuages blancs ; ch. 5 : nuit avec aurores boréales vertes et bleues, et deux lunes pâles ; ch. 6 : orage violent, éclairs ; ch. 7 : aube blafarde, brouillard épais ; ch. 8 : crépuscule violet et toxique, néons de la ville réelle qui envahit le monde ; ch. 9 : ciel d'incendie, colonnes de fumée, pluie de cendres.
> - L'identité Red Met reste visible dans tous les niveaux par un détail discret et constant : les fissures rouges lumineuses du sol, jamais par le ciel.
>
> 2. Technique
> - Crée un profil de ciel par chapitre (ScriptableObject) : couches de ciel en parallaxe, astres (soleil, lunes), éléments animés (aurores qui ondulent, nuages qui dérivent, éclairs avec flash d'éclairage, reflets de soleil, brouillard qui roule, cendres), couleur et intensité de la lumière globale 2D, couleur du brouillard, réglages de post-traitement.
> - La lumière des personnages s'accorde au ciel (liseré orange au coucher de soleil, bleu-vert sous les aurores, blanc sous le soleil) pour qu'ils restent lisibles, même sur un ciel clair.
> - Peins les ciels avec ComfyUI et Animagine XL, en anime HD cohérent avec les décors existants. Mes images de référence (Skyrim, inFAMOUS 2) servent uniquement d'inspiration, pas d'entrée.
> - Reprends les 10 fonds actuels : garde l'architecture quand elle est bonne, mais refais les ciels et les couleurs des ch. 2 à 9, qui sont aujourd'hui tous rouges.
>
> 3. Test
> Montre-moi une planche comparative des 10 ciels (avant/après), puis une capture en jeu avec Nathan pour les ch. 3, 4 et 5. Vérifie les 60 FPS sur l'orage (éclairs) et les aurores. Commit et push, puis attends ma validation.

**Requête de l'auteur sur l'écran visual novel (texte complet)** :
> Refonte de l'écran visual novel : le résultat actuel ne me convient pas (concept en pose en T dans un cadre gris, posé sur l'écran de combat). Je veux un vrai visual novel, sur le modèle de mise en page des visual novels anime modernes, aux couleurs de RED MET (noir, rouge, touches de blanc). Ne reprends que la mise en page, pas le style des personnages du modèle : Mara reste une guerrière adulte avec ses tenues M-T0 à M-T4.
>
> 1. Mise en scène
> - Pendant un visual novel : le combat est en pause, et le HUD (barres de vie, aide des touches) est caché.
> - Fond plein écran : un décor illustré propre à chaque scène (plaine de la lune rouge, ruines, salle du trône, etc.), peint avec ComfyUI et Animagine XL dans le style des décors déjà validés, légèrement flouté pour faire ressortir le personnage.
>
> 2. Portraits de Mara
> - Nouvelles illustrations dédiées au visual novel, pas les concepts en pose en T : pose naturelle et expressive (bras croisés, main sur la hanche, lame posée sur l'épaule, penchée en avant), cadrage aux genoux, grande taille (environ 85 % de la hauteur de l'écran), fond transparent (détourage propre, sans halo).
> - Pour chaque stade de tenue (M-T0 à M-T4) : une vue de face et une vue de dos (scène de fin), et au moins 5 expressions (moqueuse, méprisante, amusée, troublée, en colère).
> - Respiration légère et apparition en fondu glissé ; l'expression change via des tags Ink (par exemple #expr:moqueuse, #pose:dos).
> - Montre-moi d'abord les portraits du stade M-T0 pour validation avant de produire les autres.
>
> 3. Interface (1920x1080, mise à l'échelle propre)
> - Boîte de dialogue arrondie en bas, noire semi-transparente avec un liseré rouge ; étiquette au nom du personnage en pilule rouge qui chevauche le haut de la boîte ; texte qui s'affiche lettre par lettre (vitesse réglable) avec un indicateur « suivant ».
> - Choix : boutons en pilule empilés à droite, l'option sélectionnée surlignée en rouge, navigation au clavier, à la souris et à la manette.
> - Carte du personnage en haut à gauche : médaillon rond du visage, nom, titre (« Guerrière de la Red Met »). N'affiche aucun score moral ni d'affection chiffrée (règle du GDD).
> - Boutons ronds autour de la boîte : historique et réglages à gauche ; lecture automatique, avance rapide et sauvegarde à droite.
> - Police lisible de style anime, avec licence libre (OFL) et accents français ; indique-moi la police choisie.
>
> 4. L'Avocat garde son propre style d'intervention (surimpression déformée), distinct de l'écran de Mara.
>
> 5. Test : la scène ch01_mara_ouverture avec un choix et la scène de fin de dos, en 1920x1080, captures avant/après, 60 FPS. Commit et push, puis attends ma validation.

### Dialogues de l'auteur (étape 3 du prototype narratif, en cours en parallèle)

**État (2026-10-28)** :
- **Plan des 10 chapitres validé** (`docs/SCENARIO.md`). Boss du ch. 1 : **le Chœur des rieurs** (fiche « B1 » dans `docs/ENEMIES.md`, entrée dans `docs/BESTIARY.md`).
- **Étape 2 (état de la partie et sauvegarde) refaite sur le scénario** : catalogue des scènes et des actes importé de `SCENARIO.md`, **doses payées par des actes immoraux** (tableau dans `docs/GDD.md`), argent des activités du réel (il ne paie jamais une dose), scène en cours, sauvegarde en version 2 (détail et tests : `docs/PRODUCTION.md`, dernières entrées). Barème des axes et valeurs de puissance validés par l'auteur pour commencer.
- **Dialogues** : l'auteur les écrit lui-même et les dépose dans `docs/source/dialogues/chNN.md` (mode d'emploi et liste des 143 identifiants : `docs/source/dialogues/LISEZMOI.md`). Quand il dit qu'ils sont déposés : conversion en Ink **mot pour mot** ; seule l'orthographe est corrigée, et chaque correction est signalée ; identifiant de voix `<scène>_NNN` ; liste des scènes encore sans dialogue ; **aucune réplique inventée**, texte « PROVISOIRE » à la place.
- **Étape 3 commencée (2026-10-28)** : module Mara V4 de l'auteur adapté et intégré (`Assets/_Game/Story/Ink/mara.ink`, paquet Ink 2.0.0, 317 voix numérotées, lecteur de visual novel, F10 et F11). Reste à écrire par l'auteur : la liste « À RÉÉCRIRE », « À ÉCRIRE » et « PROVISOIRE » de `docs/source/dialogues/LISEZMOI.md` (section 4). Quand il dépose des dialogues : conversion mot pour mot, `RED MET → Numéroter les voix Ink`, compilation (`InkVoiceTagger.CompileReport`), test F10 et F11.

### Intégration du scénario (consigne de l'auteur, 2026-10-28, texte complet ; faite)

Sources : `docs/source/RED_MET_10_chapitres.md` et `docs/source/RED_MET_GDD_v0.1.md` (copies conformes des documents de l'auteur, non retouchées).

> Lis entièrement docs/source/RED_MET_10_chapitres.md et docs/source/RED_MET_GDD_v0.1.md.
> a) Mets en cohérence docs/GDD.md, docs/CHARACTERS.md, docs/ENEMIES.md et docs/BESTIARY.md avec ces sources, en gardant les décisions déjà prises : 10 niveaux et jusqu'à 10 doses, tenues de Mara selon les doses (0 = M-T0, 1-2 = M-T1, 3-4 = M-T2, 5-6 = M-T3, 7 et plus = M-T4), visual novels d'ouverture et de fin, L'Avocat, Dealer au fusil à pompe, pas de choix de difficulté dans le menu. Sur la voie sobre, Nathan ne se bat qu'avec les poings et les pieds, avec une santé faible, mais il se déplace et saute normalement.
> b) Progression des armes par doses : lames, armes à feu, lance-flammes et lance-scie, lance-roquettes et grenades, pouvoirs vampiriques ; place le fusil à pompe (stade M4) dans cette progression. Démembrements sanglants sur la voie Red Met, avec le réglage de gore.
> c) Les trois demandes du Chimiste et la scène de culpabilité avant chaque dose, comme dans le GDD v0.1 section 10, mises en scène non explicites.
> d) Fin : voie immorale = section 11.1 du GDD v0.1 (scène coupée avant l'acte, sans nudité, centrée sur la réaction de Mara) ; voie morale = section 11.2 ; puis yeux révulsés, chute, hôpital, coma et « RED MET 2 » (section 12).
> e) Crée docs/SCENARIO.md : pour chacun des 10 chapitres, le déroulé réel et parallèle, les choix et leurs conséquences, les scènes de Mara en ouverture et en fin de niveau, les interventions de L'Avocat, les ennemis et la dose proposée, avec un identifiant par scène pour les fichiers Ink. Pas encore de dialogues complets. Montre-moi les chapitres 1 à 3 avant de rédiger la suite.

**État** : points a) à e) faits et **plan des 10 chapitres validé par l'auteur** (2026-10-28). Seuils de Mara, de Nathan (M0 à M4) et du réel (R0 à R4) à 0 / 1 / 3 / 5 / 7 doses et condition de la fin immorale reportés dans le code et `Progression.asset`.

**Décisions de l'auteur du 2026-10-28** (elles remplacent celles de l'étape 1 du prototype narratif, commit `1f50321` ; déjà notées dans `docs/GDD.md`) :
- **Seuils des tenues de Mara : 0 / 1-2 / 3-4 / 5-6 / 7+** (M-T0 à M-T4), car le jeu a 10 niveaux et jusqu'à 10 doses. Reportés dans `docs/CHARACTERS.md`, `ProgressionTable.cs` et `Data/Story/Progression.asset` (0, 1, 3, 5, 7). Seuils de Nathan M0 à M4 et R0 à R4 alignés dessus (décision du 2026-10-28, reportée dans le code) ; fusil à pompe et pouvoirs vampiriques à 7 doses.
- **Fin immorale** si au moins 6 doses sur 10 **et** au moins 2 des 3 grandes demandes acceptées (humiliation, cœur de Marc, enfant).
- **Fin de la voie immorale : section 11.1 du GDD v0.1**, qui remplace la nuit consentie. **L'emplacement vide pour un illustrateur est supprimé.** Scène coupée avant l'acte, sans nudité, centrée sur la réaction de Mara.

L'étape 2 du prototype narratif (état de la partie et sauvegarde, commit `0eb0ce6`, reprise le 2026-10-28 sur le scénario) attend la validation de l'auteur (valeurs de puissance et prix des doses dans `docs/PRODUCTION.md`).

1. **Fin de la phase B** du bestiaire — *fait* (commit `d1abe6c`).
2. **Sauvegarde** — *faite*.
3. **Phase combat dynamique** (Dragon Ball FighterZ sans les super-pouvoirs, hitstop, sang au couteau) — *faite* (commit `9766bed`).
4. **Tranche verticale** Nathan M0 contre le Voyou, graphismes définitifs — *faite et validée par l'auteur le 2026-10-27* (commits `ccee514` à `5bddd2b`, puis la sauvegarde de fin de session) : cel-shading, allonge des coups, mannequin, décor peint (ComfyUI + Animagine), pose de mort, liseré rouge, 60 FPS mesurés dans un build Windows. Dernières retouches demandées après validation : sol redessiné en toon et crochet allongé (0,9 m) — faites et vérifiées en Play le 2026-10-27 (le crochet touche à 0,87 m dès sa 1re image active). Détail : `docs/PRODUCTION.md`, section « Tranche verticale ».
5. **Phase C** (reste de l'Acte I, même méthode) — **en cours**. Un ennemi à la fois :
   - **Flic corrompu terminé** : concept 13103, 1,85 m, phase D faite (armes, animations de tir avec annonce, sprites), vague 3 de l'arène.
   - **Dealer lieutenant terminé** : 1re série (14101 à 14104) rejetée, 2e série choisie : **14201** (africain à la peau noire foncée, cicatrices, chaîne et dent en or). Phase D faite avec le **fusil à pompe** (annonce, mort instantanée à bout portant, corps pulvérisés), **Nathan M4** et son fusil (touche U, vague 8), 60 FPS mesurés dans un build Windows. Détail : `docs/PRODUCTION.md`.
   - Ensuite le Soldat privé et le Mutant, un à la fois, avec arrêt après chaque série de concepts pour le choix de l’auteur, **après le prototype narratif** (point 6).

   Tailles de tous les ennemis : `docs/ENEMIES.md`, section « Tailles ».

6. **Prototype narratif** (demandé le 2026-10-27, après le Dealer et le fusil à pompe) — **en cours** : étape 1 (`docs/GDD.md`, avec le complément « Mara dans les visual novels ») validée par l’auteur le 2026-10-28, puis modifiée le même jour : seuils de Mara 0 / 1-2 / 3-4 / 5-6 / 7+ et fin immorale de la section 11.1 du GDD v0.1, sans emplacement pour un illustrateur (voir « PROCHAINE ÉTAPE » ci-dessus). Portraits de dos de Mara : rendu Blender du modèle 3D, puis ComfyUI + Animagine avec ControlNet pose et contours ; montrer un essai M-T0 à l’auteur avant les autres stades. Étape 2 (état de la partie et sauvegarde) faite le 2026-10-28, en attente de validation. Six étapes, avec arrêt pour validation de l’auteur à la fin de chaque étape : design dans `docs/GDD.md`, état de la partie et sauvegarde, dialogues visual novel avec Ink, interventions de l’Avocat, confirmation d’âge et fin de la section 11.1 (aucune image de nudité générée par Claude), mini-chapitre joué deux fois. Texte complet des deux requêtes en fin de fichier.

Piège : l'outil `RunCommand` du MCP Unity refuse `File.Delete` et `AssetDatabase.DeleteAsset` (« user interaction ») ; pour mesurer les FPS, ne lancer aucune commande MCP pendant l'enregistrement (la compilation du script de test fausse la mesure).

Outils de la tranche : `tools/blender/redmet_pipeline.py`, `tools/blender/redmet_animation.py` et `tools/blender/redmet_props.py` (mannequin, pistolet, matraque ; `cop_clips` de `redmet_animation.py` pour les clips d'un tireur), `tools/gen3d/background.py` (décor peint, ComfyUI doit tourner), menu Unity **RED MET → Build Windows** et banc d'essai `Builds/Windows/REDMET.exe -redmet-benchmark` (rapport `benchmark.txt` à côté de l'exécutable) ; (à exécuter dans Blender par le MCP, avec `exec(open(<chemin>, encoding="utf-8").read())`), `tools/gen3d/remove_floor_line.py`, menu Unity **RED MET → Importer les sprites des personnages**.

### Requête de l'auteur : phase combat dynamique (texte complet)

> APRÈS la phase B et sa sauvegarde, avant la phase C : PHASE COMBAT DYNAMIQUE.
> Objectif : des combats ultra dynamiques avec le punch, l'impact et la vitesse d'exécution de Dragon Ball FighterZ, SANS reprendre ses super-pouvoirs (pas de téléportation « vanish », pas de rayons d'énergie, pas de transformations). Tout doit marcher dès maintenant avec les personnages provisoires, et s'appliquer ensuite aux vrais sprites. Respecte CLAUDE.md et garde 60 FPS stables.
>
> 1. Données d'attaque
> - Crée un ScriptableObject AttackData : démarrage, frames actives, récupération (en images à 60 FPS), dégâts, hitstop, intensité de tremblement, type de recul (léger, lourd, projection en l'air, rebond au mur, rebond au sol), fenêtre d'annulation vers l'attaque suivante.
> - Fais passer le combo de Nathan, et les attaques des ennemis de la phase B, sur ce système.
>
> 2. Sensation d'impact
> - Hitstop : les deux combattants se figent 3 à 12 images selon la force du coup (sur leur propre vitesse d'animation, pas sur Time.timeScale global).
> - Tremblement de caméra proportionnel au coup, zoom bref vers l'impact sur les coups lourds et le coup final du combo.
> - Images d'impact façon anime : 1 à 2 images de flash (blanc ou contraste inversé) sur les coups lourds, lignes de vitesse, étincelles et ondes de choc au point de contact.
> - Le personnage touché recule en glissant avec un nuage de poussière, clignote en blanc et marque une brève pose de douleur.
> - Ralenti de 0,3 s sur le coup qui achève le dernier ennemi d'une vague.
>
> 3. Vitesse et fluidité
> - Attaques rapides : démarrage court sur les coups légers, mémoire des commandes (buffer) d'environ 6 images, annulations coup léger → moyen → lourd.
> - Ajoute une attaque lourde (touche K) et un lanceur (bas + K) qui projette l'ennemi en l'air, avec des coups aériens et un smash qui le renvoie au sol.
> - Ajoute un dash rapide au sol et en l'air (double appui sur une direction ou touche dédiée), annulable en attaque.
> - Le mur et le sol renvoient les ennemis projetés fort (rebond au mur, rebond au sol) pour prolonger les combos.
> - Mets à jour le README, l'interface et GameInput avec les nouvelles touches (clavier AZERTY et manette).
>
> 4. Sang et gore (un peu, quelques démembrements pour l'instant)
> - Coups de poing et de pied : sueur, salive, étincelles d'impact, pas de sang.
> - Coups de couteau et armes tranchantes : giclée de sang orientée dans le sens du coup, gouttes qui tombent, petites taches au sol et sur le mur le plus proche, qui s'estompent après quelques secondes. Démembrement possible après 4 coups
> - Plus de sang sur le coup final et sur les ennemis à terre ; ennemis surnaturels plus tard : sang noir, rouge lumineux ou autre selon leur fiche.
> - Tout en pool d'objets (pas d'Instantiate/Destroy en combat) et avec un nombre de taches limité pour tenir 60 FPS.
>
> 5. Options et accessibilité
> - Dans un menu Options : niveau de gore (désactivé, réduit, complet), intensité du tremblement de caméra (0 à 100 %), flashs d'impact activés ou non (risque épileptique).
>
> 6. Test
> - Ajoute à l'arène un mannequin d'entraînement qui ne riposte pas, et un affichage de debug activable (touche F1) des frames de démarrage, actives et de récupération.
> - Lance le mode Play, vérifie les 60 FPS au Profiler pendant un long combo avec beaucoup de sang à l'écran, fais-moi des captures et une courte vidéo ou une suite de captures d'un combo complet.
> - Puis commite, pousse, et attends mon feu vert pour la phase C.

### Requête de l'auteur : tranche verticale (texte complet)

> Changement de priorité, APRÈS la phase combat dynamique : avant de produire tout l'Acte I, fais une TRANCHE VERTICALE jouable avec des graphismes définitifs :
> 1. Génère uniquement le concept du Voyou classique (4 variantes, j'en choisis une), puis son modèle TripoSG.
> 2. Pour Nathan M0 et ce Voyou : squelette, animations complètes (idle, course, saut, attaques du combo, attaque lourde, lanceur, dash, esquive, coup reçu, mort), rendu toon en spritesheets, intégration dans Unity.
> 3. Décor final de l'arène de la lune rouge : couches en parallaxe, sol texturé, éclairage URP 2D, post-traitement, cendres, effets d'impact et de sang de la phase combat dynamique.
> 4. Lance le mode Play, vérifie les 60 FPS, fais-moi des captures, et dis-moi comment jouer moi-même.
> Je validerai le style graphique sur cette tranche. Ensuite seulement, tu reprendras la phase C pour le reste de l'Acte I avec la même méthode.

Décisions déjà prises par l'auteur pour la tranche : Voyou `Voyou_concept_s11103` (capuche) ; Contrôle intelligent des applications de Windows désactivé (il bloquait le Python de TripoSG).

### Requête de l'auteur : le fusil à pompe (texte complet, 2026-10-27)

À intégrer pendant la phase D du Dealer lieutenant, après le choix de son concept (2e série, seeds 14201 à 14204).

> NOUVELLE MÉCANIQUE : LE FUSIL À POMPE. À intégrer pendant la phase D du Dealer lieutenant (après le choix de son concept). Mets à jour docs/ENEMIES.md, docs/GDD.md et docs/BESTIARY.md : le Dealer porte désormais un fusil à pompe au lieu d'une arme de poing.
>
> 1. LE DEALER ET SON FUSIL À POMPE
> - Avertissement obligatoire avant chaque tir (environ 0,8 à 1 s, réglable dans AttackData) : il arme bruyamment le fusil (son « tchak-tchak » bien reconnaissable), l'épaule et vise Nathan, avec un signal visuel clair (lueur rouge au canon et cône de danger au sol). Il ne tire JAMAIS sans cette annonce.
> - Dégâts selon la distance : à bout portant (environ 2,5 m), mort instantanée de Nathan quels que soient ses PV ; à moyenne distance, gros dégâts et projection ; au-delà, dispersion des plombs et dégâts faibles.
> - Le joueur peut survivre en s'éloignant hors du cône, ou en esquivant au bon moment (l'invulnérabilité de l'esquive doit couvrir le tir, mais pas toute l'annonce, pour que ce soit un vrai réflexe).
> - Après chaque tir, le Dealer recharge lentement : c'est la fenêtre où le joueur peut l'attaquer. Il garde aussi ses gardes du corps pour se protéger, comme dans le bestiaire.
>
> 2. MORT DE NATHAN PAR LE FUSIL À POMPE
> - Si Nathan est tué par un tir à bout portant, son corps explose : morceaux (membres, torse, tête), boyaux, giclées de sang, taches au sol et sur les murs, dans le sens du tir.
> - Courte mise en scène : hitstop, flash, ralenti d'environ 0,5 s, tremblement fort, puis l'écran de défaite.
>
> 3. NATHAN M4 ET LE FUSIL À POMPE (sensation de toute-puissance)
> - Arme disponible au stade M4 (sous Red Met), sur une touche dédiée (et à la manette), ajoutée à GameInput, au README et à l'aide à l'écran.
> - À courte portée, les ennemis humains (Acte I) sont réduits en morceaux dans une explosion de sang et de boyaux, et projetés en arrière ; à moyenne portée, projection et étourdissement ; tir en cône (plusieurs plombs).
> - Impact maximal : gros recul de Nathan, flash du canon, onde de choc, tremblement de caméra fort, hitstop, cartouche éjectée, ralenti bref quand un tir tue plusieurs ennemis.
> - Équilibrage : 6 cartouches, rechargement par cartouche (interruptible par l'esquive), cadence lente avec le temps de pompe. Les ennemis surnaturels des Actes II et III et les boss ne sont pas pulvérisés en un tir (ils encaissent et réagissent selon leur fiche).
> - Ajoute une vague de test « M4 + fusil à pompe contre un groupe de Voyous » et une touche de debug pour passer Nathan en M4 avec le fusil.
>
> 4. TECHNIQUE ET OPTIONS
> - Morceaux de corps : sprites découpés à partir des personnages (membres, torse, tête) et morceaux génériques de chair, avec physique 2D, rebonds, traînées de sang ; tout en pool d'objets, nombre limité de morceaux et de taches à l'écran, disparition progressive. 60 FPS obligatoires, mesure dans un build Windows pendant un tir sur 5 ennemis.
> - Respecte le réglage de gore du menu Options : « complet » = explosion en morceaux ; « réduit » = sang sans morceaux ; « désactivé » = le personnage s'effondre simplement.
> - Respecte CLAUDE.md : tous les dégâts passent par Health.TakeDamage, la mort instantanée aussi (par exemple un paramètre « mort instantanée » dans AttackData), et le gameplay ne dépend pas des animations.
>
> 5. TEST
> Montre-moi en jeu : l'avertissement du Dealer, une esquive réussie, une mort de Nathan à bout portant, puis Nathan M4 qui pulvérise un groupe de Voyous. Captures, mesure des FPS, commit et push.

### Requête de l'auteur : prototype narratif (texte complet, 2026-10-27)

> APRÈS le Dealer et le fusil à pompe : PROTOTYPE NARRATIF. RED MET ne doit pas se limiter au combat : c'est l'histoire d'un joueur qui sombre et devient un monstre pour sa dose. Construis les systèmes maintenant ; les vrais dialogues, les voix ElevenLabs, la musique et les bruitages viendront à la toute fin. Mets à jour docs/GDD.md en conséquence, et arrête-toi pour validation à la fin de chaque étape.
>
> 1. DESIGN À METTRE À JOUR DANS docs/GDD.md
> - Pas de choix de difficulté dans le menu. La difficulté vient des choix : le joueur qui refuse la Red Met et les choix immoraux ne monte pas en puissance et doit maîtriser l'esquive, alors que les ennemis montent en puissance à chaque incursion dans le monde parallèle. Celui qui prend une dose à chaque niveau devient de plus en plus fort (stades M0 à M4, armes, PV) et le jeu devient plus facile.
> - Structure de chaque niveau du monde parallèle : scène d'ouverture avec Mara (visual novel), combats, scène de fin avec Mara qui juge le joueur.
> - Mara : au début, elle le taquine et le rabaisse ; au fil des incursions, et surtout s'il devient le Nathan M4 bestial, elle est de plus en plus attirée par sa force, de plus en plus provocante, avec des tenues de plus en plus échancrées (stades M-T0 à M-T4 de docs/CHARACTERS.md) et des dialogues coquins. En fin de combat, elle peut réagir à sa puissance.
> - Fin : si Nathan a pris la Red Met à CHAQUE niveau, Mara choisit librement de passer la nuit avec lui (scène pour adultes, consentie, entre adultes). Sinon, elle le repousse en lui reprochant de ne pas avoir eu le « courage » d'aller jusqu'au bout. Remplace la section 11.1 actuelle (agression) par cette scène consentie. Aucun contenu sexuel ne doit apparaître dans la branche de l'enfant ni à proximité.
> - L'Avocat intervient de temps en temps, dans le monde réel comme dans le monde parallèle : paranoïaque, il pousse surtout Nathan à reprendre une dose et trouve des arguments logiques pour lui faire accepter les choix amoraux.
>
> 2. ÉTAT DE LA PARTIE ET SAUVEGARDE
> - Un état central : nombre de doses, dose prise ou refusée à chaque niveau, liste des actes commis (le jeu se souvient des faits, pas seulement d'un score), les quatre axes (Humanité, Cruauté, Dépendance, Paranoïa), stade de Nathan (M0 à M4, R0 à R4), stade de Mara (M-T0 à M-T4), numéro de l'incursion.
> - La puissance de Nathan dépend des doses ; celle des ennemis dépend du numéro de l'incursion (fichiers de données, pas de valeurs en dur).
> - Sauvegarde et chargement de la partie.
>
> 3. DIALOGUES FAÇON VISUAL NOVEL
> - Utilise Ink (langage de dialogue à embranchements, licence MIT) avec son intégration Unity, pour que j'écrive les dialogues plus tard dans des fichiers texte.
> - Écran visual novel : grand portrait de Mara (tenue selon son stade, plusieurs expressions), boîte de texte, nom, choix multiples, défilement automatique, avance rapide, historique.
> - Chaque réplique a un identifiant unique pour brancher plus tard la voix ElevenLabs correspondante.
> - Les dialogues lisent l'état de la partie (doses, actes, stades) pour choisir les répliques.
> - Mets des dialogues provisoires très courts pour tester.
>
> 4. INTERVENTIONS DE L'AVOCAT
> - Répliques courtes en surimpression (sous-titre et effet visuel de distorsion) déclenchées par des événements : avant un choix de dose, après un acte immoral, quand Nathan est en manque ou blessé, de temps en temps en exploration ; avec un délai minimum pour ne pas saturer.
> - Même système d'identifiants pour les voix.
>
> 5. CONTENU ADULTE
> - Au premier lancement : écran de confirmation d'âge (18 ans et plus).
> - Option « Contenu adulte » (activé / désactivé). Désactivé : la scène finale avec Mara est remplacée par un fondu au noir.
> - Prépare la scène finale comme un emplacement vide (déclenchement, conditions, fondu, réglages) dans lequel je placerai mes propres illustrations. Ne génère pas toi-même d'images de nudité ou de scène sexuelle.
>
> 6. TEST : UN MINI-CHAPITRE
> Monde réel (court, provisoire) → choix de la dose → ouverture avec Mara → vague de combat → fin avec Mara → seconde incursion où l'on voit l'état changer (puissance de Nathan, ennemis plus forts, tenue et ton de Mara, intervention de l'Avocat). Joue-le deux fois (en prenant toutes les doses, puis en les refusant toutes) et montre-moi les différences avec captures. Commit et push.

### Requête de l'auteur : complément, Mara dans les visual novels (texte complet, 2026-10-27)

> COMPLÉMENT AU PROTOTYPE NARRATIF — MARA DANS LES VISUAL NOVELS
>
> 1. Les tenues suivent la Red Met
> - Dans chaque scène visual novel, la tenue de Mara dépend du nombre de doses prises : plus Nathan a pris de Red Met, plus sa tenue est sexy et échancrée (stades M-T0 à M-T4 de docs/CHARACTERS.md). Le stade est lu dans l'état de la partie, jamais codé en dur dans les dialogues.
> - Pour chaque stade de tenue, prévois deux types de portraits : de face (scène d'ouverture) et de dos (scène de fin), avec plusieurs expressions.
>
> 2. Scène de fin de niveau : Mara de dos
> - À la fin de chaque niveau du monde parallèle, un visual novel montre Mara de dos, mettant ses fesses en valeur, la tête à moitié tournée pour regarder Nathan par-dessus l'épaule d'un air moqueur. Elle le nargue, puis disparaît (fondu ou dissolution en brume rouge).
> - Génère les portraits de dos pour chaque stade de tenue avec la même méthode que les concepts (Animagine XL, adulte, tenue du stade, sans nudité).
> - Réplique de test pour cette scène, quand Nathan n'est pas encore assez puissant (refus de doses ou stade faible) :
>   « Mon petit Nathan, je t'excite ? Je te fais envie ? Tu aimerais que je pose mes fesses sur ton visage, petit coquin ? Eh bien, deviens plus puissant, car je recherche un vrai bonhomme 💪. Les gamins comme toi, je les envoie en colo. »
> - Quand Nathan devient plus puissant et bestial (M2 à M4), ses répliques de fin changent : moins moqueuses, plus troublées et attirées par sa force. Prévois les embranchements dans le fichier Ink ; j'écrirai ces répliques plus tard.
> - Garde l'emoji 💪 dans le texte affiché, mais retire-le du texte envoyé plus tard à ElevenLabs.

Note de Claude pour la suite : Mara est une adulte. Ses portraits restent habillés, sans nudité ni scène sexuelle générée ; la fin immorale suit la section 11.1 du GDD v0.1 (décision du 2026-10-28, l'emplacement vide est supprimé) : coupée avant l'acte, sans nudité.
