# RED MET — Scénario jouable

Déroulé des 10 chapitres, tiré des documents de l'auteur (`docs/source/RED_MET_10_chapitres.md` et `docs/source/RED_MET_GDD_v0.1.md`) et des décisions déjà prises (`docs/GDD.md`). Ce n'est pas encore le texte des dialogues : chaque scène dit **ce qui se passe**, ce que le joueur choisit et ce que le jeu retient. L'auteur écrit les répliques dans les fichiers Ink.

> **État** : principes communs et chapitres 1 à 3 **validés par l'auteur le 2026-10-28**, avec ses corrections. Chapitres 4 à 10 rédigés au même format, **en attente de validation** (points à trancher en fin de fichier).

## Principes communs

### Un chapitre = un niveau = une incursion
- 10 chapitres, 10 niveaux du monde parallèle, jusqu'à 10 doses. Le chapitre *n* est l'**incursion *n*** : les ennemis y ont la force de l'incursion *n* (`Data/Story/Incursions.asset` : +10 % de PV, +8 % de dégâts, +2 % de vitesse par incursion), quelle que soit la trajectoire.
- Déroulé fixe de chaque chapitre (`docs/GDD.md`, « Un niveau du monde parallèle ») :
  1. **Réel** : une ou plusieurs scènes, dont les dilemmes du chapitre ;
  2. **La dose** : le Chimiste propose une dose et en fixe le prix ; le joueur la prend ou la refuse (choix définitif pour ce niveau) ;
  3. **Passage** dans le monde de la Red Met ;
  4. **Ouverture avec Mara** (visual novel, Mara de face, tenue selon les doses prises) ;
  5. **Combats** (vagues, puis boss ou séquence clé), avec parfois un choix du monde parallèle ;
  6. **Fin avec Mara** (visual novel, Mara de dos), puis retour au réel.
- **On entre dans le monde parallèle même sans dose.** Le GDD v0.1 (section 2) sépare l'accès au monde (un événement) et la Red Met (la puissance). Nathan sobre y va aussi, en M0. Le passage se fait quand Nathan **s'effondre de fatigue ou de manque** (sommeil, malaise dans la salle de bains), ce qui annonce le coma de la fin (décision de l'auteur).

### La dose, son prix et la culpabilité
- **Prix d'une dose** : de l'argent au début (ch. 1 à 3), puis les trois **demandes du Chimiste** (ch. 4, 5 et 7, voir le tableau). Refuser une demande, c'est refuser la dose de ce niveau.
- **Argent** : petites sommes gagnées dans le réel (travail honnête, poubelles, usurier, vols). *Nouvelle donnée de l'état de la partie, pas encore dans `GameState`* : à ajouter à l'étape des dialogues.
- **Avant chaque dose prise**, une scène dans la salle de bains :
  - si la dose a été payée par un **acte immoral** : la **scène de culpabilité** complète (GDD v0.1, sections 4.3 et 10.4). Nathan se recroqueville en boule, la tête entre les mains, et pleure : « Qu'est-ce que je suis en train de devenir… Je deviens un monstre. » Puis il prend sa dose. Au fil du jeu, la crise devient plus courte, plus violente ou plus silencieuse ;
  - sinon, une **hésitation** courte : il regarde la dose, l'Avocat glisse un mot, il la prend.
- Les scènes sensibles suivent `docs/GDD.md` (« Lignes directrices ») : rien d'explicite, la victime au centre, aucun contenu sexuel ou suggestif près de la branche de l'enfant.

### Mara
- **Tenue** selon les doses prises : 0 = M-T0, 1-2 = M-T1, 3-4 = M-T2, 5-6 = M-T3, 7 et plus = M-T4.
- **Ton** selon le chapitre (source, section 6) et la puissance de Nathan : moqueuse et méprisante aux ch. 1-2, joueuse et provocante aux ch. 3-4, attirée au ch. 5, inquiète de sa dégradation aux ch. 6-7, alliée, adversaire ou absente aux ch. 8-10. Face à un Nathan faible (M0-M1), elle se moque ; face à un Nathan puissant (M2 et plus), elle est plus troublée et attirée.
- **Ouverture** : de face, elle accueille Nathan. **Fin** : de dos, cambrée, la tête tournée par-dessus l'épaule, elle le juge, le nargue, puis disparaît (fondu ou brume rouge).
- Elle désire la puissance de Nathan **mais peut refuser ses crimes** : certaines répliques lisent les actes, pas seulement les doses.

### L'Avocat
- Présent **par touches dès le chapitre 1** (décision de l'étape 1), mais **ambigu jusqu'au chapitre 5** : une phrase courte qu'on peut prendre pour une pensée de Nathan. Au ch. 5, il parle pour la première fois comme une voix séparée (« Tu as fait ce qu'il fallait. Tu le sais, n'est-ce pas ? »). Ensuite, il commente les choix (ch. 6), puis s'adresse au joueur (ch. 8).
- Il intervient en surimpression (sous-titre et distorsion), sans interrompre le jeu, avec un délai minimum entre deux interventions. Déclencheurs : avant une dose, après un acte immoral, quand Nathan est en manque ou blessé, et parfois en exploration. Il pousse toujours à **reprendre une dose**.

### Ce que le jeu retient
Chaque choix écrit un **acte** dans l'état de la partie (identifiant, niveau, effet sur Humanité, Cruauté, Dépendance et Paranoïa). Les effets sont notés ici par **+** et **−**, sans valeur : les nombres viendront avec les fichiers Ink. Les actes marqués **(retour ch. 9)** font revenir un personnage dans la guerre finale.

### Identifiants
- Une scène = un identifiant, qui sert aussi de nom de nœud Ink : `chNN_<lieu>_<scène>`.
- Lieux : `reel` (monde réel), `dose`, `rm` (monde de la Red Met), `mara`, `avocat`.
- Les répliques porteront ensuite l'identifiant de leur scène suivi d'un numéro (`ch01_mara_ouverture_003`), pour leurs voix ElevenLabs.
- Les actes ont leur propre identifiant (`acte_...`).

## Les 10 chapitres en un coup d'œil

Les doses indiquées sont celles d'un joueur qui prend **toutes** les doses : le stade réel dépend du nombre de doses prises.

| Ch. | Titre | Réel | Monde de la Red Met | Ennemis (bestiaire) | Prix de la dose | Doses max |
|---|---|---|---|---|---|---|
| 1 | Le garçon invisible | Humiliation, rencontre du Chimiste | Découverte, Mara, premier boss | Voyous ; boss : une créature propre au ch. 1 (design à choisir) | Offerte | 1 |
| 2 | Le grain rouge | Premières dettes | Ville pestiférée : quarantaine, évacuation ou purge | Voyous pestiférés, Flics corrompus (gardes du commandant), Dealer (contrebandier) | À crédit (dette) | 2 |
| 3 | Le prix du respect | Poubelles, usurier, petits vols, collier en or | Arme anti-peste, ancien compagnon d'armes, forteresse | Soldats privés (garnison), Voyous pestiférés, Dealer | Argent (+ la dette) | 3 |
| 4 | La couronne de cendres | Marc coupe l'argent | Révolte contre le roi | Chasseurs ; boss : le champion du roi (Mutant élite) | **Demande 1 : l'humiliation imposée** (GDD v0.1, 10.3) | 4 |
| 5 | Le fils qui tue son père | Confrontation avec Marc | Régicide ; la voix se sépare | Psychos, Chasseurs ; boss : le roi déchu (si régicide) | **Demande 2 : le cœur de Marc** (10.1) | 5 |
| 6 | La voix derrière le miroir | Perte de contrôle ; le prix de son corps | Armes à feu dans le royaume, blessures fantômes | Psycho | Argent | 6 |
| 7 | Le sang des autres | Crimes, paranoïa | Vampirisme ; Mara doute | Clown tueur (boss : maître de la fête) ; Mara si elle devient adversaire | **Demande 3 : l'enfant** (10.2) | 7 |
| 8 | Le monde est coupable | Persécution imaginaire, quatrième mur | Invasion du réel | Démon techno | Argent | 8 |
| 9 | Le roi sans royaume | Isolement | Guerre finale ; retour des alliés et des victimes | Spectre ; mini-boss : Géant de chair | Argent | 9 |
| 10 | Red Met | Fusion des deux mondes | Combat contre le double (M5) ; fin avec Mara | Porteur de l'aberration, le double | La dernière, proposée par l'Avocat | 10 |

Fin (ch. 10) : voie immorale, section 11.1 du GDD v0.1 (scène coupée avant l'acte, sans nudité, centrée sur la réaction de Mara) ; voie morale, section 11.2 (Mara ne lui accorde rien, montre ses biceps 💪 : « Moi, je veux un vrai bonhomme… »). Puis, dans tous les cas : yeux révulsés, chute, écran noir, hôpital, coma, « RED MET 2 » sur la fiche patient (section 12).

---

## Chapitre 1 — Le garçon invisible

**Réel** : humiliation · **Red Met** : découverte · **Incursion 1** · **Lieux** : rues, appartement de Marc, planque du Chimiste, salle de bains ; Arène de la lune rouge.
**Nathan** : R0 dans le réel ; M0 (sobre) ou M1 (1 dose) dans le monde parallèle.

### Réel
| Identifiant | Scène |
|---|---|
| `ch01_reel_journee` | Une journée catastrophique à son **petit boulot dans un fast-food**, jouable en quelques minutes : commandes qui s'enchaînent, un **patron** qui le rabaisse devant tout le monde, des **clients** qui se moquent de son physique et de ses lunettes. Le contrôle est lent et hésitant (posture R0, regard au sol). |
| `ch01_reel_humiliation` | L'humiliation de trop : renvoyé en fin de service, il croise dans la **rue** un groupe qui le bouscule, le filme et partage la vidéo. Nathan s'enfuit. |
| `ch01_reel_marc` | Le soir, chez Marc. Le père est fatigué, maladroit mais aimant. Il remarque que quelque chose ne va pas, et Nathan se ferme. La scène pose le lien affectif que les ch. 4 et 5 mettront à l'épreuve. |
| `ch01_reel_chimiste` | Errance nocturne. Nathan croise le Chimiste, calme et poli, presque paternel. Il ne force rien : il **propose**. « La première est offerte. » |

### La dose
| Identifiant | Scène |
|---|---|
| `ch01_avocat_dose` | Première touche de l'Avocat, juste avant le choix : une phrase qui flatte la colère de Nathan (il mérite d'être fort). Ambiguë : le joueur peut la prendre pour sa propre pensée. |
| `ch01_dose_choix` | **Dose 1, offerte.** Prendre ou refuser. |
| `ch01_dose_hesitation` | Si prise : salle de bains, hésitation devant le miroir, puis la prise. Pas de culpabilité complète, car aucun acte immoral ne l'a payée. |
| `ch01_reel_passage` | Nathan s'effondre (épuisement, ou effet de la dose) et se réveille dans la plaine de cendres sous la lune rouge. |

### Monde de la Red Met
| Identifiant | Scène |
|---|---|
| `ch01_mara_ouverture` | Mara de face (M-T0 sans dose, M-T1 avec). Elle découvre Nathan et le prend pour **un déchet incapable de survivre**. Avec une dose, elle note à peine qu'il tient debout. |
| `ch01_rm_vagues` | Arène de la lune rouge (celle de la tranche verticale). Vagues de Voyous en petits groupes : le joueur apprend l'esquive et la contre-attaque (bestiaire, « Acte I — Début »). M0 se bat aux poings et aux pieds ; M1 a un couteau (premières giclées de sang, selon le réglage de gore). |
| `ch01_rm_boss` | **Premier boss** : une créature propre au chapitre, beaucoup plus puissante que lui (pas le Mutant, réservé à l'Acte II). Trois idées de design ci-dessous, au choix de l'auteur avant tout ajout au bestiaire. Nathan la tue. |
| `ch01_rm_plaisir` | Arrêt sur image après le coup final : Nathan découvre le **plaisir inquiétant** de la victoire (rire nerveux, mains qui tremblent). |
| `ch01_rm_choix_fondateur` | **Choix fondateur** : *accepter sa faiblesse* ou *commencer à la haïr*. Pas d'effet immédiat sur le combat : ce choix colore l'Avocat et Mara jusqu'au ch. 3. |
| `ch01_mara_fin` | Mara de dos. Elle se moque (réplique de test de l'auteur, `docs/GDD.md`), puis disparaît. Si Nathan a choisi la haine, elle relève une étincelle dans son regard. |
| `ch01_reel_reveil` | Retour au réel : réveil, sueur, courbatures inexplicables. |

### Choix et conséquences
| Choix | Acte | Effet |
|---|---|---|
| Prendre la dose offerte | `acte_ch01_dose` | Dépendance +, stade M1, tenue M-T1 |
| Refuser la dose | `acte_ch01_refus` | Humanité + ; reste M0 |
| Accepter sa faiblesse | `acte_ch01_accepte` | Humanité + ; l'Avocat est plus rare au ch. 2 |
| Commencer à la haïr | `acte_ch01_haine` | Cruauté +, Paranoïa + ; l'Avocat est plus présent au ch. 2 |

### Boss du chapitre 1 : trois idées (au choix de l'auteur)
Contraintes communes : battable par Nathan M0 aux poings et aux pieds (environ 4 fois plus long qu'en M1), toutes les attaques annoncées (armement ≥ 0,35 s), une fenêtre de punition claire après chaque attaque, deux phases au plus, et une silhouette nette sur le fond rouge de l'arène.

| | **A — Le Chœur des rieurs** | **B — Le Gérant** | **C — La Bête de cendres** |
|---|---|---|---|
| **Idée** | La moquerie faite chair : un amas de silhouettes soudées, les clients et les passants du ch. 1, avec des dizaines de bouches qui rient et des téléphones à la place des yeux | Le patron du fast-food déformé en ogre : énorme, tablier taché, couperet, néon « OUVERT » planté dans le dos | Un prédateur de la plaine de cendres, quadrupède, à la crinière de braises ; la proie que Mara chassait et qu'elle laisse à Nathan |
| **Taille** | 3,2 m | 3,0 m | 2,6 m au garrot, 4 m de long |
| **Attaques** | Gifle géante (0,6 s → balayage au sol) ; éclat de rire, cri en cône (0,8 s, repousse) ; « flash » des téléphones : la zone marquée au sol aveugle brièvement (1,0 s) | Coup de couperet vertical (0,7 s) ; sonnette « Commande ! » qui appelle 2 Voyous (1,0 s) ; zone de friture au sol (marquage 1,0 s, brûlure) | Bond (0,6 s, ombre au sol) ; morsure (0,45 s) ; hurlement (1,0 s) qui fait monter des loups de cendre (2 petits ennemis) |
| **Fenêtre** | Après le rire, les bouches restent ouvertes 1,2 s : la gorge centrale est exposée | Le couperet se plante dans le sol 1,0 s | Atterrissage du bond : 0,9 s au sol, flanc exposé |
| **Phase 2** | À 50 % des PV, les silhouettes se détachent une à une : le Chœur rapetisse et accélère | Il arrache son tablier, devient plus rapide, et la sonnette appelle 3 Voyous | Crinière en feu : le bond laisse une traînée de braises au sol |
| **Ce qu'il raconte** | Nathan écrase littéralement ceux qui riaient de lui : le plaisir inquiétant du ch. 1 est évident | La revanche sur l'humiliation du jour, lisible et un peu grotesque | Pure dark fantasy : la découverte d'un monde où il peut tuer un monstre |
| **Production** | Modèle et animation les plus complexes (masse de corps) | Humain déformé : il réutilise la chaîne des ennemis humains | Quadrupède : nouveau squelette et nouvelles animations |

*Recommandation de Claude* : **A**. C'est le plus fort pour le thème du chapitre (l'humiliation qui devient une jouissance de la violence) et il reste distinct du Mutant et du Géant de chair. **B** coûte le moins cher à produire.

---

## Chapitre 2 — Le grain rouge

**Réel** : premières dettes · **Red Met** : épidémie · **Incursion 2** · **Lieux** : rues, appartement de Marc, planque du Chimiste ; Ville du grain rouge.
**Nathan** : R0 ou R1 ; M0 à M1 (2 doses au plus).

### Réel
| Identifiant | Scène |
|---|---|
| `ch02_reel_manque` | Nathan pense au monde parallèle, et au manque s'il a pris la dose. Nuit courte, premières cernes (R1 s'il a pris une dose). |
| `ch02_reel_chimiste` | La deuxième dose n'est plus offerte. Le Chimiste propose un **crédit**, avec la même politesse : « Tu me rendras quand tu pourras. » |
| `ch02_avocat_dette` | L'Avocat minimise : une dette, ce n'est rien, il sera bientôt assez fort pour tout rembourser. |

### La dose
| Identifiant | Scène |
|---|---|
| `ch02_dose_choix` | **Dose 2, à crédit.** La prendre crée une **dette** à rembourser au ch. 3. |
| `ch02_dose_hesitation` | Si prise : hésitation dans la salle de bains, puis la prise. |
| `ch02_reel_passage` | Passage dans le monde parallèle. |

### Monde de la Red Met
| Identifiant | Scène |
|---|---|
| `ch02_mara_ouverture` | Mara de face (M-T0 ou M-T1). Elle le traite toujours en **poids mort**, mais l'emmène à la Ville du grain rouge, où on a besoin de bras. |
| `ch02_rm_ville` | Ville médiévale pestiférée : les greniers sont contaminés par une peste surnaturelle et les habitants supplient qu'on les laisse partir. Combats contre des **Voyous pestiférés** (variante visuelle du Voyou : habits de la ville, peau marquée de rouge). |
| `ch02_rm_conseil` | Le **commandant** veut sacrifier la ville pour arrêter la contagion. Le **guérisseur** affirme qu'un traitement est possible, sans savoir s'il arrivera à temps. Nathan choisit. |
| `ch02_rm_quarantaine` | **Quarantaine** : Nathan tient les portes pendant que le guérisseur travaille. Il combat les **gardes du commandant** (Flics corrompus réinterprétés : tir, matraque, renforts par la radio, devenue un cor) et les hommes du **Dealer**, qui revend le grain contaminé hors de la ville. Une partie des habitants survit. |
| `ch02_rm_evacuation` | **Évacuation** : escorte d'un convoi d'habitants à travers les faubourgs. Combat plus long et plus exposé (ennemis à distance qui couvrent les mêlées). Beaucoup survivent, mais la contagion sort peut-être de la ville. Ce doute revient au ch. 3. |
| `ch02_rm_purge` | **Purge** : Nathan aide le commandant à anéantir la ville. Il combat le **Dealer** et ses gardes, qui tentent de fuir avec le grain. L'atrocité elle-même (la ville qui brûle, les habitants enfermés) **se voit sans être jouée** : le joueur ne tue pas d'habitants. Puis les survivants **acclament Nathan comme un sauveur**. C'est le moment Arthas : il est applaudi pour une atrocité présentée comme une nécessité. |
| `ch02_avocat_purge` | Si purge : l'Avocat approuve, logiquement, calmement. |
| `ch02_mara_fin` | Mara de dos. Après la purge, elle est impressionnée malgré elle, mais froide sur les morts. Après la quarantaine ou l'évacuation, elle se moque de sa « bonté de gamin ». |

### Choix et conséquences
| Choix | Acte | Effet |
|---|---|---|
| Prendre la dose à crédit | `acte_ch02_dose` | Dépendance + ; dette (ch. 3) |
| Refuser | `acte_ch02_refus` | Humanité + ; pas de dette |
| Quarantaine | `acte_ch02_quarantaine` | Humanité + ; le guérisseur survit **(retour ch. 9)** |
| Évacuation | `acte_ch02_evacuation` | Humanité + ; les réfugiés survivent **(retour ch. 9)** ; risque de contagion au ch. 3 |
| Purge | `acte_ch02_purge` | Cruauté ++, Humanité − ; le commandant devient un allié ; des survivants réclameront justice **(retour ch. 9)** |

---

## Chapitre 3 — Le prix du respect

**Réel** : vol et marchandage · **Red Met** : conquête · **Incursion 3** · **Lieux** : rues et arrière-cours, boutique de l'usurier, planque du Chimiste, salle de bains ; Forteresse de cendres (approche).
**Nathan** : R0 à R2 ; M0 à M2 (3 doses au plus). Son corps réel commence à perdre du poids et ses nuits deviennent chaotiques.

### Réel
| Identifiant | Scène |
|---|---|
| `ch03_reel_argent` | Nathan manque d'argent. S'il a une dette, le Chimiste la lui rappelle, toujours poliment. |
| `ch03_reel_poubelles` | **Fouille des poubelles** (activité jouable) : cuivre, objets cassés, biens revendables. On y trouve aussi les traces de vies ordinaires (une lettre, un jouet, des médicaments jetés, une photographie). C'est lent, humiliant et parfois insuffisant. |
| `ch03_reel_usurier` | L'**usurier** rachète les objets à vil prix. Nouveau personnage du réel, à nommer. |
| `ch03_reel_travail` | **Travail honnête** : un petit boulot payé peu et tard. |
| `ch03_reel_vol` | **Petits vols** (supérette, sacs) : de l'argent vite gagné, avec un risque d'être reconnu. |
| `ch03_reel_collier` | **Le collier en or.** Une vieille dame porte un collier qui paierait la dose. Le joueur peut détourner le regard, lui demander de l'aide, tenter de voler le bijou ou l'agresser. La scène montre sa **terreur** et les conséquences de l'acte : elle ne devient jamais une animation de récompense. |
| `ch03_reel_marc` | Marc remarque l'amaigrissement et les nuits blanches. Il pose des questions ; Nathan ment ou esquive. Cette scène prépare le ch. 4. |

### La dose
| Identifiant | Scène |
|---|---|
| `ch03_avocat_dose` | L'Avocat fait le compte : il manque de l'argent, et la vieille dame n'en a pas besoin. |
| `ch03_dose_choix` | **Dose 3, contre de l'argent.** Sans dette, le travail honnête et les poubelles suffisent : la dose s'achète sans crime. **Avec la dette du ch. 2**, il faut en plus un vol, l'usurier (des objets de Marc) ou le collier. |
| `ch03_dose_culpabilite` | Si la dose a été payée par un vol ou une agression : **scène de culpabilité complète**, puis la dose. |
| `ch03_dose_hesitation` | Si elle a été payée honnêtement : hésitation, puis la dose. |
| `ch03_reel_passage` | Passage dans le monde parallèle. |

### Monde de la Red Met
| Identifiant | Scène |
|---|---|
| `ch03_mara_ouverture` | Mara de face (M-T0 à M-T2). Pour la première fois, elle **remarque sa progression** et le provoque par jeu. Si Nathan est encore M0, elle s'en amuse. |
| `ch03_rm_arme` | Mara lui confie une **arme capable de tuer les créatures de la peste** : des **gantelets** pour Nathan sobre (il ne se bat toujours qu'avec les poings et les pieds), une **lame** pour Nathan sous Red Met (décision de l'auteur). |
| `ch03_rm_peste` | Combats contre les **Voyous pestiférés**. Si l'évacuation a fait sortir la contagion au ch. 2, ils sont plus nombreux. Sous Red Met, chaque combat **augmente la soif de violence** : les démembrements arrivent avec les armes tranchantes, selon le réglage de gore. |
| `ch03_rm_compagnon` | Un **ancien compagnon d'armes** de la Ville du grain rouge (le guérisseur, un garde du commandant ou un réfugié, selon le choix du ch. 2 ; *nom à définir*) **bloque l'accès à la forteresse**. Nathan peut le **convaincre**, le **contourner** ou l'**exécuter** pour ne pas perdre de temps. |
| `ch03_rm_convaincre` | **Convaincre** : un dialogue. Cette option n'existe que si Nathan a assez d'Humanité, ou s'il a épargné la ville au ch. 2. |
| `ch03_rm_contourner` | **Contourner** : un chemin plus long, avec davantage de combats contre la **garnison de Soldats privés** (bestiaire, « Acte I — Fin » : coordination, discipline). C'est la voie la plus dure pour Nathan sobre, mais le décor offre des abris et des chemins de fuite. |
| `ch03_rm_executer` | **Exécuter** : rapide, sans combat. Le compagnon tombe, et ceux qui l'ont vu ont **peur** de Nathan. Thème du chapitre : le respect obtenu par la peur n'est pas le respect. |
| `ch03_rm_forteresse` | Combat à la porte de la forteresse contre les **Soldats privés**. Le chapitre s'arrête devant la porte : le siège commence au ch. 4. |
| `ch03_mara_fin` | Mara de dos. Si Nathan a exécuté son compagnon, elle est troublée, entre attirance pour sa force et méfiance. Sinon, elle le provoque : « Tu deviens intéressant. » Puis elle disparaît. |

### Choix et conséquences
| Choix | Acte | Effet |
|---|---|---|
| Détourner le regard (collier) | `acte_ch03_collier_ignore` | Aucun effet |
| Demander de l'aide à la vieille dame | `acte_ch03_collier_aide` | Humanité + ; un peu d'argent, ou rien |
| Voler le collier | `acte_ch03_collier_vol` | Cruauté +, Humanité − ; argent ; culpabilité avant la dose |
| Agresser la vieille dame | `acte_ch03_collier_agression` | Cruauté ++, Humanité −− ; argent ; plainte à la police (pression aux ch. 6-7) ; culpabilité avant la dose |
| Petits vols | `acte_ch03_vol` | Cruauté + ; argent ; culpabilité avant la dose |
| Vendre des objets de Marc à l'usurier | `acte_ch03_objets_marc` | Humanité − ; Marc s'en apercevra au ch. 4 |
| Prendre la dose / la refuser | `acte_ch03_dose` / `acte_ch03_refus` | Dépendance + / Humanité + |
| Convaincre le compagnon | `acte_ch03_compagnon_convaincu` | Humanité + ; il rejoint Nathan **(retour ch. 9)** |
| Contourner le compagnon | `acte_ch03_compagnon_contourne` | Neutre ; il reste en vie, méfiant **(retour ch. 9)** |
| Exécuter le compagnon | `acte_ch03_compagnon_execute` | Cruauté ++ ; il ne revient jamais ; ses proches réclament justice **(retour ch. 9)** |

---


## Chapitre 4 — La couronne de cendres

**Réel** : rupture familiale · **Red Met** : rébellion · **Incursion 4** · **Lieux** : appartement de Marc, planque du Chimiste, salle de bains ; Forteresse et trône de cendres, province abandonnée.
**Nathan** : R0 à R2 ; M0 à M2 (4 doses au plus). Mara : de M-T0 à M-T2.

### Réel
| Identifiant | Scène |
|---|---|
| `ch04_reel_marc_argent` | Marc a compris que l'argent part ailleurs (et que des objets ont disparu, si Nathan les a vendus au ch. 3). Il **coupe l'accès à son argent**. Première grande divergence familiale : Nathan peut **se confier**, **le voler** ou **choisir l'affrontement** (cris, porte claquée, un coup qui part). |
| `ch04_reel_chimiste_demande` | Le Chimiste ne veut plus d'argent. **Demande 1 : l'humiliation imposée** (GDD v0.1, 10.3). Nathan prétend être « prêt à tout » ; le Chimiste exige une faveur sexuelle pour le prouver. Il présente cela comme une simple transaction. |
| `ch04_avocat_demande` | L'Avocat minimise : ce n'est qu'un moment, personne ne saura, et la dose est au bout. |
| `ch04_reel_humiliation` | Si Nathan accepte : la scène **s'arrête avant tout acte** (fondu au noir). On retrouve Nathan après, seul dans la rue, qui tremble et frotte ses mains jusqu'au sang. Rien d'explicite : la contrainte, la perte de dignité et d'autonomie, jamais un contenu érotique. |
| `ch04_reel_rupture` | S'il tente de **rompre avec le réseau** : il part en menaçant de parler. Le Chimiste, poli, lui rappelle qu'on ne quitte pas le réseau. Les hommes du Dealer le guettent désormais (Paranoïa +). |

### La dose
| Identifiant | Scène |
|---|---|
| `ch04_dose_choix` | **Dose 4, contre la demande 1.** La refuser ou rompre, c'est refuser la dose. |
| `ch04_dose_culpabilite` | Si la demande est acceptée : **scène de culpabilité complète** (la première vraiment longue), puis la dose. |
| `ch04_reel_passage` | Effondrement dans la salle de bains, passage. |

### Monde de la Red Met
| Identifiant | Scène |
|---|---|
| `ch04_mara_ouverture` | Mara de face (M-T0 à M-T2). Elle le provoque par jeu : elle veut voir s'il ose aller plus loin. Elle ne sait rien de son réel. |
| `ch04_rm_province` | Nathan découvre que **le roi a abandonné une province entière** pour préserver son armée : villages vides, réfugiés, cadavres de peste. Combats contre les **Chasseurs** (bestiaire, Acte II : embuscades, pièges, attaques depuis le couvert), qui rançonnent la province. |
| `ch04_rm_revolte` | Nathan **prend la tête d'une révolte** : les réfugiés et les survivants de la Ville du grain rouge se rallient à lui (selon le ch. 2). |
| `ch04_rm_siege` | Siège de la forteresse (suite du ch. 3). Combats contre la garde royale ; **boss : le champion du roi**, un **Mutant** élite (bestiaire 06 ; première apparition du Mutant : attaques lourdes, charge). |
| `ch04_rm_trone` | Dans la salle du trône, Nathan tient le roi à sa merci : **le renverser sans le tuer**, **l'exécuter devant son peuple** ou **le laisser en vie pour qu'il soit jugé**. La victoire militaire ne garantit pas la paix. |
| `ch04_avocat_roi` | L'Avocat plaide pour l'exécution : un roi vivant reste une menace. |
| `ch04_mara_fin` | Mara de dos. Si le roi est exécuté, elle est fascinée et un peu inquiète ; sinon, elle raille sa « clémence de petit garçon », mais le regarde autrement. |

### Choix et conséquences
| Choix | Acte | Effet |
|---|---|---|
| Se confier à Marc | `acte_ch04_marc_confie` | Humanité + ; Marc veut l'aider, ce qui ouvre « prévenir Marc » au ch. 5 |
| Voler Marc | `acte_ch04_marc_vole` | Humanité −, Cruauté + ; argent ; Marc ne lui fait plus confiance |
| Affronter Marc | `acte_ch04_marc_frappe` | Cruauté ++ ; Marc a peur de son fils |
| Accepter l'humiliation | `acte_ch04_humiliation_acceptee` | **Grande demande acceptée** (voir « La fin ») ; Humanité −, Paranoïa +, Dépendance + ; culpabilité |
| Refuser | `acte_ch04_humiliation_refusee` | Humanité + ; pas de dose |
| Rompre avec le réseau | `acte_ch04_rupture` | Humanité +, Paranoïa + ; pas de dose ; le Dealer le guette (ch. 5-6) |
| Renverser le roi sans le tuer | `acte_ch04_roi_renverse` | Humanité + ; le roi vit **(ch. 5)** |
| Exécuter le roi devant son peuple | `acte_ch04_roi_execute` | Cruauté ++ ; peur dans le royaume ; variante du ch. 5 |
| Laisser le roi en vie pour qu'il soit jugé | `acte_ch04_roi_juge` | Humanité + ; un procès s'ouvre **(ch. 5)** |

---

## Chapitre 5 — Le fils qui tue son père

**Réel** : point de rupture · **Red Met** : trahison · **Incursion 5** · **Lieux** : appartement de Marc, planque du Chimiste, salle de bains ; Forteresse et trône de cendres.
**Nathan** : R0 à R3 ; M0 à M3 (5 doses au plus). Mara : de M-T0 à M-T3.

### Réel
| Identifiant | Scène |
|---|---|
| `ch05_reel_marc_confrontation` | Marc découvre **jusqu'où Nathan est allé**. Il le confronte, sans violence, et lui demande d'arrêter avant qu'il ne soit trop tard. Le ton dépend du ch. 4 (confiance, vol ou peur). |
| `ch05_reel_chimiste_demande` | **Demande 2 : le cœur de Marc** (GDD v0.1, 10.1). « Les organes se revendent bien au marché noir. J'ai une demande pour un cœur. Tue ton père, ramène-le-moi, et tu auras ta prochaine dose. » |
| `ch05_avocat_demande` | L'Avocat, encore ambigu, fait le compte : Marc l'empêche d'avancer, Marc ne le comprend pas. |
| `ch05_reel_marc_meurtre` | Si Nathan accepte : **le meurtre de Marc, irréversible**. La scène est coupée avant le geste ; on voit ensuite l'appartement vide et froid, et la chaise de Marc. Rien du trafic d'organe n'est montré. |
| `ch05_reel_marc_prevenu` | **Prévenir Marc** (possible s'il s'est confié au ch. 4, ou si l'Humanité est haute) : Marc part se cacher ; il survit, loin de son fils. |
| `ch05_reel_reseau_dejoue` | **Déjouer le réseau** : Nathan donne un faux rendez-vous et prévient la police ; le Chimiste perd un lieutenant et sait qui l'a trahi. Combat de rue contre les hommes du Dealer (le Dealer et ses gardes, dans le réel, sous une lumière bleu-gris). |

### La dose
| Identifiant | Scène |
|---|---|
| `ch05_dose_choix` | **Dose 5, contre la demande 2.** Seul le meurtre la paie. |
| `ch05_dose_culpabilite` | Si Marc est tué : **la scène de culpabilité la plus violente du jeu**, puis la dose. |
| `ch05_reel_passage` | Passage. |

### Monde de la Red Met
| Identifiant | Scène |
|---|---|
| `ch05_mara_ouverture` | Mara de face (M-T0 à M-T3). Si Nathan est puissant, elle **se rapproche de lui**, attirée par son assurance et sa force ; s'il est sobre, elle reste distante, mais moins moqueuse. |
| `ch05_rm_epidemie` | La peste gagne la forteresse. Combats contre des **Psychos** (bestiaire 07 : feintes, changements de rythme), courtisans et soldats rendus fous, et contre des Chasseurs. |
| `ch05_rm_rituel` | Nathan apprend que **le roi déchu**, son père symbolique et dernier protecteur, possède le pouvoir d'arrêter l'épidémie. Le rituel exige un **sacrifice que Nathan refuse d'assumer** : renoncer à sa prochaine dose (la dose du ch. 6 devient impossible). |
| `ch05_rm_choix_roi` | **Accomplir le rituel** (le roi vit, l'épidémie recule, pas de dose au ch. 6), **tuer le roi pour prendre son pouvoir** (la voie la plus rapide vers la puissance) ou **renoncer aux deux** (l'épidémie continue : plus d'ennemis pestiférés aux ch. 6 et 9). |
| `ch05_rm_regicide` | Si Nathan choisit de tuer : **boss, le roi déchu** (design à proposer : vieux guerrier en armure de cendres, lent et dangereux, qui ne se défend qu'à moitié). Si le roi a été exécuté au ch. 4, le pouvoir dort dans la **couronne de cendres** : Nathan doit l'arracher à la garde des morts, et le remède est perdu. |
| `ch05_rm_echo` | Si Nathan a tué Marc **et** le roi, les deux actes se répondent : une vision mêle les deux visages. Il détruit les figures qui lui rappellent ses limites. |
| `ch05_mara_fin` | Mara de dos. Après le régicide, elle est troublée et attirée par sa force ; après le rituel, elle le regarde avec un respect nouveau. |
| `ch05_reel_reveil` | Fin du chapitre : Nathan se réveille dans son appartement, couvert de sueur. **Il rit sans pouvoir s'arrêter**, sans savoir pourquoi. |
| `ch05_avocat_premiere_voix` | Pour la première fois, l'Avocat parle comme une **voix séparée** : « Tu as fait ce qu'il fallait. Tu le sais, n'est-ce pas ? » |

### Choix et conséquences
| Choix | Acte | Effet |
|---|---|---|
| Tuer Marc | `acte_ch05_coeur_marc` | **Grande demande acceptée** ; irréversible : Marc disparaît de l'histoire ; Cruauté +++, Humanité −− ; culpabilité |
| Refuser | `acte_ch05_coeur_refus` | Humanité + ; pas de dose |
| Prévenir Marc | `acte_ch05_marc_prevenu` | Humanité ++ ; Marc vit, caché **(fin)** |
| Déjouer le réseau | `acte_ch05_reseau_dejoue` | Humanité +, Paranoïa + ; le Chimiste se méfie ; la demande 3 arrive quand même |
| Accomplir le rituel | `acte_ch05_rituel` | Humanité ++ ; le roi vit **(retour ch. 9)** ; **pas de dose au ch. 6** |
| Tuer le roi | `acte_ch05_regicide` | Cruauté ++ ; l'épidémie s'arrête ; le royaume le craint |
| Renoncer aux deux | `acte_ch05_epidemie` | L'épidémie continue (ch. 6 et 9) |

---

## Chapitre 6 — La voix derrière le miroir

**Réel** : perte de contrôle · **Red Met** : armes à feu · **Incursion 6** · **Lieux** : salle de bains, rues et arrière-cours, planque du Chimiste ; forteresse, puis faubourgs du royaume.
**Nathan** : R0 à R3 ; M0 à M3 (6 doses au plus). Mara : de M-T0 à M-T3.

### Réel
| Identifiant | Scène |
|---|---|
| `ch06_reel_miroir` | **La scène du miroir** (texte de l'auteur, source section 4) : Nathan ouvre la bouche, une dent tombe dans le lavabo. Le joueur presse la touche de déplacement, mais Nathan ne bouge pas. L'Avocat parle, puis un rire sec. Selon les doses, le reflet est celui d'un garçon épuisé (sobre) ou d'un corps creusé (R3). |
| `ch06_reel_intrusions` | **Premières intrusions** (source, section 5) : Nathan s'arrête quelques secondes, murmure, regarde un personnage absent ; une touche peut donner une autre réponse que celle attendue. Le contrôle revient vite, et chaque intrusion est annoncée par un indice (son, distorsion). |
| `ch06_reel_prix_corps` | **Le prix de son corps** (dilemme du réel). Un adulte propose de l'argent à Nathan en échange d'un rapport sexuel. Nathan peut **refuser**, **accepter**, **négocier** ou **demander une autre forme de travail**. Scène non explicite, coupée avant tout acte : vulnérabilité et marchandage sous dépendance. Le lendemain, Nathan se souvient mal de la nuit, en écho aux absences. |
| `ch06_reel_police` | Si Nathan a agressé la vieille dame (ch. 3) ou trahi le réseau (ch. 5) : un policier pose des questions dans le quartier. |

### La dose
| Identifiant | Scène |
|---|---|
| `ch06_avocat_dose` | L'Avocat **commente désormais les choix** : il félicite la violence, reproche les hésitations et prétend connaître les vraies intentions de Nathan. |
| `ch06_dose_choix` | **Dose 6, contre de l'argent.** Impossible si le rituel a été accompli au ch. 5 (le sacrifice). |
| `ch06_dose_culpabilite` / `ch06_dose_hesitation` | Culpabilité si l'argent vient d'un acte immoral (vol, prix de son corps accepté) ; sinon hésitation. Les crises deviennent **plus courtes et plus silencieuses**. |
| `ch06_reel_passage` | Passage. |

### Monde de la Red Met
| Identifiant | Scène |
|---|---|
| `ch06_mara_ouverture` | Mara de face (M-T0 à M-T3). Par une faille, elle aperçoit pour la première fois le **corps réel** de Nathan : elle comprend que sa puissance a un prix. |
| `ch06_rm_armes_feu` | **Les armes à feu apparaissent dans le royaume** : la garde (Soldats privés) et les mercenaires tirent. Combats plus frénétiques ; ennemis dominants : les **Psychos** (feintes). |
| `ch06_rm_blessures` | **Blessures fantômes** : les coups reçus par le double laissent des marques sur le corps réel (bleus visibles au retour, douleur qui fait trembler l'image). |
| `ch06_rm_absence` | Une intrusion en plein combat : le double s'arrête une seconde et regarde le joueur. Elle est annoncée par un son et ne cause jamais de mort injuste (les ennemis reculent pendant ce temps). |
| `ch06_mara_fin` | Mara de dos. Moins moqueuse : elle lui demande ce qu'il est prêt à perdre encore, puis disparaît. |

### Choix et conséquences
| Choix | Acte | Effet |
|---|---|---|
| Accepter (prix de son corps) | `acte_ch06_corps_accepte` | Argent ; Humanité −, Paranoïa + ; culpabilité avant la dose |
| Négocier | `acte_ch06_corps_negocie` | Moins d'argent, même scène ; Paranoïa + |
| Refuser / demander un autre travail | `acte_ch06_corps_refus` / `acte_ch06_autre_travail` | Humanité + ; peu ou pas d'argent |
| Prendre la dose / la refuser | `acte_ch06_dose` / `acte_ch06_refus` | Dépendance + / Humanité + |

---

## Chapitre 7 — Le sang des autres

**Réel** : crimes et paranoïa · **Red Met** : vampirisme · **Incursion 7** · **Lieux** : rues, planque du Chimiste, salle de bains ; royaume en ruines.
**Nathan** : R0 à R4 ; M0 à M4 (7 doses au plus). Mara : de M-T0 à M-T4.
**Règle de ce chapitre** : la branche de l'enfant n'admet **aucun contenu sexuel ni suggestif**, ni à proximité. Les scènes de Mara du ch. 7 restent sans ton coquin, quelle que soit la branche, et sa pose de fin n'est pas cambrée.

### Réel
| Identifiant | Scène |
|---|---|
| `ch07_reel_absences` | **Les absences** (source, section 5) : Nathan marche seul vers une porte ou fixe un objet ; de brèves coupures montrent des actes dont le joueur ne se souvient pas. Le doute est volontaire. |
| `ch07_reel_innocents` | Pour financer ses prises, le joueur peut **franchir de nouvelles limites** : violence contre des passants. La peur et les conséquences sont montrées, jamais une récompense. |
| `ch07_reel_chimiste_demande` | **Demande 3 : l'enfant** (GDD v0.1, 10.2). « Le marché noir me demande un enfant. » Nathan comprend que le Chimiste traite désormais les êtres humains comme une marchandise. |
| `ch07_reel_enfant` | Choix : **refuser**, **mettre l'enfant à l'abri**, **saboter la transaction** ou **livrer l'enfant**. **Aucun détail pratique.** L'enfant n'est jamais un objet d'inventaire : on voit la détresse de la famille et l'avis de recherche. |
| `ch07_reel_enquete` | Si l'enfant est livré : **enquête**, famille en détresse, **pression policière** (avis de recherche, policiers dans le quartier jusqu'au ch. 10), rupture morale majeure. Si l'enfant est mis à l'abri ou la transaction sabotée : le réseau se retourne contre Nathan. |

### La dose
| Identifiant | Scène |
|---|---|
| `ch07_avocat_dose` | L'Avocat ne cherche plus d'excuses : il explique qu'il est trop tard pour reculer. |
| `ch07_dose_choix` | **Dose 7, contre la demande 3.** Seul l'enfant livré la paie. Avec cette dose, Nathan atteint le **stade M4** : pouvoirs vampiriques et fusil à pompe. |
| `ch07_dose_culpabilite` | Si l'enfant est livré : la crise est **silencieuse**. Nathan ne pleure plus, il fixe le mur, puis prend la dose. |
| `ch07_reel_passage` | Passage. |

### Monde de la Red Met
| Identifiant | Scène |
|---|---|
| `ch07_mara_ouverture` | Mara de face (M-T0 à M-T4), sans ton coquin. Elle commence à comprendre que **l'homme qu'elle désirait n'existe peut-être plus**. |
| `ch07_rm_vampirisme` | Avec 7 doses : Nathan **draine la vitalité** de ses ennemis (soin, mobilité surnaturelle). Il devient presque impossible à arrêter, mais **chaque drain augmente la Dépendance et les hallucinations** (ennemis fantômes, décor qui saigne). Sobre : il affronte les mêmes ennemis aux poings et aux pieds, avec les gantelets. |
| `ch07_rm_combats` | Ennemi dominant : le **Clown tueur** (bestiaire 08 : pièges, tromperies, perception déstabilisée), dans les ruines d'une fête du royaume. **Boss : un Clown tueur** élite (le maître de la fête). |
| `ch07_rm_choix_mara` | **Mara choisit**, selon les actes et les axes : **tenter de le sauver** (Humanité haute, ni Marc ni l'enfant sacrifiés), **le combattre** (Cruauté haute ou enfant livré) ou **profiter de sa puissance** pour ses propres ambitions (beaucoup de doses, peu d'Humanité). Ce choix fixe son rôle aux ch. 8 à 10. |
| `ch07_rm_duel_mara` | Si Mara choisit de le combattre : **combat contre Mara** dans l'arène de la lune rouge (elle s'enfuit à 30 % de ses PV, sans mourir). |
| `ch07_mara_fin` | Mara de dos, debout, la tête tournée, grave : elle ne nargue plus. |

### Choix et conséquences
| Choix | Acte | Effet |
|---|---|---|
| Violence contre des passants | `acte_ch07_innocents` | Cruauté ++, Humanité −− ; argent ; pression policière |
| Livrer l'enfant | `acte_ch07_enfant_livre` | **Grande demande acceptée** ; rupture morale majeure ; enquête jusqu'au ch. 10 ; Humanité −−− |
| Refuser | `acte_ch07_enfant_refus` | Humanité + ; pas de dose |
| Mettre l'enfant à l'abri | `acte_ch07_enfant_abri` | Humanité +++ ; le réseau menace Nathan |
| Saboter la transaction | `acte_ch07_enfant_sabotage` | Humanité ++, Paranoïa + ; le Chimiste perd ses fournisseurs **(ch. 8)** |
| Mara : le sauver / le combattre / ses ambitions | `acte_ch07_mara_salut` / `acte_ch07_mara_ennemie` / `acte_ch07_mara_ambition` | Rôle de Mara aux ch. 8 à 10 |

---

## Chapitre 8 — Le monde est coupable

**Réel** : persécution imaginaire · **Red Met** : invasion · **Incursion 8** · **Lieux** : Ville réelle envahie (rues bleu-gris fissurées de veines rouges), planque du Chimiste.
**Nathan** : R0 à R4 ; M0 à M4 (8 doses au plus).

### Réel et invasion
| Identifiant | Scène |
|---|---|
| `ch08_reel_complots` | Nathan ne distingue plus les coïncidences des complots : refus, critiques et gestes ordinaires deviennent des attaques. Les dialogues du réel sont altérés par la Paranoïa. |
| `ch08_avocat_coupables` | L'Avocat accuse la société, les créateurs, ses anciens camarades. Ses accusations contre des boucs émissaires (dont les créateurs LGBTQ+) relèvent **de sa paranoïa, jamais du verdict du scénario**. |
| `ch08_avocat_quatrieme_mur` | **« C'est ta faute »** (texte de l'auteur, source section 4) : écran noir, menu de pause qui se déforme, la voix parle au joueur. Le personnage repart sans intervention du joueur. « Vous vous ressemblez beaucoup, tous les deux. » |
| `ch08_reel_chimiste` | Le Chimiste, pour la première fois, **a peur** : ses propres fournisseurs, bien plus dangereux que lui, réclament leur dû (surtout si la transaction a été sabotée au ch. 7). On voit ce qu'il protège. Il propose la dose 8 contre de l'argent, comme pour se rassurer. |

### La dose
| Identifiant | Scène |
|---|---|
| `ch08_dose_choix` | **Dose 8, contre de l'argent.** |
| `ch08_dose_culpabilite` / `ch08_dose_hesitation` | Selon l'origine de l'argent. Il n'y a plus de passage : **le monde parallèle envahit les rues**. |

### Le monde de la Red Met dans le réel
| Identifiant | Scène |
|---|---|
| `ch08_mara_ouverture` | Mara apparaît dans la ville réelle, alliée, adversaire ou complice selon le ch. 7 (tenue selon les doses). |
| `ch08_rm_invasion` | Séquences où le joueur doit **distinguer le réel de l'hallucination** : les créatures hallucinées ont un indice constant (ombre rouge, veines dans le décor) ; certains passants restent **volontairement ambigus**. Frapper un vrai passant écrit un acte. |
| `ch08_rm_desobeissance` | **Désobéissance** (source, section 5) : la voix commente les commandes ; Nathan peut refuser une action, ou faire l'inverse, avant que le joueur reprenne la main. |
| `ch08_rm_combats` | Ennemi dominant : le **Démon techno** (bestiaire 09 : zones électrifiées, téléportation), né des néons de la ville. |
| `ch08_mara_fin` | Mara de dos, dans la rue. Alliée, elle lui demande de s'arrêter ; adversaire, elle le menace ; complice, elle le pousse. |

### Choix et conséquences
| Choix | Acte | Effet |
|---|---|---|
| Frapper un passant réel | `acte_ch08_passant` | Cruauté +, Paranoïa ++ ; pression policière |
| Épargner un passant ambigu | `acte_ch08_epargne` | Humanité + |
| Prendre la dose / la refuser | `acte_ch08_dose` / `acte_ch08_refus` | Dépendance + / Humanité + |

---

## Chapitre 9 — Le roi sans royaume

**Réel** : isolement · **Red Met** : guerre finale · **Incursion 9** · **Lieux** : chambre vide de Nathan ; Arène de la lune rouge, champ de bataille.
**Nathan** : R0 à R4 ; M0 à M4 (9 doses au plus).

### Réel
| Identifiant | Scène |
|---|---|
| `ch09_reel_isolement` | Plus personne : Marc mort, caché ou brouillé, téléphone muet, porte fermée. Si l'enfant a été livré, l'avis de recherche est affiché sur la porte de l'immeuble. |
| `ch09_avocat_tout_detruire` | La voix soutient que Nathan **doit tout détruire** pour empêcher le monde de tomber entre de mauvaises mains. |
| `ch09_dose_choix` | **Dose 9, contre de l'argent** (ou offerte par la voix elle-même, si le réseau est tombé). Culpabilité ou hésitation. |
| `ch09_reel_passage` | Passage. |

### Monde de la Red Met
| Identifiant | Scène |
|---|---|
| `ch09_mara_ouverture` | Mara de face, selon son rôle (alliée, adversaire, complice), ou **absente** si elle a disparu au ch. 7. |
| `ch09_rm_retours` | **Les anciens alliés reviennent selon les choix** : le guérisseur ou les réfugiés (ch. 2), le commandant (purge), le compagnon d'armes (ch. 3), le roi (ch. 4-5). Certains aident Nathan ; d'autres **réclament justice** (survivants de la purge, proches du compagnon exécuté, peuple du roi exécuté) et le combattent. |
| `ch09_rm_guerre` | Bataille contre les forces qui veulent fusionner les deux mondes. Ennemi dominant : le **Spectre** (bestiaire 10 : illusions, doubles). **Mini-boss : le Géant de chair** (bestiaire 11, trois phases). |
| `ch09_rm_controle` | **Qui joue qui ?** Les interfaces des deux mondes se mélangent ; certaines animations se déclenchent avant la commande (toujours courtes et annoncées). |
| `ch09_mara_fin` | Mara de dos : selon son rôle, une promesse, une menace ou un adieu. |

### Choix et conséquences
| Choix | Acte | Effet |
|---|---|---|
| Accepter l'aide d'un allié revenu | `acte_ch09_allie_<nom>` | Humanité + ; l'allié combat à ses côtés au ch. 10 |
| Affronter ceux qui réclament justice / se rendre à eux | `acte_ch09_justice_combat` / `acte_ch09_justice_reddition` | Cruauté + / Humanité ++ (combat évité) |
| Prendre la dose / la refuser | `acte_ch09_dose` / `acte_ch09_refus` | Dépendance + / Humanité + |

---

## Chapitre 10 — Red Met

**Réel et hallucination** : fusion totale · **Incursion 10** · **Lieux** : salle de bains, ville fusionnée, puis plaine de cendres sous la lune rouge ; hôpital.
**Nathan** : R0 à R4 ; M0 à M4 (10 doses au plus).

| Identifiant | Scène |
|---|---|
| `ch10_avocat_derniere_dose` | **La dernière dose, proposée par l'Avocat** (source, section 7) : « Tu as sacrifié ton père. Tu as trahi Mara. Tu as fait des choses que tu ne peux même plus te rappeler. Tu ne vas quand même pas t'arrêter maintenant ? Tout ça doit bien servir à quelque chose. » La réplique ne cite **que les actes réellement commis**. |
| `ch10_dose_choix` | **Dose 10.** Le refus ne rend pas Nathan héroïque : il doit vivre avec ce qu'il a fait. L'acceptation fait de la voix le narrateur permanent de son existence. |
| `ch10_rm_fusion` | Traversée de la ville fusionnée : des ennemis de tous les actes, et tout ce qui a été appris. |
| `ch10_rm_double` | **Combat final contre le double** (M5), créature gigantesque qui représente tout ce que Nathan a sacrifié pour être puissant. Il reprend les mouvements et les capacités des chapitres précédents. *Proposition* : le **Porteur de l'aberration** (bestiaire 12) est la forme que prend le double dans ses premières phases (déformation de l'arène et des règles), avant de révéler le visage de Nathan, avec un fragment de ses lunettes incrusté dans l'armure. Les alliés du ch. 9 interviennent. |
| `ch10_rm_choix_final` | **Le dernier choix** : **détruire la source** de la Red Met, **prendre le contrôle** de son pouvoir ou **accepter la fusion** avec son double. Dans certaines branches (Dépendance très haute), la voix choisit un instant avant le joueur. Le vrai choix : **assumer ses actes ou continuer à les justifier**. |
| `ch10_mara_fin_immorale` | **Voie immorale** (au moins 6 doses sur 10 **et** au moins 2 des 3 grandes demandes acceptées : humiliation, cœur de Marc, enfant). Section 11.1 du GDD v0.1 : dopé et submergé par son agressivité, Nathan franchit une limite et tente d'imposer sa volonté à Mara par la force. **La scène est coupée avant l'acte, sans nudité.** Mise en scène comme un moment d'horreur et de rupture, elle reste **centrée sur la réaction de Mara** : son refus, sa colère, elle le repousse en combattante. Elle n'est jamais une récompense ; sa puissance n'apporte à Nathan ni maîtrise ni accomplissement. |
| `ch10_mara_fin_morale` | **Voie morale** (tous les autres cas). Section 11.2 du GDD v0.1 : Mara n'a aucune interaction avec lui. Elle lui montre ses biceps 💪 et lance : « Moi, je veux un vrai bonhomme. Toi, tu n'es qu'un gamin, et les gamins, je les envoie en colo. » C'est humiliant et frustrant : il a gardé son intégrité, pas ce qu'il désirait. |
| `ch10_effondrement` | Dans tous les cas : **les yeux de Nathan se révulsent** (comme l'Undertaker), il s'effondre. Écran noir. |
| `ch10_hopital` | **Lit d'hôpital**, coma, sons calmes et cliniques. La caméra s'approche de la fiche patient au pied du lit : en bas, **« RED MET 2 »**. *Proposition* : si Marc est vivant et n'est pas brouillé avec lui, il est assis au chevet ; sinon, la chaise est vide. |
| `ch10_generique` | Le générique varie selon le dernier choix (survivre, régner, disparaître, recommencer le cycle), comme une dernière image avant le noir. |

### La fin : condition de la voie immorale
Évaluée au moment de la scène avec Mara, dose 10 comprise : **au moins 6 doses prises** et **au moins 2** des actes `acte_ch04_humiliation_acceptee`, `acte_ch05_coeur_marc` et `acte_ch07_enfant_livre`. Les valeurs sont dans `Data/Story/Progression.asset` (`immoralEndingMinDoses`, `immoralEndingMinDemands`, `chimisteDemandActs`) et lues par `GameSession.ImmoralEnding`.

---

## Décisions de l'auteur (2026-10-28)
1. Accès au monde parallèle même sans dose, par l'effondrement de fatigue ou de manque.
2. Boss du ch. 1 : une créature propre au chapitre, pas le Mutant (réservé à l'Acte II). Trois idées sont proposées plus haut, à choisir avant de l'ajouter au bestiaire.
3. Humiliation du ch. 1 : au fast-food (patron et clients), puis dans la rue. Pas de lycée.
4. Demandes du Chimiste : humiliation au ch. 4, cœur de Marc au ch. 5, enfant au ch. 7 ; le prix de son corps (dilemme du réel) au ch. 6.
5. Stades de Nathan alignés sur Mara : M0 / R0 à 0 dose, M1 / R1 à 1-2, M2 / R2 à 3-4, M3 / R3 à 5-6, M4 / R4 à 7 et plus ; fusil à pompe et pouvoirs vampiriques à 7. Reporté dans le code et les données.
6. Fin immorale : au moins 6 doses sur 10 **et** au moins 2 des 3 grandes demandes acceptées.
7. Gantelets pour Nathan sobre, lame sous Red Met (ch. 3).

## Points à valider (chapitres 4 à 10)
1. **Boss du ch. 1** : A (le Chœur des rieurs), B (le Gérant) ou C (la Bête de cendres).
2. **Ch. 4** : le champion du roi, un Mutant élite, marque la première apparition du Mutant.
3. **Ch. 5** : le sacrifice du rituel consiste à renoncer à la dose du ch. 6 ; le roi déchu devient un boss (design à proposer) ; si le roi a été exécuté au ch. 4, le remède est perdu.
4. **Ch. 5** : « déjouer le réseau » donne un combat de rue dans le réel contre le Dealer.
5. **Ch. 7** : Mara choisit son rôle (le sauver, le combattre ou servir ses ambitions) selon les actes ; combat contre Mara si elle devient son adversaire.
6. **Ch. 8** : frapper un vrai passant pendant l'invasion écrit un acte.
7. **Ch. 10** : le Porteur de l'aberration est la première forme du double ; Marc est au chevet à l'hôpital s'il est vivant.
