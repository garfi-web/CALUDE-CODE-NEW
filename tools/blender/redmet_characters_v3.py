"""RED MET — personnages 3D refaits (option 1 de l'auteur, 2026-10-30 : rendu anime cel-shading proche de
Dragon Ball FighterZ). Exécuté par le MCP Blender, avec l'extension MPFB 2.0.18 installée (ressources de base CC0
seulement : docs/LICENCES_TIERS.md).

Un seul corps de base humain (MPFB) pour tous les humains ; chaque personnage et chaque stade de Nathan est un réglage
de ce corps (paramètres macro + quelques cibles de morphologie de base).

    exec(open(r"<dépôt>/tools/blender/redmet_characters_v3.py", encoding="utf-8").read())
    body = create_body("Nathan_M0")

Étapes, chacune validée par l'auteur avant la suivante (règle « valider avant de produire ») :
1. corps nu réglé (fait pour Nathan M0 le 2026-10-30, en attente du OK) ;
2. vêtements modélisés, plis peints dans la texture, vraies mains (poings fermés) ;
3. tête 3D, visage en texture anime d'après la tête A (s9104), une texture par expression ;
4. image fixe de Nathan M0 (face, 3/4, en jeu à côté d'un Voyou), puis seulement l'animation.
"""
import os

import bpy

from bl_ext.user_default.mpfb.entities.objectproperties import HumanObjectProperties
from bl_ext.user_default.mpfb.services.humanservice import HumanService
from bl_ext.user_default.mpfb.services.locationservice import LocationService
from bl_ext.user_default.mpfb.services.targetservice import TargetService

# Réglages par personnage : paramètres macro de MPFB (0 à 1) et cibles de base (fichier relatif à data/targets, poids).
# Âge MPFB : 0,1875 = 11 ans, 0,5 = 25 ans (19 ans ≈ 0,366). Taille réglée pour la hauteur de docs/CHARACTERS.md.
BODIES = {
    # Nathan R0/M0 : 19 ans, 1,70 m, adulte un peu enrobé (ventre rond, peu musclé).
    "Nathan_M0": {
        "macro": {"gender": 1.0, "age": 0.366, "weight": 1.0, "muscle": 0.25, "height": 0.59, "proportions": 0.5},
        "targets": [("stomach/stomach-pregnant-incr.target.gz", 0.35)],
        "x": 50.0,
    },
}


def create_body(character):
    """Crée (ou recrée) le corps nu du personnage dans sa collection <Perso>_v3, à côté des anciens modèles."""
    cfg = BODIES[character]
    name = f"{character}_v3"
    coll = bpy.data.collections.get(name) or bpy.data.collections.new(name)

    if coll.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(coll)

    bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children[coll.name]
    old = bpy.data.objects.get(f"{name}_Corps")

    if old is not None:
        bpy.data.objects.remove(old)

    macro = TargetService.get_default_macro_info_dict()
    macro.update(cfg["macro"])
    body = HumanService.create_human(mask_helpers=True, detailed_helpers=False, extra_vertex_groups=True,
                                     feet_on_ground=True, scale=0.1, macro_detail_dict=macro)
    body.name = f"{name}_Corps"
    body.location.x = cfg["x"]
    folder = LocationService.get_mpfb_data("targets")

    for path, weight in cfg["targets"]:
        TargetService.load_target(body, os.path.join(folder, path), weight=weight,
                                  name=os.path.basename(path).replace(".target.gz", ""))

    for key, value in cfg["macro"].items():
        HumanObjectProperties.set_value(key, value, entity_reference=body)

    bpy.context.view_layer.update()
    return body
