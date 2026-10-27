# RED MET — Fiches techniques des ennemis

Traduction jouable de [`docs/BESTIARY.md`](BESTIARY.md) pour un jeu d'action 2D latéral. Valeurs **de départ**, à ajuster au test.
Unités : 1 unité Unity ≈ 1 m (Nathan mesure environ 1,6 unité de haut), temps en secondes, vitesses en unités par seconde.

## Tailles

Hauteur debout, du sol au sommet du crâne, à laquelle chaque modèle est mis dans Blender (`ArtSource/Blender/REDMET_personnages.blend`) avant le rendu des sprites. Repère : Nathan R0 et M0 mesurent 1,70 m, M2 et M4 2,05 m. Les tailles **proposées** seront confirmées par l'auteur au moment des concepts de chaque ennemi.

| # | Ennemi | Taille | État |
|---|---|---|---|
| 01 | Voyou classique | 1,78 m | Modèle et sprites faits (tranche verticale) |
| 02 | Dealer (lieutenant) | 1,80 m | Proposée |
| 03 | Flic corrompu | **1,85 m** | Validée par l'auteur le 2026-10-27, modèle et sprites faits |
| 04 | Soldat privé | 1,88 m | Proposée (casque compris) |
| 05 | Chasseur | 1,83 m | Proposée |
| 06 | Mutant | 2,40 m | Proposée (zone de collision actuelle : 2,4 unités) |
| 07 | Psycho | 1,75 m | Proposée |
| 08 | Clown tueur | 1,90 m | Proposée |
| 09 | Démon techno | 2,10 m | Proposée |
| 10 | Spectre | 2,00 m | Proposée (flotte au-dessus du sol) |
| 11 | Géant de chair (mini-boss) | 4,50 m | Proposée |
| 12 | Porteur de l'aberration (boss final) | 3,00 m (forme de départ) | Proposée, change selon les phases |
| — | Mannequin d'entraînement | 1,75 m | Fait (accessoire) |

La zone de collision (`EnemyArchetype.bodySize`) suit la taille du modèle quand l'ennemi passe en sprites (Flic : 0,9 × 1,85).

## 0. Règles communes

**Équilibrage (BESTIARY.md, section 6) : l'ennemi ne change pas selon la trajectoire.** Ses PV, ses dégâts, ses temps d'armement et son comportement sont identiques sur la voie sobre et sur la voie Red Met. Ce qui change :
- les **capacités de Nathan** (profils ci-dessous) ;
- la **composition des groupes** et les **occasions tactiques** du niveau (abris, chemins de fuite, éléments du décor), plus généreuses sur la voie sobre ;
- jamais les statistiques d'un même ennemi.

**Profils de Nathan utilisés pour les calculs**

| Profil | PV | Combo (coups 1-2-3) | Distance | Esquive |
|---|---|---|---|---|
| **Sobre** (dose 0, code actuel) | 100 | 12 / 16 / 25 (53 par combo) | — | 0,18 s de déplacement, cooldown 0,65 s |
| **Red Met** (dose 2, Nathan M2, prototype 1b) | 200 | 60 / 75 / 110 (245 par combo) | Arme à feu, 20 par tir, 4 tirs/s | Identique |

Le GDD demande qu'un ennemi standard réclame **environ 4 fois plus d'actions offensives** sur la voie sobre : le Voyou (60 PV) tombe en 4 coups sobre et en 1 coup sous Red Met.

**Lisibilité et esquive**
- **Toute attaque est annoncée** avant de frapper : posture d'armement, couleur, ligne de visée ou zone au sol. Pas de dégât sans annonce.
- **Armement minimal : 0,35 s.** Aucune attaque ne frappe plus vite.
- **L'esquive doit toujours pouvoir l'éviter.** `dodgeInvulnerability` était de 0,25 s, plus court que l'armement minimal (0,35 s) : une esquive lancée au tout début de l'annonce prenait le coup (constaté en test le 2026-10-25). **Passé à 0,40 s en phase B**, pour couvrir l'armement le plus court quel que soit le moment où le joueur réagit. Pendant l'esquive, Nathan traverse aussi les ennemis : sans cela, la charge du Mutant ne s'évite pas en 2D.
- **L'ordre du Dealer ne descend jamais sous 0,35 s** : l'armement réduit de 20 % est plafonné à l'armement minimal (coup de poing : 0,35 s, couteau : 0,45 → 0,36 s).
- **Fenêtre de punition** : après chaque attaque, une récupération pendant laquelle l'ennemi ne peut ni attaquer ni bloquer.
- **Étourdissement** : un coup reçu pendant l'armement l'interrompt (comportement actuel de `EnemyAI`), sauf pour les ennemis lourds (« super-armure » indiquée dans la fiche).

**Lecture des colonnes « Coups pour tuer »** : nombre d'actions offensives de Nathan pour vaincre l'ennemi. Combo sobre = 12 + 16 + 25 enchaînés ; Red Met = combo ou tirs, le plus efficace. **« Coups encaissables »** : nombre de coups de l'attaque principale qui tuent Nathan (PV ÷ dégâts, arrondi au supérieur).

---

## ACTE I — La violence du monde réel

### 01 — Le Voyou classique
**Rôle** : corps à corps de base, dangereux en groupe. **Acte** : I (début), versions déformées ensuite. **Comportement** : `MeleeAggressive`.

| PV | Vitesse | Détection | Poids |
|---|---|---|---|
| 60 | 3,5 | 9 | Léger (recul complet) |

| Attaque | Variante | Portée | Armement | Actif | Récupération | Dégâts | Annonce |
|---|---|---|---|---|---|---|---|
| Coup de poing | Mains nues | 1,2 | **0,35** | 0,10 | 0,35 | 10 | Épaule armée, flash jaune |
| Coup de couteau | Couteau | 1,4 | 0,45 | 0,08 | 0,45 | 15 | Lame levée, reflet |
| Balayage à la barre | Barre de fer | 1,8 | 0,60 | 0,15 | 0,60 | 18 | Barre ramenée en arrière |

- **Fenêtres de vulnérabilité** : chaque récupération. Interrompu par n'importe quel coup pendant l'armement.
- **Ce qu'il punit** : attaquer à l'aveugle, rester au contact de plusieurs voyous, encaisser au lieu d'esquiver.
- **Contre-stratégie** : esquiver l'annonce, punir la récupération, ne jamais se laisser prendre entre deux voyous.
- **Groupe** : 2 attaquent au maximum en même temps (jeton d'attaque), les autres tournent autour à 2,5 unités.

| Trajectoire | Coups pour tuer | Coups encaissables (poing / couteau / barre) |
|---|---|---|
| Sobre | 4 | 10 / 7 / 6 |
| Red Met | 1 | 20 / 14 / 12 |

### 02 — Le Dealer (lieutenant de rue du Chimiste)
**Rôle** : commandant qui reste à distance et donne des ordres. **Acte** : I (première moitié). **Comportement** : `Commander`.
**À ne pas confondre avec le Chimiste.** Le Dealer est un **lieutenant de rue** du réseau du Chimiste : tenue voyante, bijoux, lunettes teintées, assurance tranquille. Le **Chimiste** (`docs/CHARACTERS.md`) reste le personnage narratif, calme, élégant et **non combattant**, qui ne se montre jamais dans une rixe.

| PV | Vitesse | Détection | Poids |
|---|---|---|---|
| 90 | 2,5 (fuite 4,5) | 12 | Moyen |

| Attaque / action | Portée | Armement | Actif | Récupération | Dégâts | Annonce |
|---|---|---|---|---|---|---|
| Ordre « Chopez-le » (renforce les gardes : armement −20 % pendant 5 s) | Écran | 1,0 | — | 0,5 | — | Bras tendu vers Nathan, réplique |
| Tir opportuniste au pistolet (2 tirs) | 8 | 0,60 | 0,05 par tir | 0,9 | 14 par tir | Arme levée, ligne de visée rouge |
| Coup de crosse | 1,1 | 0,40 | 0,10 | 0,50 | 10 | Arme retournée |

- **Comportement** : reste à 6-9 unités derrière ses gardes. Quand il n'a plus de garde, il fuit vers une sortie ; s'il est acculé, il combat.
- **Fenêtres** : pendant l'ordre (1,0 s, immobile), pendant la récupération du tir, et quand il fuit (dos tourné).
- **Ce qu'il punit** : ignorer ses ordres, se laisser encercler par les gardes renforcés, le poursuivre en tournant le dos aux gardes.
- **Contre-stratégie** : interrompre l'ordre (coup ou projectile), éliminer ou contourner les gardes, le coincer contre un mur.

| Trajectoire | Coups pour tuer | Coups encaissables (tir) |
|---|---|---|
| Sobre | 6 | 8 |
| Red Met | 1 combo ou 5 tirs | 15 |

### 03 — Le Flic corrompu
**Rôle** : tireur à couvert, contrôle de zone. **Acte** : I (milieu). **Comportement** : `CoverShooter`.
**Uniforme fictif** : aucun insigne, logo ni nom d'une police réelle. Seul un texte générique « POLICE » est autorisé. La corruption est celle d'individus fictifs, sans caricature des forces de l'ordre (`BESTIARY.md`).

| PV | Vitesse | Détection | Poids |
|---|---|---|---|
| 80 | 3,0 | 14 | Moyen |

| Attaque | Portée | Armement | Actif | Récupération | Dégâts | Annonce |
|---|---|---|---|---|---|---|
| Tir visé (projectile, 18 unités/s) | 9 | 0,70 | 0,05 | 0,40 | 14 | Ligne de visée laser + clignement |
| Rafale de 3 (rare, 1 fois sur 4) | 9 | 0,90 | 0,30 | 0,70 | 3 × 10 | Ligne de visée qui épaissit |
| Coup de matraque | 1,5 | 0,45 | 0,12 | 0,50 | 12 | Matraque levée |
| Appel radio (renfort, 1 fois par combat) | — | 1,2 | — | — | — | Main à l'épaule, grésillement |

- **Comportement** : cherche l'abri le plus proche qui le sépare de Nathan. Tire depuis l'abri, recharge après 6 tirs (1,5 s). Passe à la matraque si Nathan est à moins de 2 unités.
- **Abri** : derrière un abri, il ne reçoit que 25 % des dégâts à distance (attaques au corps à corps et tirs de flanc : 100 %).
- **Fenêtres** : rechargement (1,5 s), récupération du tir, appel radio (interruptible).
- **Ce qu'il punit** : rester immobile à découvert, foncer en ligne droite dans sa visée.
- **Contre-stratégie** : progresser d'abri en abri, esquiver à travers la ligne de visée, le prendre de flanc pendant le rechargement.

| Trajectoire | Coups pour tuer | Coups encaissables (tir) |
|---|---|---|
| Sobre | 5 | 8 |
| Red Met | 1 combo ou 4 tirs (16 s'il est à l'abri) | 15 |

### 04 — Le Soldat privé
**Rôle** : élite tactique coordonnée. **Acte** : I (fin). **Comportement** : `Tactical`.

| PV | Vitesse | Détection | Poids |
|---|---|---|---|
| 110 | 3,2 | 16 | Moyen (recul réduit de moitié) |

| Attaque | Portée | Armement | Actif | Récupération | Dégâts | Annonce |
|---|---|---|---|---|---|---|
| Rafale visée (3 projectiles, 22 unités/s) | 12 | 0,80 | 0,25 | 0,60 | 3 × 8 | Ligne de visée + voyant rouge sur le casque |
| Grenade (zone de 2,5 unités, explose 1,2 s après l'impact) | 10 | 0,60 (lancer) | 1,2 (fusée) | 0,80 | 30 | Bras armé, puis cercle rouge au sol |
| Coup de crosse | 1,3 | 0,40 | 0,10 | 0,45 | 15 | Fusil ramené |

- **Coordination** : en groupe, un soldat tient la position et tire pendant que l'autre contourne ; jamais plus d'un tir à la fois sur Nathan (jeton de tir) ; une grenade toutes les 8 s au plus, pour déloger Nathan d'un abri.
- **Fenêtres** : récupération après rafale, lancer de grenade, changement de position (0,6 s à découvert).
- **Ce qu'il punit** : rester derrière un abri (grenade), les ruées frontales, ignorer le soldat qui contourne.
- **Contre-stratégie** : observer qui contourne, le punir pendant son déplacement, sortir de la zone de grenade, attaquer pendant la récupération.

| Trajectoire | Coups pour tuer | Coups encaissables (rafale) |
|---|---|---|
| Sobre | 7 | 5 |
| Red Met | 1 combo ou 6 tirs | 9 |

---

## ACTE II — La réalité commence à se déformer

### 05 — Le Chasseur
**Rôle** : prédateur, embuscades et pièges. **Comportement** : `Ambusher` (phase B : point d'extension seulement).

| PV | Vitesse | Détection |
|---|---|---|
| 100 | 4,5 (traque), 2 (furtif) | 18 |

| Attaque | Portée | Armement | Récupération | Dégâts | Annonce |
|---|---|---|---|---|---|
| Embuscade depuis l'ombre | 2,0 | 0,50 | 0,70 | 22 | Yeux qui s'allument, souffle |
| Piège à mâchoires (posé, immobilise 1,2 s) | Contact | Posé | — | 8 + immobilisation | Reflet métallique au sol |
| Couteau | 1,4 | 0,40 | 0,40 | 14 | Lame levée |

- **Fenêtres** : après l'embuscade (0,7 s), quand il pose un piège (1,0 s).
- **Ce qu'il punit** : avancer sans regarder le sol ni les zones sombres.
- **Contre-stratégie** : repérer indices et pièges, le forcer à sortir de l'ombre, punir l'embuscade ratée.
- Sobre : 6 coups / encaisse 5 embuscades. Red Met : 1 combo / encaisse 10.

### 06 — Le Mutant
**Rôle** : lourd, charge et dégâts massifs. **Comportement** : `HeavyCharger` (implémenté dès la phase B, pour tester un ennemi lourd).

| PV | Vitesse | Détection | Poids |
|---|---|---|---|
| 260 | 2,0 (charge 11) | 10 | **Lourd : super-armure pendant l'armement** (les coups ne l'interrompent pas), pas de recul |

| Attaque | Portée | Armement | Actif | Récupération | Dégâts | Annonce |
|---|---|---|---|---|---|---|
| Charge | 10 | 1,00 | Jusqu'au mur ou 1,2 s | 1,50 (**2,5 s s'il heurte un mur, sonné**) | 30 | Se ramasse, gratte le sol, rugit |
| Frappe au sol (onde de 3,5 unités) | 3,5 | 0,90 | 0,20 | 1,20 | 22 | Bras levés, poussière |
| Balayage | 2,0 | 0,70 | 0,15 | 0,80 | 18 | Torse qui pivote |

- **Fenêtres** : récupérations longues, surtout après une charge dans un mur.
- **Ce qu'il punit** : rester au contact, esquiver trop tôt (la charge suit Nathan pendant les 0,6 premières secondes de l'armement).
- **Contre-stratégie** : esquiver tard, l'attirer contre un mur, frapper pendant la récupération.
- Sobre : 15 coups (5 combos) / encaisse 4 charges. Red Met : 2 combos ou 13 tirs / encaisse 7 charges.

### 07 — Le Psycho
**Rôle** : feintes et changements de rythme. **Comportement** : `Feinter` (point d'extension : armement « annulé » volontairement).

| PV | Vitesse |
|---|---|
| 90 | 3,8, accélérations brusques à 6 |

| Attaque | Portée | Armement | Récupération | Dégâts | Annonce |
|---|---|---|---|---|---|
| Feinte (armement interrompu) | — | 0,35 puis annulation | — | — | Identique à l'attaque réelle, mais **le flash est bleu et non jaune** |
| Attaque retardée | 1,3 | 0,90 (longue hésitation) | 0,50 | 16 | Tremblement, rire |
| Ruée rapide | 4,0 | 0,45 | 0,70 | 14 | Recul brusque avant la ruée |

- **Règle de lisibilité** : une feinte se reconnaît toujours (couleur de l'annonce, son différent). Le joueur doit pouvoir l'apprendre, jamais la deviner au hasard.
- **Ce qu'il punit** : esquiver au premier mouvement.
- Sobre : 6 coups / encaisse 7. Red Met : 1 combo / encaisse 13.

### 08 — Le Clown tueur
**Rôle** : pièges et fausses ouvertures. **Comportement** : `Trickster` (points d'extension : leurres, fausse vulnérabilité).

| PV | Vitesse |
|---|---|
| 140 | 4,0 |

| Attaque | Portée | Armement | Récupération | Dégâts | Annonce |
|---|---|---|---|---|---|
| Masse | 2,0 | 0,60 | 0,60 | 20 | Masse levée en tournant |
| Objet lancé (leurre ou bombe) | 7 | 0,50 | 0,50 | 0 ou 18 | Bombe : mèche allumée ; leurre : confettis |
| Fausse ouverture (fait mine d'être sonné, contre si frappé) | 1,5 | 0,40 après le premier coup reçu | 0,80 | 22 | Étoiles **violettes** (une vraie ouverture a des étoiles jaunes) |

- **Ce qu'il punit** : frapper chaque ouverture sans la lire.
- Sobre : 9 coups / encaisse 5 coups de masse. Red Met : 1 combo / encaisse 10.

---

## ACTE III — Le cauchemar absolu
Valeurs indicatives. Les mécaniques (zones dangereuses, téléportation, illusions, phases de boss) sont des **points d'extension** prévus en phase B, pas encore codés.

| # | Ennemi | PV | Attaques principales (armement → dégâts) | Mécanique spéciale | Ce qu'il punit | Sobre / Red Met (coups pour tuer) |
|---|---|---|---|---|---|---|
| 09 | Démon techno | 180 | Arc électrique (0,6 → 18), téléportation-frappe (0,5, réapparition annoncée par des étincelles → 20), zone électrifiée (1,0 de marquage → 10/s) | `IHazardSpawner`, `ITeleport` | Rester dans les zones marquées | 11 / 1 combo |
| 10 | Spectre | 120 | Griffe (0,5 → 16), cri (0,9, cône de 4 unités → 12 + ralentissement) | `IIllusionCaster` : 2 doubles sans collision, le vrai Spectre a une ombre au sol | Frapper les illusions | 8 / 1 combo |
| 11 | Géant de chair (mini-boss) | 900 | Écrasement (1,2 → 35), balayage (1,0 → 25), effondrement local (1,5 de marquage → 30) | `IBossPhases` : 3 phases à 66 % et 33 % des PV, points faibles qui se déplacent | Rester dans la portée | 51 / 4 combos |
| 12 | Porteur de l'aberration (boss final) | 1 500 | Selon la phase, toutes annoncées (≥ 0,6) | `IBossPhases` + `IArenaModifier` (plateformes, distances, perspective) | Ne pas comprendre la règle de la phase | Selon les phases |

---

## Composition des rencontres de l'arène de test (phase B)

| Touche | Vague | Rôle du test |
|---|---|---|
| 1 | 1 Voyou (mains nues) | Esquive et contre |
| 2 | 1 Dealer + 2 Voyous | Interrompre l'ordre, gérer les gardes |
| 3 | 1 Flic corrompu + 2 abris | Progression à couvert |
| 4 | 1 Soldat privé + 2 abris | Grenade, lecture des déplacements |
| 5 | 1 Mutant | Ennemi lourd, super-armure, charge dans un mur |
| (groupe) | 3 Voyous (couteau, barre, mains nues) + 1 Flic | Mixte : corps à corps couvert par un tireur |

Sur la voie sobre, le niveau offre des abris et des chemins de fuite en plus ; les vagues sont les mêmes.

## Implémentation (phase B)

| Fiche | Code |
|---|---|
| Statistiques et attaques | `Assets/_Game/Data/Enemies/*.asset` (`EnemyArchetype`), un fichier par ennemi et par variante d'arme du Voyou ; images (armement, actif, récupération), dégâts, impact et recul dans `Data/Attacks/Enemies/*.asset` (`AttackData`, 60 FPS : 0,35 s = 21 images) |
| Règles communes (armement minimal, interruption, super-armure, récupération, jetons) | `Scripts/Enemies/Enemy.cs`, `AttackTokens.cs` |
| Comportements | `MeleeAggressive`, `Commander`, `CoverShooter`, `Tactical`, `HeavyCharger` ; `Ambusher`, `Feinter`, `Trickster` se battent encore comme des Voyous |
| Abri (25 % des dégâts à distance de face) | `Cover.cs` + `Enemy.Filter` |
| Points d'extension de l'Acte III | `Scripts/Enemies/EnemyExtensions.cs` (interfaces) |

Choix faits là où la fiche ne tranchait pas : le Flic appelle la radio quand il passe sous 50 % de ses PV ; le renfort est un Voyou mains nues ; un abri libre arrête tous les tirs (Nathan s'en sert) et un abri occupé les laisse passer ; la charge s'arrête sur Nathan si elle le touche ; le jeton de tir unique vaut pour tous les tireurs.
