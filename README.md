# RED MET

*Jusqu'où irez-vous pour avoir votre dose ?*

Jeu d'action 2D latéral, dark fantasy et horreur psychologique, pour PC (Unity 6, URP 2D, C#).
Le document de conception est dans [`docs/GDD.md`](docs/GDD.md).

## Prototype 1 : arène de combat

Le contenu de `Assets/_Game/` :

| Dossier | Contenu |
|---|---|
| `Scripts/Player` | `PlayerController` (déplacement, saut, coyote time), `PlayerCombat` (combo à 3 coups), `PlayerDodge` (esquive invulnérable) |
| `Scripts/Combat` | `Health` (vie, dégâts, invulnérabilité), `Hitbox` (relais du trigger d'attaque) |
| `Scripts/Enemies` | `EnemyAI` (poursuite, attaque annoncée, étourdissement) |
| `Scripts/Animation` | `CharacterAnimator` (pont optionnel vers l'Animator) |
| `Scripts/UI`, `Scripts/Core`, `Scripts/CameraSystem` | HUD, GameManager (victoire, défaite, relance), commandes, caméra |
| `Editor/ArenaBuilder.cs` | Menu **RED MET → Construire l'arène prototype** |

### Installation

Le dépôt contient le projet Unity complet (Unity 6000.6.6f1, URP 2D). Le détail, y compris les MCP et la reprise de session Claude, est dans [`docs/REPRISE.md`](docs/REPRISE.md).

1. Clonez le dépôt et ajoutez le dossier dans Unity Hub (**Add project from disk**).
2. Ouvrez la scène `Assets/_Game/Scenes/Arena.unity`. Pour la recréer de zéro, utilisez **RED MET → Construire l'arène prototype** : le script crée le layer `Ground`, les sprites provisoires et la scène.
3. Appuyez sur **Play**. Dans la vue Game, choisissez la résolution 1920×1080.

Les commandes fonctionnent avec le nouvel Input System comme avec l'ancien : aucun réglage des Project Settings n'est nécessaire.

### Commandes

| Action | Clavier (AZERTY) | Manette |
|---|---|---|
| Se déplacer | Q / D ou les flèches | Stick gauche |
| Sauter | Espace | A / Croix |
| Attaquer (combo x3) | J | X / Carré |
| Esquiver | Maj gauche | RB / R1 |
| Recommencer | R | Start |

### Écarts avec le code d'origine

- **Esquive** : le contrôleur réécrivait la vitesse à chaque frame physique et annulait l'esquive. Il existe maintenant un verrou de mouvement pendant l'esquive et le recul.
- **Hitbox** : sa position était inversée deux fois quand le personnage regardait à gauche, une fois par l'échelle du parent et une fois par le script. Les contacts passent désormais par le relais `Hitbox`.
- **Recul de l'ennemi** : l'IA annulait le recul à la frame suivante. L'ennemi est maintenant étourdi et son attaque est interrompue.
- **Esquive et attaque** : une esquive annule maintenant le coup en cours.
- **Mort** : un personnage mort ne bouge plus et n'attaque plus.

### Plan de test

- [ ] Déplacement, saut, plateformes traversables par le dessous
- [ ] Combo J-J-J : 12, 16 puis 25 dégâts, recul plus fort sur le 3e coup
- [ ] L'ennemi devient jaune avant de frapper ; une esquive à ce moment évite les dégâts
- [ ] L'ennemi meurt après environ 4 coups (60 PV) : message VICTOIRE
- [ ] Le joueur meurt après 10 coups reçus : message DÉFAITE, R relance l'arène
