# RED MET — Bible des personnages et des niveaux

Références visuelles fournies par l'auteur : `docs/references/` (moodboard interne, ne pas publier).
Les prompts Tripo sont en anglais, car le modèle les suit mieux. Il faut toujours demander une **pose en T, vue de face, fond neutre**, pour le rig et les rendus de sprites dans Blender.

---

## Nathan Voss : deux corps, une chute

Nathan a **18 ans au début de l'histoire**. Son visage et ses proportions doivent être ceux d'un jeune adulte, pas d'un collégien.

Principe directeur : **le corps réel se vide pendant que le double se remplit**. Les deux silhouettes doivent pouvoir se lire en ombre chinoise à chaque stade.

### Monde réel

| Stade | Chapitres | Silhouette | Visage | Tenue | Animation |
|---|---|---|---|---|---|
| **R0 — Le garçon invisible** | 1 | Adulte de 18 ans, 1,70 m, un peu enrobé (chubby), épaules rentrées, ventre mou | Mâchoire marquée, barbe naissante, **acné** sur le front et les joues, **lunettes à double foyer** épaisses à monture noire, cheveux bruns bouclés en bataille | Hoodie noir uni trop grand, cargo gris, baskets usées (cf. `nathan-reel-style.webp`) | Regard au sol, mains dans les poches, démarche hésitante |
| **R1 — Premières semaines** | 2–3 | Même garçon, 18-19 ans. A perdu un peu de ventre, joues affinées, le hoodie noir commence à flotter | Cernes, teint fatigué, regard nerveux, pupilles un peu dilatées, légère transpiration sur le front, acné encore visible, mêmes lunettes | Même hoodie noir et même cargo | Posture tendue, épaules remontées, mains crispées |
| **R2 — Le manque** | 4–5 | Nettement amaigri mais crédible : cou fin, pas encore la peau sur les os | Visage creusé, pommettes visibles, cernes profonds, teint pâle et terne (peau humaine, pas grise), lèvres sèches, barbe de quelques jours, cheveux gras, lunettes de travers et rayées, un verre fêlé | Hoodie gris foncé usé et trop grand, t-shirt défraîchi, jean noir élimé (pas en lambeaux), vieilles baskets ou bottes. Petites rougeurs de grattage sur les avant-bras et le cou, aux endroits des futures fissures du double. Aucune blessure | Voûté, bras serrés contre le corps comme s'il avait froid : on doit sentir le manque |
| **R3 — La peau sur les os** | 6–7 | **Émacié**, clavicules et poignets saillants | Dents abîmées, barbe négligée, yeux trop grands, verres fêlés | Vêtements qui pendent | Trébuche, tics, mouvements involontaires (cf. `nathan-reel-hallucination.webp`) |
| **R4 — Corps brisé** | 8–10 | Squelettique, voûté | Teint gris, regard halluciné, rictus | Hoodie gris taché et trop grand, jean noir en lambeaux, vieilles bottes. Marques rouges sur les avant-bras et le cou. Blouse d'hôpital pour l'épilogue | Marche saccadée, absences |

Le stade réel se calcule à partir des doses prises, comme celui du monde parallèle. Seuils (décision de l'auteur du 2026-10-28) : R0 à 0 dose, R1 à 1-2, R2 à 3-4, R3 à 5-6, R4 à 7 et plus. La colonne des chapitres vaut pour un joueur qui prend toutes ses doses ; le joueur sobre reste R0.

Ces stades sont une représentation **fictionnelle** de la dépendance, pas un jugement sur l'apparence. Le passage de « chubby » à « maigre » doit se lire comme une **perte de santé**, jamais comme une amélioration.

**De R0 à R2, on doit voir un jeune homme malade et épuisé, jamais un mort-vivant** : peau humaine (pâle, pas grise), aucune plaie ouverte, pas de sang, pas d'yeux vides ni de pupilles blanches. L'aspect « zombie » est réservé à R4 et aux hallucinations.

Concepts : `Assets/_Game/Art/Characters/Nathan_R0/` … `Nathan_R4/`, dans `Concept/`. Les premiers concepts générés pour R3 se lisaient comme trop « zombie » : ils ont été reclassés en R4. **R3 n'a pas encore de concept.**

### Monde de la Red Met

| Stade | Doses (décision du 2026-10-28) | Silhouette | Détails | Références |
|---|---|---|---|---|
| **M0 — Le faible** | 0 | Même corps que R0, sans les lunettes | Poings nus, garde maladroite | — |
| **M1 — L'éveil** | 1–2 | Musclé, posture droite | Barbe naissante, cheveux qui poussent, couteau | — |
| **M2 — Le barbare** | 3–4 | **Colosse**, épaules massives | Longue chevelure brun-roux, barbe épaisse, fourrures, cicatrices | `nathan-redmet-barbare.webp`, `nathan-redmet-guerrier.webp` |
| **M3 — Le seigneur de guerre** | 5–6 | Armure de plaques sombres | Peinture de guerre bleue, yeux rouges, armes lourdes | `nathan-redmet-guerrier.webp` |
| **M4 — Le prédateur** | 7 et plus | Peau grise, comme de la **pierre fissurée** | **Veines rouges incandescentes** (la Red Met visible sous la peau), yeux rouges luminescents | `nathan-redmet-final-double.webp` |
| **M5 — Le double** (boss du ch. 10) | — | Gigantesque, presque minéral | Les fissures rouges couvrent tout le corps ; un fragment des lunettes de R0 incrusté dans l'armure | `nathan-redmet-final-double.webp` |

**Fil rouge visuel :** les **fissures rouges** apparaissent sur le double au stade M3, puis gagnent du terrain. Dans le monde réel, des marques rouges discrètes apparaissent aux mêmes endroits (avant-bras, cou) : le joueur doit pouvoir faire le lien.

### Prompts Tripo

```
R0: stylized anime 3D character, 18-year-old young man, slightly chubby, rounded shoulders,
    curly messy brown hair, thick bifocal glasses, acne on cheeks and forehead,
    oversized black hoodie, cargo pants, worn sneakers, T-pose, front view, neutral background

R3: stylized anime 3D character, 18-year-old young man, emaciated, skin and bones,
    hollow cheeks, missing teeth, cracked bifocal glasses, unkempt stubble,
    loose dirty black hoodie, T-pose, front view, neutral background

M2: stylized anime 3D character, massive barbarian warrior, long reddish-brown hair,
    thick beard, fur cloak, leather straps, scars, bare muscular torso, T-pose, front view

M4: stylized anime 3D character, giant dark warrior, grey cracked stone-like skin,
    glowing red veins, glowing red eyes, curly black hair, beard, dark plate armor,
    T-pose, front view, neutral background
```

---

## Mara

Adulte, guerrière du monde de la Red Met (cf. `mara.webp`).

- **Silhouette :** grande, voluptueuse, généreuse en poitrine (110E fictive selon le GDD), hanches marquées, assurance totale.
- **Tenue :** combinaison ajustée noire et brillante, col montant, sangles et laçages, cuissardes, longue jupe ou cape fendue rouge.
- **Cheveux et yeux :** très longs cheveux noirs à mèches **rouges**, yeux rouges.
- **Arme :** lame à énergie rouge (un sabre lumineux réinterprété en lame forgée dans la Red Met).
- **Signature visuelle :** une **lune rouge** derrière elle lors de ses apparitions clés.
- **Évolution :** son armure s'assombrit et se fissure selon la trajectoire de Nathan. Elle reste combattante dans toutes les branches, jamais un trophée.
- **Commun à toutes les tenues :** femme adulte d'environ 28 ans, visage nettement adulte, expression froide et sûre d'elle. Grande et très voluptueuse (poitrine très généreuse, taille fine, hanches larges, cuisses pleines). Très longs cheveux noirs à mèches rouges, frange, yeux rouges. Lame à énergie rouge.

### Progression des tenues

La tenue de Mara suit la corruption de Nathan : le stade se calcule à partir du **nombre de doses prises**, lu dans l'état de la partie (`docs/GDD.md`, « Visual novels ») ; la colonne des chapitres n'est qu'indicative, pour un joueur qui prend toutes ses doses. Seuils (décision de l'auteur du 2026-10-28, pour 10 niveaux et jusqu'à 10 doses) : 0 dose = M-T0, 1 ou 2 = M-T1, 3 ou 4 = M-T2, 5 ou 6 = M-T3, 7 et plus = M-T4. Chaque stade a un portrait de face (ouverture) et un portrait de dos (fin de niveau), avec plusieurs expressions ; les portraits de dos partent d'un rendu Blender du modèle 3D, affiné dans ComfyUI (Animagine XL 4.0, ControlNet pose et contours). Elle reste toujours une combattante redoutable, jamais une proie. Les scènes restent non explicites (cf. `docs/GDD.md`, lignes directrices des scènes sensibles).

| Stade | Doses (chapitres si toutes prises) | Tenue |
|---|---|---|
| **M-T0** | 0 | La référence (`mara.webp`). Combinaison noire brillante à col montant, sangles rouges croisées, corset à boucles, jupe longue fendue, cuissardes. Entièrement couverte. |
| **M-T1** | 1–2 (ch. 1–2) | Combinaison ouverte en V sur un décolleté plongeant, fentes de jupe jusqu'aux hanches. |
| **M-T2** | 3–4 (ch. 3–4) | Bustier échancré, épaules et ventre nus, jupe courte fendue, porte-jarretelles et cuissardes. |
| **M-T3** | 5–6 (ch. 5–6) | Tenue de combat minimale : haut très échancré, bas de type string sous une demi-jupe qui découvre les fesses de dos, sangles et pièces d'armure rouges. Concept de dos en plus du concept de face. |
| **M-T4** | 7 et plus (ch. 7–10) | Armure noire fissurée de veines rouges, plus agressive et provocante, mais habillée. |

Concepts : `Assets/_Game/Art/Characters/Mara/Concept/` (prompts et seeds dans `docs/PRODUCTION.md`). Prompt négatif imposé : `nsfw, nude, child, loli, young`.

```
Mara: stylized anime 3D character, adult woman warrior, tall, curvy, very long black hair
      with red streaks, red eyes, glossy black high-collar bodysuit with straps,
      thigh-high boots, long red split skirt, holding a glowing red energy blade,
      T-pose, front view, neutral background
```

---

## Le dealer de Red Met (le Chimiste)

C'est l'homme qui fournit la Red Met à Nathan, et **le moteur des choix moraux du monde réel** : chaque exigence plus monstrueuse que la précédente vient de lui.

- **Apparence :** homme noir d'origine ivoirienne, d'âge mûr, peau noire foncée et traits africains, **collier en or**, **dent en or** visible quand il sourit. Il est soigné, calme, sûr de lui, souvent assis. Il n'a jamais besoin d'élever la voix. Concept retenu : `Assets/_Game/Art/Characters/Chimiste/Concept/Chimiste_concept_v2_s10103.png` (locks courtes attachées, chemise ouverte, pantalon sombre, chaîne et montre en or).
- **Le Chimiste n'est pas l'ennemi « Dealer » du bestiaire.** Il ne se bat jamais et ne se montre pas dans les rixes. Les dealers que Nathan affronte (ennemi 02 de `docs/BESTIARY.md`, fiche dans `docs/ENEMIES.md`) sont ses **lieutenants de rue** : tenue voyante, bijoux, lunettes teintées, des gardes autour d'eux. Ce contraste fait partie du personnage : plus on monte dans le réseau, plus la violence devient calme et polie.
- **Voix :** français avec un accent **ivoirien** (ElevenLabs « RED MET - Le Dealer », voice_id `JNw5fj1CT1bsksfAG4Fs`). Il parle posément, avec une politesse presque paternelle qui rend ses demandes plus glaçantes.
- **Ce qui le rend dangereux :** il ne force jamais Nathan. Il **propose** et laisse Nathan choisir. Il présente chaque horreur comme une simple transaction (« le marché fixe les prix, pas moi »), à l'image de L'Avocat qui la présente comme une nécessité.
- **À développer pour qu'il soit un personnage et non un cliché :** un nom, une histoire, une logique propre (dettes, survie, fournisseurs plus dangereux que lui), de l'intelligence, et au moins une scène où l'on voit ce qu'il protège ou ce qu'il craint.

### L'escalade des demandes

Ch. 1 à 3 : la dose s'achète (offerte, à crédit, puis contre de l'argent). Ensuite viennent les trois demandes du GDD v0.1 (section 10). Placement validé par l'auteur (`docs/SCENARIO.md`).

| Ch. | Demande | Choix de Nathan | Conséquences |
|---|---|---|---|
| 4 | **L'humiliation imposée** : une faveur sexuelle exigée pour prouver qu'il est « prêt à tout » (scène non explicite : contrainte et perte de contrôle, jamais un spectacle érotique) | Accepter, refuser, ou tenter de rompre avec le réseau | Dignité et autonomie sacrifiées, Paranoïa +, Humanité − |
| 5 | **Rapporter un cœur pour le marché noir**, celui de Marc, son père | Refuser, prévenir Marc, déjouer le réseau, ou tuer | Irréversible : Marc disparaît de l'histoire. Cruauté ++, crise de culpabilité |
| 7 | **Livrer un enfant au réseau** | Refuser, mettre l'enfant à l'abri, saboter la transaction, ou livrer | Enquête, détresse de la famille, pression policière, rupture morale majeure. Aucun détail pratique |

Après chaque acceptation : scène de culpabilité, puis la dose. Après chaque refus : moins d'argent, pas de dose, et le dealer reste disponible… avec la demande suivante.

**Le prix de son corps** n'est pas une demande du Chimiste mais un dilemme du réel (source, section 2) : un adulte propose de l'argent à Nathan en échange d'un rapport sexuel. Nathan peut refuser, accepter, négocier ou demander une autre forme de travail. La scène est non explicite et traite la vulnérabilité et le marchandage sous dépendance. Elle modifie l'image que Nathan a de lui-même et ses souvenirs troubles de la nuit (Paranoïa +, Humanité −). Placement : ch. 6 (validé).

```
Tripo : stylized anime 3D character, mature Black African man, calm confident expression,
        gold chain necklace, gold tooth, well-groomed short beard, dark open shirt,
        rings, T-pose, front view, neutral background
```

---

## L'Avocat

Il n'a pas de corps fixe. Il se manifeste par :
- le **reflet** de Nathan qui ne suit plus ses gestes (cf. `niveau-miroir-ch6.webp`) ;
- des **graffitis** qui apparaissent sur les murs et les miroirs ;
- au chapitre 8, des menus qui se déforment ;
- une voix ElevenLabs : la proposition 1 (generated_voice_id `S5eI0SqRnF4fZukvJLif`).

---

## Niveaux

| Lieu | Monde | Chapitres | Ambiance | Gameplay |
|---|---|---|---|---|
| **Salle de bains de Nathan** | Réel | 1, 5, 6, 10 | Carrelage bleu-gris, néon blafard, miroir couvert de graffitis (`niveau-miroir-ch6.webp`) | Lieu des scènes de culpabilité et du miroir ; pertes de contrôle |
| **Rues et arrière-cours** | Réel | 1–8 | Bitume mouillé, néons, conteneurs | Fouille des poubelles, rencontres, dilemmes |
| **Planque du Chimiste** | Réel | 1–7 | Rouge sang, fumée, laboratoire de fortune | Achat des doses, missions |
| **Appartement de Marc** | Réel | 4–5 | Chaud au début, de plus en plus froid | Choix irréversibles |
| **Arène de la lune rouge** | Red Met | 1, 9 | Plaine de cendres, lune rouge géante | Boss, combats contre Mara |
| **Ville du grain rouge** | Red Met | 2 | Ville médiévale pestiférée, greniers contaminés | Quarantaine, évacuation ou purge |
| **Forteresse et trône de cendres** | Red Met | 3–5 | Pierre noire, braseros | Siège, régicide |
| **Ville réelle envahie** | Fusion | 8–10 | Rues du réel fissurées de veines rouges | Distinguer le réel de l'hallucination |

**Règle de palette :** le réel est **désaturé, bleu-gris** ; le monde de la Red Met est **noir et rouge**. La fusion des chapitres 8 à 10 mélange les deux palettes : des veines rouges apparaissent dans le décor bleu-gris.
