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

**Fin préférée.** La voix n'a jamais eu besoin de contrôler Nathan : elle a seulement présenté chaque mauvaise décision comme une nécessité. Le dernier choix ne se fait pas entre le bien et le mal, mais entre **assumer ses actes** et **continuer à les justifier**. Variante du GDD v0.1 : effondrement, coma, fiche médicale « RED MET 2 ».

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
| Niveau | Débloque | Effet |
|---|---|---|
| 0 — Sobre | Poings, pieds, esquive | Santé faible, combat exigeant |
| 1 — Première dose | Armes blanches | PV et force accrus |
| 2 — Dépendance | Armes à feu | Éliminations rapides |
| 3 — Escalade | Lance-flammes, lance-scie | Dégâts de zone |
| 4 — Arsenal extrême | Lance-roquettes, grenades | Destruction massive, gestion des munitions |
| 5 — Mutation | Pouvoirs vampiriques | Drain de vie, mobilité surnaturelle, ultime |

**Équilibrage.** Sur la voie sobre, un ennemi standard demande environ 4 fois plus d'actions offensives. La difficulté doit venir du timing, des fenêtres de contre-attaque, de l'endurance et de la fuite, jamais de dégâts ennemis artificiellement gonflés.

### Scène de culpabilité
Après chaque acte immoral majeur, Nathan se recroqueville et pleure (« Qu'est-ce que je suis en train de devenir… Je deviens un monstre »), puis consomme sa dose. Au fil du jeu, ces crises deviennent plus courtes, plus violentes ou plus silencieuses.

### Perte de contrôle (chapitres 6 à 10)
Cela commence par des intrusions brèves (ch. 6), puis des absences (ch. 7), de la désobéissance (ch. 8), et enfin des animations qui précèdent la commande (ch. 9-10). Ces pertes doivent toujours rester **courtes, scénarisées et annoncées**. Il faut prévoir des options d'accessibilité et ne jamais laisser croire au joueur que son matériel est défectueux.

## Personnages

- **Nathan Voss** (18 ans, **adulte**). Dans la réalité, il se dégrade : acné et maigreur, puis insomnie, dents abîmées, tremblements, corps émacié. Son double, lui, gagne en puissance : d'abord faible, il devient un guerrier massif, puis un prédateur vampirique et une créature quasi divine. Les animations portent ce contraste : dans la réalité Nathan trébuche, dans l'autre monde ses pas font trembler le sol.
- **L'Avocat** : la voix intérieure, véritable antagoniste après le chapitre 5. Sarcastique, paranoïaque, vulgaire, parfois drôle, puis soudain terrifiante. Elle trouve une justification à chaque atrocité et brise le quatrième mur. Ses accusations contre des boucs émissaires (dont les créateurs LGBTQ+) relèvent **de sa paranoïa, jamais du verdict du scénario**.
- **Mara** (adulte) : guerrière charismatique et provocante. D'abord méprisante, elle est ensuite attirée par la force de Nathan, puis horrifiée par sa dégradation. Elle peut le sauver, le combattre ou poursuivre ses propres ambitions.
- **Marc** : le père. Le confronter, le voler ou le tuer sont des choix irréversibles.
- **Le Chimiste**, le dealer, l'usurier.

## Lignes directrices des scènes sensibles

Le jeu n'interdit pas les actes les plus sombres, mais il leur donne un **poids narratif durable**. Des portes se ferment et des personnages ne reviennent jamais.
- Les victimes ne sont jamais des objets d'inventaire ni des récompenses. Leur peur et les conséquences restent au centre de la scène.
- Les scènes de contrainte sexuelle restent **non explicites**. Elles sont mises en scène comme de la vulnérabilité et une perte de contrôle, jamais comme un contenu érotique. Tous les personnages concernés sont adultes.
- Les branches de rapt ou de livraison d'enfant ne contiennent **aucun détail pratique**. Elles entraînent une enquête, la détresse de la famille et une rupture morale majeure.
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

### Pipeline d'assets
- **Tripo** (via Blender MCP `generate_3d`, ou le MCP Tripo) : modèles 3D de base des personnages et des accessoires.
- **Blender** : pose, éclairage toon, rendu orthographique en planches de sprites (2D latéral).
- **Unity** : import des sprites et animations dans `Assets/_Game/Art` et `Assets/_Game/Animations`, raccordement à `CharacterAnimator`.
- **ElevenLabs** : voix de L'Avocat, de Nathan et de Mara, plus les effets sonores. Dossier `Assets/_Game/Audio/Voice`.
