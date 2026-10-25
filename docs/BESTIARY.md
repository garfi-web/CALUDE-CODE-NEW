# RED MET
## BESTIAIRE — CONCEPTION DES ENNEMIS ET PROGRESSION DES ACTES
**Document de game design — Version 1.0**

## 1. Objectifs de conception du bestiaire

Le bestiaire de RED MET doit accompagner la transformation progressive du jeu. Les ennemis rencontrés au début doivent être crédibles, humains et familiers, inspirés de la violence urbaine contemporaine. Plus le protagoniste s'enfonce dans la dépendance à la Red Met et progresse dans les mondes parallèles, plus ses adversaires doivent devenir grotesques, imprévisibles et surnaturels.

Le joueur doit ressentir une rupture progressive entre la réalité et le cauchemar. Un individu qui aurait pu être croisé dans une rue ordinaire peut laisser place à un monstre dont la silhouette, les mouvements et les capacités défient les lois physiques.

Cette évolution doit être visuelle, narrative et mécanique. Il ne suffit pas de rendre les ennemis plus résistants : leurs comportements, leurs animations, leurs modes d'attaque et les règles des affrontements doivent progressivement changer.

Le bestiaire repose sur trois catégories :

- **Les menaces humaines :** des adversaires crédibles dont la supériorité repose sur le nombre, les armes, la discipline ou la manipulation.
- **Les menaces déformées :** des ennemis dont le corps et le comportement deviennent anormaux, annonçant la transition vers le cauchemar.
- **Les aberrations :** des créatures surnaturelles capables de déformer l'environnement, de manipuler les perceptions du joueur et de remettre en question les règles habituelles du combat.

Le jeu comprend douze archétypes principaux. Chacun doit posséder une silhouette reconnaissable, une fonction précise dans les combats et une réponse tactique que le joueur peut apprendre à maîtriser.

---

# 2. ACTE I — LA VIOLENCE DU MONDE RÉEL

## Direction générale

Le premier acte se déroule dans un univers urbain contemporain : rues dégradées, parkings, commerces nocturnes, immeubles délabrés, entrepôts et quartiers contrôlés par différents réseaux criminels.

Les ennemis sont intégralement humains. Ils utilisent des armes conventionnelles, se déplacent de manière crédible et disposent de comportements immédiatement compréhensibles.

Cet acte sert à apprendre au joueur les fondamentaux du système de combat. Le protagoniste est encore relativement faible. Sans Red Met, il doit éviter les affrontements prolongés, exploiter les ouvertures et accepter de battre en retraite lorsque la situation devient trop dangereuse.

La difficulté repose avant tout sur les rapports de force, les adversaires armés et les combats en infériorité numérique.

## ENNEMI 01 — LE VOYOU CLASSIQUE

**Rôle :** ennemi de base au corps à corps.

**Première apparition :** début de l'acte I.

### Apparence et direction artistique

Le Voyou classique représente une menace humaine familière. Il porte une veste en cuir ou une doudoune sombre, un sweat à capuche, un pantalon de survêtement ou un jean usé et des baskets. Son apparence est volontairement réaliste, sans équipement militaire ni signes surnaturels.

Les variantes doivent comprendre différentes morphologies, coiffures, accessoires et tenues afin d'éviter l'impression de combattre continuellement le même personnage.

### Comportement et combat

Le Voyou classique se montre agressif, peu discipliné et plus dangereux en groupe. Il se précipite sur le protagoniste, tente de le frapper rapidement et profite de ses moments de vulnérabilité.

Certains exemplaires se battent à mains nues, tandis que d'autres utilisent des couteaux ou des objets improvisés.

Le joueur doit apprendre à esquiver, à interrompre une attaque et à frapper au moment opportun. Un affrontement contre plusieurs voyous doit rapidement devenir dangereux pour un protagoniste qui n'a pas consommé de Red Met.

### Progression et variantes

Au fil de l'acte, ces ennemis peuvent devenir plus agressifs, attaquer simultanément et profiter davantage des erreurs du joueur.

Ils réapparaissent ponctuellement dans les actes suivants sous des versions visuellement déformées. Leur rôle consiste alors à rappeler que le cauchemar conserve des racines dans la violence humaine initiale.

**Objectif de game design :** apprendre au joueur que chaque coup reçu compte et que l'esquive est indispensable sur la trajectoire morale.

## ENNEMI 02 — LE DEALER

**Rôle :** adversaire manipulateur, personnage de mission et chef d'affrontement secondaire.

**Première apparition :** première moitié de l'acte I.

> **Note de cohérence :** le Dealer est un **lieutenant de rue** du réseau du Chimiste, pas le Chimiste lui-même. Le Chimiste reste le personnage narratif calme, élégant et non combattant de `docs/CHARACTERS.md`. Fiche jouable : `docs/ENEMIES.md`.

### Apparence et direction artistique

Le Dealer affiche une apparence ostentatoire : vêtements de marque, survêtement luxueux ou manteau voyant, bijoux imposants, lunettes teintées et accessoires coûteux.

Il se distingue des voyous ordinaires par son assurance. Il est rarement le premier à se battre et préfère laisser ses hommes intervenir à sa place.

Ses environnements comprennent des clubs, des arrière-salles, des appartements sécurisés et des lieux de transaction baignés de lumière artificielle.

### Comportement et combat

Le Dealer est avant tout un organisateur. Pendant un affrontement, il se tient généralement à distance et donne des ordres à ses gardes. Il peut tenter de fuir, de négocier ou d'utiliser le décor pour retarder le joueur.

Lorsqu'il devient un adversaire direct, il privilégie les attaques opportunistes et les moyens de pression plutôt que les duels loyaux.

Sa fonction principale est narrative : il relie la violence urbaine à l'économie de la Red Met.

### Intégration dans le scénario

Le Dealer introduit la logique de dépendance. Il fait comprendre au protagoniste que l'argent, les services rendus et les actes immoraux peuvent être convertis en doses.

Au fil des actes, les intermédiaires deviennent plus inquiétants. Certains semblent connaître des informations impossibles sur le protagoniste ou parlent du monde parallèle comme s'il s'agissait d'un lieu ordinaire.

**Objectif de game design :** matérialiser le lien entre choix moraux, récompenses financières, consommation et progression en puissance.

## ENNEMI 03 — LE FLIC CORROMPU

**Rôle :** ennemi armé à distance et spécialiste du contrôle de zone.

**Première apparition :** milieu de l'acte I.

### Apparence et direction artistique

Il porte un uniforme de police contemporain, un gilet de protection et un équipement de service. Son comportement doit le distinguer d'un simple bandit : il est habitué à donner des ordres et à occuper l'espace.

Le design évite toute caricature de l'ensemble des forces de l'ordre. La corruption relève d'individus fictifs du scénario.

> **Note de production :** l'uniforme est fictif. Il ne porte aucun insigne, logo ni nom d'une police réelle ; seul un texte générique « POLICE » est autorisé.

### Comportement et combat

Le Flic corrompu utilise les armes à feu, cherche des positions couvertes et force le joueur à se déplacer. Il peut signaler sa position à ses alliés, bloquer une sortie ou déclencher une poursuite.

Ses attaques doivent être annoncées suffisamment clairement pour que l'esquive reste possible, sans rendre l'ennemi inoffensif.

Le joueur moral doit apprendre à exploiter les couvertures et à interrompre les lignes de tir. Le joueur sous Red Met dispose de davantage de possibilités offensives pour neutraliser rapidement la menace.

### Intégration dans la progression

Les premières rencontres restent limitées à des individus isolés. Plus tard dans l'acte, les policiers corrompus apparaissent dans des groupes organisés et protègent des sites liés au réseau clandestin.

Dans le monde parallèle, leurs uniformes et leurs équipements peuvent se déformer, jusqu'à devenir des figures anonymes qui évoquent une autorité devenue hostile.

**Objectif de game design :** obliger le joueur à comprendre le danger des ennemis à distance et à utiliser intelligemment le décor.

## ENNEMI 04 — LE SOLDAT PRIVÉ

**Rôle :** ennemi d'élite humain et adversaire tactique.

**Première apparition :** fin de l'acte I.

### Apparence et direction artistique

Le Soldat privé porte une tenue tactique sombre, un équipement de protection, un casque et un arsenal militaire. Sa silhouette est plus compacte et plus fonctionnelle que celle du voyou classique.

Son apparence évoque un professionnel entraîné. Chaque élément de son équipement doit sembler avoir une utilité.

### Comportement et combat

Contrairement aux voyous, il ne fonce pas systématiquement sur le joueur. Il cherche à se mettre à couvert, coordonne ses déplacements avec ses alliés et contraint le protagoniste à quitter les positions défensives.

Il constitue une menace particulièrement importante pour le personnage non dépendant, car ses attaques précises punissent les erreurs et les déplacements mal anticipés.

Les affrontements avec plusieurs soldats doivent favoriser la mobilité, l'observation et la recherche d'ouvertures plutôt que les échanges de dégâts.

### Intégration dans la progression

Les soldats privés apparaissent lorsqu'il devient clair que le réseau criminel dispose de moyens dépassant le simple banditisme de rue.

Ils gardent des installations, escortent des cargaisons et protègent des opérations liées à la Red Met.

Leur introduction ferme symboliquement le premier acte : la menace n'est plus uniquement locale. Elle est organisée et possède des ressources considérables.

**Objectif de game design :** faire passer le joueur d'un combat instinctif à des affrontements exigeant une réelle compréhension des déplacements ennemis.

---

# 3. ACTE II — LA RÉALITÉ COMMENCE À SE DÉFORMER

## Direction générale

Le deuxième acte marque la transition vers le monde parallèle. Les lieux familiers deviennent progressivement anormaux : les couloirs semblent trop longs, les rues se répètent, les bâtiments changent de proportions et les sons proviennent parfois de directions impossibles.

Les premières créatures ne doivent pas immédiatement ressembler à des monstres fantastiques. Elles conservent des éléments humains reconnaissables, mais leur corps, leurs réactions ou leurs déplacements ne correspondent plus à la réalité.

Le joueur doit commencer à remettre en question ses automatismes. Une esquive qui fonctionne contre un humain ne sera pas toujours suffisante face à un ennemi capable de feinter ou d'attaquer depuis une position inattendue.

## ENNEMI 05 — LE CHASSEUR

**Rôle :** prédateur, ennemi de traque et spécialiste des embuscades.

**Première apparition :** début de l'acte II.

### Apparence et direction artistique

Le Chasseur porte un manteau usé à capuche, un masque respiratoire ou un visage partiellement dissimulé. Ses vêtements semblent provenir d'un environnement abandonné : tissus déchirés, sangles, protections improvisées et accessoires récupérés.

Ses yeux peuvent présenter une faible lueur anormale, mais le design conserve une base humaine crédible.

### Comportement et combat

Le Chasseur observe d'abord le protagoniste. Il utilise les angles morts, les obstacles et les zones sombres pour s'approcher sans être immédiatement repéré.

Il place des pièges, attaque lorsque le joueur est occupé et tente de rompre le combat avant de revenir par surprise.

Une poursuite contre lui doit être aussi importante que le duel lui-même. Le joueur peut devoir examiner l'environnement, repérer les indices de passage et anticiper ses embuscades.

### Intégration dans l'acte II

Le Chasseur apparaît dans des friches, des hôpitaux abandonnés, des parkings souterrains et des zones industrielles où le monde commence à perdre sa cohérence.

Il constitue le premier ennemi qui donne réellement l'impression que le protagoniste est lui-même devenu une proie.

Dans les apparitions ultérieures, son comportement peut devenir presque surnaturel : il semble connaître les déplacements du joueur et se retrouver dans des lieux qu'il ne pouvait pas atteindre normalement.

**Objectif de game design :** transformer l'exploration en source de tension et apprendre au joueur à se méfier de l'environnement.

## ENNEMI 06 — LE MUTANT

**Rôle :** adversaire lourd, spécialiste de la charge et des dégâts massifs.

**Première apparition :** début ou milieu de l'acte II.

### Apparence et direction artistique

Le Mutant est une créature humanoïde dont l'anatomie présente des déformations manifestes. Sa peau est abîmée, ses muscles semblent avoir pris des proportions anormales et sa posture traduit une violence permanente.

Il ne doit pas ressembler à un simple culturiste monstrueux. Ses mouvements, ses proportions et sa façon de respirer doivent suggérer un organisme dont le fonctionnement est devenu anormal.

### Comportement et combat

Le Mutant privilégie les attaques lourdes, les charges et les frappes qui obligent le joueur à se déplacer constamment.

Ses mouvements sont puissants, mais relativement lisibles. Il doit annoncer ses attaques par une posture ou une animation spécifique, afin que le joueur puisse apprendre à les éviter.

Le joueur moral doit observer ses séquences d'attaque, esquiver et frapper pendant ses phases de récupération.

Le joueur sous Red Met peut davantage miser sur les dégâts directs et les armes puissantes, sans pour autant ignorer les attaques du monstre.

### Variantes

Des Mutants plus résistants peuvent apparaître plus tard, avec des mouvements supplémentaires ou des points faibles spécifiques.

Certains exemplaires peuvent protéger leur corps par des plaques de matière anormale, imposant au joueur de viser des zones précises plutôt que d'enchaîner aveuglément les attaques.

**Objectif de game design :** introduire un ennemi qui teste la patience, la lecture des animations et la gestion des distances.

## ENNEMI 07 — LE PSYCHO

**Rôle :** adversaire imprévisible, spécialiste des feintes et des changements de rythme.

**Première apparition :** milieu de l'acte II.

### Apparence et direction artistique

Le Psycho conserve une apparence humaine, mais semble profondément dérangé. Sa tenue est désordonnée, son expression faciale instable et sa posture difficile à interpréter.

Il peut être vêtu d'habits ordinaires tachés et abîmés, comme s'il avait traversé un événement traumatique sans jamais parvenir à en sortir.

Son étrangeté provient d'abord de son comportement, et non de modifications physiques excessives.

### Comportement et combat

Le Psycho utilise des feintes, change brusquement de rythme et interrompt parfois ses propres attaques pour provoquer une réaction prématurée du joueur.

Il peut reculer de façon inattendue, tenter une attaque rapide après une longue hésitation ou exploiter une esquive mal synchronisée.

Le joueur doit apprendre à observer l'ensemble de son comportement au lieu de réagir systématiquement au premier mouvement.

### Intégration dans l'acte II

Les premiers Psycho apparaissent dans des lieux où la perception du protagoniste est déjà fragilisée.

Ils deviennent ensuite plus difficiles à lire, car certaines de leurs animations peuvent être accompagnées de perturbations visuelles ou sonores.

Ces effets doivent rester cohérents et prévisibles à force d'observation. L'objectif n'est pas de tromper arbitrairement le joueur, mais de le pousser à remettre en question ses réflexes.

**Objectif de game design :** casser les habitudes de combat acquises durant l'acte I et introduire un adversaire qui teste la capacité d'adaptation.

## ENNEMI 08 — LE CLOWN TUEUR

**Rôle :** ennemi de terreur psychologique, spécialiste des pièges et des fausses indications.

**Première apparition :** fin de l'acte II.

### Apparence et direction artistique

Le Clown tueur porte un costume coloré, déchiré et excessivement voyant. Son maquillage est abîmé, son sourire semble figé et ses accessoires évoquent un spectacle pour enfants devenu cauchemardesque.

La palette doit contraster fortement avec les environnements sombres. Ses couleurs vives attirent l'œil et rendent sa silhouette immédiatement identifiable.

Il constitue le premier ennemi dont l'apparence peut sembler volontairement absurde, presque ridicule, avant que son comportement ne révèle son danger.

### Comportement et combat

Le Clown tueur combine mobilité, pièges et attaques trompeuses. Il peut lancer des objets destinés à distraire le joueur, disparaître derrière un élément du décor ou simuler une ouverture avant de contre-attaquer.

Ses affrontements doivent créer un sentiment de malaise et d'incertitude.

Certains mouvements peuvent sembler exagérés ou théâtraux, mais ils doivent correspondre à des règles de combat cohérentes. Le joueur doit progressivement apprendre à distinguer une véritable vulnérabilité d'une mise en scène.

### Intégration narrative

Le Clown apparaît dans un parc d'attractions abandonné ou un espace festif déformé. Les lumières fonctionnent par intermittence, les musiques se répètent et certains éléments du décor changent lorsque le joueur détourne le regard.

Il annonce clairement l'évolution du jeu vers un univers où l'absurde devient une forme d'horreur.

**Objectif de game design :** faire naître la peur par le contraste entre une apparence grotesque, un comportement théâtral et une réelle difficulté tactique.

---

# 4. ACTE III — LE CAUCHEMAR ABSOLU

## Direction générale

Le troisième acte plonge le protagoniste dans les régions les plus déformées du monde parallèle. Les règles physiques semblent désormais instables. Les distances changent, les objets flottent, les bâtiments se replient sur eux-mêmes et les créatures ne respectent plus une anatomie humaine crédible.

La progression des ennemis ne doit toutefois pas devenir une simple escalade de taille ou de puissance. Chaque nouvelle créature introduit un principe de combat différent.

Le joueur peut rencontrer des ennemis qui manipulent l'espace, altèrent ses perceptions ou nécessitent de comprendre le fonctionnement de l'arène plutôt que de simplement infliger des dégâts.

## ENNEMI 09 — LE DÉMON TECHNO

**Rôle :** adversaire surnaturel mobile, spécialiste des attaques électriques et de la perturbation de l'espace.

**Première apparition :** début de l'acte III.

### Apparence et direction artistique

Le Démon techno est un hybride de chair et de technologie. Son corps est parcouru de câbles, de structures métalliques et de dispositifs lumineux qui semblent intégrés à son organisme.

Ses mouvements ne sont pas entièrement naturels : certaines parties de son corps peuvent se déplacer avec un léger décalage, comme si elles suivaient une animation différente.

Les lumières rouges et les contrastes métalliques doivent le rendre immédiatement reconnaissable au milieu des environnements les plus sombres.

### Comportement et combat

Le Démon techno alterne attaques électriques, déplacements rapides et perturbations temporaires de certaines fonctions de l'arène.

Il peut créer des zones dangereuses ou forcer le joueur à modifier son positionnement.

Ses attaques doivent suivre des règles identifiables : le joueur observe les signes annonciateurs, évite les zones affectées et exploite les périodes de récupération.

### Intégration dans l'acte III

Le Démon techno apparaît dans des zones où des éléments numériques et physiques se superposent : bâtiments industriels impossibles, couloirs traversés de câbles et espaces saturés d'écrans.

Son introduction marque une rupture avec les ennemis précédents. Il ne représente plus seulement une créature violente, mais une anomalie capable d'agir sur le fonctionnement même du monde.

**Objectif de game design :** introduire des combats où l'environnement devient un élément actif de la menace.

## ENNEMI 10 — LE SPECTRE

**Rôle :** adversaire immatériel, spécialiste des illusions et des déplacements impossibles.

**Première apparition :** début ou milieu de l'acte III.

### Apparence et direction artistique

Le Spectre possède une silhouette presque humaine, mais son corps semble constitué d'une matière translucide, de brume et de fragments d'ombre.

Ses vêtements paraissent flotter indépendamment de son corps. Son visage reste difficile à distinguer, ce qui renforce son caractère anormal.

Il ne doit pas être représenté comme une simple silhouette transparente : son apparence doit donner l'impression que plusieurs versions de lui-même occupent parfois le même espace.

### Comportement et combat

Le Spectre se déplace par phases. Il peut traverser certaines structures, se dédoubler ou générer des apparitions temporaires pour détourner l'attention du joueur.

Le combat repose sur l'identification de sa véritable position et sur la compréhension des moments où il devient vulnérable.

Le joueur doit apprendre à ne pas gaspiller ses attaques sur les illusions et à observer les indices visuels ou sonores révélant la présence réelle de l'ennemi.

### Intégration narrative

Les Spectres peuvent apparaître dans des rues désertes, des hôpitaux impossibles et des espaces dont l'architecture rappelle certains lieux de la vie du protagoniste.

Leurs apparitions peuvent évoquer ses regrets ou ses souvenirs sans nécessairement fournir une explication explicite de leur origine.

**Objectif de game design :** faire passer le combat d'un affrontement physique à une épreuve d'observation et d'interprétation.

## ENNEMI 11 — LE GÉANT DE CHAIR

**Rôle :** mini-boss ou boss lourd, spécialiste du contrôle de zone et des attaques dévastatrices.

**Première apparition :** milieu ou fin de l'acte III.

### Apparence et direction artistique

Le Géant de chair est une créature gigantesque dont le corps semble formé d'une accumulation anarchique de tissus, de muscles et de structures organiques déformées.

Sa silhouette doit être immédiatement imposante, mais rester lisible : la tête, les membres et les zones d'attaque doivent permettre au joueur d'anticiper ses mouvements.

Sa démarche provoque des vibrations au sol. Sa respiration est lourde et ses mouvements font réagir les objets alentour.

### Comportement et combat

Le Géant de chair contrôle une grande partie de l'arène par sa portée et ses attaques de zone.

Il peut frapper le sol, balayer un espace devant lui ou provoquer des effondrements locaux. Ses attaques sont lentes mais extrêmement dangereuses.

Le joueur doit exploiter sa lenteur, apprendre à se déplacer autour de lui et identifier ses points faibles.

Les armes lourdes et les armes à feu peuvent lui infliger des dégâts importants, tandis que le joueur moral doit exploiter les moments de vulnérabilité avec beaucoup plus de précision.

### Évolution en cours de combat

Le boss peut comporter plusieurs phases. À mesure qu'il subit des dégâts, ses mouvements changent et certains accès à ses points faibles se modifient.

Les transitions doivent modifier les décisions tactiques du joueur plutôt que simplement augmenter les dégâts infligés.

**Objectif de game design :** proposer un affrontement spectaculaire qui teste toutes les compétences acquises jusqu'à présent.

## ENNEMI 12 — LE PORTEUR DE L'ABERRATION

**Rôle :** boss majeur de fin de jeu et manifestation ultime du monde parallèle.

**Première apparition :** dernier chapitre.

### Apparence et direction artistique

Le Porteur de l'aberration constitue l'aboutissement esthétique du bestiaire.

Son apparence combine des proportions humanoïdes, des éléments technologiques, des formes organiques et des objets impossibles. Sa tête peut évoquer un écran ou une surface réfléchissante affichant des expressions contradictoires.

Autour de lui, des fragments de décor et des structures géométriques semblent flotter sans respecter la gravité.

Il ne doit pas ressembler simplement à une version plus grande du Démon techno. Son étrangeté provient du fait que son corps et son environnement semblent obéir à des règles différentes.

### Comportement et combat

Le Porteur de l'aberration agit directement sur l'arène.

Il peut modifier certaines distances, faire apparaître des plateformes impossibles, déformer temporairement les perspectives ou provoquer des illusions qui perturbent la lecture des déplacements.

Ses attaques doivent s'organiser en phases clairement identifiables. Chaque phase introduit une modification de l'environnement ou des règles du combat.

La victoire ne dépend pas seulement de la puissance offensive : le joueur doit comprendre la logique de ses transformations et exploiter les ouvertures correspondantes.

### Intégration narrative

Le Porteur constitue la dernière confrontation avec l'univers de cauchemar. Il peut être interprété comme une entité autonome, une manifestation de la Red Met ou une expression de la psyché du protagoniste.

Le jeu ne doit pas apporter de réponse définitive à toutes ces hypothèses.

Les détails de son comportement et de l'arène finale peuvent varier selon la trajectoire du joueur, notamment son niveau de consommation et ses choix moraux.

**Objectif de game design :** faire du dernier affrontement une rupture avec tous les combats précédents, sans perdre la lisibilité ni le sentiment de maîtrise du joueur.

---

# 5. INTÉGRATION DES ENNEMIS DANS LA PROGRESSION DES ACTES

| Phase du jeu | Ennemis dominants | Nouvelle difficulté introduite | Compétence recherchée |
|---|---|---|---|
| Acte I — Début | Voyou classique | Combats au corps à corps et supériorité numérique | Esquiver et contre-attaquer |
| Acte I — Milieu | Dealer, Flic corrompu | Pression à distance et soutien ennemi | Utiliser le décor |
| Acte I — Fin | Soldat privé | Coordination et discipline tactique | Observer et anticiper |
| Acte II — Début | Chasseur, Mutant | Embuscades et attaques lourdes | Explorer et lire les animations |
| Acte II — Milieu | Psycho | Feintes et changements de rythme | Ne pas réagir trop tôt |
| Acte II — Fin | Clown tueur | Pièges, tromperies et perception déstabilisée | Analyser les indices |
| Acte III — Début | Démon techno | Environnement dangereux et perturbations | Contrôler l'espace |
| Acte III — Milieu | Spectre | Illusions et déplacements impossibles | Identifier la vraie menace |
| Acte III — Fin | Géant de chair | Combat long, points faibles et phases successives | Maîtriser les fenêtres d'attaque |
| Acte III — Final | Porteur de l'aberration | Déformation de l'arène et des règles | Mobiliser toutes les compétences acquises |

Cette structure fait évoluer la difficulté de manière qualitative. Le jeu commence par tester les réflexes, puis l'observation, ensuite la capacité d'adaptation et, enfin, la compréhension de règles nouvelles.

---

# 6. VARIANTES LIÉES À LA RED MET ET AUX CHOIX MORAUX

Les rencontres doivent tenir compte de la progression du protagoniste sans simplement transformer chaque ennemi en sac de points de vie.

### Si le joueur reste moral

Le personnage ne bénéficie pas des améliorations de santé et de puissance offertes par la Red Met. Les ennemis conservent des dégâts dangereux, même dans les actes avancés.

Le jeu doit donc favoriser les affrontements où la lecture des animations, le positionnement, les esquives et les possibilités de contournement permettent de survivre.

Certains ennemis peuvent présenter des ouvertures plus longues ou des possibilités d'environnement exploitables, mais la trajectoire morale ne doit pas se transformer en mode facile déguisé.

### Si le joueur devient dépendant

Le protagoniste dispose progressivement d'un arsenal plus puissant, d'une santé maximale supérieure et de nouvelles capacités.

Cette puissance permet de neutraliser beaucoup plus vite les ennemis humains. Cependant, l'introduction des créatures surnaturelles empêche le joueur de se reposer uniquement sur les dégâts bruts.

Les adversaires de fin de jeu imposent des changements de stratégie, des ouvertures précises ou des mécaniques spéciales, même lorsque le protagoniste possède des armes avancées.

### Règle d'équilibrage fondamentale

Les ennemis doivent garder une identité mécanique stable entre les deux trajectoires. Les changements de difficulté proviennent principalement des capacités du protagoniste, de la composition des groupes et des occasions tactiques offertes au joueur.

Les variantes peuvent être ajoutées pour renforcer l'escalade narrative, mais ne doivent pas rendre un même ennemi inexplicablement plus résistant simplement parce que le joueur a fait un choix moral.

---

# 7. COMPOSITION DES RENCONTRES

La difficulté ne repose pas uniquement sur les caractéristiques individuelles des ennemis. Elle découle également de leurs associations.

Au début, le joueur affronte des voyous en petits groupes. Les combats deviennent plus difficiles lorsque des adversaires à distance couvrent les attaques au corps à corps.

Dans l'acte II, le Chasseur peut isoler le joueur pendant que le Mutant bloque une zone. Le Psycho peut profiter des erreurs provoquées par cette pression pour attaquer au mauvais moment.

Dans l'acte III, les ennemis peuvent combiner illusions et contrôle de zone. Le Spectre désoriente le joueur tandis qu'une autre menace limite ses déplacements.

Les compositions doivent être introduites progressivement. Chaque nouvelle rencontre présente d'abord les mécaniques séparément, avant de les combiner dans des affrontements plus complexes.

Les grandes arènes doivent également proposer des chemins de fuite, des positions avantageuses et des éléments exploitables, en particulier pour le joueur qui a refusé la Red Met.

---

# 8. PRINCIPES DE DIRECTION ARTISTIQUE ET DE PRODUCTION

Chaque archétype doit être reconnaissable en un coup d'œil, même lorsque plusieurs ennemis apparaissent simultanément.

La silhouette constitue le premier critère : un ennemi lourd doit être immédiatement différent d'un ennemi mobile, même dans une scène sombre.

Les couleurs, les équipements, la posture et les animations renforcent cette distinction. Les effets lumineux et surnaturels doivent être réservés aux stades où le monde commence réellement à basculer dans l'irréel.

Pour chaque ennemi principal, la production devra prévoir :

- Un modèle 3D principal et ses variantes éventuelles.
- Des vues de référence de face, de profil et de dos.
- Une bibliothèque d'animations d'attente, de déplacement, d'attaque, de réaction et de défaite.
- Les effets visuels et sonores propres à ses capacités.
- Les règles de détection, de poursuite et d'engagement en combat.
- Les paramètres de santé, de dégâts, de résistance et de vulnérabilité.
- Une fiche de test décrivant les stratégies attendues et les erreurs que l'ennemi doit sanctionner.

Les boss nécessitent également une documentation distincte de leurs phases, de leurs transitions, de leurs points faibles et des signaux permettant au joueur d'anticiper leurs attaques.

---

# 9. OBJECTIF FINAL DU BESTIAIRE

L'évolution des douze ennemis doit accompagner la dégradation du monde et la transformation du protagoniste.

Le joueur commence par combattre des individus qui pourraient exister dans la réalité. Il découvre ensuite des êtres déformés dont le comportement devient difficile à interpréter, avant de rencontrer des créatures qui bouleversent les règles mêmes de son environnement.

Cette progression ne doit jamais être purement cosmétique. Chaque transformation visuelle doit s'accompagner d'une nouvelle manière de combattre, d'explorer ou de comprendre le monde.

**Dans RED MET, les ennemis deviennent de plus en plus absurdes à mesure que le protagoniste s'éloigne de son humanité et que le monde parallèle perd toute cohérence.**

Le bestiaire devient ainsi un outil de narration, un système de progression et une source de renouvellement du gameplay jusqu'à la confrontation finale avec le Porteur de l'aberration.