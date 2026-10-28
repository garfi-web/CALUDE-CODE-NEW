# RED MET — Intégration du module Ink de Mara

## Fichier
`RED_MET_Mara_V4.ink`

## Points d'entrée
Le moteur peut appeler individuellement les nœuds suivants via `ChoosePathString("nom_du_knot")` :

- `mara_incursion_01_open` / `mara_incursion_01_close`
- `mara_incursion_02_open` / `mara_incursion_02_close`
- `mara_incursion_03_open` / `mara_incursion_03_close`
- `mara_incursion_04_open` / `mara_incursion_04_close`
- `mara_incursion_05_open` / `mara_incursion_05_close`
- `mara_incursion_06_open` / `mara_incursion_06_close`

Chaque nœud se termine par `-> DONE`, afin de rendre le contrôle au jeu sans terminer définitivement l'histoire.

## Variables et contrat de données

| Variable | Type | Valeurs recommandées | Usage |
|---|---|---|---|
| `redmet_doses` | entier | 0 ou plus | Distingue le parcours sans consommation du parcours ayant déjà pris de la Red Met. |
| `redmet_taken_this_level` | booléen | `true` / `false` | Détermine la réaction de Mara à la fin du niveau courant. |
| `nathan_stage` | entier | 0 à 4 | Progression physique de Nathan : T0, T1, T2, M3, M4. |
| `corruption_score` | entier | 0 à 100 | Ajuste certaines répliques de jugement moral. Seuil indicatif dans ce fichier : 3. |
| `immoral_streak` | entier | 0 ou plus | Historique de choix immoraux consécutifs, disponible pour les extensions du script. |
| `mara_stage` | entier | 0 à 4 | Stade de portrait/tenue M-T0 à M-T4. Calculé par la logique du jeu. |
| `father_alive` | booléen | `true` / `false` | Conséquence narrative de la scène du cœur ; passer à `false` si le père est tué. |
| `child_mission_choice` | entier | 0 ou 1 | `0` = refus/empêchement de la transaction ; `1` = acceptation/participation. |

Les variables sont déclarées dans le fichier avec des valeurs par défaut pour faciliter le prototypage. Le jeu doit les synchroniser avec sa sauvegarde avant d'appeler les nœuds concernés.

### Ordre d'exécution conseillé

1. Au début du niveau, actualiser `redmet_doses`, `nathan_stage`, `corruption_score`, `immoral_streak` et `mara_stage`.
2. Appeler `mara_incursion_XX_open`.
3. Exécuter le niveau et enregistrer les choix.
4. Mettre à jour les variables, y compris `redmet_taken_this_level`, avant la scène de fin.
5. Appeler `mara_incursion_XX_close`.
6. Après la clôture, remettre `redmet_taken_this_level = false` avant l'incursion suivante.

`redmet_taken_this_level` doit indiquer si Nathan a pris une dose associée au niveau courant. Si la prise se produit après le dialogue d'ouverture, laisser cette variable à `false` jusqu'au choix réel. La scène de fermeture doit être appelée après l'actualisation de la valeur.

## Stades visuels de Mara

`mara_stage` est un index visuel ; les vêtements ne sont pas décrits ni codés en dur dans les dialogues.

Variables narratives supplémentaires : `father_alive` (booléen, passer à `false` après la scène du cœur) et `child_mission_choice` (0 = refus/empêchement de la mission, 1 = acceptation/participation). Elles empêchent le script de confondre automatiquement la prise d’une dose avec un choix immoral particulier.

- `0` = M-T0
- `1` = M-T1
- `2` = M-T2
- `3` = M-T3
- `4` = M-T4

Le moteur sélectionne les portraits de face/dos, les expressions et la tenue à partir de cette variable. Les répliques utilisent `mara_stage` uniquement pour quelques intensifications de ton. Le stade visuel ne doit pas, à lui seul, être traité comme une preuve de consentement ou d'attirance.

## Notes d'implémentation

- Les préfixes `MARA:` et `NATHAN:` sont inclus dans le texte pour simplifier le prototypage. Pour une interface de visual novel, le moteur peut les extraire et placer le nom dans un champ de locuteur.
- Les descriptions précédées de `// VISUEL` sont des commentaires de production et ne doivent pas apparaître au joueur.
- Le fichier est un module de contenu : les scènes de Nathan avec l'Avocat, le Dealer et le Chimiste sont conservées dans les scripts narratifs principaux, pas reproduites ici.
- La fin commune (effondrement, hôpital, coma et révélation RED MET 2) doit être déclenchée par le jeu après `mara_incursion_06_close`.
- Les seuils du score moral sont provisoires et doivent être alignés sur le barème définitif du game design. La scène du père se branche sur `father_alive`, et celle du parc des sourires sur `child_mission_choice` : ces conséquences ne sont pas déduites uniquement de la consommation de Red Met.
