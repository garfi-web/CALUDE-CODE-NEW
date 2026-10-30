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
    """Cache la tête et les mains MPFB (remplacées par les dessins 2D). Le cou 3D reste (même peau que la tête A) ; le
    bout de poignet est caché dès qu'il dépend un peu de la main (il dépassait de la manche)."""
    body = bpy.data.objects[f"{character}_v3_Corps"]
    hand = {g.index for g in body.vertex_groups if g.name.split("_")[0] in ("hand", "thumb", "index", "middle", "ring", "pinky")}
    head = {g.index for g in body.vertex_groups if g.name == "head"}
    group = body.vertex_groups.get("Cache_2D") or body.vertex_groups.new(name="Cache_2D")
    group.remove(range(len(body.data.vertices)))
    hidden = {v.index for v in body.data.vertices
              if sum(x.weight for x in v.groups if x.group in hand) > 0.05 or sum(x.weight for x in v.groups if x.group in head) > 0.5}
    # Moitié basse de l'avant-bras (au repos) : sous la manche il ne se voit pas, mais il dépassait du revers
    # dans les poses où la manche s'ouvre vers la caméra.
    rig = bpy.data.objects[f"{character}_v3_Rig"]
    # Positions au repos avec les cibles de morphologie, sans aucun modificateur (mêmes indices que le maillage).
    states = {m.name: m.show_viewport for m in body.modifiers}

    for m in body.modifiers:
        m.show_viewport = False

    dg = bpy.context.evaluated_depsgraph_get()
    rest_mesh = bpy.data.meshes.new_from_object(body.evaluated_get(dg), depsgraph=dg)

    for m in body.modifiers:
        m.show_viewport = states[m.name]

    mw = body.matrix_world

    for side in "lr":
        bone = rig.data.bones[f"lowerarm_{side}"]
        a_, b_ = rig.matrix_world @ bone.head_local, rig.matrix_world @ bone.tail_local
        arm = {g.index for g in body.vertex_groups if g.name in (f"lowerarm_{side}", f"hand_{side}")}

        for v, rv in zip(body.data.vertices, rest_mesh.vertices):
            if sum(x.weight for x in v.groups if x.group in arm) > 0.3:
                p = mw @ rv.co
                t = (p - a_).dot(b_ - a_) / (b_ - a_).length_squared
                if t > 0.4:
                    hidden.add(v.index)

    bpy.data.meshes.remove(rest_mesh)
    hidden = sorted(hidden)
    group.add(hidden, 1.0, 'REPLACE')
    mask = body.modifiers.get("Sans main") or body.modifiers.new("Sans main", 'MASK')
    mask.vertex_group = group.name
    mask.invert_vertex_group = True
    mask.show_viewport = mask.show_render = True

    for side in "lr":
        fist = bpy.data.objects.get(f"{character}_v3_Poing_{side}")

        if fist is not None:
            fist.hide_render = True
            fist.hide_set(True)


# Volumes « trous » (holdout) aux places de la tête et des mains 2D : rendus invisibles, ils percent l'image du corps
# là où le 3D est derrière eux ; ce qui est devant (col, capuche avant, revers de manche, autre bras) reste dessiné.
HEAD_PROXY_RADIUS = 0.115
NECK_PROXY_RADIUS = 0.05
HAND_PROXY = (0.15, 0.045)   # longueur depuis le poignet, rayon


def _capsule(name, a, b, radius, coll):
    import bmesh

    old = bpy.data.objects.get(name)

    if old is not None:
        bpy.data.objects.remove(old)

    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    axis = b - a
    steps = max(1, int(axis.length / (radius * 0.5)))

    for i in range(steps + 1):   # chapelet de sphères : une capsule sans calcul d'orientation
        c = a.lerp(b, i / steps)
        bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=10, radius=radius,
                                  matrix=__import__("mathutils").Matrix.Translation(c))

    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    coll.objects.link(obj)
    obj.is_holdout = True
    return obj


def proxies(character, name):
    rig = bpy.data.objects[f"{character}_v3_Rig"]
    coll = bpy.data.collections[f"{character}_v3"]
    mw = rig.matrix_world
    P = rig.pose.bones
    made = []
    neck, head_a, head_b = mw @ P["neck_01"].head, mw @ P["head"].head, mw @ P["head"].tail
    # Tout le cou 3D passe sous la tête dessinée ; le col et l'avant de la capuche (à 10 cm de l'axe du cou) restent devant.
    centre = head_a.lerp(head_b, 0.55)
    made.append(_capsule(f"{character}_Trou_tete", centre, centre, HEAD_PROXY_RADIUS, coll))
    made.append(_capsule(f"{character}_Trou_cou", neck - (head_a - neck) * 0.3, centre, NECK_PROXY_RADIUS, coll))

    for side in "lr":
        if HANDS[name][side] is None:
            if f"{character}_Trou_main_{side}" in bpy.data.objects:
                bpy.data.objects.remove(bpy.data.objects[f"{character}_Trou_main_{side}"])
            continue

        wrist, elbow = mw @ P[f"hand_{side}"].head, mw @ P[f"lowerarm_{side}"].head
        d = (wrist - elbow).normalized()
        # Le volume commence un rayon après le poignet : il ne mord pas le revers de la manche.
        made.append(_capsule(f"{character}_Trou_main_{side}", wrist + d * (HAND_PROXY[1] + 0.005), wrist + d * HAND_PROXY[0],
                             HAND_PROXY[1], coll))

    return made


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
    """Par pose, deux images 3/4 du jeu du corps sans tête ni mains : « dessous » (complète, posée sous la tête et
    les mains 2D) et « dessus » (percée par les volumes de la tête et des mains, posée par-dessus) ; plus les positions
    de la base du cou, de la tête et des poignets dans l'image."""
    import os

    scene = bpy.context.scene
    rig = bpy.data.objects[f"{character}_v3_Rig"]
    feet = bpy.data.objects[f"{character}_v3_Corps"].matrix_world.translation
    hide_head_and_hands(character)
    data = {"size": size, "ortho": SPRITE_ORTHO, "poses": {}}
    shot = ("34", 18.0, 30.0, Vector((feet.x, feet.y, SPRITE_CENTER_Z)), SPRITE_ORTHO)

    for name in poses:
        set_pose(character, name)
        holes = proxies(character, name)
        layers = {}

        for layer, show in (("dessous", False), ("dessus", True)):
            for o in holes:
                o.hide_render = not show

            path = still(character, out_dir, size, shots=[shot])[0]
            layers[layer] = path.replace("_34.png", f"_34_{name}_{layer}.png")
            os.replace(path, layers[layer])

        for o in holes:
            o.hide_render = True
            o.hide_set(True)

        cam = bpy.data.objects["RM_Still"]
        # Projection avec le cadre carré du rendu (still() a remis la résolution de la scène).
        res = (scene.render.resolution_x, scene.render.resolution_y)
        scene.render.resolution_x = scene.render.resolution_y = size
        bpy.context.view_layer.update()
        mw = rig.matrix_world
        P = rig.pose.bones
        px = lambda p: _pixel(scene, cam, p, size)
        neck_a, neck_b = mw @ P["neck_01"].head, mw @ P["neck_01"].tail
        entry = {"under": layers["dessous"], "over": layers["dessus"],
                 "neck_base": px(neck_a),
                 "neck_dir": [b - a for a, b in zip(px(neck_a), px(neck_b))],
                 "chin": px(mw @ P["head"].head),
                 "feet": px(feet),
                 "hands": {}}

        for side in "lr":
            wrist = mw @ P[f"hand_{side}"].head
            elbow = mw @ P[f"lowerarm_{side}"].head
            entry["hands"][side] = {"pose": HANDS[name][side], "wrist": px(wrist), "elbow": px(elbow),
                                    "depth": (cam.matrix_world.inverted() @ wrist).z}

        scene.render.resolution_x, scene.render.resolution_y = res
        data["poses"][name] = entry

    clear_ik(character)

    with open(f"{out_dir}/{character}_poses.json", "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)

    return data
