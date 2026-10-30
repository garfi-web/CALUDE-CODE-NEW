"""RED MET — qualité des personnages en vue de 3/4 (demande de l'auteur du 2026-10-30).

Exécuté par le MCP Blender, après redmet_pipeline.py et redmet_animation.py (mêmes squelettes, mêmes actions) :

    exec(open(r"<dépôt>/tools/blender/redmet_animation.py", encoding="utf-8").read())
    exec(open(r"<dépôt>/tools/blender/redmet_quality.py", encoding="utf-8").read())
    upgrade("Nathan_M0")            # texture projetée + matériau à 4 aplats
    render_34("Nathan_M0")          # sprites 3/4 en double résolution + piste du visage

1. Texture : dépliage UV (Smart UV Project) et projection de face du concept validé. La projection utilise une UV
   « Proj » calculée sur la pose en T du maillage, qui est celle du concept (TripoSG est parti de cette image). Elle est
   fondue dans les aplats par zones selon l'orientation de la surface : le dos et les côtés gardent les aplats (il n'y a
   pas de concept de profil validé, et la règle de l'auteur interdit d'en dériver un). La zone yeux-bouche reçoit un
   aplat de peau : le visage vient du calque 2D dessiné à part (tools/gen3d/faces.py, FaceLayer25D dans Unity). Le tout
   est cuit dans une texture de 2048 px (ArtSource/Blender/Textures). Les traits fins du dessin du concept (plis,
   coutures, mains) restent dans la texture : ce sont les contours intérieurs, plus fins que le contour principal.
2. Matériau toon à 4 aplats (au lieu de 3) sur cette texture ; contour principal par coque, épais, inchangé.
3. Rendu en vue de 3/4 (caméra surélevée de 18° et tournée de 30°, choix de l'auteur), en double résolution
   (1024 px pour 2,6 m), dans <Perso>/Sprites25D, sans toucher aux sprites de l'arène latérale.
4. Piste du visage, image par image : position, inclinaison, retournement et visibilité (tête tournée, ou bras devant le
   visage) → face.json, à côté de sprites.json.
"""
import json
import math
import os

import bpy
import numpy as np
from bpy_extras.object_utils import world_to_camera_view
from mathutils import Vector

REPO = r"C:\Users\Home\Unity user templates\REDMET"
TEXTURES = os.path.join(REPO, "ArtSource", "Blender", "Textures")
FRAMES = os.path.join(REPO, "ArtSource", "Blender", "Frames25D")
CHARACTERS = os.path.join(REPO, "Assets", "_Game", "Art", "Characters")

ELEVATION = 18.0
YAW = 30.0
SIZE = 1024
ORTHO = 2.6
HEAD_BONE = "spine.006"
# Un point touché à moins de cette distance du centre de la tête appartient à la tête (joue, nez, coque du contour).
HEAD_SIZE = 0.15

# Par personnage : maillage, squelette, préfixe des actions, concept projeté, peau de la zone du visage (aplat sous le
# calque 2D), hauteur des yeux sur l'os de la tête (fraction), rayon de la zone du visage, couleur du contour.
CONFIG = {
    "Nathan_M0": {
        "mesh": "Nathan_M0_M", "rig": "Nathan_M0_Rig", "prefix": "NathanM0",
        "concept": os.path.join(REPO, "Assets", "_Game", "Art", "Characters", "Nathan_R0", "Concept",
                                "Nathan_R0_concept_legs_d05_s4303.png"),
        "skin": (0.93, 0.76, 0.66), "eyes": 0.40, "face_radius": 0.075,
        # Liseré rouge sombre propre au joueur : Nathan se distingue des Voyous dans la mêlée (demande de l'auteur).
        "outline": (0.2, 0.02, 0.03),
    },
    "Voyou": {
        "mesh": "Voyou_M", "rig": "Voyou_Rig", "prefix": "Voyou",
        # Copie technique sans le faux texte du sweat (ArtSource/Blender/Textures, faite le 2026-10-30).
        "concept": os.path.join(TEXTURES, "Voyou_concept_s11103_projection.png"),
        # Peau plus claire sous la capuche : le visage doit se voir (demande de l'auteur).
        "skin": (0.72, 0.55, 0.45), "eyes": 0.36, "face_radius": 0.08,
        # La capuche est bien plus large qu'une tête nue : ses bords appartiennent encore à la « tête ».
        "head_size": 0.23,
        "outline": (0.0, 0.0, 0.0),
    },
}


# --------------------------------------------------------------------- Outils

def _image_array(image):
    w, h = image.size
    return np.array(image.pixels[:], dtype=np.float32).reshape(h, w, 4)


def concept_bbox(path, tolerance=0.09):
    """Boîte du personnage dans l'image (UV, origine en bas à gauche) : tout ce qui s'écarte du fond uni des coins."""
    image = bpy.data.images.load(path, check_existing=True)
    a = _image_array(image)[..., :3]
    h, w = a.shape[:2]
    corners = np.concatenate([a[:8, :8].reshape(-1, 3), a[:8, -8:].reshape(-1, 3), a[-8:, :8].reshape(-1, 3), a[-8:, -8:].reshape(-1, 3)])
    background = np.median(corners, axis=0)
    mask = np.abs(a - background).max(axis=2) > tolerance
    rows = np.where(mask.any(axis=1))[0]
    cols = np.where(mask.any(axis=0))[0]
    return image, (cols[0] / w, (cols[-1] + 1) / w, rows[0] / h, (rows[-1] + 1) / h)


def _rest(rig, rest=True):
    rig.data.pose_position = 'REST' if rest else 'POSE'
    bpy.context.view_layer.update()


def smart_uv(obj, name="UVMap"):
    """Dépliage propre pour la texture cuite (îlots par angle, marges pour le filtrage)."""
    uv = obj.data.uv_layers.get(name) or obj.data.uv_layers.new(name=name)
    obj.data.uv_layers.active = uv
    view_layer = bpy.context.view_layer
    previous = view_layer.objects.active

    with bpy.context.temp_override(active_object=obj, object=obj, selected_objects=[obj], selected_editable_objects=[obj]):
        view_layer.objects.active = obj
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.004)
        bpy.ops.object.mode_set(mode='OBJECT')

    view_layer.objects.active = previous
    return uv


def projection_uv(obj, bbox, name="Proj"):
    """UV de projection de face : la pose en T du maillage (vue de -Y, +X à droite) sur la boîte du personnage du concept."""
    me = obj.data
    world = obj.matrix_world
    co = np.array([tuple(world @ v.co) for v in me.vertices])
    x0, x1 = co[:, 0].min(), co[:, 0].max()
    z0, z1 = co[:, 2].min(), co[:, 2].max()
    u0, u1, v0, v1 = bbox
    uv = me.uv_layers.get(name) or me.uv_layers.new(name=name)
    loops = np.zeros(len(me.loops), dtype=np.int64)
    me.loops.foreach_get("vertex_index", loops)
    p = co[loops]
    coords = np.stack([u0 + (p[:, 0] - x0) / (x1 - x0) * (u1 - u0), v0 + (p[:, 2] - z0) / (z1 - z0) * (v1 - v0)], axis=1)
    uv.data.foreach_set("uv", coords.astype(np.float32).ravel())
    return uv


def face_point(obj, rig, eyes):
    """Point du visage (milieu, à hauteur des yeux), sur la surface avant de la tête, en pose de repos (monde)."""
    bone = rig.data.bones[HEAD_BONE]
    head = rig.matrix_world @ bone.head_local
    tail = rig.matrix_world @ bone.tail_local
    level = head.lerp(tail, eyes)
    depsgraph = bpy.context.evaluated_depsgraph_get()
    inverse = obj.matrix_world.inverted()
    origin = inverse @ (level + Vector((0, -1.0, 0)))
    direction = (inverse.to_3x3() @ Vector((0, 1, 0))).normalized()
    hit, location, _, _ = obj.evaluated_get(depsgraph).ray_cast(origin, direction)

    if not hit:
        raise RuntimeError(f"Visage introuvable devant {HEAD_BONE} ({obj.name})")

    return obj.matrix_world @ location


# ------------------------------------------------------------- Texture cuite

def bake_albedo(obj, rig, cfg, size=2048):
    """Cuit concept projeté + aplats + zone de peau du visage dans une texture sur l'UV propre."""
    os.makedirs(TEXTURES, exist_ok=True)
    concept, bbox = concept_bbox(cfg["concept"])
    concept.colorspace_settings.name = 'sRGB'
    outline = obj.modifiers.get("Outline")
    armature = next((m for m in obj.modifiers if m.type == 'ARMATURE'), None)
    states = {m: (m.show_viewport, m.show_render) for m in (outline, armature) if m is not None}

    for m in states:
        m.show_viewport = m.show_render = False

    _rest(rig, True)
    face = face_point(obj, rig, cfg["eyes"])
    projection_uv(obj, bbox)
    smart_uv(obj)

    name = f"{obj.name}_albedo"
    target = bpy.data.images.get(name) or bpy.data.images.new(name, size, size, alpha=False)
    target.scale(size, size)
    target.filepath_raw = os.path.join(TEXTURES, name + ".png")
    target.file_format = 'PNG'

    mat = bpy.data.materials.get("RM_Bake") or bpy.data.materials.new("RM_Bake")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    n = nt.nodes.new
    proj = n("ShaderNodeUVMap"); proj.uv_map = "Proj"
    tex = n("ShaderNodeTexImage"); tex.image = concept; tex.extension = 'EXTEND'
    col = n("ShaderNodeVertexColor"); col.layer_name = "Col"
    geo = n("ShaderNodeNewGeometry")
    facing = n("ShaderNodeVectorMath"); facing.operation = 'DOT_PRODUCT'; facing.inputs[1].default_value = (0, -1, 0)
    fade = n("ShaderNodeMapRange"); fade.inputs[1].default_value = 0.15; fade.inputs[2].default_value = 0.55
    mix = n("ShaderNodeMix"); mix.data_type = 'RGBA'
    dist = n("ShaderNodeVectorMath"); dist.operation = 'DISTANCE'; dist.inputs[1].default_value = tuple(face)
    mask = n("ShaderNodeMapRange")
    mask.inputs[1].default_value = cfg["face_radius"] * 0.7
    mask.inputs[2].default_value = cfg["face_radius"]
    mask.inputs[3].default_value = 1.0
    mask.inputs[4].default_value = 0.0
    skin = n("ShaderNodeMix"); skin.data_type = 'RGBA'; skin.inputs[7].default_value = (*cfg["skin"], 1)
    emit = n("ShaderNodeEmission")
    out = n("ShaderNodeOutputMaterial")
    bake_node = n("ShaderNodeTexImage"); bake_node.image = target
    link = nt.links.new
    link(tex.inputs[0], proj.outputs[0])
    link(facing.inputs[0], geo.outputs["Normal"])
    link(fade.inputs[0], facing.outputs["Value"])
    link(mix.inputs[0], fade.outputs[0])
    link(mix.inputs[6], col.outputs[0])
    link(mix.inputs[7], tex.outputs[0])
    link(dist.inputs[0], geo.outputs["Position"])
    link(mask.inputs[0], dist.outputs["Value"])
    link(skin.inputs[0], mask.outputs[0])
    link(skin.inputs[6], mix.outputs[2])
    link(emit.inputs[0], skin.outputs[2])
    link(out.inputs[0], emit.outputs[0])
    nt.nodes.active = bake_node

    materials = list(obj.data.materials)
    obj.data.materials[0] = mat
    scene = bpy.context.scene
    engine = scene.render.engine
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 1

    try:
        with bpy.context.temp_override(active_object=obj, object=obj, selected_objects=[obj]):
            bpy.ops.object.bake(type='EMIT', margin=12, use_clear=True)
    finally:
        scene.render.engine = engine
        obj.data.materials[0] = materials[0]

        for m, (viewport, render) in states.items():
            m.show_viewport, m.show_render = viewport, render

        _rest(rig, False)

    target.save()
    return target, face


def toon4_material(name, image, steps=(0.0, 0.05, 0.28, 0.6), tones=(0.30, 0.52, 0.76, 1.0)):
    """Texture cuite × rampe à 4 paliers : ombre portée, ombre propre, demi-teinte, lumière (au lieu de 3)."""
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    n = nt.nodes.new
    uv = n("ShaderNodeUVMap"); uv.uv_map = "UVMap"
    tex = n("ShaderNodeTexImage"); tex.image = image
    diffuse = n("ShaderNodeBsdfDiffuse")
    to_rgb = n("ShaderNodeShaderToRGB")
    ramp = n("ShaderNodeValToRGB"); ramp.color_ramp.interpolation = 'CONSTANT'

    while len(ramp.color_ramp.elements) < len(steps):
        ramp.color_ramp.elements.new(1.0)

    for element, position, tone in zip(ramp.color_ramp.elements, steps, tones):
        element.position = position
        element.color = (tone * 0.95, tone * 0.92, min(1.0, tone * 1.08), 1)

    mix = n("ShaderNodeMix"); mix.data_type = 'RGBA'; mix.blend_type = 'MULTIPLY'; mix.inputs[0].default_value = 1.0
    emission = n("ShaderNodeEmission")
    out = n("ShaderNodeOutputMaterial")
    link = nt.links.new
    link(tex.inputs[0], uv.outputs[0])
    link(to_rgb.inputs[0], diffuse.outputs[0])
    link(ramp.inputs[0], to_rgb.outputs[0])
    link(mix.inputs[6], tex.outputs[0])
    link(mix.inputs[7], ramp.outputs[0])
    link(emission.inputs[0], mix.outputs[2])
    link(out.inputs[0], emission.outputs[0])
    return mat


def upgrade(character):
    """Texture projetée et matériau à 4 aplats ; contour à la couleur du personnage. Rejouable."""
    cfg = CONFIG[character]
    obj = bpy.data.objects[cfg["mesh"]]
    rig = bpy.data.objects[cfg["rig"]]
    image, face = bake_albedo(obj, rig, cfg)
    obj.data.materials[0] = toon4_material(f"RM_Toon4_{character}", image)
    obj.data.materials[1] = outline_material(f"RM_Outline_{character}", cfg["outline"])
    # Le point du visage, en repère de l'os de la tête (repos), pour la piste du visage.
    bone = rig.data.bones[HEAD_BONE]
    local = (rig.matrix_world @ bone.matrix_local).inverted() @ face
    obj["face_local"] = list(local)
    return image.filepath_raw


# ------------------------------------------------------------------ Rendu 3/4

def place_camera(cam, target, elevation=ELEVATION, yaw=YAW, distance=6.0):
    """Caméra orthographique surélevée et tournée vers l'avant du personnage (qui regarde vers -Y)."""
    e, y = math.radians(elevation), math.radians(yaw)
    d = Vector((math.cos(y), math.sin(y), 0))
    cam.location = target - d * distance * math.cos(e) + Vector((0, 0, distance * math.sin(e)))
    cam.rotation_euler = (math.radians(90 - elevation), 0, math.radians(-90 + yaw))


def _face_sample(scene, cam, rig, local, depsgraph, head_size=None):
    """Visage dans l'image courante : (u, v, angle, échelle, visible, retourné)."""
    pb = rig.pose.bones[HEAD_BONE]
    matrix = rig.matrix_world @ pb.matrix
    anchor = matrix @ Vector(local)
    rest = (rig.matrix_world @ pb.bone.matrix_local).to_3x3()
    turn = matrix.to_3x3() @ rest.inverted()
    forward = (turn @ Vector((0, -1, 0))).normalized()
    up = (turn @ Vector((0, 0, 1))).normalized()

    view = (cam.matrix_world.to_3x3() @ Vector((0, 0, -1))).normalized()
    right = (cam.matrix_world.to_3x3() @ Vector((1, 0, 0))).normalized()
    p = world_to_camera_view(scene, cam, anchor)
    q = world_to_camera_view(scene, cam, anchor + up * 0.1)
    angle = math.degrees(math.atan2(q.y - p.y, q.x - p.x)) - 90.0
    facing = forward.dot(-view)
    # Largeur apparente : 1 dans la vue de 3/4 de référence (tête au repos), moins quand la tête se détourne.
    reference = math.sin(math.radians(YAW)) * math.cos(math.radians(ELEVATION))
    scale = max(0.55, min(1.15, facing / max(0.3, reference) if facing > 0 else 0.0))
    flipped = forward.dot(right) < -0.05

    # Bras ou arme devant le visage : on tire un rayon depuis la caméra vers trois points (œil côté caméra, bouche,
    # milieu). En vue de 3/4 (60° de la face), le rayon rencontre d'abord la joue côté caméra : tout point touché près du
    # centre de la tête appartient à la tête. Ce qui est plus loin (poing, avant-bras, arme) cache le point.
    # Le visage reste affiché si l'œil côté caméra se voit, et la bouche ou le milieu.
    x, y, z = local
    center = matrix @ Vector((x, y, z - 0.07))

    def clear(point):
        origin = point - view * 4.0
        hit, location, _, _, _, _ = scene.ray_cast(depsgraph, origin, view)
        return not hit or (location - center).length < (head_size or HEAD_SIZE) or (location - origin).length > (point - origin).length

    near = matrix @ Vector((x - 0.032, y, z - 0.01))
    mouth = matrix @ Vector((x - 0.005, y - 0.05, z - 0.005))
    visible = facing > 0.12 and clear(near) and (clear(mouth) or clear(anchor))
    return [round(p.x, 5), round(p.y, 5), round(angle, 2), round(scale, 3), int(visible), int(flipped)]


def write_face_json(out_dir, faces):
    """face.json, lisible par JsonUtility : une liste de clips, six valeurs par image
    (u, v depuis le bas à gauche de l'image, angle en degrés, largeur, visible, retourné)."""
    clips = [{"name": name, "data": [value for frame in track for value in frame]} for name, track in sorted(faces.items())]

    with open(os.path.join(out_dir, "face.json"), "w", encoding="utf-8") as f:
        json.dump({"values_per_frame": 6, "clips": clips}, f)


def track_faces(character, size=SIZE, ortho=ORTHO, center_z=1.0):
    """Recalcule seulement la piste du visage (face.json), sans refaire le rendu des images."""
    cfg = CONFIG[character]
    mesh = bpy.data.objects[cfg["mesh"]]
    rig = bpy.data.objects[cfg["rig"]]
    prefix = cfg["prefix"]
    scene, cam = setup_render_scene(size=size, ortho=ortho, center_z=center_z)
    out_dir = os.path.join(CHARACTERS, character, "Sprites25D")

    for obj in (rig, mesh):
        if obj.name not in scene.collection.objects:
            scene.collection.objects.link(obj)

    feet = mesh.matrix_world.translation
    place_camera(cam, Vector((feet.x, feet.y, center_z)))
    window = bpy.context.window
    previous = window.scene
    window.scene = scene
    faces = {}

    try:
        for action in [a for a in bpy.data.actions if a.name.startswith(prefix + "_")]:
            rig.animation_data.action = action
            length = int(action["length"])
            track = []

            for frame in range(0, length if action["loop"] else length + 1):
                scene.frame_set(frame)
                track.append(_face_sample(scene, cam, rig, mesh["face_local"], bpy.context.evaluated_depsgraph_get(),
                                          cfg.get("head_size")))

            faces[action.name[len(prefix) + 1:]] = track
    finally:
        window.scene = previous

    write_face_json(out_dir, faces)
    return {k: (sum(f[4] for f in v), len(v)) for k, v in faces.items()}


def render_34(character, clips=None, size=SIZE, ortho=ORTHO, center_z=1.0):
    """Toutes les animations du personnage en 3/4, double résolution, avec la piste du visage (face.json)."""
    cfg = CONFIG[character]
    mesh = bpy.data.objects[cfg["mesh"]]
    rig = bpy.data.objects[cfg["rig"]]
    prefix = cfg["prefix"]
    scene, cam = setup_render_scene(size=size, ortho=ortho, center_z=center_z)
    scene.render.resolution_x = scene.render.resolution_y = size
    out_dir = os.path.join(CHARACTERS, character, "Sprites25D")
    frames_dir = os.path.join(FRAMES, character)
    os.makedirs(out_dir, exist_ok=True)
    os.makedirs(frames_dir, exist_ok=True)
    local = mesh["face_local"]

    actions = {a.name[len(prefix) + 1:]: a for a in bpy.data.actions if a.name.startswith(prefix + "_")}

    if clips:
        actions = {k: v for k, v in actions.items() if k in clips}

    for obj in (rig, mesh):
        if obj.name not in scene.collection.objects:
            scene.collection.objects.link(obj)

    hidden = [o for o in scene.objects if o.type == 'MESH' and o != mesh and not o.hide_render]

    for o in hidden:
        o.hide_render = True

    feet = mesh.matrix_world.translation
    place_camera(cam, Vector((feet.x, feet.y, center_z)))
    window = bpy.context.window
    previous = window.scene
    window.scene = scene
    sheets, faces = {}, {}

    try:
        feet_view = world_to_camera_view(scene, cam, Vector((feet.x, feet.y, 0.0)))

        for name, action in sorted(actions.items()):
            rig.animation_data.action = action
            length = int(action["length"])
            images, track = [], []

            for frame in range(0, length if action["loop"] else length + 1):
                scene.frame_set(frame)
                depsgraph = bpy.context.evaluated_depsgraph_get()
                track.append(_face_sample(scene, cam, rig, local, depsgraph, cfg.get("head_size")))
                path = os.path.join(frames_dir, f"{prefix}_{name}_{frame:03d}.png")
                scene.render.filepath = path
                bpy.ops.render.render(write_still=True)
                images.append(path)

            sheets[name] = _strip(images, os.path.join(out_dir, f"{prefix}_{name}.png"))
            faces[name] = track
    finally:
        window.scene = previous

        for o in hidden:
            o.hide_render = False

    write_sheet_info(sheets, out_dir, prefix, size=size, ortho=ortho, feet_from_bottom=feet_view.y * ortho)

    write_face_json(out_dir, faces)

    return out_dir, {k: v["frames"] for k, v in sheets.items()}


# --------------------------------------------------------- Têtes illustrées
# Retour de l'auteur (2026-10-30) : le calque de visage est abandonné. La tête entière (visage, cheveux, capuche, haut
# du cou) est remplacée par une tête illustrée (tools/gen3d/heads.py), placée à chaque image sur l'os de la tête.
# Le corps est rendu avec la tête en « réserve » (holdout) : là où la tête 3D est devant, le sprite du corps est
# transparent, et la tête illustrée, dessinée DERRIÈRE le corps dans Unity, s'y voit. Un poing ou un bras qui passe
# devant la tête reste dans le sprite du corps, donc par-dessus la tête : l'ordre de profondeur est juste.

HEAD_REF = os.path.join(REPO, "ArtSource", "Blender", "Heads")
HEAD_ORTHO = 0.48  # cadre du rendu de référence de la tête (m) : 1024 px, soit 2133 px/m (tête de plus de 512 px)


def head_zone(obj, rig, threshold=0.5, neck_fraction=0.35):
    """Sommets de la tête et du haut du cou (poids de spine.006 + spine.005, au-dessus du milieu du cou)."""
    groups = {g.name: g.index for g in obj.vertex_groups}
    wanted = {groups.get(HEAD_BONE), groups.get("spine.005")}
    neck = (rig.matrix_world @ rig.data.bones["spine.005"].head_local).z
    head = (rig.matrix_world @ rig.data.bones[HEAD_BONE].head_local).z
    cut = neck + (head - neck) * neck_fraction
    zone = set()

    for v in obj.data.vertices:
        weight = sum(g.weight for g in v.groups if g.group in wanted)

        if weight > threshold and (obj.matrix_world @ v.co).z > cut:
            zone.add(v.index)

    return zone


def holdout_material(name="RM_Holdout"):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(out.inputs[0], nt.nodes.new("ShaderNodeHoldout").outputs[0])
    return mat


def set_head_holdout(character, enabled=True):
    """Faces de la tête en réserve (emplacement 2 ; leur coque de contour en 3, par le décalage du Solidify)."""
    cfg = CONFIG[character]
    obj = bpy.data.objects[cfg["mesh"]]
    rig = bpy.data.objects[cfg["rig"]]
    mats = obj.data.materials

    while len(mats) < 4:
        mats.append(None)

    mats[2] = holdout_material()
    mats[3] = holdout_material()
    zone = head_zone(obj, rig)
    count = 0

    for poly in obj.data.polygons:
        inside = all(i in zone for i in poly.vertices)
        poly.material_index = 2 if (enabled and inside) else 0
        count += inside

    obj.data.update()
    return count


def head_center(rig):
    pb = rig.pose.bones[HEAD_BONE]
    return rig.matrix_world @ pb.head.lerp(pb.tail, 0.5)


def _head_sample(scene, cam, rig):
    """Tête dans l'image courante : (u, v du milieu de l'os de la tête, angle d'inclinaison, 1, 1, retournée)."""
    pb = rig.pose.bones[HEAD_BONE]
    matrix = rig.matrix_world @ pb.matrix
    rest = (rig.matrix_world @ pb.bone.matrix_local).to_3x3()
    turn = matrix.to_3x3() @ rest.inverted()
    forward = (turn @ Vector((0, -1, 0))).normalized()
    up = (turn @ Vector((0, 0, 1))).normalized()
    right = (cam.matrix_world.to_3x3() @ Vector((1, 0, 0))).normalized()
    center = head_center(rig)
    p = world_to_camera_view(scene, cam, center)
    q = world_to_camera_view(scene, cam, center + up * 0.1)
    angle = math.degrees(math.atan2(q.y - p.y, q.x - p.x)) - 90.0
    # Tête nettement tournée vers l'arrière de l'image (demi-tour) : la tête illustrée est retournée.
    flipped = forward.dot(right) < -0.25
    return [round(p.x, 5), round(p.y, 5), round(angle, 2), 1.0, 1, int(flipped)]


def head_reference(character, size=1024):
    """Rendu de la tête seule sous l'angle des sprites, cadré sur son centre : profondeur (guide ControlNet) et
    aplats toon (contrôle). La tête illustrée reprend ce cadre : son pivot est le centre de l'image."""
    import bmesh

    cfg = CONFIG[character]
    mesh = bpy.data.objects[cfg["mesh"]]
    rig = bpy.data.objects[cfg["rig"]]
    os.makedirs(HEAD_REF, exist_ok=True)
    scene, cam = setup_render_scene(size=size, ortho=HEAD_ORTHO)
    scene.render.resolution_x = scene.render.resolution_y = size

    # Copie qui ne garde que la tête et le haut du cou (mêmes modificateurs : squelette, contour).
    zone = head_zone(mesh, rig)
    head = mesh.copy()
    head.data = mesh.data.copy()
    head.name = mesh.name + "_HeadRef"
    scene.collection.objects.link(head)
    bm = bmesh.new()
    bm.from_mesh(head.data)
    bm.verts.ensure_lookup_table()
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.index not in zone], context='VERTS')
    bm.to_mesh(head.data)
    bm.free()

    if rig.name not in scene.collection.objects:
        scene.collection.objects.link(rig)

    hidden = [o for o in scene.objects if o.type == 'MESH' and o != head and not o.hide_render]

    for o in hidden:
        o.hide_render = True

    # Pose de repos : tête droite, regard devant (dans la garde, elle est penchée vers le bas).
    rig.data.pose_position = 'REST'
    window = bpy.context.window
    previous = window.scene
    window.scene = scene
    paths = {}

    try:
        bpy.context.view_layer.update()
        center = head_center(rig)
        place_camera(cam, center, distance=4.0)
        distance = (cam.location - center).length

        # Carte des normales vues de la caméra (guide ControlNet « normal ») : la forme exacte de la tête.
        depth = bpy.data.materials.get("RM_Normal") or bpy.data.materials.new("RM_Normal")
        depth.use_nodes = True
        nt = depth.node_tree
        nt.nodes.clear()
        geometry = nt.nodes.new("ShaderNodeNewGeometry")
        to_camera = nt.nodes.new("ShaderNodeVectorTransform")
        to_camera.vector_type = 'NORMAL'
        to_camera.convert_from = 'WORLD'
        to_camera.convert_to = 'CAMERA'
        scale = nt.nodes.new("ShaderNodeVectorMath")
        scale.operation = 'MULTIPLY'
        scale.inputs[1].default_value = (0.5, 0.5, 0.5)
        shift = nt.nodes.new("ShaderNodeVectorMath")
        shift.operation = 'ADD'
        shift.inputs[1].default_value = (0.5, 0.5, 0.5)
        emit = nt.nodes.new("ShaderNodeEmission")
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        nt.links.new(to_camera.inputs[0], geometry.outputs["Normal"])
        nt.links.new(scale.inputs[0], to_camera.outputs[0])
        nt.links.new(shift.inputs[0], scale.outputs[0])
        nt.links.new(emit.inputs[0], shift.outputs[0])
        nt.links.new(out.inputs[0], emit.outputs[0])

        toon = list(head.data.materials[:2])
        outline = head.modifiers.get("Outline")

        for name, materials, shell in (("toon", toon, True), ("normal", [depth, depth], False)):
            for i, m in enumerate(materials):
                head.data.materials[i] = m

            for poly in head.data.polygons:
                poly.material_index = 0

            if outline is not None:
                outline.show_render = shell

            path = os.path.join(HEAD_REF, f"{character}_head_ref_{name}.png")
            scene.render.filepath = path
            bpy.ops.render.render(write_still=True)
            paths[name] = path

        sample = _head_sample(scene, cam, rig)
        info = {"character": character, "pose": "rest", "size": size, "ortho": HEAD_ORTHO,
                "pixels_per_meter": size / HEAD_ORTHO, "angle": sample[2], "pivot": [0.5, 0.5]}

        with open(os.path.join(HEAD_REF, f"{character}_head_ref.json"), "w", encoding="utf-8") as f:
            json.dump(info, f, indent=2)
    finally:
        window.scene = previous
        rig.data.pose_position = 'POSE'

        for o in hidden:
            o.hide_render = False

        data = head.data
        bpy.data.objects.remove(head)
        bpy.data.meshes.remove(data)

    return paths


def track_heads(character, size=SIZE, ortho=ORTHO, center_z=1.0):
    """Piste de la tête (heads.json, même format que face.json) : centre de l'os de la tête, inclinaison, retournement."""
    cfg = CONFIG[character]
    mesh = bpy.data.objects[cfg["mesh"]]
    rig = bpy.data.objects[cfg["rig"]]
    prefix = cfg["prefix"]
    scene, cam = setup_render_scene(size=size, ortho=ortho, center_z=center_z)
    out_dir = os.path.join(CHARACTERS, character, "Sprites25D")
    feet = mesh.matrix_world.translation
    place_camera(cam, Vector((feet.x, feet.y, center_z)))
    tracks = {}

    for action in [a for a in bpy.data.actions if a.name.startswith(prefix + "_")]:
        rig.animation_data.action = action
        length = int(action["length"])
        track = []

        for frame in range(0, length if action["loop"] else length + 1):
            scene.frame_set(frame)
            track.append(_head_sample(scene, cam, rig))

        tracks[action.name[len(prefix) + 1:]] = track

    clips = [{"name": n, "data": [x for f in t for x in f]} for n, t in sorted(tracks.items())]

    with open(os.path.join(out_dir, "heads.json"), "w", encoding="utf-8") as f:
        json.dump({"values_per_frame": 6, "clips": clips}, f)

    return {k: len(v) for k, v in tracks.items()}
