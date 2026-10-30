# RED MET — Étude : passer en beat'em up 2,5D (façon Streets of Rage 4)

Demande de l'auteur du 2026-10-30. Rien de l'existant n'est converti : le prototype vit dans une scène à part,
`Assets/_Game/Scenes/Arena25D.unity`, construite par **RED MET → Construire l'arène 2,5D (étude)**, avec des composants
nouveaux (`Assets/_Game/Scripts/Brawler25D/`). **Aucune autre conversion ne sera faite avant la décision de l'auteur.**

## 1. Recommandation

**Oui au 2,5D pour les combats, mais en deux temps, et seulement après deux décisions de l'auteur.**

1. Le prototype tient : combat encerclé à 8 ennemis à **0,66 ms de thread principal en moyenne et 1,89 ms par image**
   (aucune image au-delà de 16,6 ms sur 30 s, §4). Le combat existant (AttackData, hitstop, caméra, gore, Health) marche
   tel quel en profondeur, sans duplication de ses règles. Les ennemis qui encerclent, les arrivées annoncées et les
   jetons d'attaque donnent déjà le rythme d'un beat'em up de rue, ce qui colle à l'Acte I (Voyous, Flics, Dealers).
2. Le coût est réel mais borné : le rendu des sprites est déjà automatisé dans Blender (seule la caméra change), le gros
   du travail est dans les **ennemis à distance** (abri, grenades, charge, onde) et dans **les décors des 10 niveaux**,
   à redessiner en profondeur. Les plateformes disparaissent.
3. Ordre conseillé : (a) l'auteur choisit le cadrage des sprites (§2) ; (b) on unifie le code du combat de Nathan
   (§5.3) pour ne garder qu'une seule version ; (c) on convertit l'Acte I seul (ch. 1 à 3) et on le joue ; (d) le reste
   seulement si l'Acte I convainc.

**Décisions demandées à l'auteur :**
- **Cadrage des sprites** (planche `docs/captures/2026-10-30_25d_comparaison_profil_3q.png`) : profil actuel,
  profil surélevé de 18°, ou **3/4 surélevé de 18° et tourné de 30°** (mon conseil : on voit le visage et le torse, la
  profondeur se lit mieux, c'est la vue de Streets of Rage 4 ; inconvénient : le sprite retourné inverse les détails
  asymétriques, comme l'étui du Flic). **Aucune animation n'est rendue avant ce choix.**
- **Convertir ou non**, et si oui, l'Acte I d'abord.

## 2. Sprites en vue de 3/4 (à valider)

Rendus dans Blender avec la scène de sprites existante (`RM_Sprites`, caméra orthographique, cel-shading, contours),
sans toucher aux personnages ni aux animations : seule la caméra bouge. Nathan M0, le Voyou et le Flic, chacun au repos
et en attaque, dans les trois cadrages.

- Planche : `docs/captures/2026-10-30_25d_comparaison_profil_3q.png`.
- Le style validé tient dans les trois (aplats, contours noirs). Le liseré coloré vient de Unity (`SkyController`), il
  s'applique pareil.
- Rendre toutes les animations d'un personnage = relancer `render_clips` (`tools/blender/redmet_animation.py`) avec la
  caméra choisie : quelques minutes par personnage, sans retouche manuelle.

**Constat de lisibilité** : en mêlée, Nathan M0 et les Voyous ont presque la même silhouette (capuche grise, pantalon
clair). En 2,5D, où l'on joue au milieu d'un groupe, Nathan se perd (`2026-10-30_25d_combat_encercle_suite.png`). Il
faudra le distinguer : couleur de vêtement, liseré propre au joueur, ou repère au sol sous lui.

## 3. Ce que fait le prototype

Commandes : Q/D et Z/S pour marcher (profondeur comprise), Espace saut, J léger (et ramasser), K lourd, **arrière + K
lanceur** (le bas sert maintenant à marcher vers l'avant), Maj esquive dans les 4 directions, U fusil, I recharger,
8 : 8 ennemis autour de Nathan, F1 images par seconde, R recommencer.

| Demande | Fait | Où |
|---|---|---|
| Sol en plan X × profondeur, axe de hauteur séparé, pas de gravité Rigidbody2D ni de plateforme | Oui : Rigidbody2D cinématique pour le sol, hauteur et gravité propres | `Body25D`, `Playfield25D` |
| Ombre ovale qui reste au sol | Oui, rétrécit et pâlit avec la hauteur | `Body25D`, capture « saut » |
| Ordre d'affichage par profondeur | Oui (SortingGroup, 100 crans par mètre) | `Playfield25D.SortingOrder` |
| Coups seulement sur la même bande de profondeur, tolérance dans AttackData | Oui : `AttackData.depthTolerance` (0,35 m par défaut) | `Hit25D.Strike` |
| Lanceur, smash, fusil adaptés | Lanceur : la cible monte (jonglage). Smash : plaquée, rebond. Coup lourd : rebond sur le bord de la zone. **Fusil : cône qui s'élargit aussi en profondeur** | `HitReaction25D`, `Hit25D.Fire`, capture « cône du Dealer » |
| Esquive dans les 4 directions | Oui, pas de côté vers le fond ou l'avant, invulnérabilité via Health | `Brawler25D` |
| Réutiliser le combat sans le dupliquer | AttackData (les mêmes fichiers), `Combat.Strike`, `HitStop`, `Health`, `CombatVfx` (gore, flashs, tremblement), `CameraFollow`, `AttackTokens`, archétypes et contrôleurs d'animation | voir §5.3 pour ce qui reste en double |
| Arrivées de partout, annoncées | Bord gauche et droit (repère clignotant + course), premier plan (repère en bas), toit (**ombre qui grandit**, puis chute), porte (**qui s'ouvre**), fenêtre (vitre qui éclate, saut), ruelle | `ArenaDirector25D`, `SpawnPoint25D`, planche `2026-10-30_25d_arrivees.png` |
| Vagues, caméra bloquée, tirage pondéré, flèche GO | Oui : une direction tirée perd 70 % de poids, les autres en regagnent 0,25 | `ArenaDirector25D.PickEntry` |
| Intelligence collective | Les ennemis sans jeton se placent sur deux arcs, à gauche et à droite de Nathan, étagés en profondeur et qui tournent lentement ; menaces (pas vif puis retour) ; **2 au plus frappent à la fois** (jetons existants), de préférence **dans le dos** de Nathan ; 1 tir à la fois | `Enemy25D` |
| Arme ramassable, objet cassable avec nourriture | Barre de fer (8 coups avant de casser), caisse qui lâche du pain et une pomme (+30 PV) | `Pickup25D`, `Breakable25D` |
| Décor | Rue en ruines, ciel du ch. 3 (coucher de soleil, sans lune rouge), entrées visibles, premier plan qui passe devant | `tools/gen3d/street25d.py` |
| 60 FPS à 8 ennemis, captures, suite d'images | Oui (§4) | `Benchmark25D` |

Les règles communes des ennemis sont reprises : armement d'au moins 0,35 s annoncé par un voile de couleur, interruption
par un coup sauf super-armure, valeurs lues dans les archétypes. Ce qui n'est **pas** joué dans ce prototype : les tirs
au pistolet, grenades, charges, ondes, ordres et renforts (seuls le corps à corps et le fusil à pompe le sont), le dash
de Nathan, les formes de Nathan (M0 seulement, fusil disponible pour le test).

### Décor

Une génération libre d'Animagine donnait des paysages en perspective inutilisables (essai 1, gardé dans
`ArtSource/Street25D/rejet_essai1/`), puis des façades trop propres (essai 2, `rejet_essai2/`). La version retenue
dessine chaque élément en traits (porte, fenêtre, ruelle, pavés en perspective, silhouettes des objets) : ce dessin
guide ControlNet et sert aussi de masque de détourage, si bien que les entrées sont exactement là où le code les attend.
Le ciel est celui du ch. 3, déjà peint. La caisse et la nourriture sont passables (prototype), la barre est bonne.

### Sons

ElevenLabs, sans voix (aucune réplique inventée) : porte, vitre, course et « GO », dans `Assets/_Game/Audio/Voice/SFX_*.mp3`.
Le toit (la course sert d'annonce), la réception et la caisse **n'ont pas été générés** : le forfait gratuit a atteint
sa limite par minute (rien n'a été facturé pour ceux-là).

## 4. Mesures et captures

Build Windows de l'arène 2,5D (**RED MET → Build Windows (arène 2,5D)**, `Builds/Windows25D/REDMET25D.exe`), RTX 3070
Laptop, 1920 × 1080, vsync coupée, gore complet. Rapport : `docs/captures/2026-10-30_25d_benchmark.txt`.

| Mesure (30 s, combat encerclé, Nathan piloté) | Résultat |
|---|---|
| Ennemis à l'écran | 4 à 9, moyenne 7,4 (8 demandés ; un mort est remplacé après son annonce) |
| Thread principal | moyenne 0,66 ms, 99e centile 1,11 ms, pire 3,13 ms |
| Durée d'image | moyenne 1,89 ms (≈ 530 images/s), 99e centile 3,52 ms, pire 5,76 ms |
| Images au-delà de 16,6 ms | 0 |

Arguments du build : `-redmet-benchmark-25d` (mesure), `-redmet-capture-25d` (16 images à 0,2 s d'écart d'un combat
encerclé), `-redmet-showcase-25d` (partie normale avec une capture par type d'arrivée, la flèche GO, le saut, l'esquive,
la barre, le tir, le cône du Dealer, l'encerclement). Pendant ces tests, Nathan est soigné à chaque image et ne peut
pas être pulvérisé.

Captures (`docs/captures/`) : `2026-10-30_25d_arrivees.png`, `2026-10-30_25d_fonctions.png`,
`2026-10-30_25d_combat_encercle_suite.png`, `2026-10-30_25d_encercle.jpg`, `2026-10-30_25d_comparaison_profil_3q.png`.

Défauts vus sur les captures, non corrigés : le flash d'impact plein écran existant reste blanc plusieurs images quand
un tir tue plusieurs ennemis (ralenti), voir images 7 et 8 de la suite ; la capture « tir » tombe juste avant la gerbe.

## 5. Ce que demanderait la conversion complète

### 5.1 Personnages à rendre de nouveau

Le rendu est automatique (Blender, `redmet_animation.py`) une fois la caméra choisie ; il faut surtout **revoir les
poses** que le profil cachait (bras côté caméra, armes) et **régénérer les morceaux de gore** au même cadrage.

| Personnage | Clips existants | Images | Remarque |
|---|---|---|---|
| Nathan M0 | 16 | 177 | Clips d'esquive à refaire en 4 directions (pas de côté vers le fond / l'avant) |
| Nathan M4 + fusil | 18 | 212 | Le fusil en 3/4 demande un contrôle du canon (origine du cône) |
| Voyou (3 armes) | 17 | 203 | |
| Flic corrompu | 12 | 274 | Matraque et pistolet ; étui inversé par le retournement |
| Dealer | 10 | 201 | |
| Pas encore animés | — | — | Nathan R0 à R4 et M1 à M3, Mara M-T0 à M-T4, Chimiste, Soldat privé, Mutant, boss : à rendre directement dans le cadrage choisi |

Nouveaux clips propres au genre : marche en profondeur (vers le fond, vers l'avant), saisie et projection (attendues
dans un beat'em up, absentes aujourd'hui), ramassage d'objet, coups avec arme ramassée, relevé après une mise au sol.

### 5.2 Niveaux

- Les **10 niveaux** sont à redessiner en profondeur : sol en perspective, façades ou parois de fond avec leurs entrées,
  premier plan. Le procédé de `street25d.py` (traits → ControlNet → masque exact) se réutilise par chapitre.
- Les **ciels** des ch. 1 à 10 (`SkyProfile`, `Data/Sky/`) restent utilisables tels quels en fond.
- Chaque niveau devient une suite de **zones de combat** (caméra bloquée, vagues, GO) au lieu d'un couloir continu.
- Les fissures rouges du sol (identité Red Met) passent du sol latéral au sol en perspective.

### 5.3 Code

- **Disparaît** dans les scènes 2,5D : plateformes (`PlatformEffector2D`), gravité Rigidbody2D, détection du sol par
  collision, rebond sur les murs physiques, sauts entre étages.
- **À unifier avant de convertir** (sinon deux versions vivent en parallèle) : `PlayerCombat` / `BrawlerCombat25D` et
  `PlayerController` / `Brawler25D` (la logique de combo est en double), `HitReaction` / `HitReaction25D`,
  `CharacterAnimator` / `CharacterAnimator25D`, `Enemy` / `Enemy25D` (les règles communes sont reprises, pas partagées).
  Piste : un « corps » commun (latéral ou 2,5D) derrière une interface, que les scripts de combat utilisent.
- **À adapter** :
  - `Projectile` : trajectoire dans le plan, tolérance de profondeur ;
  - `Grenade` : zone en ellipse au sol ;
  - `CoverShooter` / `Cover` : abris posés au sol, à revoir ;
  - `HeavyCharger` : charge dans le plan ;
  - onde au sol : anneau en ellipse ;
  - `PlayerDash` ;
  - `EnemyWaveManager` → `ArenaDirector25D` ;
  - `ComboDemo` et `BenchmarkRunner`.
- **Gore** : sans sol physique, le sang et les morceaux de `CombatVfx` tombent hors de l'écran au lieu de se poser à
  la profondeur de la victime. Il faut une « hauteur » simulée pour les particules et les morceaux (même principe que
  `Body25D`).
- **Commandes** : le bas sert à marcher. Le lanceur passe à arrière + K. Il reste à décider où placer la saisie.
- **Inchangés** : visual novel, état de la partie, doses, Ink, options, HUD (à part les repères d'arrivée).

### 5.4 Ce qui change dans le jeu

- Fin du jeu de plateformes : plus de saut entre étages, de chute dans le vide ni de verticalité. Le saut reste pour
  les attaques aériennes et les esquives par-dessus.
- Le combat devient un combat de foule : encerclement, gestion de l'espace, objets. Il faudra revoir le nombre et la
  force des ennemis par vague dans `BESTIARY.md` / `ENEMIES.md`.
- La voie sobre (poings et pieds, santé faible) devient plus dure à plusieurs ennemis : 2 attaquants au plus à la fois
  (jetons) reste le garde-fou principal.

## 6. Fichiers de l'étude

- Code : `Assets/_Game/Scripts/Brawler25D/` (`Playfield25D`, `Body25D`, `Hit25D`, `HitReaction25D`, `Brawler25D`,
  `BrawlerCombat25D`, `Enemy25D`, `ArenaDirector25D`, `SpawnPoint25D`, `Pickup25D`, `Breakable25D`,
  `CharacterAnimator25D`, `HUD25D`, `Benchmark25D`), `Assets/_Game/Editor/Arena25DBuilder.cs`,
  menu de build dans `WindowsBuild.cs`.
- Ajouts à l'existant, sans changement de comportement : `AttackData.depthTolerance`, `CameraFollow.SetBounds`,
  `Shotgun.StrikeTarget` (sortie de `Shotgun.Fire`, même règle), `BenchmarkRunner.Report` rendu public. Le banc d'essai
  du fusil à pompe de l'arène latérale a été relancé après ces changements (§7).
- Données : `Data/Attacks/Nathan25D/` (deux coups de barre de fer), décor `Art/Backgrounds/Street25D/`, originaux et
  guides dans `ArtSource/Street25D/`.

## 7. Contrôle de non-régression de l'arène latérale

`Arena.unity` n'est pas modifiée. Après les ajouts listés au §6, le build de l'arène latérale a été refait et son banc
d'essai du fusil à pompe relancé (`-redmet-benchmark-shotgun`, 2026-10-30) : Nathan M4 contre 5 Voyous, 13 tirs, 65 corps
pulvérisés (5 par tir, comme prévu), thread principal 1,00 ms en moyenne, durée d'image 2,77 ms, aucune image au-delà de
16,6 ms, aucune exception dans le journal.
