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


# ------------------------------------------------------------------ Squelette et poings

def add_rig(character):
    """Squelette « game_engine » de MPFB (doigts compris), réaligné sur le corps : MPFB le crée environ 0,8 m trop bas
    quand le corps a été posé au sol ; on le remonte de la médiane des écarts os / sommets qu'ils déforment."""
    import numpy as np
    from mathutils import Vector

    body = bpy.data.objects[f"{character}_v3_Corps"]
    rig = HumanService.add_builtin_rig(body, "game_engine", import_weights=True)
    rig.name = f"{character}_v3_Rig"
    acc = {}

    for v in body.data.vertices:
        p = body.matrix_world @ v.co
        for g in v.groups:
            name = body.vertex_groups[g.group].name
            if g.weight > 0.6 and name in rig.pose.bones:
                acc.setdefault(name, []).append(p)

    diffs = []
    for name, pts in acc.items():
        if len(pts) > 20:
            pb = rig.pose.bones[name]
            diffs.append(tuple(sum(pts, Vector()) / len(pts) - rig.matrix_world @ pb.head.lerp(pb.tail, 0.5)))

    offset = float(np.median(np.array(diffs), axis=0)[2])
    world = body.matrix_world.copy()
    body.parent = None
    body.matrix_world = world
    rig.location.z += offset

    # Lissage correctif après le squelette : moins de pincements aux jointures (poings).
    smooth = body.modifiers.new("CorrectiveSmooth", 'CORRECTIVE_SMOOTH')
    smooth.iterations = 25
    smooth.smooth_type = 'LENGTH_WEIGHTED'
    smooth.rest_source = 'ORCO'
    with bpy.context.temp_override(object=body, active_object=body):
        bpy.ops.object.modifier_move_to_index(modifier=smooth.name, index=1)

    for pb in rig.pose.bones:
        pb.rotation_mode = 'XYZ'

    # Contour aminci sur les mains (35 %) : un contour épais entre les doigts repliés fait des taches sombres.
    group = body.vertex_groups.get("Outline_Poids") or body.vertex_groups.new(name="Outline_Poids")
    hand = {g.index for g in body.vertex_groups if g.name.split("_")[0] in ("hand", "thumb", "index", "middle", "ring", "pinky")}

    for v in body.data.vertices:
        group.add([v.index], 0.0 if sum(x.weight for x in v.groups if x.group in hand) > 0.5 else 1.0, 'REPLACE')

    line = body.modifiers.get("Outline")

    if line is not None:
        line.vertex_group = group.name
        line.thickness_vertex_group = 0.35

    return rig


def fist(rig, side):
    """Poing fermé (essais du 2026-10-30) : phalanges pliées autour de leur axe Z, léger écart des doigts, pouce replié
    sur l'index et le majeur."""
    import math

    for finger, spread in (("index", -4), ("middle", 0), ("ring", 3), ("pinky", 6)):
        for i, angle in zip((1, 2, 3), (90, 100, 70)):   # serrés contre la paume, sans trou
            rig.pose.bones[f"{finger}_0{i}_{side}"].rotation_euler = (0, math.radians(spread) if i == 1 else 0, math.radians(angle))

    for i, xyz in zip((1, 2, 3), ((10, 20, -20), (20, 0, -45), (10, 0, -45))):
        rig.pose.bones[f"thumb_0{i}_{side}"].rotation_euler = tuple(math.radians(a) for a in xyz)


def stance(rig):
    """Pose de l'image fixe : bras un peu baissés le long du corps, avant-bras tournés pour montrer les poings."""
    import math

    for side, k in (("l", 1), ("r", -1)):
        rig.pose.bones[f"upperarm_{side}"].rotation_euler = (0, 0, math.radians(-22 * k))
        rig.pose.bones[f"lowerarm_{side}"].rotation_euler = (0, math.radians(-50 * k), 0)
        fist(rig, side)
