"""RED MET — animations, rendu toon et spritesheets (exécuté par le MCP Blender, après redmet_pipeline.py).

Les poses sont écrites en rotations autour des axes du MONDE, autour de la tête de chaque os,
appliquées dans l'ordre depuis la pose de repos (pose en T). Le personnage regarde vers -Y ;
la caméra des sprites est en -X, il regarde donc vers la droite de l'image.
Repères de signe (vue de côté) :
- membre orienté vers le bas (bras baissé, jambe) : rotation X négative = vers l'avant ;
- os orienté vers le haut (colonne, tête) : rotation X positive = penché en avant ;
- bras gauche (+X) baissé : Y +78 ; bras droit (-X) : Y -78. Le bras droit est côté caméra.

Les durées des attaques reprennent les AttackData de Nathan (images à 60 FPS), rendues à 30 FPS.
"""
import math
import os

import bpy
import numpy as np
from mathutils import Matrix, Vector

SPRITE_FPS = 30
AXES = {"X": Vector((1, 0, 0)), "Y": Vector((0, 1, 0)), "Z": Vector((0, 0, 1))}


# ------------------------------------------------------------------------ Poses

def _apply_ops(rig, ops):
    for pb in rig.pose.bones:
        pb.rotation_mode = 'QUATERNION'
        pb.location = (0, 0, 0)
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.scale = (1, 1, 1)

    bpy.context.view_layer.update()

    for bone, axis, value in ops:
        pb = rig.pose.bones[bone]

        if axis == "T":
            move = Matrix.Translation(Vector(value))
            pb.matrix = move @ pb.matrix
        elif axis == "S":
            # Étirement le long de l'os (Y local), hérité par les os enfants.
            pb.matrix = pb.matrix @ Matrix.Scale(value, 4, Vector((0, 1, 0)))
        else:
            pivot = pb.head.copy()
            rotation = Matrix.Rotation(math.radians(value), 4, AXES[axis])
            pb.matrix = Matrix.Translation(pivot) @ rotation @ Matrix.Translation(-pivot) @ pb.matrix

        bpy.context.view_layer.update()


def arms_down():
    return [("upper_arm.L", "Y", 78), ("upper_arm.R", "Y", -78)]


def guard(lean=6, breathe=0):
    """Garde de boxeur de rue : poings devant le visage, appuis décalés."""
    return arms_down() + [
        ("spine.001", "X", lean), ("spine.004", "X", breathe),
        ("upper_arm.L", "X", -40), ("forearm.L", "X", -115),
        ("upper_arm.R", "X", -25), ("forearm.R", "X", -120),
        ("thigh.L", "X", -12), ("shin.L", "X", 14),
        ("thigh.R", "X", 12), ("shin.R", "X", 10),
    ]


def legs_stance():
    return [("thigh.L", "X", -12), ("shin.L", "X", 14), ("thigh.R", "X", 12), ("shin.R", "X", 10)]


def legs_tuck():
    return [("thigh.L", "X", -55), ("shin.L", "X", 85), ("thigh.R", "X", -30), ("shin.R", "X", 75)]


def legs_lunge():
    """Fente : jambe gauche avant fléchie, jambe droite arrière tendue (le poids passe devant)."""
    return [("thigh.L", "X", -38), ("shin.L", "X", 34), ("thigh.R", "X", 28), ("shin.R", "X", 4)]


# Étirement des bras au sommet d'un coup (style anime), réglé par personnage dans build_actions :
# les bras de Nathan M0 sont courts sur le modèle (0,32 m de l'épaule au poignet).
PUNCH_STRETCH = 1.0


def punch(side, reach=1.0, lean=34, legs=None):
    """Bras « side » tendu vers l'avant, épaule et buste engagés ; l'autre reste en garde.
    Le bras est ramené dans l'axe après la rotation des épaules (sinon il part sur le côté, invisible
    de profil), puis étiré de PUNCH_STRETCH au sommet du coup."""
    other = "L" if side == "R" else "R"
    sign = 1 if side == "R" else -1
    return arms_down() + [
        ("spine.001", "X", lean * reach), ("spine.002", "Z", 44 * sign * reach),
        (f"upper_arm.{side}", "X", -90 * reach), (f"forearm.{side}", "X", -10 * (1 - reach)),
        (f"upper_arm.{side}", "Z", 35 * sign * reach),
        (f"upper_arm.{other}", "X", -40), (f"forearm.{other}", "X", -115),
    ] + (legs_lunge() if legs is None else legs) + [
        (f"upper_arm.{side}", "S", 1 + (PUNCH_STRETCH - 1) * reach),
    ]


def windup(side):
    """Bras armé vers l'arrière : l'annonce du coup."""
    other = "L" if side == "R" else "R"
    return arms_down() + [
        ("spine.001", "X", -6), ("spine.002", "Z", -15 if side == "R" else 15),
        (f"upper_arm.{side}", "X", 35), (f"forearm.{side}", "X", -120),
        (f"upper_arm.{other}", "X", -40), (f"forearm.{other}", "X", -115),
    ] + legs_stance()


def run_pose(phase):
    """Course : phase 0 = jambe gauche devant, 1 = croisement, 2 = jambe droite devant."""
    s = 1 if phase == 0 else -1

    if phase == 1:
        legs = [("thigh.L", "X", -5), ("shin.L", "X", 20), ("thigh.R", "X", 10), ("shin.R", "X", 95)]
        arms = [("upper_arm.L", "X", 0), ("upper_arm.R", "X", 0)]
    else:
        legs = [("thigh.L", "X", -38 * s), ("shin.L", "X", 15 if s > 0 else 75),
                ("thigh.R", "X", 38 * s), ("shin.R", "X", 75 if s > 0 else 15)]
        arms = [("upper_arm.L", "X", 35 * s), ("upper_arm.R", "X", -35 * s)]

    return arms_down() + [("spine.001", "X", 14)] + arms + [
        ("forearm.L", "X", -75), ("forearm.R", "X", -75)] + legs


# Clips : nom → (liste (image, ops), boucle). Les images sont à 30 FPS de sprite.
# drop : descente du bassin (m) dans la pose de mort, propre à chaque corps (voir fit_death_drop).
def clips(drop=0.8):
    g = guard()
    return {
        "idle": ([(0, g), (6, guard(lean=8, breathe=4)), (12, g)], True),
        "run": ([(0, run_pose(0)), (2, run_pose(1)), (4, run_pose(2)), (6, run_pose(1)), (8, run_pose(0))], True),
        "jump": ([(0, guard() + [("spine.001", "X", 8)]),
                  (2, arms_down() + [("upper_arm.L", "X", -60), ("upper_arm.R", "X", -40),
                                     ("forearm.L", "X", -60), ("forearm.R", "X", -60)] + legs_tuck()),
                  (4, arms_down() + [("upper_arm.L", "Y", -30), ("upper_arm.R", "Y", 30),
                                     ("thigh.L", "X", -20), ("shin.L", "X", 25), ("thigh.R", "X", 5), ("shin.R", "X", 20)])], False),
        # Minutage : le coup est tendu dès sa première image active, (démarrage - 1) / 2 à 30 FPS au plus tard
        # (un décalage d'une image de l'Animator compris) ; le hitstop fige ensuite cette pose à l'impact.
        # Combo J : jab (démarrage 4, 17 images à 60 FPS), direct (5, 20), crochet (7, 29).
        "jab": ([(0, g), (1, punch("R")), (4, punch("R")), (9, g)], False),
        "cross": ([(0, g), (1, punch("L", 0.5)), (2, punch("L")), (5, punch("L")), (10, g)], False),
        # Crochet : bras peu plié et ramené dans l'axe, buste plus engagé et étirement plus fort que les autres
        # coups, pour la même portée que le jab (poing à 0,9 m de l'axe chez Nathan M0).
        "hook": ([(0, g), (1, windup("R")),
                  (3, arms_down() + [("spine.001", "X", 32), ("spine.002", "Z", 45), ("upper_arm.R", "X", -88),
                                     ("forearm.R", "X", -35), ("upper_arm.R", "Z", 30), ("upper_arm.L", "X", -40),
                                     ("forearm.L", "X", -115)] + legs_lunge() + [("upper_arm.R", "S", PUNCH_STRETCH * 1.15)]),
                  (5, arms_down() + [("spine.001", "X", 32), ("spine.002", "Z", 50), ("upper_arm.R", "X", -88),
                                     ("forearm.R", "X", -35), ("upper_arm.R", "Z", 30), ("upper_arm.L", "X", -40),
                                     ("forearm.L", "X", -115)] + legs_lunge() + [("upper_arm.R", "S", PUNCH_STRETCH * 1.15)]),
                  (15, g)], False),
        # Coup de pied lourd (démarrage 10, 34) : jambe droite armée puis tendue.
        "heavy": ([(0, g),
                   (2, guard(lean=-8) + [("thigh.R", "X", -80), ("shin.R", "X", 110)]),
                   (4, guard(lean=-18) + [("thigh.R", "X", -95), ("shin.R", "X", 5)]),
                   (8, guard(lean=-18) + [("thigh.R", "X", -95), ("shin.R", "X", 5)]),
                   (17, g)], False),
        # Lanceur, uppercut (démarrage 8, 32).
        "launcher": ([(0, g),
                      (2, guard(lean=18) + [("thigh.L", "X", -25), ("shin.L", "X", 45), ("thigh.R", "X", 20), ("shin.R", "X", 45)]),
                      (3, arms_down() + [("spine.001", "X", -10), ("upper_arm.R", "X", -160), ("forearm.R", "X", -25),
                                         ("upper_arm.L", "X", -40), ("forearm.L", "X", -115)]),
                      (7, arms_down() + [("spine.001", "X", -12), ("upper_arm.R", "X", -165), ("forearm.R", "X", -20),
                                         ("upper_arm.L", "X", -40), ("forearm.L", "X", -115)]),
                      (16, g)], False),
        # Aériens (16, 18, 22) et smash (27) : jambes repliées.
        "air1": ([(0, guard() + legs_tuck()), (1, punch("R", legs=legs_tuck())), (4, punch("R", legs=legs_tuck())), (8, guard() + legs_tuck())], False),
        "air2": ([(0, guard() + legs_tuck()), (2, punch("L", legs=legs_tuck())), (5, punch("L", legs=legs_tuck())), (9, guard() + legs_tuck())], False),
        "air3": ([(0, guard() + legs_tuck()),
                  (2, guard(lean=-15) + [("thigh.L", "X", -40), ("shin.L", "X", 80), ("thigh.R", "X", -90), ("shin.R", "X", 0)]),
                  (6, guard(lean=-15) + [("thigh.L", "X", -40), ("shin.L", "X", 80), ("thigh.R", "X", -90), ("shin.R", "X", 0)]),
                  (11, guard() + legs_tuck())], False),
        "smash": ([(0, guard() + legs_tuck()),
                   (1, arms_down() + [("spine.001", "X", -12), ("upper_arm.L", "X", -170), ("upper_arm.R", "X", -170),
                                      ("forearm.L", "X", -40), ("forearm.R", "X", -40)] + legs_tuck()),
                   (3, arms_down() + [("spine.001", "X", 30), ("upper_arm.L", "X", -50), ("upper_arm.R", "X", -50)] + legs_tuck()),
                   (8, arms_down() + [("spine.001", "X", 30), ("upper_arm.L", "X", -40), ("upper_arm.R", "X", -40)] + legs_tuck()),
                   (14, guard() + legs_tuck())], False),
        "dash": ([(0, arms_down() + [("spine.001", "X", 30), ("upper_arm.L", "X", 50), ("upper_arm.R", "X", 50),
                                     ("thigh.L", "X", -45), ("shin.L", "X", 40), ("thigh.R", "X", 35), ("shin.R", "X", 30)]),
                  (5, arms_down() + [("spine.001", "X", 30), ("upper_arm.L", "X", 55), ("upper_arm.R", "X", 55),
                                     ("thigh.L", "X", -45), ("shin.L", "X", 40), ("thigh.R", "X", 35), ("shin.R", "X", 30)])], False),
        "dodge": ([(0, g),
                   (2, guard(lean=-25) + [("thigh.L", "X", -30), ("shin.L", "X", 60), ("thigh.R", "X", 30), ("shin.R", "X", 40)]),
                   (4, guard(lean=-25) + [("thigh.L", "X", -30), ("shin.L", "X", 60), ("thigh.R", "X", 30), ("shin.R", "X", 40)]),
                   (6, g)], False),
        "hurt": ([(0, g),
                  (1, arms_down() + [("spine.001", "X", -22), ("spine.004", "X", -20), ("upper_arm.L", "X", 25), ("upper_arm.R", "X", 30),
                                     ("forearm.L", "X", -30), ("forearm.R", "X", -30)] + legs_stance()),
                  (5, guard(lean=-6))], False),
        "death": ([(0, arms_down() + [("spine.001", "X", -22), ("spine.004", "X", -20)] + legs_stance()),
                   (5, arms_down() + [("spine", "X", -45), ("spine", "T", (0, 0, -drop * 0.31)), ("upper_arm.L", "X", 60),
                                      ("upper_arm.R", "X", 50), ("thigh.L", "X", -20), ("thigh.R", "X", -10)]),
                   # Couché : bras le long du corps (s'ils pendaient, ils soulèveraient le corps du sol).
                   (10, arms_down() + [("spine", "X", -88), ("spine", "T", (0, 0, -drop + 0.02)), ("upper_arm.L", "X", 12),
                                       ("upper_arm.R", "X", 8), ("thigh.L", "X", -10), ("thigh.R", "X", 5)]),
                   (14, arms_down() + [("spine", "X", -90), ("spine", "T", (0, 0, -drop)), ("upper_arm.L", "X", 10),
                                       ("upper_arm.R", "X", 6), ("thigh.L", "X", -8), ("thigh.R", "X", 5)])], False),
        # Coup de poing du Voyou : armement 21 images à 60 FPS (11 à 30), actif 6 (3), récupération 21 (11) ;
        # tendu à l'image 10, comme les coups de Nathan.
        "enemy_punch": ([(0, g), (8, windup("R")), (9, windup("R")), (10, punch("R")), (13, punch("R")), (25, g)], False),
    }


def build_actions(rig, prefix, drop=0.8, only=None, stretch=1.0):
    """Crée une action par clip pour ce squelette (« <prefix>_<clip> ») ; only limite aux clips nommés,
    stretch règle l'étirement des bras au sommet des coups (PUNCH_STRETCH)."""
    global PUNCH_STRETCH
    PUNCH_STRETCH = stretch
    actions = {}
    rig.animation_data_create()

    for name, (keys, loop) in clips(drop).items():
        if only is not None and name not in only:
            continue

        action_name = f"{prefix}_{name}"
        old = bpy.data.actions.get(action_name)

        if old:
            bpy.data.actions.remove(old)

        action = bpy.data.actions.new(action_name)
        action.use_fake_user = True
        rig.animation_data.action = action

        for frame, ops in keys:
            _apply_ops(rig, ops)

            for pb in rig.pose.bones:
                pb.keyframe_insert("rotation_quaternion", frame=frame)
                pb.keyframe_insert("location", frame=frame)
                pb.keyframe_insert("scale", frame=frame)

        action["loop"] = loop
        action["length"] = keys[-1][0]
        actions[name] = action

    return actions


def lowest_point(mesh, frame):
    """Hauteur du point le plus bas du maillage déformé (contour exclu), par rapport à ses pieds."""
    outline = mesh.modifiers.get("Outline")

    if outline:
        outline.show_viewport = False

    bpy.context.scene.frame_set(frame)
    dg = bpy.context.evaluated_depsgraph_get()
    evaluated = mesh.evaluated_get(dg)
    data = evaluated.to_mesh()
    co = np.zeros(len(data.vertices) * 3)
    data.vertices.foreach_get("co", co)
    evaluated.to_mesh_clear()

    if outline:
        outline.show_viewport = True

    world = np.array(mesh.matrix_world)
    z = co.reshape(-1, 3) @ world[2, :3] + world[2, 3]
    return float(z.min() - mesh.matrix_world.translation.z)


def fit_death_drop(rig, mesh, prefix, drop=0.8):
    """Pose de mort posée au sol : la descente du bassin est réduite jusqu'à ce que le point le plus
    bas du corps couché touche le sol (sinon une partie du corps passe sous la ligne du sol)."""
    for _ in range(3):
        rig.animation_data.action = build_actions(rig, prefix, drop, only={"death"}, stretch=PUNCH_STRETCH)["death"]
        low = lowest_point(mesh, 14)

        if abs(low) < 0.005:
            break

        drop += low

    return drop


# ------------------------------------------------------------------- Rendu toon

def toon_material(name="RM_Toon", steps=(0.0, 0.08, 0.5), tones=(0.38, 0.66, 1.0)):
    """Couleur du sommet × rampe à paliers, sans reflet : rendu cel-shading.
    Trois aplats francs (ombre portée, ombre propre, lumière) ; l'ombre tire un peu vers le violet."""
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()

    color = nt.nodes.new("ShaderNodeVertexColor")
    color.layer_name = "Col"
    diffuse = nt.nodes.new("ShaderNodeBsdfDiffuse")
    to_rgb = nt.nodes.new("ShaderNodeShaderToRGB")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.interpolation = 'CONSTANT'

    while len(ramp.color_ramp.elements) < len(steps):
        ramp.color_ramp.elements.new(1.0)

    for element, position, tone in zip(ramp.color_ramp.elements, steps, tones):
        element.position = position
        element.color = (tone * 0.95, tone * 0.92, min(1.0, tone * 1.08), 1)
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = 'RGBA'
    mix.blend_type = 'MULTIPLY'
    mix.inputs[0].default_value = 1.0
    emission = nt.nodes.new("ShaderNodeEmission")
    out = nt.nodes.new("ShaderNodeOutputMaterial")

    nt.links.new(to_rgb.inputs[0], diffuse.outputs[0])
    nt.links.new(ramp.inputs[0], to_rgb.outputs[0])
    nt.links.new(mix.inputs[6], color.outputs[0])
    nt.links.new(mix.inputs[7], ramp.outputs[0])
    nt.links.new(emission.inputs[0], mix.outputs[2])
    nt.links.new(out.inputs[0], emission.outputs[0])
    return mat


def clean_colors(mesh, merge=0.07, iterations=3):
    """Aplats nets : fusionne les teintes presque identiques (deux noirs du sweat, deux tons de peau),
    puis filtre majoritaire entre voisins, qui efface les petits îlots de couleur projetés depuis le concept."""
    import numpy as np

    attribute = mesh.data.color_attributes["Col"]
    colors = np.zeros(len(attribute.data) * 4, dtype=np.float32)
    attribute.data.foreach_get("color", colors)
    colors = colors.reshape(-1, 4)

    palette, index, counts = np.unique(np.round(colors[:, :3], 3), axis=0, return_inverse=True, return_counts=True)
    index = index.ravel()

    # Chaque teinte rejoint la plus fréquente de celles qui lui sont proches.
    target = np.arange(len(palette))

    for i in np.argsort(-counts):
        for j in range(len(palette)):
            if target[j] == j and j != i and counts[i] >= counts[j] and np.linalg.norm(palette[i] - palette[j]) < merge:
                target[j] = target[i]

    index = target[index]

    edges = np.zeros(len(mesh.data.edges) * 2, dtype=np.int64)
    mesh.data.edges.foreach_get("vertices", edges)
    edges = edges.reshape(-1, 2)
    pairs = np.concatenate([edges, edges[:, ::-1], np.repeat(np.arange(len(index))[:, None], 2, axis=1)])

    for _ in range(iterations):
        votes = np.zeros((len(index), len(palette)), dtype=np.int32)
        np.add.at(votes, (pairs[:, 0], index[pairs[:, 1]]), 1)
        index = votes.argmax(axis=1)

    colors[:, :3] = palette[index]
    attribute.data.foreach_set("color", colors.ravel())
    mesh.data.update()
    return len(np.unique(index))


def outline_material(name="RM_Outline", color=(0.0, 0.0, 0.0)):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    emission = nt.nodes.new("ShaderNodeEmission")
    emission.inputs[0].default_value = (*color, 1)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(out.inputs[0], emission.outputs[0])
    mat.use_backface_culling = True
    return mat


def setup_toon(obj, thickness=0.024):
    """Matériau toon + contour par coque inversée (Solidify, normales retournées)."""
    obj.data.materials.clear()
    obj.data.materials.append(toon_material())
    obj.data.materials.append(outline_material())

    solid = obj.modifiers.get("Outline") or obj.modifiers.new("Outline", 'SOLIDIFY')
    solid.thickness = thickness
    solid.offset = 1.0
    solid.use_flip_normals = True
    solid.material_offset = 1
    solid.use_rim = False


def smooth_shading(mesh, iterations=300):
    """Ombrage sur les grandes formes : les normales viennent d'une copie très lissée du maillage,
    transférées en pose de repos puis enregistrées dans le maillage (normales personnalisées, qui
    suivent ensuite la déformation du squelette). La silhouette ne change pas, mais les petites
    bosses du modèle TripoSG ne tachettent plus les aplats. Relancer recalcule depuis la géométrie."""
    for modifier in [m for m in mesh.modifiers if m.type == 'DATA_TRANSFER']:
        mesh.modifiers.remove(modifier)

    old = bpy.data.objects.get(mesh.name + "_NormalSrc")

    if old is not None:
        bpy.data.objects.remove(old)

    src = mesh.copy()
    src.data = mesh.data.copy()
    src.modifiers.clear()

    for collection in mesh.users_collection:
        collection.objects.link(src)

    # La copie repart des normales de la géométrie, pas de celles d'un passage précédent.
    if "custom_normal" in src.data.attributes:
        src.data.attributes.remove(src.data.attributes["custom_normal"])

    smooth = src.modifiers.new("Smooth", 'SMOOTH')
    smooth.factor = 1.0
    smooth.iterations = iterations

    transfer = mesh.modifiers.new("SmoothNormals", 'DATA_TRANSFER')
    transfer.object = src
    transfer.use_loop_data = True
    transfer.data_types_loops = {'CUSTOM_NORMAL'}
    transfer.loop_mapping = 'POLYINTERP_NEAREST'

    # La copie lissée doit être évaluée avant l'application, sinon le transfert ne copie rien.
    bpy.context.view_layer.update()

    # En tête de pile (pose de repos), puis appliqué : le maillage garde les normales lissées.
    with bpy.context.temp_override(object=mesh, active_object=mesh):
        bpy.ops.object.modifier_move_to_index(modifier=transfer.name, index=0)
        bpy.ops.object.modifier_apply(modifier=transfer.name)

    src_data = src.data
    bpy.data.objects.remove(src)
    bpy.data.meshes.remove(src_data)


def setup_render_scene(name="RM_Sprites", size=512, ortho=2.6, center_z=1.0):
    """Scène de rendu des sprites : caméra orthographique en -X, fond transparent, soleil de face."""
    scene = bpy.data.scenes.get(name) or bpy.data.scenes.new(name)
    scene.render.resolution_x = size
    scene.render.resolution_y = size
    scene.render.film_transparent = True
    scene.render.fps = SPRITE_FPS
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'

    try:
        scene.render.engine = 'BLENDER_EEVEE_NEXT'
    except TypeError:
        scene.render.engine = 'BLENDER_EEVEE'

    scene.view_settings.view_transform = 'Standard'

    cam_data = bpy.data.cameras.get(name) or bpy.data.cameras.new(name)
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = ortho
    cam = bpy.data.objects.get(name + "_Cam") or bpy.data.objects.new(name + "_Cam", cam_data)

    if cam.name not in scene.collection.objects:
        scene.collection.objects.link(cam)

    scene.camera = cam

    sun_data = bpy.data.lights.get(name + "_Sun") or bpy.data.lights.new(name + "_Sun", 'SUN')
    sun_data.energy = 3.0
    # Ombres portées à bord net : un soleil sans diamètre apparent.
    sun_data.use_shadow = True
    sun_data.angle = 0.0
    sun = bpy.data.objects.get(name + "_Sun") or bpy.data.objects.new(name + "_Sun", sun_data)

    if sun.name not in scene.collection.objects:
        scene.collection.objects.link(sun)

    # Lumière de trois quarts, haute, venant de l'avant du personnage : une moitié dans l'ombre propre.
    sun.rotation_euler = (math.radians(55), 0, math.radians(-150))
    scene["center_z"] = center_z
    return scene, cam


def render_clips(scene, cam, rig, mesh, actions, out_dir, prefix, frames_dir=None):
    """Rend chaque clip image par image et l'assemble en bande horizontale <prefix>_<clip>.png.
    Les images intermédiaires vont dans frames_dir (hors du projet Unity de préférence)."""
    os.makedirs(out_dir, exist_ok=True)
    frames_dir = frames_dir or os.path.join(out_dir, "_frames")

    for obj in (rig, mesh):
        if obj.name not in scene.collection.objects:
            scene.collection.objects.link(obj)

    # Seul ce personnage est rendu : les autres maillages de la scène sont masqués le temps du rendu.
    hidden = [o for o in scene.objects if o.type == 'MESH' and o != mesh and not o.hide_render]

    for o in hidden:
        o.hide_render = True

    # Caméra en -X du personnage, centrée sur ses pieds en Y, à center_z de haut.
    feet = mesh.matrix_world.translation
    cam.location = (feet.x - 6.0, feet.y, scene["center_z"])
    cam.rotation_euler = (math.radians(90), 0, math.radians(-90))
    window = bpy.context.window
    previous = window.scene
    window.scene = scene
    sheets = {}

    try:
        for name, action in actions.items():
            rig.animation_data.action = action
            length = int(action["length"])
            frames = range(0, length if action["loop"] else length + 1)
            images = []

            for frame in frames:
                scene.frame_set(frame)
                path = os.path.join(frames_dir, f"{prefix}_{name}_{frame:03d}.png")
                scene.render.filepath = path
                bpy.ops.render.render(write_still=True)
                images.append(path)

            sheets[name] = _strip(images, os.path.join(out_dir, f"{prefix}_{name}.png"))
    finally:
        window.scene = previous

        for o in hidden:
            o.hide_render = False

    return sheets


def _strip(paths, out_path, columns=8):
    """Feuille en grille (columns images par ligne, de gauche à droite puis de haut en bas) :
    la plus longue bande dépasserait sinon la taille maximale d'une texture (8192 px)."""
    tiles = []

    for p in paths:
        img = bpy.data.images.load(p)
        w, h = img.size
        tiles.append(np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4))
        bpy.data.images.remove(img)

    cols = min(columns, len(tiles))
    rows = -(-len(tiles) // cols)
    th, tw, _ = tiles[0].shape
    sheet = np.zeros((rows * th, cols * tw, 4), dtype=np.float32)

    # Les pixels Blender commencent en bas : la première ligne de la grille est la plus haute.
    for i, tile in enumerate(tiles):
        r, c = divmod(i, cols)
        y = (rows - 1 - r) * th
        sheet[y:y + th, c * tw:(c + 1) * tw] = tile

    h, w, _ = sheet.shape
    out = bpy.data.images.new(os.path.basename(out_path), w, h, alpha=True)
    out.pixels = sheet.ravel()
    out.filepath_raw = out_path
    out.file_format = 'PNG'
    out.save()
    bpy.data.images.remove(out)
    return {"path": out_path, "frames": len(paths), "size": (tw, th), "columns": cols, "rows": rows}


def repack(frames_dir, out_dir, prefix, columns=8):
    """Refait les feuilles à partir des images déjà rendues (sans nouveau rendu)."""
    groups = {}

    for name in sorted(os.listdir(frames_dir)):
        if name.startswith(prefix + "_") and name.endswith(".png"):
            clip = name[len(prefix) + 1:].rsplit("_", 1)[0]
            groups.setdefault(clip, []).append(os.path.join(frames_dir, name))

    return {clip: _strip(paths, os.path.join(out_dir, f"{prefix}_{clip}.png"), columns) for clip, paths in groups.items()}


def write_sheet_info(sheets, out_dir, prefix, size=512, ortho=2.6, feet_from_bottom=0.3):
    """sprites.json : ce qu'il faut à l'outil d'import Unity (SpriteAnimationBuilder) pour découper
    les feuilles, régler l'échelle, le pivot aux pieds, la cadence et les boucles."""
    import json

    clips_info = []

    for clip, sheet in sorted(sheets.items()):
        action = bpy.data.actions[f"{prefix}_{clip}"]
        clips_info.append({"name": clip, "frames": sheet["frames"], "columns": sheet["columns"],
                           "rows": sheet["rows"], "size": list(sheet["size"]), "loop": bool(action["loop"])})

    data = {"fps": SPRITE_FPS, "pixels_per_meter": size / ortho, "feet_from_bottom_m": feet_from_bottom, "clips": clips_info}

    with open(os.path.join(out_dir, "sprites.json"), "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2, ensure_ascii=False)
