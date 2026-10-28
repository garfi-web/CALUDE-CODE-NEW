# Déposer vos dialogues

Ce dossier reçoit **vos** dialogues, tels que vous les écrivez. Claude les convertit ensuite en fichiers Ink pour le jeu, **mot pour mot**.

## 1. Un fichier par chapitre

```
docs/source/dialogues/ch01.md
docs/source/dialogues/ch02.md
…
docs/source/dialogues/ch10.md
```

Encodage UTF-8 (le défaut de presque tous les éditeurs). Vous pouvez déposer un chapitre incomplet et le compléter plus tard.

## 2. Format

- **Une scène = un titre** `## ` suivi de l'identifiant de la scène, tiré de la liste ci-dessous (et de `docs/SCENARIO.md`).
- **Une réplique par ligne** : `PERSONNAGE : réplique`. Le nom en majuscules, puis un espace, deux-points, un espace.
- **Mise en scène entre crochets**, sur sa propre ligne ou dans la réplique : `[Mara se retourne]`.
- **Les choix du joueur** commencent par `* ` (astérisque et espace). Les répliques qui suivent un choix sont décalées de deux espaces.
- **Une variante** (selon les doses, un acte, un stade) s'écrit entre crochets avant les répliques concernées : `[si Nathan a pris la dose]`, `[si M2 ou plus]`, `[si acte_ch02_purge]`. Claude la traduit en condition Ink.
- Une ligne qui commence par `//` est une **note pour Claude**, jamais affichée en jeu.
- Les emojis sont permis (💪) : ils restent à l'écran et sont retirés du texte envoyé à la synthèse vocale.

Noms à utiliser : `NATHAN`, `MARA`, `L'AVOCAT`, `LE CHIMISTE`, `MARC`, `LE PATRON`, `CLIENT`, `PASSANT`, `L'USURIER`, `LA VIEILLE DAME`, `LE COMMANDANT`, `LE GUÉRISSEUR`, `LE COMPAGNON`, `LE ROI`… Pour un nouveau personnage, écrivez son nom en majuscules : il sera ajouté.

### Exemple

```
## ch01_mara_ouverture

[Lune rouge. Mara apparaît de face, la lame à la main.]
MARA : Encore un déchet que la lune m'envoie.
[si Nathan a pris la dose]
MARA : Tiens. Celui-là tient debout, au moins.
NATHAN : Je… où suis-je ?
* Se taire
  MARA : Bien. Les déchets n'ont rien à dire.
* « Je ne suis pas un déchet. »
  MARA : [elle rit] On verra.

## ch01_mara_fin

[Mara de dos, la tête tournée par-dessus l'épaule.]
MARA : Mon petit Nathan, je t'excite ? …
```

(Les répliques de cet exemple ne servent qu'à montrer le format : elles ne seront pas reprises.)

## 3. Ce que fait Claude quand vous lui dites que des dialogues sont déposés

1. Il lit vos fichiers et les convertit en Ink (`Assets/_Game/Story/Ink/chNN.ink`, un nœud Ink par identifiant de scène).
2. Il garde **votre texte mot pour mot**. Il ne corrige **que l'orthographe** (accents, accords, fautes de frappe), et vous **signale chaque correction** (avant → après). Il ne reformule rien, ne coupe rien et n'ajoute rien.
3. Il donne à chaque réplique un **identifiant de voix** pour ElevenLabs : l'identifiant de la scène suivi d'un numéro à trois chiffres (`ch01_mara_ouverture_001`, `_002`…). Les identifiants déjà attribués ne changent pas si vous ajoutez des répliques plus tard : les nouvelles prennent les numéros suivants.
4. Il vous **signale les scènes encore sans dialogue**.
5. Il **n'invente jamais de répliques à votre place**. Tant que vous n'avez pas écrit une scène, elle garde dans le jeu un texte de remplacement marqué **PROVISOIRE**, que vous remplacerez par le vôtre.


## 4. Ce qu'il vous reste à écrire

Les marques de statut restent visibles en jeu (en rouge, au-dessus de la réplique) tant que vous n'avez pas écrit la scène. Pour la remplacer, déposez votre texte sous le même identifiant (`## ch07_mara_fin`, etc.) : Claude remplace la marque par vos répliques.

**Scènes de Mara déjà dans le jeu** : `Assets/_Game/Story/Ink/mara.ink` reprend le module Mara V4 déposé dans `mara_v4/`, réparti sur les chapitres. Le détail est dans `mara_v4/ANALYSE.md`.

### « À RÉÉCRIRE » : votre texte existe, mais il faut le réécrire
| Scène | Répliques | Pourquoi |
|---|---|---|
| `ch01_mara_ouverture` | MARA : « Attends. » / MARA : « C'est vraiment toi ? » / NATHAN : « Tu comptes me demander ça à chaque fois ? » / MARA : « Non. Je voulais juste vérifier qu'il n'y avait pas eu une erreur de casting. » | Elles supposent que Nathan et Mara se sont déjà vus, alors que c'est leur première rencontre (« Mara. Enchantée. ») |

### « À ÉCRIRE » : la scène, ou une partie, n'a pas encore de dialogue
| Scène | Ce qui manque |
|---|---|
| `ch03_mara_fin` | Toute la scène (Mara de dos, fin du ch. 3) |
| `ch04_mara_ouverture` | Toute la scène |
| `ch04_mara_fin` | Toute la scène |
| `ch05_mara_ouverture` | Toute la scène |
| `ch06_mara_ouverture` | Toute la scène |
| `ch06_mara_fin` | Toute la scène |
| `ch07_mara_fin` | Branche « enfant livré » : la fin de la réaction de Mara après « Tu sais ce qui me fait le plus peur ?… », sans aucun contenu suggestif (les répliques suivantes ont été retirées) |
| `ch09_mara_ouverture` | Toute la scène |
| `ch10_mara_fin_immorale` | Fin immorale, section 11.1 : Nathan tente d'imposer sa volonté ; scène coupée avant l'acte, sans nudité, centrée sur la réaction et le refus de Mara |
| `ch10_mara_fin_morale` | Fin morale, section 11.2 : Mara n'a aucune interaction, montre ses biceps 💪 et lance votre réplique « Moi, je veux un vrai bonhomme… » |

### « PROVISOIRE » : un texte de remplacement attend le vôtre
| Scène | Ce qui manque |
|---|---|
| `ch08_mara_ouverture` | Branche « Mara adversaire » (si elle a choisi de combattre Nathan au ch. 7) : ses répliques hostiles |
| `ch08_mara_fin` | Branche « Mara adversaire » |
| `ch09_mara_fin` | Branche « Mara adversaire » |
| `ch10_mara_ouverture` | Branche « Mara adversaire » |

Toutes les autres scènes de `SCENARIO.md` (réel, dose, l'Avocat, combats) n'ont pas encore de dialogue. La liste complète est en section 5.

## 5. Liste des identifiants de scènes

Toutes les scènes des 10 chapitres, dans l'ordre (détail dans `docs/SCENARIO.md`). Les scènes de combat (`_rm_…`) peuvent n'avoir que quelques répliques, ou aucune.
Préfixes : `reel` monde réel · `dose` choix et prise de la dose · `rm` monde de la Red Met · `mara` visual novel de Mara (`ouverture` : de face, `fin` : de dos) · `avocat` intervention de l'Avocat.

### Chapitre 1 — Le garçon invisible (`ch01.md`)

| Identifiant | Scène |
|---|---|
| `ch01_reel_journee` | Une journée catastrophique à son petit boulot dans un fast-food, jouable en quelques minutes :… |
| `ch01_reel_humiliation` | L'humiliation de trop : renvoyé en fin de service, il croise dans la rue un groupe qui le bouscule, le… |
| `ch01_reel_marc` | Le soir, chez Marc |
| `ch01_reel_chimiste` | Errance nocturne |
| `ch01_avocat_dose` | Première touche de l'Avocat, juste avant le choix : une phrase qui flatte la colère de Nathan (il… |
| `ch01_dose_choix` | Dose 1, offerte |
| `ch01_dose_hesitation` | Si prise : salle de bains, hésitation devant le miroir, puis la prise |
| `ch01_reel_passage` | Nathan s'effondre (épuisement, ou effet de la dose) et se réveille dans la plaine de cendres sous la… |
| `ch01_mara_ouverture` | Mara de face (M-T0 sans dose, M-T1 avec) |
| `ch01_rm_vagues` | Arène de la lune rouge (celle de la tranche verticale) |
| `ch01_rm_boss` | Premier boss : le Chœur des rieurs (choix de l'auteur), la moquerie faite chair : une masse de clients,… |
| `ch01_rm_plaisir` | Arrêt sur image après le coup final : Nathan découvre le plaisir inquiétant de la victoire (rire… |
| `ch01_rm_choix_fondateur` | Choix fondateur : accepter sa faiblesse ou commencer à la haïr |
| `ch01_mara_fin` | Mara de dos |
| `ch01_reel_reveil` | Retour au réel : réveil, sueur, courbatures inexplicables |

### Chapitre 2 — Le grain rouge (`ch02.md`)

| Identifiant | Scène |
|---|---|
| `ch02_reel_manque` | Nathan pense au monde parallèle, et au manque s'il a pris la dose |
| `ch02_reel_chimiste` | La deuxième dose n'est plus offerte, et l'argent ne l'intéresse pas |
| `ch02_avocat_poubelles` | L'Avocat minimise : ce ne sont que des déchets, personne ne le verra |
| `ch02_reel_poubelles` | Fouille des poubelles (activité jouable) : cuivre, objets cassés, biens revendables |
| `ch02_dose_choix` | Dose 2, contre la fouille des poubelles |
| `ch02_dose_culpabilite` | Si prise : première scène de culpabilité, courte (la honte plus que le crime), puis la dose |
| `ch02_reel_passage` | Passage dans le monde parallèle |
| `ch02_mara_ouverture` | Mara de face (M-T0 ou M-T1) |
| `ch02_rm_ville` | Ville médiévale pestiférée : les greniers sont contaminés par une peste surnaturelle et les… |
| `ch02_rm_conseil` | Le commandant veut sacrifier la ville pour arrêter la contagion |
| `ch02_rm_quarantaine` | Quarantaine : Nathan tient les portes pendant que le guérisseur travaille |
| `ch02_rm_evacuation` | Évacuation : escorte d'un convoi d'habitants à travers les faubourgs |
| `ch02_rm_purge` | Purge : Nathan aide le commandant à anéantir la ville |
| `ch02_avocat_purge` | Si purge : l'Avocat approuve, logiquement, calmement |
| `ch02_mara_fin` | Mara de dos |

### Chapitre 3 — Le prix du respect (`ch03.md`)

| Identifiant | Scène |
|---|---|
| `ch03_reel_chimiste` | Le Chimiste a remarqué une vieille dame du quartier qui porte un collier en or |
| `ch03_reel_collier` | Le collier en or |
| `ch03_reel_marc` | Marc remarque l'amaigrissement et les nuits blanches |
| `ch03_avocat_dose` | L'Avocat fait le compte : la vieille dame n'en a pas besoin, et lui, si |
| `ch03_dose_choix` | Dose 3, contre le collier (volé ou arraché) |
| `ch03_dose_culpabilite` | Si prise : scène de culpabilité complète, puis la dose |
| `ch03_reel_passage` | Passage dans le monde parallèle |
| `ch03_mara_ouverture` | Mara de face (M-T0 à M-T2) |
| `ch03_rm_arme` | Mara lui confie une arme capable de tuer les créatures de la peste : des gantelets pour Nathan sobre… |
| `ch03_rm_peste` | Combats contre les Voyous pestiférés |
| `ch03_rm_compagnon` | Un ancien compagnon d'armes de la Ville du grain rouge (le guérisseur, un garde du commandant ou un… |
| `ch03_rm_convaincre` | Convaincre : un dialogue |
| `ch03_rm_contourner` | Contourner : un chemin plus long, avec davantage de combats contre la garnison de Soldats privés… |
| `ch03_rm_executer` | Exécuter : rapide, sans combat |
| `ch03_rm_forteresse` | Combat à la porte de la forteresse contre les Soldats privés |
| `ch03_mara_fin` | Mara de dos |

### Chapitre 4 — La couronne de cendres (`ch04.md`)

| Identifiant | Scène |
|---|---|
| `ch04_reel_marc_argent` | Marc a compris que quelque chose ne va pas (et, si la vieille dame a été agressée, il a vu l'avis… |
| `ch04_reel_chimiste_demande` | Le prix de la dose 4 : le portefeuille de Marc |
| `ch04_avocat_demande` | L'Avocat minimise : Marc le lui doit bien, et il ne s'en rendra même pas compte |
| `ch04_reel_portefeuille` | Le vol du portefeuille : la nuit, Nathan entre dans la chambre de Marc qui dort |
| `ch04_dose_choix` | Dose 4, contre le portefeuille de Marc |
| `ch04_dose_culpabilite` | Si prise : scène de culpabilité complète, la première vraiment longue, puis la dose |
| `ch04_reel_passage` | Effondrement dans la salle de bains, passage |
| `ch04_mara_ouverture` | Mara de face (M-T0 à M-T2) |
| `ch04_rm_province` | Nathan découvre que le roi a abandonné une province entière pour préserver son armée : villages… |
| `ch04_rm_revolte` | Nathan prend la tête d'une révolte : les réfugiés et les survivants de la Ville du grain rouge se… |
| `ch04_rm_siege` | Siège de la forteresse (suite du ch. 3) |
| `ch04_rm_trone` | Dans la salle du trône, Nathan tient le roi à sa merci : le renverser sans le tuer, l'exécuter devant… |
| `ch04_avocat_roi` | L'Avocat plaide pour l'exécution : un roi vivant reste une menace |
| `ch04_mara_fin` | Mara de dos |

### Chapitre 5 — Le fils qui tue son père (`ch05.md`)

| Identifiant | Scène |
|---|---|
| `ch05_reel_marc_confrontation` | Marc découvre jusqu'où Nathan est allé |
| `ch05_reel_chimiste_demande` | Grande demande : le cœur de Marc (GDD v0.1, 10.1). « Les organes se revendent bien au marché noir.… |
| `ch05_avocat_demande` | L'Avocat, encore ambigu, fait le compte : Marc l'empêche d'avancer, Marc ne le comprend pas |
| `ch05_reel_marc_meurtre` | Si Nathan accepte : le meurtre de Marc, irréversible |
| `ch05_reel_marc_prevenu` | Prévenir Marc (possible s'il s'est confié au ch. 4, ou si l'Humanité est haute) : Marc part se cacher… |
| `ch05_reel_reseau_dejoue` | Déjouer le réseau : Nathan donne un faux rendez-vous et prévient la police ; le Chimiste perd un… |
| `ch05_dose_choix` | Dose 5, contre le cœur de Marc |
| `ch05_dose_culpabilite` | Si Marc est tué : la scène de culpabilité la plus violente du jeu, puis la dose |
| `ch05_reel_passage` | Passage |
| `ch05_mara_ouverture` | Mara de face (M-T0 à M-T3) |
| `ch05_rm_epidemie` | La peste gagne la forteresse |
| `ch05_rm_rituel` | Nathan apprend que le roi déchu, son père symbolique et dernier protecteur, possède le pouvoir… |
| `ch05_rm_choix_roi` | Accomplir le rituel (le roi vit, l'épidémie recule, pas de dose au ch. 6), tuer le roi pour prendre… |
| `ch05_rm_regicide` | Si Nathan choisit de tuer : boss, le roi déchu (design à proposer : vieux guerrier en armure de… |
| `ch05_rm_echo` | Si Nathan a tué Marc et le roi, les deux actes se répondent : une vision mêle les deux visages |
| `ch05_mara_fin` | Mara de dos |
| `ch05_reel_reveil` | Fin du chapitre : Nathan se réveille dans son appartement, couvert de sueur |
| `ch05_avocat_premiere_voix` | Pour la première fois, l'Avocat parle comme une voix séparée : « Tu as fait ce qu'il fallait |

### Chapitre 6 — La voix derrière le miroir (`ch06.md`)

| Identifiant | Scène |
|---|---|
| `ch06_reel_miroir` | La scène du miroir (texte de l'auteur, source section 4) : Nathan ouvre la bouche, une dent tombe dans… |
| `ch06_reel_intrusions` | Premières intrusions (source, section 5) : Nathan s'arrête quelques secondes, murmure, regarde un… |
| `ch06_reel_chimiste_demande` | Grande demande : l'acte imposé (GDD v0.1, 10.3, fusionné avec « le prix de son corps »). Nathan… |
| `ch06_avocat_demande` | L'Avocat minimise : ce n'est qu'un moment, personne ne saura, et la dose est au bout |
| `ch06_reel_humiliation` | Si Nathan accepte : la scène s'arrête avant tout acte (fondu au noir) |
| `ch06_reel_rupture` | S'il tente de rompre avec le réseau : il part en menaçant de parler |
| `ch06_reel_police` | Si Nathan a agressé la vieille dame (ch. 3) ou trahi le réseau (ch. 5) : un policier pose des… |
| `ch06_avocat_dose` | L'Avocat commente désormais les choix : il félicite la violence, reproche les hésitations et prétend… |
| `ch06_dose_choix` | Dose 6, contre l'acte imposé |
| `ch06_dose_culpabilite` | Si prise : scène de culpabilité |
| `ch06_reel_passage` | Passage |
| `ch06_mara_ouverture` | Mara de face (M-T0 à M-T3) |
| `ch06_rm_armes_feu` | Les armes à feu apparaissent dans le royaume : la garde (Soldats privés) et les mercenaires tirent |
| `ch06_rm_blessures` | Blessures fantômes : les coups reçus par le double laissent des marques sur le corps réel (bleus… |
| `ch06_rm_absence` | Une intrusion en plein combat : le double s'arrête une seconde et regarde le joueur |
| `ch06_mara_fin` | Mara de dos |

### Chapitre 7 — Le sang des autres (`ch07.md`)

| Identifiant | Scène |
|---|---|
| `ch07_reel_absences` | Les absences (source, section 5) : Nathan marche seul vers une porte ou fixe un objet ; de brèves… |
| `ch07_reel_innocents` | La violence déborde dans le réel : pendant ses absences, Nathan frappe des passants sans raison |
| `ch07_reel_chimiste_demande` | Grande demande : l'enfant (GDD v0.1, 10.2). « Le marché noir me demande un enfant. » Nathan comprend… |
| `ch07_reel_enfant` | Choix : refuser, mettre l'enfant à l'abri, saboter la transaction ou enlever l'enfant et le livrer |
| `ch07_reel_enquete` | Si l'enfant est livré : enquête, famille en détresse, pression policière (avis de recherche,… |
| `ch07_avocat_dose` | L'Avocat ne cherche plus d'excuses : il explique qu'il est trop tard pour reculer |
| `ch07_dose_choix` | Dose 7, contre l'enfant enlevé et livré |
| `ch07_dose_culpabilite` | Si l'enfant est livré : la crise est silencieuse |
| `ch07_reel_passage` | Passage |
| `ch07_mara_ouverture` | Mara de face (M-T0 à M-T4), sans ton coquin |
| `ch07_rm_vampirisme` | Avec 7 doses : Nathan draine la vitalité de ses ennemis (soin, mobilité surnaturelle) |
| `ch07_rm_combats` | Ennemi dominant : le Clown tueur (bestiaire 08 : pièges, tromperies, perception déstabilisée), dans… |
| `ch07_rm_choix_mara` | Mara choisit, selon les actes et les axes : tenter de le sauver (Humanité haute, ni Marc ni l'enfant… |
| `ch07_rm_duel_mara` | Si Mara choisit de le combattre : combat contre Mara dans l'arène de la lune rouge (elle s'enfuit à 30… |
| `ch07_mara_fin` | Mara de dos, debout, la tête tournée, grave : elle ne nargue plus |

### Chapitre 8 — Le monde est coupable (`ch08.md`)

| Identifiant | Scène |
|---|---|
| `ch08_reel_complots` | Nathan ne distingue plus les coïncidences des complots : refus, critiques et gestes ordinaires… |
| `ch08_avocat_coupables` | L'Avocat accuse la société, les créateurs, ses anciens camarades |
| `ch08_avocat_quatrieme_mur` | « C'est ta faute » (texte de l'auteur, source section 4) : écran noir, menu de pause qui se déforme,… |
| `ch08_reel_chimiste` | Le Chimiste, pour la première fois, a peur : ses propres fournisseurs, bien plus dangereux que lui,… |
| `ch08_reel_dents` | Les dents en or : Nathan suit un passant qui sourit avec des dents en or et le frappe jusqu'à les lui… |
| `ch08_dose_choix` | Dose 8, contre les dents en or |
| `ch08_dose_culpabilite` | Si prise : culpabilité, presque sans larmes |
| `ch08_mara_ouverture` | Mara apparaît dans la ville réelle, alliée, adversaire ou complice selon le ch. 7 (tenue selon les… |
| `ch08_rm_invasion` | Séquences où le joueur doit distinguer le réel de l'hallucination : les créatures hallucinées ont… |
| `ch08_rm_desobeissance` | Désobéissance (source, section 5) : la voix commente les commandes ; Nathan peut refuser une action,… |
| `ch08_rm_combats` | Ennemi dominant : le Démon techno (bestiaire 09 : zones électrifiées, téléportation), né des… |
| `ch08_mara_fin` | Mara de dos, dans la rue |

### Chapitre 9 — Le roi sans royaume (`ch09.md`)

| Identifiant | Scène |
|---|---|
| `ch09_reel_isolement` | Plus personne : Marc mort, caché ou brouillé, téléphone muet, porte fermée |
| `ch09_avocat_tout_detruire` | La voix soutient que Nathan doit tout détruire pour empêcher le monde de tomber entre de mauvaises… |
| `ch09_reel_innocent` | Faire accuser un innocent : la police cherche un coupable (l'enlèvement, les agressions, les dents) |
| `ch09_dose_choix` | Dose 9, contre l'innocent accusé |
| `ch09_reel_passage` | Passage |
| `ch09_mara_ouverture` | Mara de face, selon son rôle (alliée, adversaire, complice), ou absente si elle a disparu au ch. 7 |
| `ch09_rm_retours` | Les anciens alliés reviennent selon les choix : le guérisseur ou les réfugiés (ch. 2), le commandant… |
| `ch09_rm_guerre` | Bataille contre les forces qui veulent fusionner les deux mondes |
| `ch09_rm_controle` | Qui joue qui ? Les interfaces des deux mondes se mélangent ; certaines animations se déclenchent avant… |
| `ch09_mara_fin` | Mara de dos : selon son rôle, une promesse, une menace ou un adieu |

### Chapitre 10 — Red Met (`ch10.md`)

| Identifiant | Scène |
|---|---|
| `ch10_avocat_derniere_dose` | La dernière dose, proposée par l'Avocat (source, section 7) : « Tu as sacrifié ton père |
| `ch10_reel_sang` | Le faux don du sang : Nathan se présente à l'hôpital pour donner son sang |
| `ch10_dose_choix` | Dose 10, contre les poches de sang volées |
| `ch10_rm_fusion` | Traversée de la ville fusionnée : des ennemis de tous les actes, et tout ce qui a été appris |
| `ch10_rm_double` | Combat final contre le double (M5), créature gigantesque qui représente tout ce que Nathan a sacrifié… |
| `ch10_rm_choix_final` | Le dernier choix : détruire la source de la Red Met, prendre le contrôle de son pouvoir ou accepter la… |
| `ch10_mara_fin_immorale` | Voie immorale (au moins 6 doses sur 10 et au moins 2 des 3 grandes demandes acceptées : cœur de Marc,… |
| `ch10_mara_fin_morale` | Voie morale (tous les autres cas) |
| `ch10_effondrement` | Dans tous les cas : les yeux de Nathan se révulsent (comme l'Undertaker), il s'effondre |
| `ch10_hopital` | Lit d'hôpital, coma, sons calmes et cliniques |
| `ch10_generique` | Le générique varie selon le dernier choix (survivre, régner, disparaître, recommencer le cycle),… |
