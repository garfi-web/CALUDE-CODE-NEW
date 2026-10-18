# RED MET — notes pour Claude

Jeu d'action 2D latéral (Unity 6, URP 2D, C#), dark fantasy et horreur psychologique. Le design est dans `docs/GDD.md`.

## Code
- Tout le jeu vit dans `Assets/_Game/`. Les scripts sont dans `Scripts/<Domaine>/`, les outils d'éditeur dans `Editor/`.
- Le gameplay ne dépend jamais des animations : l'Animator est piloté par `CharacterAnimator` via des événements (`AttackStarted`, `DodgeStarted`, `Health.Damaged`, `Health.Died`).
- Tous les dégâts passent par `Health.TakeDamage`. L'invulnérabilité passe par `Health.GrantInvulnerability`.
- Les commandes passent par `GameInput`, jamais par `Input` ou `Keyboard` directement.
- Mouvement physique : `Rigidbody2D.linearVelocity`. Un script qui impose une vitesse temporaire (esquive, recul) appelle `PlayerController.LockMovement`.
- Les commentaires sont en français, le code (noms) en anglais.
- `Editor/ArenaBuilder.cs` reconstruit la scène de test : mettez-le à jour quand vous ajoutez un composant requis.

## Outils MCP (session locale sur le PC de l'auteur)
- `unity-mcp` : l'éditeur Unity (scène, GameObjects, scripts, console).
- `blender` : MCP for Blender, avec `generate_3d` pour Tripo, Hunyuan3D et Rodin.
- ElevenLabs : voix et effets sonores, à ranger dans `Assets/_Game/Audio/Voice`.
- Après une modification de script, lisez la console Unity pour vérifier la compilation.
