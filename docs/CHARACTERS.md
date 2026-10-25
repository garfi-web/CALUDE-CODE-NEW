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
| **R0 — Le garçon invisible** | 1 | Un peu enrobé (chubby), épaules rentrées, ventre mou | Joues rondes, **acné** sur le front et les joues, **lunettes à double foyer** épaisses, cheveux bruns bouclés en bataille | Hoodie noir trop grand, cargo, baskets usées (cf. `nathan-reel-style.webp`) | Regard au sol, mains dans les poches, démarche hésitante |
| **R1 — Insomnie** | 2–3 | Visage qui s'affine, le ventre fond | Cernes, acné irritée, lunettes de travers | Même hoodie, désormais trop large | Se gratte les avant-bras, bâille |
| **R2 — Les dents** | 4–5 | Mince, épaules osseuses | Joues creuses, **dents abîmées**, lèvres gercées, peau terne | Hoodie taché, capuche relevée | Mâchoires serrées, tremblements des mains |
| **R3 — La peau sur les os** | 6–7 | **Émacié**, clavicules et poignets saillants | Dents manquantes, barbe négligée, yeux trop grands, verres fêlés | Vêtements qui pendent, pieds nus ou tongs | Trébuche, tics, mouvements involontaires (cf. `nathan-reel-hallucination.webp`) |
| **R4 — Corps brisé** | 8–10 | Squelettique, voûté | Teint gris, regard vide ou halluciné | Blouse d'hôpital (épilogue) | Marche saccadée, absences |

Ces stades sont une représentation **fictionnelle** de la dépendance, pas un jugement sur l'apparence. Le passage de « chubby » à « maigre » doit se lire comme une **perte de santé**, jamais comme une amélioration.

### Monde de la Red Met

| Stade | Dose | Silhouette | Détails | Références |
|---|---|---|---|---|
| **M0 — Le faible** | 0 | Même corps que R0, sans les lunettes | Poings nus, garde maladroite | — |
| **M1 — L'éveil** | 1 | Musclé, posture droite | Barbe naissante, cheveux qui poussent, couteau | — |
| **M2 — Le barbare** | 2–3 | **Colosse**, épaules massives | Longue chevelure brun-roux, barbe épaisse, fourrures, cicatrices | `nathan-redmet-barbare.webp`, `nathan-redmet-guerrier.webp` |
| **M3 — Le seigneur de guerre** | 4 | Armure de plaques sombres | Peinture de guerre bleue, yeux rouges, armes lourdes | `nathan-redmet-guerrier.webp` |
| **M4 — Le prédateur** | 5 | Peau grise, comme de la **pierre fissurée** | **Veines rouges incandescentes** (la Red Met visible sous la peau), yeux rouges luminescents | `nathan-redmet-final-double.webp` |
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

La tenue de Mara suit la corruption de Nathan. Elle reste toujours une combattante redoutable, jamais une proie. Les scènes restent non explicites (cf. `docs/GDD.md`, lignes directrices des scènes sensibles).

| Stade | Chapitres | Tenue |
|---|---|---|
| **M-T0** | 1–2 | La référence (`mara.webp`). Combinaison noire brillante à col montant, sangles rouges croisées, corset à boucles, jupe longue fendue, cuissardes. Entièrement couverte. |
| **M-T1** | 3–4 | Combinaison ouverte en V sur un décolleté plongeant, fentes de jupe jusqu'aux hanches. |
| **M-T2** | 5 | Bustier échancré, épaules et ventre nus, jupe courte fendue, porte-jarretelles et cuissardes. |
| **M-T3** | 6–7 | Tenue de combat minimale : haut très échancré, bas de type string sous une demi-jupe qui découvre les fesses de dos, sangles et pièces d'armure rouges. Concept de dos en plus du concept de face. |
| **M-T4** | 8–10 (voie immorale) | Armure noire fissurée de veines rouges, plus agressive et provocante, mais habillée. |

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

- **Apparence :** homme noir africain d'âge mûr, **collier en or**, **dent en or** visible quand il sourit. Il est soigné, calme, sûr de lui, souvent assis. Il n'a jamais besoin d'élever la voix.
- **Voix :** français avec un accent **ivoirien** (ElevenLabs « RED MET - Le Dealer », voice_id `JNw5fj1CT1bsksfAG4Fs`). Il parle posément, avec une politesse presque paternelle qui rend ses demandes plus glaçantes.
- **Ce qui le rend dangereux :** il ne force jamais Nathan. Il **propose** et laisse Nathan choisir. Il présente chaque horreur comme une simple transaction (« le marché fixe les prix, pas moi »), à l'image de L'Avocat qui la présente comme une nécessité.
- **À développer pour qu'il soit un personnage et non un cliché :** un nom, une histoire, une logique propre (dettes, survie, fournisseurs plus dangereux que lui), de l'intelligence, et au moins une scène où l'on voit ce qu'il protège ou ce qu'il craint.

### L'escalade des demandes

| Ch. | Demande | Choix de Nathan | Conséquences |
|---|---|---|---|
| 3–4 | **Se prostituer** pour payer sa dose (scène non explicite) | Refuser, accepter, négocier, demander un autre travail | Image de soi, souvenirs troubles de la nuit, Paranoïa +, Humanité − |
| 5 | **Rapporter un cœur pour le marché noir**, celui de Marc, son père | Refuser, prévenir Marc, déjouer le réseau, ou tuer | Irréversible : Marc disparaît de l'histoire. Cruauté ++, crise de culpabilité |
| 7 | **Livrer un enfant au réseau** | Refuser, mettre l'enfant à l'abri, saboter la transaction, ou livrer | Enquête, détresse de la famille, pression policière, rupture morale majeure. Aucun détail pratique |

Après chaque acceptation : scène de culpabilité, puis la dose. Après chaque refus : moins d'argent, pas de dose, et le dealer reste disponible… avec la demande suivante.

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
