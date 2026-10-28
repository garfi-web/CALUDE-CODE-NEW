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

> Lis CLAUDE.md, docs/REPRISE.md (sections 7 et 8) et la section « État d’avancement » de docs/PRODUCTION.md. Vérifie que le MCP Unity est connecté au projet REDMET (scène Assets/_Game/Scenes/Arena.unity ouverte) et que la console compile, puis que le MCP Blender répond (fichier ArtSource/Blender/REDMET_personnages.blend ouvert). Ensuite, commence le PROTOTYPE NARRATIF (programme, point 6 ; requêtes complètes en fin de docs/REPRISE.md) par l’étape 1 (mise à jour de docs/GDD.md), et arrête-toi pour validation à la fin de chaque étape, en respectant CLAUDE.md.

L'arène de test se lance avec **Play** (mannequin au lancement) ; les touches **1 à 8** choisissent la vague (8 : Nathan M4 et le fusil à pompe), F1 à F4 le debug (détail dans le `README.md`).

## 8. Programme à venir (dans l'ordre)

1. **Fin de la phase B** du bestiaire — *fait* (commit `d1abe6c`).
2. **Sauvegarde** — *faite*.
3. **Phase combat dynamique** (Dragon Ball FighterZ sans les super-pouvoirs, hitstop, sang au couteau) — *faite* (commit `9766bed`).
4. **Tranche verticale** Nathan M0 contre le Voyou, graphismes définitifs — *faite et validée par l'auteur le 2026-10-27* (commits `ccee514` à `5bddd2b`, puis la sauvegarde de fin de session) : cel-shading, allonge des coups, mannequin, décor peint (ComfyUI + Animagine), pose de mort, liseré rouge, 60 FPS mesurés dans un build Windows. Dernières retouches demandées après validation : sol redessiné en toon et crochet allongé (0,9 m) — faites et vérifiées en Play le 2026-10-27 (le crochet touche à 0,87 m dès sa 1re image active). Détail : `docs/PRODUCTION.md`, section « Tranche verticale ».
5. **Phase C** (reste de l'Acte I, même méthode) — **en cours**. Un ennemi à la fois :
   - **Flic corrompu terminé** : concept 13103, 1,85 m, phase D faite (armes, animations de tir avec annonce, sprites), vague 3 de l'arène.
   - **Dealer lieutenant terminé** : 1re série (14101 à 14104) rejetée, 2e série choisie : **14201** (africain à la peau noire foncée, cicatrices, chaîne et dent en or). Phase D faite avec le **fusil à pompe** (annonce, mort instantanée à bout portant, corps pulvérisés), **Nathan M4** et son fusil (touche U, vague 8), 60 FPS mesurés dans un build Windows. Détail : `docs/PRODUCTION.md`.
   - Ensuite le Soldat privé et le Mutant, un à la fois, avec arrêt après chaque série de concepts pour le choix de l’auteur, **après le prototype narratif** (point 6).

   Tailles de tous les ennemis : `docs/ENEMIES.md`, section « Tailles ».

6. **Prototype narratif** (demandé le 2026-10-27, après le Dealer et le fusil à pompe) — **en cours** : étape 1 (`docs/GDD.md`, avec le complément « Mara dans les visual novels ») validée par l’auteur le 2026-10-28 (seuils de Mara : 0 dose M-T0, 1 M-T1, 2-3 M-T2, 4 M-T3, 5+ M-T4 ; version « agression » de la v0.1 abandonnée ; scène finale vide pour un illustrateur). Portraits de dos de Mara : rendu Blender du modèle 3D, puis ComfyUI + Animagine avec ControlNet pose et contours ; montrer un essai M-T0 à l’auteur avant les autres stades. Étape 2 (état de la partie et sauvegarde) faite le 2026-10-28, en attente de validation. Six étapes, avec arrêt pour validation de l’auteur à la fin de chaque étape : design dans `docs/GDD.md`, état de la partie et sauvegarde, dialogues visual novel avec Ink, interventions de l’Avocat, contenu adulte (confirmation d’âge, option, scène finale vide pour les illustrations de l’auteur ; aucune image de nudité générée par Claude), mini-chapitre joué deux fois. Texte complet des deux requêtes en fin de fichier.

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

Note de Claude pour la suite : Mara est une adulte. Ses portraits restent habillés, sans nudité ni scène sexuelle générée ; la scène finale reste un emplacement vide pour les illustrations de l'auteur.
