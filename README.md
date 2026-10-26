# RED MET

*Jusqu'où irez-vous pour avoir votre dose ?*

Jeu d'action 2D latéral, dark fantasy et horreur psychologique, pour PC (Unity 6, URP 2D, C#).
Le document de conception est dans [`docs/GDD.md`](docs/GDD.md).

## Prototype 1 : arène de combat

Le contenu de `Assets/_Game/` :

| Dossier | Contenu |
|---|---|
| `Scripts/Player` | `PlayerController` (déplacement, saut, coyote time), `PlayerCombat` (attaques image par image : combo léger → moyen → lourd, lanceur, aériens, smash, mémoire des commandes), `PlayerDash` (dash au sol et en l'air), `PlayerDodge` (esquive invulnérable) |
| `Scripts/Combat` | `AttackData` (attaque en images à 60 FPS : démarrage, actif, récupération, annulation, hitstop, tremblement, recul), `Combat` (point d'entrée des coups), `Health` (vie, dégâts, filtres), `DamageInfo`, `HitStop` (arrêt sur image par combattant), `HitReaction` (recul, glissade, jonglage, rebonds mur et sol, flash blanc, pose de douleur), `CombatVfx` (étincelles, ondes de choc, lignes de vitesse, image d'impact, sueur, sang, taches, démembrement, ralenti, tout en réserve) |
| `Scripts/Enemies` | `EnemyArchetype` (fiche d'ennemi, ScriptableObject), `Enemy` (socle : armement annoncé, actif, récupération, étourdissement, super-armure), `EnemyTelegraph` (annonces), `AttackTokens` (2 au contact, 1 tireur), `Projectile`, `Grenade`, `Cover`, `EnemyWaveManager` (vagues de test) |
| `Scripts/Enemies/Behaviours` | `MeleeAggressive` (Voyou), `Commander` (Dealer), `CoverShooter` (Flic), `Tactical` (Soldat), `HeavyCharger` (Mutant), points d'extension de l'Acte II |
| `Data/Enemies` | Archétypes des ennemis 01 à 04 et 06 (valeurs de `docs/ENEMIES.md`) et du mannequin |
| `Data/Attacks` | Attaques de Nathan (`Nathan/`) et des ennemis (`Enemies/`), modifiables dans l'inspecteur |
| `Prefabs` | `Enemy`, `Projectile`, `Grenade`, `Cover` (générés par l'ArenaBuilder) |
| `Scripts/Animation` | `CharacterAnimator` (pont optionnel vers l'Animator) |
| `Scripts/UI`, `Scripts/Core`, `Scripts/CameraSystem` | HUD, menu Options, debug des images (F1), démo de combo (F3), GameManager, commandes, options, réserve d'objets (`Pool`), temps de jeu (pause, ralenti), caméra (suivi, tremblement, zoom d'impact) |
| `Editor/ArenaBuilder.cs` | Menu **RED MET → Construire l'arène prototype** |

### Installation

Le dépôt contient le projet Unity complet (Unity 6000.6.6f1, URP 2D). Le détail, y compris les MCP et la reprise de session Claude, est dans [`docs/REPRISE.md`](docs/REPRISE.md).

1. Clonez le dépôt et ajoutez le dossier dans Unity Hub (**Add project from disk**).
2. Ouvrez la scène `Assets/_Game/Scenes/Arena.unity`. Pour la recréer de zéro, utilisez **RED MET → Construire l'arène prototype** : le script crée les layers `Ground`, `Player`, `Enemy` et `Debris`, règle le pas physique à 1/60 s, crée les sprites provisoires, les prefabs, les attaques et archétypes manquants et la scène. Les attaques et archétypes existants ne sont pas écrasés.
3. Appuyez sur **Play**. Dans la vue Game, choisissez la résolution 1920×1080.

Les commandes fonctionnent avec le nouvel Input System comme avec l'ancien : aucun réglage des Project Settings n'est nécessaire.

### Commandes

| Action | Clavier (AZERTY) | Manette |
|---|---|---|
| Se déplacer | Q / D ou les flèches | Stick gauche |
| Bas (pour le lanceur) | S ou flèche bas | Stick ou croix vers le bas |
| Sauter | Espace | A / Croix |
| Coup léger (J ×3 : jab, direct, crochet) | J | X / Carré |
| Coup lourd (rebond au mur) | K | Y / Triangle |
| Lanceur (projette en l'air) | S + K | Bas + Y / Triangle |
| Dash (au sol et en l'air, annulable en attaque) | L, ou double appui sur Q ou D | LB / L1 |
| Esquiver (invulnérable 0,40 s, traverse les ennemis) | Maj gauche | RB / R1 |
| Options (gore, tremblement, flashs) | Échap | Select / View |
| Lancer une vague de test | 1 à 7 (rangée du haut ou pavé numérique) | — |
| Debug des images / arme de test / démo de combo | F1 / F2 / F3 | — |
| Recommencer après une défaite | R | Start |

### Combos

- **Au sol** : J → J → J (12, 16, 25), ou J → J → K. Chaque coup s'annule dans le suivant pendant sa fenêtre d'annulation ; les commandes restent en mémoire 6 images.
- **Jonglage** : J → J → S + K (lanceur), puis **Espace** dès que le lanceur touche, J → J → J en l'air, puis **K** (smash) : l'ennemi rebondit au sol, on le relance au sol.
- **Mur** : un coup lourd (K) près d'un mur fait rebondir l'ennemi vers Nathan.
- **Dash** : L (ou double appui) puis J pendant le dash pour attaquer avec l'élan.
- Le mannequin (touche 7, présent au lancement) ne riposte pas et se soigne seul après 2 s.

### Vagues de test

| Touche | Vague | Ce qu'on teste |
|---|---|---|
| 1 | 1 Voyou | Esquive et contre |
| 2 | Dealer + 2 Voyous | Interrompre l'ordre (gardes plus rapides), gérer les gardes |
| 3 | Flic corrompu + 2 abris | Progression à couvert, rechargement, appel radio sous 50 % de PV |
| 4 | Soldat privé + 2 abris | Grenade quand Nathan se cache, rafales, changements de position |
| 5 | Mutant | Super-armure, charge à esquiver tard, charge dans le mur (sonné 2,5 s) |
| 6 | 3 Voyous + 1 Flic | Mixte : 2 Voyous frappent au plus en même temps, les autres tournent |
| 7 | Mannequin d'entraînement | Combos, rebonds, sang et démembrement (avec l'arme de test F2) |

Le mannequin apparaît au lancement. Chaque vague remet Nathan à pleine vie et efface le sang.

### Écarts avec le code d'origine

- **Esquive** : le contrôleur réécrivait la vitesse à chaque frame physique et annulait l'esquive. Il existe maintenant un verrou de mouvement pendant l'esquive et le recul.
- **Hitbox** : sa position était inversée deux fois quand le personnage regardait à gauche, une fois par l'échelle du parent et une fois par le script. Les contacts sont désormais testés image par image par `PlayerCombat` (zone de frappe de chaque `AttackData`, visible avec F1).
- **Recul de l'ennemi** : l'IA annulait le recul à la frame suivante. L'ennemi est maintenant étourdi et son attaque est interrompue.
- **Esquive et attaque** : une esquive annule maintenant le coup en cours.
- **Mort** : un personnage mort ne bouge plus et n'attaque plus.

### Plan de test

- [ ] Déplacement, saut, plateformes traversables par le dessous
- [ ] Combo J-J-J : 12, 16 puis 25 dégâts, recul plus fort sur le 3e coup
- [ ] Le Voyou devient jaune avant de frapper ; une esquive lancée au début de l'annonce évite les dégâts
- [ ] Un coup pendant l'armement l'interrompt (sauf le Mutant)
- [ ] Le Voyou meurt après environ 4 coups (60 PV) : message VAGUE TERMINÉE
- [ ] Ligne de visée rouge avant chaque tir ; les balles traversent Nathan pendant l'esquive
- [ ] Un abri libre arrête les balles ; une grenade passe par-dessus et marque un cercle rouge au sol
- [ ] Le joueur meurt après 10 coups de poing : message DÉFAITE, R relance l'arène
- [ ] Mannequin (au lancement) : J J J, J J S+K puis Espace, J J J en l'air, K : rebond au sol
- [ ] Coup lourd K près d'un mur : rebond au mur ; F1 montre la frise démarrage / actif / récupération
- [ ] F2 puis combo : sang orienté, taches au sol et au mur qui s'estompent, membre arraché après 4 coups
- [ ] Échap : gore désactivé (plus de sang), tremblement à 0 %, flashs désactivés
- [ ] Le dernier ennemi d'une vague tombe au ralenti
