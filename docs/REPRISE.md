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

Vérifier dans Claude Code : `/mcp` (état et reconnexion). Unity se déconnecte à chaque recompilation, c'est normal : attendre quelques secondes, puis reconnecter. Le relais s'arrête aussi seul après 3 minutes sans requête (« Auto-shutdown timer fired ») : la requête suivante le relance, il suffit parfois de la refaire. Le journal de l'éditeur est dans `Logs/Editor.log` (dans le projet).

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

> Lis CLAUDE.md, docs/REPRISE.md et la section « État d'avancement » de docs/PRODUCTION.md. Vérifie que le MCP Unity est connecté au projet REDMET et que la console compile, puis que le MCP Blender répond. Ensuite, reprends la PHASE COMBAT DYNAMIQUE (consignes de l'auteur dans la conversation du 2026-10-26), en respectant CLAUDE.md, puis commite, pousse et attends mon feu vert pour la phase C.

L'arène de test de la phase B se lance avec **Play** ; les touches **1 à 6** choisissent la vague (détail dans le `README.md`).
