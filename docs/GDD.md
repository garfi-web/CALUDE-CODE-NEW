# RED MET — Game Design Document (synthèse v0.1)

> Synthèse du GDD conceptuel et de la direction narrative « Dix chapitres pour devenir un monstre ».
> Public : adultes. Thèmes : dépendance, violence, dégradation psychologique, dilemmes moraux.

## Vision

**High concept.** Nathan Voss, 18 ans, fragile et en colère, découvre la **Red Met**, une drogue fictive qui l'envoie dans un monde parallèle de dark fantasy où il devient puissant. Chaque dose le rend plus fort là-bas et le détruit davantage dans la réalité.

**Le paradoxe :** plus le joueur devient efficace au combat, plus il perd ce qui faisait de lui un être humain.

**La question finale :** « Jusqu'où suis-je prêt à aller pour ne plus être faible ? »

Les inspirations : Arthas (*Warcraft III*) pour la chute du héros, une succession de choix présentés comme « nécessaires », et *Fallout 2/3* et *Wasteland 3* pour le karma et les conséquences durables.

### Les quatre piliers
1. **La morale a un coût réel.** Sans Red Met, il n'y a pas de progression de puissance ; la survie passe par la maîtrise.
2. **La puissance est une dépendance.** Les avantages sont immédiats et le cercle est vicieux.
3. **Le corps raconte l'histoire.** La transformation physique est visible.
4. **Le monde parallèle reflète l'état intérieur.** Le décor, les ennemis, les effets et les sons évoluent avec le personnage.

## Structure : deux mondes en alternance

Le déroulé détaillé de chaque chapitre (scènes, choix, ennemis, dose proposée, identifiants Ink) est dans [`docs/SCENARIO.md`](SCENARIO.md).

Chaque chapitre contient au moins une séquence dans la **réalité** (descente sociale, physique et psychologique) et une dans le **monde de la Red Met** (épopée dark fantasy). Les actes réels modifient les ressources, les relations et les ennemis de l'autre monde.

| Ch. | Titre | Réalité | Red Met |
|---|---|---|---|
| 1 | Le garçon invisible | Humiliation, rencontre du Chimiste | Découverte, Mara, premier boss |
| 2 | Le grain rouge | Premières dettes | Ville pestiférée : quarantaine, évacuation ou purge |
| 3 | Le prix du respect | Poubelles, usurier, petits vols | Arme anti-peste, ancien compagnon d'armes |
| 4 | La couronne de cendres | Rupture familiale | Révolte contre le roi |
| 5 | Le fils qui tue son père | Point de rupture avec Marc | Régicide ; la voix apparaît |
| 6 | La voix derrière le miroir | Perte de contrôle | Armes à feu, blessures fantômes |
| 7 | Le sang des autres | Crimes, paranoïa | Vampirisme ; Mara doute |
| 8 | Le monde est coupable | Persécution imaginaire | Invasion du réel |
| 9 | Le roi sans royaume | Isolement | Guerre finale ; les alliés reviennent selon les choix |
| 10 | Red Met | Fusion des deux mondes | Combat contre son double ; fins multiples |

### Un niveau du monde parallèle
Chaque incursion dans le monde de la Red Met suit le même déroulé :
1. **Dans le réel, le choix de la dose** : la prendre ou la refuser. L'Avocat peut intervenir juste avant.
2. **Ouverture avec Mara** (visual novel, Mara de face) : elle accueille Nathan selon ce qu'il est devenu.
3. **Combats** (vagues, puis boss selon le chapitre). En fin de combat, Mara peut réagir à la puissance de Nathan par une courte réplique.
4. **Fin avec Mara** (visual novel, Mara de dos) : elle juge Nathan, le nargue, puis disparaît (fondu ou dissolution en brume rouge).

Une partie est une suite d'incursions numérotées ; ce numéro règle la force des ennemis (voir « Difficulté »).

**Fin préférée.** La voix n'a jamais eu besoin de contrôler Nathan : elle a seulement présenté chaque mauvaise décision comme une nécessité. Le dernier choix ne se fait pas entre le bien et le mal, mais entre **assumer ses actes** et **continuer à les justifier**. Le ch. 10 se conclut ensuite par la scène avec Mara (voie immorale ou morale), puis par l'effondrement, le coma et la fiche médicale « RED MET 2 » (voir « La fin avec Mara »).

## Systèmes

### Karma caché : quatre axes
| Axe | Effet |
|---|---|
| **Humanité** | Réconciliations possibles, alliés disponibles |
| **Cruauté** | Intimidation, mais peur, représailles et isolement |
| **Dépendance** | Hallucinations, pertes de contrôle, résistance à la prochaine prise |
| **Paranoïa** | Altère les dialogues, l'affichage, les sons et parfois les commandes |

Le jeu **se souvient des actes**, et pas seulement d'un score. Les contradictions sont permises : un remords sincère après une atrocité, un geste généreux qui sert à manipuler.

Le GDD v0.1 propose aussi deux variables simples, l'**indice de corruption morale** et le **niveau de consommation** ; elles peuvent servir d'implémentation de départ.

### Progression des doses
Le jeu a **10 niveaux** et le joueur peut prendre **jusqu'à 10 doses**, une par niveau. Chaque dose augmente les PV et la force de Nathan, et débloque les armes dans l'ordre des documents de l'auteur. Les stades suivent les mêmes seuils que les tenues de Mara (décision de l'auteur du 2026-10-28, reportée dans `Data/Story/Progression.asset`).

| Doses | Stade | Débloque | Effet |
|---|---|---|---|
| 0 — Sobre | M0 | Poings, pieds, esquive | Santé faible et fixe, combat exigeant |
| 1 — Première dose | M1 | Lames et armes tranchantes | PV et force accrus ; premiers démembrements |
| 2 | M1 | — | PV et force |
| 3 — Dépendance | M2 | Armes à feu (pistolet 9 mm) | Éliminations rapides |
| 5 — Escalade | M3 | Lance-flammes, lance-scie découpeuse | Dégâts de zone |
| 6 — Arsenal extrême | M3 | Lance-roquettes, grenades | Destruction massive, gestion des munitions |
| 7 — Mutation | M4 | Pouvoirs vampiriques (*inFAMOUS 2: Festival of Blood*) et **fusil à pompe** | Drain de vie, mobilité surnaturelle ; pulvérise les humains de l'Acte I |
| 8 à 10 | M4 | Pouvoirs renforcés, capacité ultime (jauge) | Vers le double du ch. 10 |

- **Voie sobre** : Nathan ne se bat qu'avec **les poings et les pieds**, avec une **santé faible** qui n'augmente pas, mais il **se déplace et saute normalement** (course, saut, dash, esquive identiques). Il lui faut environ 4 fois plus de coups pour tuer un ennemi.
- **Gore** : la voie Red Met amène des **démembrements sanglants**, dans l'esprit de *L'Ombre de la Guerre* : armes tranchantes dès la 1re dose, puis armes lourdes et fusil, qui pulvérisent les corps. Le réglage de gore (`GameOptions`) s'applique à tout : « complet », démembrements et morceaux ; « réduit », du sang sans morceaux ; « désactivé », aucun sang, et le corps s'effondre. Les coups de poing et de pied ne font jamais saigner : sueur, salive, étincelles.

### Le fusil à pompe (Dealer et Nathan M4)
Le **Dealer** porte un fusil à pompe (et non plus une arme de poing). Il ne tire jamais sans avertissement : environ 0,9 s (réglable dans son AttackData) pendant lesquelles il arme le fusil avec un « tchak-tchak » reconnaissable, l'épaule et vise Nathan. Un cône de danger rouge part du canon, plus dense à bout portant, et une lueur rouge pulse au bout du canon. La visée se fige 0,25 s avant le coup.
- **Bout portant (2,5 m)** : mort instantanée de Nathan, quels que soient ses PV. Son corps explose dans le sens du tir (membres, torse, tête, boyaux, sang, taches), avec arrêt sur image, flash, ralenti de 0,5 s, fort tremblement, puis l'écran de défaite.
- **Moyenne distance (jusqu'à 5,5 m)** : gros dégâts et projection. **Au-delà** : plombs dispersés, dégâts faibles.
- **Survivre** : sortir du cône, ou esquiver au dernier moment. L'invulnérabilité de l'esquive (0,40 s) couvre le tir, mais pas toute l'annonce : c'est un réflexe.
- Après chaque tir, il recharge lentement (1,25 s) : c'est la fenêtre pour le frapper. Il garde ses gardes du corps autour de lui.

**Nathan M4** (sous Red Met) dispose du même fusil, sur une touche dédiée : c'est la sensation de toute-puissance.
- À courte portée, les humains de l'Acte I sont réduits en morceaux et projetés. À moyenne portée : projection contre les murs et étourdissement. Le tir est en cône.
- Impact maximal : gros recul de Nathan, flash du canon, onde de choc, fort tremblement, arrêt sur image, douille éjectée, ralenti bref quand un tir tue plusieurs ennemis.
- **Équilibrage** : 6 cartouches, rechargement cartouche par cartouche (l'esquive l'interrompt), cadence lente avec le temps de pompe. Les ennemis surnaturels des Actes II et III et les boss ne sont jamais pulvérisés en un tir : ils encaissent et réagissent selon leur fiche.
- Le réglage de gore s'applique : « complet », explosion en morceaux ; « réduit », sang sans morceaux ; « désactivé », le corps s'effondre simplement.

**Équilibrage.** Sur la voie sobre, un ennemi standard demande environ 4 fois plus d'actions offensives. La difficulté doit venir du timing, des fenêtres de contre-attaque, de l'endurance et de la fuite, jamais de dégâts gonflés pour punir la voie sobre.

### Difficulté : elle vient des choix
Il n'y a **aucun choix de difficulté dans le menu**.
- **Les ennemis montent en puissance à chaque incursion**, quelle que soit la trajectoire : PV, dégâts, nombre, armures, nouvelles attaques. Les valeurs vivent dans des fichiers de données par numéro d'incursion, jamais en dur.
- **Nathan monte en puissance avec les doses** : stade (M0 à M4), armes et PV suivent le nombre de doses prises (tableau ci-dessus et `docs/CHARACTERS.md`).
- Celui qui **prend une dose à chaque niveau** devient de plus en plus fort et le jeu devient plus facile : la facilité est la tentation.
- Celui qui **refuse la Red Met et les choix immoraux** reste M0 face à des ennemis qui grandissent : il doit maîtriser l'esquive, les fenêtres de contre-attaque et le placement. C'est la voie exigeante, et toujours possible à terminer.

### Les demandes du Chimiste
Au début, la dose s'achète (offerte, puis à crédit, puis contre de l'argent). Ensuite, **le Chimiste exige des choix moraux** (GDD v0.1, section 10). Refuser une demande, c'est refuser la dose de ce niveau ; il reste disponible avec la demande suivante.
1. **L'humiliation imposée** (ch. 4, section 10.3) : une faveur sexuelle exigée pour prouver que Nathan est « prêt à tout ». Mise en scène **non explicite**, comme une contrainte et une perte de contrôle, jamais comme une récompense érotique. Nathan peut accepter, refuser ou tenter de rompre avec le réseau.
2. **Le cœur de Marc** (ch. 5, section 10.1) : « Les organes se revendent bien au marché noir. J'ai une demande pour un cœur. Tue ton père, ramène-le-moi, et tu auras ta prochaine dose. » Nathan peut refuser, protéger son père ou déjouer le réseau. Le meurtre est irréversible.
3. **L'enfant** (ch. 7, section 10.2) : « Le marché noir me demande un enfant. » Nathan peut refuser, mettre l'enfant à l'abri ou saboter la transaction. Aucun détail pratique ; enquête, détresse de la famille, pression policière.

Placement dans les chapitres validé par l'auteur (`docs/SCENARIO.md`) ; « le prix de son corps », dilemme du réel, se place au ch. 6.

### Scène de culpabilité
**Avant chaque dose** payée par un acte immoral, Nathan se recroqueville en boule, la tête entre les mains, et pleure (« Qu'est-ce que je suis en train de devenir… Je deviens un monstre »), puis consomme sa dose (GDD v0.1, sections 4.3 et 10.4). Une dose payée sans acte immoral n'a droit qu'à une courte hésitation. Au fil du jeu, ces crises deviennent plus courtes, plus violentes ou plus silencieuses.

### Perte de contrôle (chapitres 6 à 10)
Cela commence par des intrusions brèves (ch. 6), puis des absences (ch. 7), de la désobéissance (ch. 8), et enfin des animations qui précèdent la commande (ch. 9-10). Ces pertes doivent toujours rester **courtes, scénarisées et annoncées**. Il faut prévoir des options d'accessibilité et ne jamais laisser croire au joueur que son matériel est défectueux.

### État de la partie
Un état central, sauvegardé et rechargé avec la partie, décide de tout ce qui précède :
- les **doses** : leur nombre, et pour chaque niveau, dose prise ou refusée ;
- les **actes commis** (une liste de faits, pas un score) ;
- les **quatre axes** (Humanité, Cruauté, Dépendance, Paranoïa) ;
- le **stade de Nathan** dans chaque monde (M0 à M4, R0 à R4) et le **stade de tenue de Mara** (M-T0 à M-T4) ;
- le **numéro de l'incursion** ;
- l'**argent** et la **dette** envers le Chimiste (prix des doses : `docs/SCENARIO.md`) ;
- la **scène en cours** et les scènes vues (identifiants de `docs/SCENARIO.md`), pour reprendre la partie.

Les dialogues, la puissance de Nathan, celle des ennemis et la tenue de Mara lisent cet état. Aucune scène ne code un stade en dur.

## Visual novels : Mara et l'Avocat

Les scènes de dialogue prennent la forme d'un visual novel : grand portrait, boîte de texte avec le nom, choix multiples, défilement automatique, avance rapide, historique. Les dialogues sont écrits dans des fichiers **Ink** (texte à embranchements) que l'auteur rédige lui-même ; chaque réplique porte un identifiant unique pour y brancher plus tard sa voix ElevenLabs. Les emojis restent dans le texte affiché mais sont retirés du texte envoyé à la synthèse vocale.

### Mara suit la Red Met
- **Tenue** : elle dépend du **nombre de doses prises**, pas du chapitre. Plus Nathan a pris de Red Met, plus la tenue est sexy et échancrée, du stade M-T0 (entièrement couverte) au stade M-T4 (`docs/CHARACTERS.md`). Le refus de toutes les doses garde Mara en M-T0.

  | Doses prises | 0 | 1 ou 2 | 3 ou 4 | 5 ou 6 | 7 et plus |
  |---|---|---|---|---|---|
  | Tenue de Mara | M-T0 | M-T1 | M-T2 | M-T3 | M-T4 |

  *Décision de l'auteur du 2026-10-28* : ces seuils (0 / 1-2 / 3-4 / 5-6 / 7+) remplacent ceux validés le même jour (0 / 1 / 2-3 / 4 / 5+), car le jeu a 10 niveaux et jusqu'à 10 doses.

- **Portraits** : pour chaque stade de tenue, un portrait **de face** (scène d'ouverture) et un portrait **de dos** (scène de fin), chacun avec plusieurs expressions. Mara est toujours une femme adulte, habillée, sans nudité. Les portraits de dos partent d'un rendu du modèle 3D de Mara dans Blender (pose, tête tournée par-dessus l'épaule), pour garantir la ressemblance, puis sont affinés en illustration HD dans ComfyUI avec Animagine XL 4.0 (ControlNet sur la pose et les contours). Un essai en M-T0 est validé par l'auteur avant les autres stades.
- **Ton** : au début, elle taquine et rabaisse Nathan. Au fil des incursions, surtout s'il devient le Nathan M4 bestial, elle est de plus en plus attirée par sa force, provocante et coquine. En fin de combat, elle peut réagir à sa puissance.

### La scène de fin de niveau : Mara de dos
À la fin de chaque niveau du monde parallèle, Mara apparaît de dos, cambrée, la tête à moitié tournée pour regarder Nathan par-dessus l'épaule d'un air moqueur. Elle le nargue, puis disparaît (fondu ou dissolution en brume rouge).
- **Nathan encore faible** (doses refusées ou stade M0-M1) : elle se moque. Réplique de test de l'auteur :
  > « Mon petit Nathan, je t'excite ? Je te fais envie ? Tu aimerais que je pose mes fesses sur ton visage, petit coquin ? Eh bien, deviens plus puissant, car je recherche un vrai bonhomme 💪. Les gamins comme toi, je les envoie en colo. »
- **Nathan puissant et bestial** (M2 à M4) : ses répliques sont moins moqueuses, plus troublées et attirées par sa force. Les embranchements existent dans Ink ; l'auteur écrira les répliques.

### L'Avocat
Il intervient de temps en temps, **dans le réel comme dans le monde parallèle**, dès le début du jeu ; il ne devient le véritable antagoniste qu'après le chapitre 5. Paranoïaque, il pousse surtout Nathan à reprendre une dose et trouve des arguments logiques pour lui faire accepter les choix amoraux.
- Répliques **courtes en surimpression** : sous-titre et effet de distorsion, sans interrompre le jeu.
- Déclencheurs : avant un choix de dose, après un acte immoral, quand Nathan est en manque ou blessé, et parfois en exploration.
- Un délai minimum entre deux interventions évite de saturer le joueur. Même système d'identifiants pour les voix.

## La fin avec Mara
*Décision de l'auteur du 2026-10-28* : la fin suit les sections 11.1, 11.2 et 12 du GDD v0.1 (`docs/source/RED_MET_GDD_v0.1.md`). La section 11.1 remplace la nuit consentie validée le même jour, et **l'emplacement vide pour un illustrateur est supprimé**.
- **Voie immorale** (section 11.1), si Nathan a pris **au moins 6 doses sur 10 et accepté au moins 2 des 3 grandes demandes** du Chimiste (humiliation, cœur de Marc, enfant ; `GameSession.ImmoralEnding`) : Nathan, dopé et submergé par son agressivité, franchit une limite et tente d'imposer sa volonté à Mara par la force. La scène est mise en scène comme un moment d'horreur et de rupture, **coupée avant l'acte, sans nudité ni représentation explicite**. Le refus et la réaction de Mara, et les conséquences, restent au centre ; elle n'est jamais une récompense. Sa puissance n'apporte à Nathan ni maîtrise de lui-même ni accomplissement.
- **Voie morale** (section 11.2), dans tous les autres cas : Mara le jauge, met en avant sa propre force et le renvoie : « Moi, je veux un vrai bonhomme. Toi, tu n'es qu'un gamin, et les gamins, je les envoie en colo. » Scène volontairement humiliante et frustrante : l'intégrité n'apporte pas de récompense romantique.
- **Conclusion commune** (section 12) : yeux révulsés, chute, écran noir, lit d'hôpital, coma, fiche médicale qui porte en bas la mention **RED MET 2**.
- **Avertissement** : au premier lancement, un écran de confirmation d'âge (18 ans et plus) et un avertissement sur les thèmes du jeu. Claude ne génère aucune image de nudité ni de scène sexuelle.

## Personnages

- **Nathan Voss** (18 ans, **adulte**). Fiche complète dans `docs/CHARACTERS.md`. Au départ il est un peu enrobé, avec des lunettes à double foyer et de l'acné. Dans la réalité, il se dégrade : insomnie, amaigrissement, dents abîmées, tremblements, corps émacié. Son double, lui, gagne en puissance : d'abord faible, il devient un guerrier massif, puis un prédateur vampirique et une créature quasi divine. Les animations portent ce contraste : dans la réalité Nathan trébuche, dans l'autre monde ses pas font trembler le sol.
- **L'Avocat** : la voix intérieure, présente par touches dès le début (voir « Visual novels »), véritable antagoniste après le chapitre 5. Sarcastique, paranoïaque, vulgaire, parfois drôle, puis soudain terrifiante. Elle trouve une justification à chaque atrocité et brise le quatrième mur. Ses accusations contre des boucs émissaires (dont les créateurs LGBTQ+) relèvent **de sa paranoïa, jamais du verdict du scénario**.
- **Mara** (adulte) : guerrière charismatique et provocante. D'abord moqueuse et méprisante, elle est de plus en plus attirée par la force de Nathan à mesure qu'il prend la Red Met, et sa tenue suit ses doses (voir « Visual novels »). Elle peut le sauver, le combattre ou poursuivre ses propres ambitions, et reste une combattante dans toutes les branches.
- **Marc** : le père. Le confronter, le voler ou le tuer sont des choix irréversibles.
- **Le Chimiste**, le dealer, l'usurier. Le Chimiste est le fournisseur de Red Met, calme, élégant et non combattant (`docs/CHARACTERS.md`) ; les dealers qu'on affronte sont ses lieutenants de rue (ennemi 02 du bestiaire).

## Bestiaire

Les 12 ennemis, répartis en 3 actes (des menaces humaines aux aberrations), sont décrits dans [`docs/BESTIARY.md`](BESTIARY.md). Leurs fiches jouables (PV, dégâts, armements, fenêtres de vulnérabilité, contre-stratégies, valeurs sobre et Red Met) sont dans [`docs/ENEMIES.md`](ENEMIES.md). Règle d'équilibrage : un ennemi ne change pas selon la trajectoire, ce sont les capacités de Nathan qui changent. Les ennemis se renforcent seulement avec le numéro de l'incursion, de la même façon pour tous les joueurs.

## Lignes directrices des scènes sensibles

Le jeu n'interdit pas les actes les plus sombres, mais il leur donne un **poids narratif durable**. Des portes se ferment et des personnages ne reviennent jamais.
- Les victimes ne sont jamais des objets d'inventaire ni des récompenses. Leur peur et les conséquences restent au centre de la scène.
- Les scènes de contrainte sexuelle restent **non explicites**. Elles sont mises en scène comme de la vulnérabilité et une perte de contrôle, jamais comme un contenu érotique. Tous les personnages concernés sont adultes.
- La scène finale de la voie immorale (section 11.1 du GDD v0.1) suit la règle ci-dessus : coupée avant l'acte, sans nudité, centrée sur la réaction de Mara.
- Les branches de rapt ou de livraison d'enfant ne contiennent **aucun détail pratique**. Elles entraînent une enquête, la détresse de la famille et une rupture morale majeure. **Aucun contenu sexuel ni suggestif** dans cette branche ni à proximité : les scènes de Mara du même chapitre restent sans ton coquin, et aucune scène pour adultes n'y fait suite ou n'y renvoie.
- La dégradation physique est une représentation **fictionnelle**. Elle n'est ni une description universelle des personnes dépendantes ni un jugement moral sur l'apparence.

## Direction artistique et sonore

- Anime HD, dark fantasy, une esthétique organique et dérangeante. Les références visuelles des personnages sont fournies par l'auteur.
- **Son** : respiration, battements cardiaques et voix qui évoluent avec la dépendance. Les phases sobres reposent sur le silence et la tension, les phases Red Met sur l'agressivité. La fin revient à une ambiance clinique.
- **Interface** : santé, endurance, munitions, capacités et argent. Aucun score moral affiché. Options pour régler les secousses de caméra et les distorsions.

## Production

| Prototype | Objectif | État |
|---|---|---|
| 1 — Arène de combat | Déplacement, combo x3, esquive, IA, PV, HUD | ✅ Code dans `Assets/_Game` |
| 1b — Sobre contre Red Met | Même arène, deux configurations ; mesurer survie et satisfaction | À faire |
| 2 — Transformation physique | Plusieurs versions de Nathan cohérentes avec son gameplay | À faire |
| 3 — Dilemme | Mission courte à deux choix aux conséquences fortes | À faire |
| 4 — Narratif | État de la partie et sauvegarde, visual novels (Ink), Avocat, confirmation d’âge et fin de la section 11.1 ; mini-chapitre joué deux fois (toutes les doses, puis aucune) | En cours (étape 1 : ce document) |

### Pipeline d'assets
- **Tripo** (via Blender MCP `generate_3d`, ou le MCP Tripo) : modèles 3D de base des personnages et des accessoires.
- **Blender** : pose, éclairage toon, rendu orthographique en planches de sprites (2D latéral).
- **Unity** : import des sprites et animations dans `Assets/_Game/Art` et `Assets/_Game/Animations`, raccordement à `CharacterAnimator`.
- **ElevenLabs** : voix de L'Avocat, de Nathan et de Mara, plus les effets sonores. Dossier `Assets/_Game/Audio/Voice`.
