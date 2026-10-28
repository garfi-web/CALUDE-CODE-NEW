# Module Mara V4 : analyse et intégration (validées par l'auteur le 2026-10-28, intégré)

Fichiers lus en entier : `RED_MET_Mara_V4_bundle/RED_MET_Mara_V4.ink` (12 nœuds, 590 lignes) et `RED_MET_Mara_V4_Integration.md`. Aucune réplique n'a été modifiée. L'intégration (étape 4) n'a pas commencé.

## 1. Répartition des 12 scènes sur les 10 chapitres

Deux scènes ont leur place imposée par l'intrigue : `03_close` dépend du meurtre du père (ch. 5), et `04_close` de la mission de l'enfant (ch. 7). Les autres suivent la progression du ton.

| Ch. | Ouverture (`chNN_mara_ouverture`) | Fin (`chNN_mara_fin`) | Raison |
|---|---|---|---|
| 1 | `mara_incursion_01_open` | `mara_incursion_01_close` | Première rencontre, Nathan encore faible |
| 2 | `mara_incursion_02_open` | `mara_incursion_02_close` | « Tu es revenu », Mara commence à le trouver séduisant |
| 3 | `mara_incursion_03_open` | **manque** | « Tu as l'air épuisé » : Nathan perd du poids et ne dort plus au ch. 3 |
| 4 | **manque** | **manque** | |
| 5 | **manque** | `mara_incursion_03_close` | Branche `father_alive` : le cœur de Marc |
| 6 | **manque** | **manque** | |
| 7 | `mara_incursion_04_open` ⚠ | `mara_incursion_04_close` ⚠ | Parc des sourires, branche `child_mission_choice` (répliques à revoir, section 3) |
| 8 | `mara_incursion_05_open` | `mara_incursion_05_close` | « Je commence à croire que tu vas vraiment aller jusqu'au bout » |
| 9 | **manque** | `mara_incursion_06_close` | Bilan avant la fin (« regarde ce que ça t'a coûté ») ; il ne peut pas servir de fin du jeu (section 3) |
| 10 | `mara_incursion_06_open` | **manquent** : `ch10_mara_fin_immorale` (11.1) et `ch10_mara_fin_morale` (11.2) | « On est arrivés au dernier chapitre » |

**Scènes encore sans dialogue** (8 emplacements, plus les 2 fins) :
- `ch03_mara_fin`
- `ch04_mara_ouverture`, `ch04_mara_fin`
- `ch05_mara_ouverture`
- `ch06_mara_ouverture`, `ch06_mara_fin`
- `ch09_mara_ouverture`
- `ch10_mara_fin_immorale`, `ch10_mara_fin_morale`

Variante possible : le décor des « couloirs de verre » (miroirs, reflets) irait aussi au ch. 6 (« La voix derrière le miroir »), mais son ton (Mara qui découvre qu'il devient séduisant) appartient au début du jeu. Je le laisse au ch. 2.

**Noms des nœuds** : je propose de renommer les nœuds selon nos identifiants (`mara_incursion_01_open` → `ch01_mara_ouverture`, `mara_incursion_03_close` → `ch05_mara_fin`…). Le numéro d'incursion du module ne correspond plus au chapitre, et les identifiants de voix (`ch05_mara_fin_001`…) en découlent. Seuls les noms de nœuds changent, pas les répliques.

**Locuteurs** : les préfixes `MARA:` et `NATHAN:` restent dans le texte ; le jeu les lit pour afficher le nom (aucune réplique n'est réécrite).

## 2. Variables du module et état de la partie

Le jeu calcule tout dans `GameSession`, puis copie les valeurs dans Ink juste avant chaque nœud (une seule fonction de synchronisation). Les `VAR` du fichier ne servent que de valeurs par défaut. Ink ne recalcule aucun seuil : les stades viennent de `ProgressionTable` (0 / 1-2 / 3-4 / 5-6 / 7+ doses, `docs/CHARACTERS.md`).

| Variable Ink | Source dans le jeu | Remarque |
|---|---|---|
| `redmet_doses` | `GameSession.DosesTaken` | |
| `redmet_taken_this_level` | `State.DoseAt(chapitre) == Taken` | Chez nous, la dose se choisit **avant** l'incursion : la valeur est déjà juste à l'ouverture, ce que le module accepte |
| `nathan_stage` | `(int) GameSession.NathanStage` (M0 à M4) | Le commentaire du module dit « T0, T1, T2, M3, M4 » : c'est bien M0 à M4 |
| `mara_stage` | `(int) GameSession.MaraOutfit` (M-T0 à M-T4) | Mêmes seuils que Nathan : les deux valent toujours la même chose |
| `father_alive` | `GameSession.MarcAlive` (pas d'acte `acte_ch05_coeur_marc`) | |
| `child_mission_choice` | 1 si `acte_ch07_enfant_livre`, sinon 0 | |
| `corruption_score` | **À valider** : `max(0, Cruauté − Humanité)`, calculé dans `GameSession` à partir des axes | Le module n'utilise que le seuil 3. Exemples : voie sobre → 0 ; poubelles seules → 1 ; poubelles et collier arraché → 5 ; fin de la voie immorale → ≈ 24 |
| `immoral_streak` | Non synchronisée (reste 0) | Déclarée, jamais lue par les répliques |

**Logique propre au module, différente de la nôtre** (je ne la modifie pas sans votre accord) :
- `06_open` et `06_close` choisissent la branche « corrompue » avec `nathan_stage >= 3 and corruption_score >= 3`. Notre fin immorale dépend d'autre chose : au moins 6 doses et 2 grandes demandes (`GameSession.ImmoralEnding`). Ce n'est pas bloquant tant que `06_close` ne sert pas de fin du jeu.
- Le module ignore le **rôle de Mara** décidé au ch. 7 (le sauver, le combattre, ses ambitions). Si Mara est devenue son adversaire, les répliques de désir du ch. 8 (`05_open`, `05_close` : « J'ai envie de toi ») et du ch. 10 (`06_open` : « Oui. Je te désire ») la contredisent. Il faudrait une branche, ou une autre scène, pour la Mara adversaire ou absente.

## 3. Répliques incompatibles avec nos règles (non modifiées, à trancher)

**Règle du ch. 7 : aucun contenu suggestif dans le chapitre de l'enfant** (`docs/GDD.md`, lignes directrices ; `docs/SCENARIO.md`, ch. 7).
- `04_open`, branche puissante (lignes 352 à 359) : « Je commence à avoir des pensées très inconvenantes à ton sujet. » / NATHAN : « Tu pourrais le faire au lieu d'en parler. » / « Oh ! Il est devenu audacieux… » / « …si je vais réussir à garder mes distances… ». **Incompatible.**
- `04_close`, branche `child_mission_choice == 1` (lignes 378 à 386) : juste après la livraison de l'enfant, Mara parle de son attirance (« en espérant que je vais te trouver attirant après », « Tu veux que je puisse te désirer… », « Une partie de moi le peut… je peux être attirée par ton assurance », « tu as fini par devenir un sacré spectacle »). Le désir est mis juste à côté du crime contre l'enfant. **Incompatible.**
- `04_close`, branche du refus (lignes 400 et 402) : « …pour autre chose que tes muscles », « si tu me demandes si je te trouve attirant… ». Allusions légères, **à votre appréciation**.
- `04_open`, autre branche (ligne 362) : « …pas me faire perdre la tête ». Très léger, **à votre appréciation**.
- La pose de fin du ch. 7 n'est pas cambrée (`docs/SCENARIO.md`) : c'est un réglage de portrait, pas de texte.

**Règle de la fin : section 11.1 (voie immorale) et 11.2 (voie morale)**.
- `06_close`, branche corrompue (lignes 553 à 556) : Mara déclare son désir et l'invite (« viens ici, juste à côté de moi »). C'est l'ancienne fin consentie, que la section 11.1 a remplacée. Elle ne peut donc pas servir de `ch10_mara_fin_immorale`, d'où sa place au ch. 9 dans la répartition. **La scène 11.1 (Nathan tente d'imposer sa volonté, coupée avant l'acte, centrée sur le refus de Mara) reste à écrire.**
- `06_close`, branche sobre (lignes 561 à 577) : un discours de respect, alors que la section 11.2 veut une Mara sans aucune interaction, qui montre ses biceps 💪 et lance « Moi, je veux un vrai bonhomme… ». Elle ne peut pas servir de `ch10_mara_fin_morale` en l'état. **La scène 11.2 reste à écrire** (la réplique de l'auteur existe déjà dans la source).

**Cohérence (à votre appréciation, pas des règles)** :
- `01_open`, lignes 66 à 68 : « Attends. C'est vraiment toi ? » / NATHAN : « Tu comptes me demander ça à chaque fois ? » suppose une rencontre antérieure, alors que c'est la première (« Mara. Enchantée. »).
- Répliques en double, qui recevront chacune leur identifiant de voix :
  - `03_open` (lignes 252 à 254) et `04_open` (lignes 355 à 357) : « Tu me plais davantage… projet de rénovation… très beau chantier » ;
  - `03_open` (lignes 250-251) et `05_close` (lignes 460-461) : « Tu y penses beaucoup ? / Assez pour que tu arrêtes de sourire comme un idiot » ;
  - `01_close` (lignes 136 à 143) et `03_close` (lignes 315 à 318) : « Je suppose que tu attends que je te dise que tu es admirable… ».

**Orthographe** : aucune faute relevée à la lecture. Je revérifierai pendant la conversion et signalerai chaque correction.

## 4. Étape 4, une fois la validation donnée
- Installer l'intégration Ink pour Unity (inkle, licence MIT, avec inklecate). Elle n'est pas encore dans le projet.
- Copier le module adapté dans `Assets/_Game/Story/Ink/`, renommer les nœuds, compiler et corriger les erreurs.
- Écrire la synchronisation des variables (section 2) et un lecteur de visual novel minimal (portrait, nom, texte, choix).
- Attribuer un identifiant de voix à chaque réplique (`<scène>_NNN`, sous forme d'étiquette Ink).
- Tester en jeu une ouverture et une fin, avec captures, puis commiter et pousser.

## 5. Décisions de l'auteur et intégration (2026-10-28)
- **Validé** : la répartition, le renommage des nœuds et `corruption_score` = Cruauté − Humanité, au minimum 0 (`GameSession.CorruptionScore`).
- **Ch. 7** : retrait des lignes 352 à 359 et 378 à 386, et des allusions des lignes 362, 400 et 402. Conséquences :
  - la ligne 360 (fin du bloc `mara_stage >= 3`) et la condition de la ligne 351 sont adaptées, et non le texte : la branche puissante de `ch07_mara_ouverture` n'a plus de réplique propre ;
  - les lignes 403-404 (« Je n'allais pas te le demander. » / « Bien sûr que non. ») répondaient à la ligne 402 retirée : retirées aussi ;
  - la branche « enfant livré » de `ch07_mara_fin` s'arrête sur une amorce (« Tu sais ce qui me fait le plus peur ?… ») : une marque « À ÉCRIRE » y est ajoutée.
- **Doublons retirés** :
  - `ch07_mara_ouverture`, lignes 355 à 357, déjà comprises dans le retrait du ch. 7 ;
  - `ch08_mara_fin`, lignes 460-461 : la ligne 461 n'était pas un doublon exact, puisque la ligne 251 du ch. 3 se termine par « , oui. » ;
  - `ch05_mara_fin`, lignes 316-317. Les lignes 315 et 318, proches de `ch01_mara_fin` mais pas identiques, sont gardées.
- **Ch. 1** : lignes 66 à 69 marquées « À RÉÉCRIRE ». La ligne 69 (« Non. Je voulais juste vérifier… ») répond à la ligne 68 : elle est marquée elle aussi.
- **Ch. 9** : `06_close` devient `ch09_mara_fin`, sans changement.
- **Ch. 10** : nœuds `ch10_mara_fin_immorale` et `ch10_mara_fin_morale` créés, marqués « À ÉCRIRE ». Les 7 scènes manquantes ont aussi leur nœud « À ÉCRIRE ».
- **Mara adversaire** (`mara_role == 2`) : une branche « PROVISOIRE » en tête de `ch08_mara_ouverture`, `ch08_mara_fin`, `ch09_mara_fin` et `ch10_mara_ouverture`.
- **Vérification automatique** : toutes les répliques gardées sont identiques à la source, au caractère près. Aucune correction d'orthographe n'a été nécessaire.
- **Intégration** :
  - `Assets/_Game/Story/Ink/mara.ink`, compilé par le paquet Ink 2.0.0 d'inkle (licence MIT) : 0 erreur, 0 avertissement ;
  - 317 identifiants de voix `#voix:<scène>_NNN`, tous uniques ; les lignes marquées n'en ont pas.
