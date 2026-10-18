# RED MET — Reprendre le travail

Ce dépôt contient le projet Unity complet : `Assets/` (avec les `.meta`), `ProjectSettings/` et `Packages/`.
Les dossiers régénérés par Unity (`Library/`, `Temp/`, `Logs/`, `UserSettings/`) et les fichiers d'IDE ne sont pas versionnés.

| Élément | Valeur |
|---|---|
| Unity | **6000.6.6f1** (Unity 6), installé via Unity Hub |
| Rendu | URP 2D : `Assets/_Game/Settings/URP2D_RPAsset.asset` + `Renderer2D.asset` |
| Scène de test | `Assets/_Game/Scenes/Arena.unity` |
| Blender | 5.2.2 LTS, addon « Interface: Blender MCP » |
| Branche de travail | `claude/jolly-wright-b7a83l` |

## 1. Récupérer le projet

Sur le PC habituel, le projet est dans `C:\Users\Home\Unity user templates\REDMET` : un `git pull` suffit.

Sur une autre machine :

```bash
git clone -b claude/jolly-wright-b7a83l https://github.com/garfi-web/CALUDE-CODE-NEW.git REDMET
```

## 2. Ouvrir Unity

1. Unity Hub → **Add → Add project from disk** → choisir le dossier `REDMET`.
2. Ouvrir avec l'éditeur **6000.6.6f1**. La première ouverture reconstruit `Library/` : comptez plusieurs minutes.
3. Ouvrir `Assets/_Game/Scenes/Arena.unity` et appuyer sur **Play**. En cas de doute, le menu **RED MET → Construire l'arène prototype** recrée la scène de zéro.
4. Vérifier que **Project Settings → Graphics** pointe bien sur `URP2D_RPAsset`, ainsi que chaque niveau de **Quality**.

**N'ouvrez qu'un seul projet Unity à la fois.** Le MCP Unity se connecte à un seul éditeur. Avec deux éditeurs ouverts, il peut basculer sur le mauvais projet après une recompilation.

## 3. Relancer les MCP

Les deux serveurs sont déclarés dans la configuration utilisateur de Claude Code (`~/.claude.json`), pas dans le dépôt.

| Serveur | Commande | Côté application |
|---|---|---|
| `unity-mcp` | `C:\Users\Home\.unity\relay\relay_win.exe --mcp` | Fourni par le paquet `com.unity.ai.assistant`. Il démarre avec l'éditeur. |
| `blender` | `uvx mcp-for-blender` | Dans Blender : panneau MCP de la vue 3D (touche N) → **Start MCP Server** |

Sur une nouvelle machine, déclarez-les à nouveau :

```bash
claude mcp add --scope user unity-mcp -- "C:\Users\Home\.unity\relay\relay_win.exe" --mcp
claude mcp add --scope user blender -- uvx mcp-for-blender
```

Pour l'addon Blender, lancez `uvx mcp-for-blender install-addon`, puis activez « Interface: Blender MCP » dans Preferences → Add-ons.

**Vérifier les connexions** dans Claude Code :
- `/mcp` liste les serveurs et leur état. Depuis ce menu, on peut reconnecter un serveur tombé.
- Unity : demander à Claude d'afficher `Application.dataPath` (outil `Unity_RunCommand`). Le chemin doit être celui de ce projet.
- Blender : demander l'état de l'addon (`get_addon_status`) ou la création d'un cube de test.

**Unity se déconnecte après chaque recompilation** (rechargement du domaine). C'est normal : attendez quelques secondes, puis reconnectez avec `/mcp` si besoin.

## 4. Reprendre la session Claude

Depuis le dossier du projet :

```bash
cd "C:\Users\Home\Unity user templates\REDMET"
claude --continue    # reprend la dernière conversation de ce dossier
claude --resume      # ou choisir une conversation dans la liste
```

L'historique des conversations reste en local sur le PC (`~/.claude/projects/`) et n'est pas dans le dépôt. Sur une autre machine, ouvrez une nouvelle session : Claude lit `CLAUDE.md`, puis `docs/`.

Pour repartir proprement, cette consigne suffit :

> Lis CLAUDE.md, docs/ et docs/REPRISE.md. Vérifie la connexion Unity (bon projet), la compilation dans la console, puis la connexion Blender.

## 5. Où en est le projet

- **Prototype 1 (arène de combat)** : le code est dans `Assets/_Game`, et la scène `Arena.unity` est générée et versionnée. Le plan de test est dans le `README.md` et n'a pas encore été déroulé en Play.
- **Pipeline 2D** : URP 2D actif. `ArenaBuilder` ajoute la `Global Light 2D`.
- **Suite** : prototype 1b (sobre contre Red Met), répliques ElevenLabs restantes (`docs/VOICE_LINES.md`), modèles Tripo et rendus de sprites dans Blender (`docs/CHARACTERS.md`).
- Les assets du modèle Unity Get Started (`Assets/Scenes`, `SourceFiles`, `Tutorials`…) sont versionnés mais ne servent pas au jeu. Leurs scènes 3D s'affichent mal avec le renderer 2D.
