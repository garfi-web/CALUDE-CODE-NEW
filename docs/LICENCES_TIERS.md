# RED MET — Ressources de tiers et licences

Recensement des ressources externes utilisées pour fabriquer le jeu, avec leur licence vérifiée et la date de
vérification. Règle (décision de l'auteur, 2026-10-30) : seules des ressources CC0 ou compatibles avec un usage
commercial, licence montrée à l'auteur avant tout téléchargement.

## Personnages 3D (refonte « Dragon Ball FighterZ », décidée le 2026-10-30)

### MPFB 2 (MakeHuman Plugin For Blender) — corps de base de tous les personnages humains

| Élément | Valeur |
|---|---|
| Version | 2.0.18 (extension Blender, publiée le 2026-10-10) |
| Source | https://extensions.blender.org/add-ons/mpfb/ |
| Paquet | `add-on-mpfb-v2.0.18.zip`, SHA-256 `c075b3e9b94b8e5556a5f03f4653602d02ea306de08126e8f55ed6dee28a2dad` (copie hors dépôt : `C:\RED MET\ai\downloads\mpfb`) |
| Licence du code | GPL v3 ou ultérieure (manifeste du paquet : `SPDX:GPL-3.0-or-later`). Le code n'est qu'un outil dans Blender : il n'est ni modifié ni livré avec le jeu. |
| Licence des ressources de base | **CC0** : « All core assets (the base mesh, targets, skins…) are shared under CC0 » (FAQ officielle, https://static.makehumancommunity.org/mpfb/faq/can_i_sell_models.html) ; « You do not need to pay anything, include any copyright notices nor give attribution » (https://static.makehumancommunity.org/about/license.html). |
| Usage commercial | Oui, y compris dans un jeu à code fermé (https://static.makehumancommunity.org/mpfb/faq/use_in_closed_source.html). |
| Restriction appliquée | **Seules les ressources de base fournies dans le paquet** (maillage de base `data/3dobjs/base.obj`, cibles de morphologie `data/targets/`, squelettes, peaux) sont utilisées. Aucune ressource communautaire ou de tiers (vêtements, cheveux, peaux téléchargés à part), qui ont leur propre licence. Les vêtements sont modélisés pour RED MET. |
| Vérifié le | 2026-10-30 |

### Human Base Meshes (Blender Studio) — tête stylisée, seulement si la tête de MPFB est trop réaliste

| Élément | Valeur |
|---|---|
| Version | 1.4.1 (49 Mo, Blender 4.2 LTS et plus) |
| Source | https://www.blender.org/download/demo-files/ (section « Asset Bundles ») |
| Licence | **CC0** (indiquée sur la page officielle de Blender) |
| Statut | **Téléchargé le 2026-10-30** pour la méthode A des poings (main stylisée « Hand - Stylized »), depuis le miroir officiel `https://mirror.blender.org/demo/asset-bundles/human-base-meshes/human-base-meshes-bundle-v1.4.1.zip` (50 643 039 octets, SHA-256 `811f43accbb31a88266d932f8f5563b2d13586fca0ba2693aad1f5fe582b3515`), copie hors dépôt : `C:\RED MET\ai\downloads\human_base_meshes`. |
| Vérifié le | 2026-10-30 |

## Autres outils et modèles déjà recensés ailleurs

Animagine XL 4.0, ComfyUI, TripoSG, ControlNet Union SDXL, BiRefNet, police M PLUS Rounded 1c : voir `docs/PRODUCTION.md`
(décisions et licences, phase 0 et refonte du visual novel).

## Planche de mains de l'auteur (utilisée dans le jeu)

| Élément | Valeur |
|---|---|
| Fichier | `docs/references/planche_mains_redmet/planche_mains_redmet.png` (1312 × 1199) |
| Provenance | **Générée par IA par l'auteur du projet, pour RED MET** (communiqué le 2026-10-30). |
| Licence / usage | **Utilisation autorisée par l'auteur**, y compris directement dans le jeu. |
| Usage | Rangée « DESSIN 2D » découpée (BiRefNet) : `Assets/_Game/Art/Characters/Hands/Hand_*.png`, puis adaptée par personnage (`tools/gen3d/hands2d.py`). |

## Images de référence (comparaison seulement, jamais dans le jeu)

| Image | Licence | Usage |
|---|---|---|
| « Clenched fist », Genusfotografen (Tomas Gunnarsson) / Wikimedia Sverige, https://commons.wikimedia.org/wiki/File:Clenched_fist.jpg | CC BY-SA 4.0 (attribution sur les planches) | Photo de référence à côté des poings 3D (`docs/captures/2026-10-30_poing*.png`) ; non copiée dans le dépôt. |
