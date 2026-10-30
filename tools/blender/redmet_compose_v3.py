"""RED MET — Nathan M0 complet en image fixe : corps 3D (vêtements) + tête A et mains illustrées 2D posées dessus
(demande de l'auteur, 2026-10-30). Ce module pose le corps (repos mains dans la poche kangourou, garde, coup tendu),
cache la tête et les mains MPFB, rend la vue 3/4 du jeu et écrit, pour chaque pose, où tombent dans l'image la tête,
les poignets et les avant-bras (JSON lu par tools/gen3d/compose_still.py, qui pose la tête et les mains 2D).

    exec(open(r"<dépôt>/tools/blender/redmet_characters_v3.py", encoding="utf-8").read())
    exec(open(r"<dépôt>/tools/blender/redmet_clothes_v3.py", encoding="utf-8").read())
    exec(open(r"<dépôt>/tools/blender/redmet_compose_v3.py", encoding="utf-8").read())
    render_poses("Nathan_M0", out_dir)
"""
import json
import math

import bpy
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Vector

# Cibles des poignets (repère du personnage : x vers sa gauche, -y devant lui, z vers le haut, en mètres) et pôles des
# coudes. Nathan regarde vers -Y.
POSES = {
    # Repos : mains enfoncées dans la poche kangourou, coudes le long du corps.
    "repos": {"l": ((0.06, -0.17, 0.98), (0.35, 0.3, 1.0)), "r": ((-0.06, -0.17, 0.98), (-0.35, 0.3, 1.0))},
    # Garde : poings serrés devant le menton, coudes rentrés.
    "garde": {"l": ((0.1, -0.33, 1.36), (0.3, 0.0, 0.9)), "r": ((-0.12, -0.3, 1.33), (-0.3, 0.0, 0.9))},
    # Coup tendu du bras droit (côté caméra en 3/4), le gauche reste en garde.
    "coup": {"l": ((0.1, -0.31, 1.36), (0.3, 0.0, 0.9)), "r": ((-0.15, -0.66, 1.36), (-0.4, 0.1, 1.0))},
}
# Main 2D de chaque poignet par pose (lu par le montage).
HANDS = {"repos": {"l": None, "r": None},
         "garde": {"l": "fist_back", "r": "fist_back"},
         "coup": {"l": "fist_back", "r": "fist_side"}}


def hide_head_and_hands(character):
    """Cache la tête, le cou et les mains MPFB (remplacés par les dessins 2D : la tête A a son propre cou, glissé
    sous le col du sweat)."""
    body = bpy.data.objects[f"{character}_v3_Corps"]
    names = ("hand", "thumb", "index", "middle", "ring", "pinky", "head", "neck")
    ids = {g.index for g in body.vertex_groups if g.name.split("_")[0] in names}
    group = body.vertex_groups.get("Cache_2D") or body.vertex_groups.new(name="Cache_2D")
    group.remove(range(len(body.data.vertices)))
    group.add([v.index for v in body.data.vertices if sum(x.weight for x in v.groups if x.group in ids) > 0.5], 1.0, 'REPLACE')
    mask = body.modifiers.get("Sans main") or body.modifiers.new("Sans main", 'MASK')
    mask.vertex_group = group.name
    mask.invert_vertex_group = True
    mask.show_viewport = mask.show_render = True

    for side in "lr":
        fist = bpy.data.objects.get(f"{character}_v3_Poing_{side}")

        if fist is not None:
            fist.hide_render = True
            fist.hide_set(True)


def set_pose(character, name):
    """Bras placés par cinématique inverse (contrainte IK sur l'avant-bras, 2 os) vers les cibles de la pose."""
    rig = bpy.data.objects[f"{character}_v3_Rig"]
    body = bpy.data.objects[f"{character}_v3_Corps"]
    origin = body.matrix_world.translation
    coll = bpy.data.collections[f"{character}_v3"]

    for side in "lr":
        target, pole = POSES[name][side]
        empties = []

        for label, co in (("cible", target), ("pole", pole)):
            ename = f"{character}_IK_{label}_{side}"
            e = bpy.data.objects.get(ename) or bpy.data.objects.new(ename, None)

            if e.name not in coll.objects:
                coll.objects.link(e)

            e.location = origin + Vector(co)
            empties.append(e)

        pb = rig.pose.bones[f"lowerarm_{side}"]
        ik = pb.constraints.get("IK") or pb.constraints.new('IK')
        ik.target = empties[0]
        ik.pole_target = empties[1]
        ik.pole_angle = math.radians(-90)
        ik.chain_count = 2
        rig.pose.bones[f"upperarm_{side}"].rotation_euler = (0, 0, 0)
        pb.rotation_euler = (0, 0, 0)

    bpy.context.view_layer.update()


def clear_ik(character):
    rig = bpy.data.objects[f"{character}_v3_Rig"]

    for side in "lr":
        pb = rig.pose.bones[f"lowerarm_{side}"]

        for c in list(pb.constraints):
            pb.constraints.remove(c)


def _pixel(scene, cam, p, size):
    v = world_to_camera_view(scene, cam, p)
    return [v.x * size, (1 - v.y) * size]


# Cadrage des sprites du jeu (redmet_quality.py) : 1024 px pour 2,6 m, visée à 1 m au-dessus des pieds, 3/4 surélevé
# de 18° et tourné de 30° (personnage tourné vers la droite) ; pieds à 0,349 m du bas de l'image.
SPRITE_SIZE, SPRITE_ORTHO, SPRITE_CENTER_Z = 1024, 2.6, 1.0


def render_poses(character, out_dir, size=SPRITE_SIZE, poses=("repos", "garde", "coup")):
    """Une image 3/4 du jeu par pose (corps sans tête ni mains) + positions de la tête et des poignets."""
    scene = bpy.context.scene
    rig = bpy.data.objects[f"{character}_v3_Rig"]
    feet = bpy.data.objects[f"{character}_v3_Corps"].matrix_world.translation
    hide_head_and_hands(character)
    data = {"size": size, "ortho": SPRITE_ORTHO, "poses": {}}

    for name in poses:
        set_pose(character, name)
        shot = ("34", 18.0, 30.0, Vector((feet.x, feet.y, SPRITE_CENTER_Z)), SPRITE_ORTHO)
        paths = [None] + still(character, out_dir, size, shots=[shot])   # la caméra RM_Still reste sur ce cadrage
        cam = bpy.data.objects["RM_Still"]
        # Projection avec le cadre carré du rendu (still() a remis la résolution de la scène).
        res = (scene.render.resolution_x, scene.render.resolution_y)
        scene.render.resolution_x = scene.render.resolution_y = size
        bpy.context.view_layer.update()
        mw = rig.matrix_world
        P = rig.pose.bones
        head_a, head_b = mw @ P["head"].head, mw @ P["head"].tail
        entry = {"image": paths[1].replace("_34.png", f"_34_{name}.png"),
                 "head_center": _pixel(scene, cam, (head_a + head_b) / 2, size),
                 "head_dir": [b - a for a, b in zip(_pixel(scene, cam, head_a, size), _pixel(scene, cam, head_b, size))],
                 "feet": _pixel(scene, cam, bpy.data.objects[f"{character}_v3_Corps"].matrix_world.translation, size),
                 "hands": {}}

        for side in "lr":
            wrist = mw @ P[f"hand_{side}"].head
            elbow = mw @ P[f"lowerarm_{side}"].head
            entry["hands"][side] = {"pose": HANDS[name][side], "wrist": _pixel(scene, cam, wrist, size),
                                    "elbow": _pixel(scene, cam, elbow, size),
                                    "depth": (cam.matrix_world.inverted() @ wrist).z}

        scene.render.resolution_x, scene.render.resolution_y = res
        import os
        os.replace(paths[1], entry["image"])
        data["poses"][name] = entry

    clear_ik(character)

    with open(f"{out_dir}/{character}_poses.json", "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)

    return data
