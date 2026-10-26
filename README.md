# RED MET

*Jusqu'où irez-vous pour avoir votre dose ?*

Jeu d'action 2D latéral, dark fantasy et horreur psychologique, pour PC (Unity 6, URP 2D, C#).
Le document de conception est dans [`docs/GDD.md`](docs/GDD.md).

## Prototype 1 : arène de combat

Le contenu de `Assets/_Game/` :

| Dossier | Contenu |
|---|---|
| `Scripts/Player` | `PlayerController` (déplacement, saut, coyote time), `PlayerCombat` (combo à 3 coups), `PlayerDodge` (esquive invulnérable) |
| `Scripts/Combat` | `Health` (vie, dégâts, invulnérabilité, filtres de dégâts), `DamageInfo` (corps à corps, tir, explosion), `Hitbox` (relais du trigger d'attaque) |
| `Scripts/Enemies` | `EnemyArchetype` (fiche d'ennemi, ScriptableObject), `Enemy` (socle : armement annoncé, actif, récupération, étourdissement, super-armure), `EnemyTelegraph` (annonces), `AttackTokens` (2 au contact, 1 tireur), `Projectile`, `Grenade`, `Cover`, `EnemyWaveManager` (vagues de test) |
| `Scripts/Enemies/Behaviours` | `MeleeAggressive` (Voyou), `Commander` (Dealer), `CoverShooter` (Flic), `Tactical` (Soldat), `HeavyCharger` (Mutant), points d'extension de l'Acte II |
| `Data/Enemies` | Archétypes des ennemis 01 à 04 et 06 (valeurs de `docs/ENEMIES.md`) |
| `Prefabs` | `Enemy`, `Projectile`, `Grenade`, `Cover` (générés par l'ArenaBuilder) |
| `Scripts/Animation` | `CharacterAnimator` (pont optionnel vers l'Animator) |
| `Scripts/UI`, `Scripts/Core`, `Scripts/CameraSystem` | HUD, GameManager (vagues, défaite, relance), commandes, caméra |
| `Editor/ArenaBuilder.cs` | Menu **RED MET → Construire l'arène prototype** |

### Installation

Le dépôt contient le projet Unity complet (Unity 6000.6.6f1, URP 2D). Le détail, y compris les MCP et la reprise de session Claude, est dans [`docs/REPRISE.md`](docs/REPRISE.md).

1. Clonez le dépôt et ajoutez le dossier dans Unity Hub (**Add project from disk**).
2. Ouvrez la scène `Assets/_Game/Scenes/Arena.unity`. Pour la recréer de zéro, utilisez **RED MET → Construire l'arène prototype** : le script crée les layers `Ground`, `Player` et `Enemy`, les sprites provisoires, les prefabs, les archétypes manquants et la scène. Les archétypes existants ne sont pas écrasés.
3. Appuyez sur **Play**. Dans la vue Game, choisissez la résolution 1920×1080.

Les commandes fonctionnent avec le nouvel Input System comme avec l'ancien : aucun réglage des Project Settings n'est nécessaire.

### Commandes

| Action | Clavier (AZERTY) | Manette |
|---|---|---|
| Se déplacer | Q / D ou les flèches | Stick gauche |
| Sauter | Espace | A / Croix |
| Attaquer (combo x3) | J | X / Carré |
| Esquiver (invulnérable 0,40 s, traverse les ennemis) | Maj gauche | RB / R1 |
| Lancer une vague de test | 1 à 6 (rangée du haut ou pavé numérique) | — |
| Recommencer après une défaite | R | Start |

### Vagues de test

| Touche | Vague | Ce qu'on teste |
|---|---|---|
| 1 | 1 Voyou | Esquive et contre |
| 2 | Dealer + 2 Voyous | Interrompre l'ordre (gardes plus rapides), gérer les gardes |
| 3 | Flic corrompu + 2 abris | Progression à couvert, rechargement, appel radio sous 50 % de PV |
| 4 | Soldat privé + 2 abris | Grenade quand Nathan se cache, rafales, changements de position |
| 5 | Mutant | Super-armure, charge à esquiver tard, charge dans le mur (sonné 2,5 s) |
| 6 | 3 Voyous + 1 Flic | Mixte : 2 Voyous frappent au plus en même temps, les autres tournent |

La vague 1 démarre au lancement. Chaque vague remet Nathan à pleine vie.

### Écarts avec le code d'origine

- **Esquive** : le contrôleur réécrivait la vitesse à chaque frame physique et annulait l'esquive. Il existe maintenant un verrou de mouvement pendant l'esquive et le recul.
- **Hitbox** : sa position était inversée deux fois quand le personnage regardait à gauche, une fois par l'échelle du parent et une fois par le script. Les contacts passent désormais par le relais `Hitbox`.
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
