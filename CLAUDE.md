# RED MET — notes pour Claude

Jeu d'action 2D latéral (Unity 6, URP 2D, C#), dark fantasy et horreur psychologique. Le design est dans `docs/GDD.md`, le bestiaire dans `docs/BESTIARY.md` et les fiches techniques des ennemis dans `docs/ENEMIES.md`.

## Méthode de travail : VALIDER AVANT DE PRODUIRE (règle de l'auteur, 2026-10-30)
- Ne jamais engager beaucoup de travail (animations complètes, rendu de toutes les images, application à plusieurs personnages) sur une méthode ou un rendu que l'auteur n'a pas validé.
- Avancer par petits paliers (essai minimal → essai en jeu sur une seule image → variantes → animation complète → autres personnages) ; à chaque palier : montrer le résultat, dire le temps et l'effort du palier suivant, et attendre un « OK » explicite.
- Deux refus de suite sur une même méthode : arrêter et proposer une autre méthode (par exemple confier le travail à un illustrateur) au lieu d'insister.

## Montrer un visuel à l'auteur (règle de l'auteur, 2026-10-30)
- Le terminal n'affiche pas les images. Tout visuel montré à l'auteur est enregistré dans `docs/captures/` avec un nom clair (date + sujet, par exemple `2026-10-30_tete_nathan_essai1.png`), jamais dans un dossier temporaire.
- L'ouvrir à l'écran avec PowerShell : `Start-Process "<chemin complet>"` (visionneuse de Windows), et donner aussi le chemin complet dans la réponse.

## Code
- Tout le jeu vit dans `Assets/_Game/`. Les scripts sont dans `Scripts/<Domaine>/`, les outils d'éditeur dans `Editor/`.
- Le gameplay ne dépend jamais des animations : l'Animator est piloté par `CharacterAnimator` via des événements (`AttackStarted`, `DodgeStarted`, `Health.Damaged`, `Health.Died`).
- Tous les dégâts passent par `Health.TakeDamage` (avec un `DamageInfo` pour les tirs et les explosions). L'invulnérabilité passe par `Health.GrantInvulnerability`.
- Combat : toute attaque (Nathan et ennemis) est un `AttackData` en images à 60 FPS ; le pas physique vaut 1/60 s. Les coups passent par `Combat.Strike`. Un compteur d'images de combat n'avance jamais pendant un `HitStop` (arrêt sur image par combattant, jamais `Time.timeScale`). Seul `GameTime` touche à `Time.timeScale` (pause, ralenti).
- Effets de combat et gore : dans `CombatVfx`, en réserve (`Pool`, particules par `Emit`), jamais d'`Instantiate` ou de `Destroy` pendant un combat ; ils respectent `GameOptions` (gore, tremblement, flashs).
- Ennemis : les valeurs sont dans les archétypes (`Data/Enemies/*.asset`, `EnemyArchetype`), jamais en dur dans un comportement. Les règles communes (armement minimal 0,35 s, interruption, super-armure, jetons) vivent dans `Enemy` ; un comportement ne fait que choisir quoi faire.
- Les commandes passent par `GameInput`, jamais par `Input` ou `Keyboard` directement.
- Mouvement physique : `Rigidbody2D.linearVelocity`. Un script qui impose une vitesse temporaire (esquive, recul) appelle `PlayerController.LockMovement`.
- Les commentaires sont en français, le code (noms) en anglais.
- `Editor/ArenaBuilder.cs` reconstruit la scène de test : mettez-le à jour quand vous ajoutez un composant requis.

## Outils MCP (session locale sur le PC de l'auteur)
- `unity-mcp` : l'éditeur Unity (scène, GameObjects, scripts, console).
- `blender` : MCP for Blender, avec `generate_3d` pour Tripo, Hunyuan3D et Rodin.
- ElevenLabs : voix et effets sonores, à ranger dans `Assets/_Game/Audio/Voice`.
- Après une modification de script, lisez la console Unity pour vérifier la compilation.
- Reprise du travail, relance des MCP et pièges connus (un seul éditeur Unity ouvert) : `docs/REPRISE.md`.
