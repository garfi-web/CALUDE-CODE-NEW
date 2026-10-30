"""RED MET — essai de direction artistique « roman graphique » (demande de l'auteur, 2026-10-30, esprit The Darkness II).

Tout le personnage vient d'un seul modèle 3D rendu en style BD, sans élément 2D collé :
- aplats sourds, ombres noires franches (seuil, aucun dégradé) ;
- hachures dans les zones d'ombre (simples dans la pénombre, croisées plus bas), tracées en coordonnées d'écran ;
- contour de silhouette à l'encre en coque inversée, épaisseur variable (bruit : aspect pinceau) ;
- traits intérieurs fins calculés sur une passe de normales (tools/blender/redmet_hand_hbm.py, ink) ;
- grain de papier et légère désaturation au montage (tools/gen3d/bd_post.py).

    exec(open(r"<dépôt>/tools/blender/redmet_bd.py", encoding="utf-8").read())
"""
import math

import bpy
from mathutils import Vector

INK = (0.02, 0.018, 0.02)
PAPER = (0.86, 0.82, 0.74)


def bd_material(name, color=None, image=None, desaturate=0.55, hatch_scale=170.0):
    """Matériau BD : lumière = couleur sourde ; pénombre = hachures simples ; ombre = hachures croisées ; ombre
    profonde = noir. Les hachures suivent l'écran (comme un dessin), pas la surface."""
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    n, link = nt.nodes.new, nt.links.new

    diffuse = n("ShaderNodeBsdfDiffuse")
    to_rgb = n("ShaderNodeShaderToRGB")
    link(to_rgb.inputs[0], diffuse.outputs[0])
    value = n("ShaderNodeRGBToBW")
    link(value.inputs[0], to_rgb.outputs[0])

    # Couleur de base (aplat ou texture), désaturée vers une palette sourde.
    if image is not None:
        uv = n("ShaderNodeUVMap")
        uv.uv_map = "UVMap"
        tex = n("ShaderNodeTexImage")
        tex.image = image
        link(tex.inputs[0], uv.outputs[0])
        base = tex.outputs[0]
    else:
        rgb = n("ShaderNodeRGB")
        rgb.outputs[0].default_value = (*color, 1)
        base = rgb.outputs[0]

    hsv = n("ShaderNodeHueSaturation")
    hsv.inputs["Saturation"].default_value = 1.0 - desaturate
    link(hsv.inputs["Color"], base)

    # Hachures : bandes fines sur les coordonnées d'écran, tournées de +45° et -45°.
    coords = n("ShaderNodeTexCoord")

    def hatch(angle):
        mapping = n("ShaderNodeMapping")
        mapping.inputs["Rotation"].default_value = (0, 0, math.radians(angle))
        mapping.inputs["Scale"].default_value = (hatch_scale * 16 / 9, hatch_scale, 1)
        link(mapping.inputs[0], coords.outputs["Window"])
        sep = n("ShaderNodeSeparateXYZ")
        link(sep.inputs[0], mapping.outputs[0])
        frac = n("ShaderNodeMath")
        frac.operation = 'FRACT'
        link(frac.inputs[0], sep.outputs[0])
        line = n("ShaderNodeMath")
        line.operation = 'LESS_THAN'          # 1 sur le trait (28 % de la période)
        line.inputs[1].default_value = 0.28
        link(line.inputs[0], frac.outputs[0])
        return line.outputs[0]

    h1, h2 = hatch(45), hatch(-45)

    def below(threshold):
        m = n("ShaderNodeMath")
        m.operation = 'LESS_THAN'
        m.inputs[1].default_value = threshold
        link(m.inputs[0], value.outputs[0])
        return m.outputs[0]

    def mul(a, b):
        m = n("ShaderNodeMath")
        m.operation = 'MULTIPLY'
        link(m.inputs[0], a)
        link(m.inputs[1], b)
        return m.outputs[0]

    def maximum(a, b):
        m = n("ShaderNodeMath")
        m.operation = 'MAXIMUM'
        link(m.inputs[0], a)
        link(m.inputs[1], b)
        return m.outputs[0]

    # Encre = ombre profonde, ou hachure 1 dans la pénombre, ou hachure 2 en plus dans l'ombre.
    ink = maximum(below(0.2), maximum(mul(below(0.62), h1), mul(below(0.42), h2)))
    mix = n("ShaderNodeMix")
    mix.data_type = 'RGBA'
    link(mix.inputs[0], ink)
    link(mix.inputs[6], hsv.outputs[0])
    mix.inputs[7].default_value = (*INK, 1)
    emission = n("ShaderNodeEmission")
    link(emission.inputs[0], mix.outputs[2])
    out = n("ShaderNodeOutputMaterial")
    link(out.inputs[0], emission.outputs[0])
    return mat


def ink_material():
    mat = bpy.data.materials.get("RM_BD_Encre") or bpy.data.materials.new("RM_BD_Encre")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs[0].default_value = (*INK, 1)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(out.inputs[0], em.outputs[0])
    mat.use_backface_culling = True
    return mat


def brush_outline(obj, thickness=0.011, seed=3):
    """Contour de silhouette en coque inversée, épaisseur variable : groupe de sommets bruité (trait de pinceau)."""
    import random

    rnd = random.Random(seed)
    group = obj.vertex_groups.get("BD_Pinceau") or obj.vertex_groups.new(name="BD_Pinceau")
    mw = obj.matrix_world

    for v in obj.data.vertices:
        p = mw @ v.co
        w = 0.55 + 0.45 * math.sin(p.x * 37 + p.z * 23 + rnd.random() * 0.6) * math.sin(p.y * 29 + p.z * 41)
        group.add([v.index], max(0.25, min(1.0, w + 0.25)), 'REPLACE')

    mats = obj.data.materials
    ink = ink_material()

    if ink.name not in [m.name for m in mats if m]:
        mats.append(ink)

    for m in list(obj.modifiers):
        if m.type == 'SOLIDIFY' and m.name in ("Outline", "BD_Contour"):
            m.show_render = m.show_viewport = m.name == "BD_Contour"

    line = obj.modifiers.get("BD_Contour") or obj.modifiers.new("BD_Contour", 'SOLIDIFY')
    line.thickness = thickness
    line.offset = 1.0
    line.use_flip_normals = True
    line.use_rim = False
    line.vertex_group = group.name
    line.thickness_vertex_group = 0.3
    line.material_offset = [m.name if m else "" for m in mats].index(ink.name)
    return line


def restore_outline(obj):
    for m in obj.modifiers:
        if m.name == "Outline":
            m.show_render = m.show_viewport = True
        if m.name == "BD_Contour":
            m.show_render = m.show_viewport = False


# ------------------------------------------------------------------ Nathan : cheveux et visage à l'encre

# Repères du visage de Nathan M0 (MPFB réglé, mesurés le 2026-10-30) : yeux (aides MPFB), bouche (groupe « lips »).
FACE = {"eye_z": 1.561, "eye_x": 0.031, "mouth_z": 1.507, "nose_z": 1.55, "head_c": (0.0, -0.035, 1.615), "head_r": 0.1}


def rest_object(body, name="BD_Repos_tmp"):
    """Copie au repos (cibles de morphologie appliquées, sans modificateur) avec les UV : sert à peindre la texture."""
    states = {m.name: m.show_viewport for m in body.modifiers}

    for m in body.modifiers:   # seule la géométrie d'aide reste masquée (yeux et cubes de MPFB arrêtaient les rayons)
        m.show_viewport = m.name == "Hide helpers"

    dg = bpy.context.evaluated_depsgraph_get()
    mesh = bpy.data.meshes.new_from_object(body.evaluated_get(dg), depsgraph=dg)

    for m in body.modifiers:
        m.show_viewport = states[m.name]

    old = bpy.data.objects.get(name)

    if old is not None:
        bpy.data.objects.remove(old)

    obj = bpy.data.objects.new(name, mesh)
    obj.matrix_world = body.matrix_world
    bpy.context.scene.collection.objects.link(obj)
    return obj


def make_hair(character, seed=11):
    """Masse de boucles brunes en 3D : petites sphères semées sur le crâne, fondues (voxels) puis bosselées."""
    import random

    import bmesh
    from mathutils import Matrix

    rnd = random.Random(seed)
    body = bpy.data.objects[f"{character}_v3_Corps"]
    rig = bpy.data.objects[f"{character}_v3_Rig"]
    name = f"{character}_v3_Cheveux"

    if name in bpy.data.objects:
        bpy.data.objects.remove(bpy.data.objects[name])

    origin = body.matrix_world.translation
    c = origin + Vector(FACE["head_c"])
    r = FACE["head_r"]
    bm = bmesh.new()

    for _ in range(260):
        d = Vector((rnd.gauss(0, 1), rnd.gauss(0, 1), rnd.gauss(0, 1))).normalized()
        p = c + d * (r * 1.02)
        front = -d.y   # 1 devant (visage), -1 derrière
        side = abs(d.x)
        # Limite basse : frange au-dessus des sourcils devant, au-dessus des oreilles sur les côtés, nuque derrière.
        limit = 1.665 if front > 0.55 else (1.6 if front > 0 and side > 0.5 else (1.585 if front > -0.3 else 1.53))

        if p.z < origin.z + limit:
            continue

        size = rnd.uniform(0.022, 0.034)
        bmesh.ops.create_icosphere(bm, subdivisions=2, radius=size, matrix=Matrix.Translation(p + d * size * 0.4))

    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.data.collections[f"{character}_v3"].objects.link(obj)
    remesh = obj.modifiers.new("Fusion", 'REMESH')
    remesh.mode = 'VOXEL'
    remesh.voxel_size = 0.004
    dg = bpy.context.evaluated_depsgraph_get()
    fused = bpy.data.meshes.new_from_object(obj.evaluated_get(dg))
    obj.modifiers.remove(remesh)
    old, obj.data = obj.data, fused
    bpy.data.meshes.remove(old)

    for p in obj.data.polygons:
        p.use_smooth = True

    smooth = obj.modifiers.new("Lisse", 'SMOOTH')
    smooth.factor, smooth.iterations = 0.6, 3
    group = obj.vertex_groups.new(name="head")
    group.add(range(len(obj.data.vertices)), 1.0, 'REPLACE')
    arm = obj.modifiers.new("Armature", 'ARMATURE')
    arm.object = rig
    return obj


def paint_face(character, folder, size=4096):
    """Visage de Nathan peint à l'encre sur la texture de peau du modèle (concept R0) : sourcils épais, yeux en
    amande avec iris, lunettes à monture noire rectangulaire et ligne de double foyer, nez, bouche, double menton,
    acné sur les joues et le front, barbe naissante."""
    import random

    import bmesh  # noqa: F401  (Painter)

    rnd = random.Random(5)
    body = bpy.data.objects[f"{character}_v3_Corps"]
    rest = rest_object(body)
    p = Painter(rest, srgb(SKIN), size=size, scale=1.0)
    dg = bpy.context.evaluated_depsgraph_get()
    inv = rest.matrix_world.inverted()
    ox, oz = body.matrix_world.translation.x, body.matrix_world.translation.z

    def at(x, z, inset=0.0):
        hit, loc, _, _ = rest.ray_cast(inv @ Vector((ox + x, -1.0, oz + z)), inv.to_3x3() @ Vector((0, 1, 0)))
        return (rest.matrix_world @ loc) + Vector((0, inset, 0)) if hit else None

    def line(pts, width, color=(0.02, 0.02, 0.03), taper=True):
        p.stroke([at(x, z) for x, z in pts], width, color, taper=taper)

    ink = (0.03, 0.025, 0.03)
    ez, ex, mz = FACE["eye_z"], FACE["eye_x"], FACE["mouth_z"]

    for s in (1, -1):
        # Sourcils épais, un peu froncés (mal à l'aise).
        line([(s * 0.012, ez + 0.022), (s * 0.03, ez + 0.027), (s * 0.05, ez + 0.022)], 26, ink)
        # Œil : paupière supérieure épaisse, inférieure fine, iris plein.
        line([(s * (ex - 0.014), ez + 0.001), (s * ex, ez + 0.007), (s * (ex + 0.015), ez + 0.002)], 16, ink)
        line([(s * (ex - 0.011), ez - 0.004), (s * (ex + 0.012), ez - 0.003)], 6, ink)
        line([(s * ex, ez + 0.004), (s * ex, ez - 0.002)], 22, ink, taper=False)
        # Lunettes : monture rectangulaire épaisse, ligne du double foyer.
        x0, x1, z0, z1 = s * (ex - 0.022), s * (ex + 0.024), ez - 0.017, ez + 0.014
        line([(x0, z1), (x1, z1), (x1, z0), (x0, z0), (x0, z1)], 14, ink, taper=False)
        line([(x0, ez - 0.006), (x1, ez - 0.006)], 5, ink, taper=False)
        line([(s * (ex + 0.024), ez + 0.008), (s * 0.068, ez + 0.006)], 12, ink, taper=False)   # branche
        # Joue ronde : pli sous la pommette.
        line([(s * 0.035, mz + 0.032), (s * 0.05, mz + 0.012), (s * 0.048, mz - 0.006)], 7, ink)
        # Acné : petits points rouge sombre sur la joue.
        for _ in range(7):
            q = at(s * rnd.uniform(0.03, 0.055), rnd.uniform(mz + 0.0, mz + 0.04))
            if q:
                p.stroke([q, q + Vector((0, 0, 0.0008))], rnd.uniform(9, 14), (0.45, 0.08, 0.07), taper=False, samples=2)

    line([(-0.008, ez - 0.0), (-0.004, ez - 0.002)], 1, ink)
    # Pont des lunettes, nez, narines.
    line([(-0.009, ez + 0.006), (0.009, ez + 0.006)], 12, ink, taper=False)
    line([(0.006, FACE["nose_z"] + 0.02), (0.012, FACE["nose_z"] - 0.002), (0.004, FACE["nose_z"] - 0.012)], 8, ink)
    line([(-0.012, FACE["nose_z"] - 0.013), (-0.006, FACE["nose_z"] - 0.016)], 8, ink)
    # Bouche fermée, un peu de travers ; double menton.
    line([(-0.018, mz + 0.001), (0.0, mz - 0.001), (0.019, mz + 0.003)], 9, ink)
    line([(-0.028, mz - 0.042), (0.0, mz - 0.05), (0.028, mz - 0.041)], 8, ink)

    # Acné sur le front, barbe naissante (pointillé) sur la mâchoire.
    for _ in range(6):
        q = at(rnd.uniform(-0.04, 0.04), rnd.uniform(ez + 0.035, ez + 0.05))
        if q:
            p.stroke([q, q + Vector((0, 0, 0.0008))], rnd.uniform(9, 13), (0.45, 0.08, 0.07), taper=False, samples=2)

    for _ in range(140):
        x = rnd.uniform(-0.05, 0.05)
        q = at(x, rnd.uniform(mz - 0.035, mz - 0.01) + abs(x) * 0.5)
        if q:
            p.stroke([q, q + Vector((0, 0, 0.0005))], rnd.uniform(3, 5), (0.25, 0.2, 0.2), taper=False, samples=2)

    image = p.save("T_Peau_BD_" + character, folder)
    bpy.data.objects.remove(rest)
    return image


def make_eyes(character, depth=-0.128, radius=0.0125):
    """Globes oculaires (les aides de MPFB sont masquées) : blanc cassé, iris brun foncé tourné vers l'avant."""
    import bmesh
    from mathutils import Matrix

    body = bpy.data.objects[f"{character}_v3_Corps"]
    rig = bpy.data.objects[f"{character}_v3_Rig"]
    o = body.matrix_world.translation
    name = f"{character}_v3_Yeux"

    if name in bpy.data.objects:
        bpy.data.objects.remove(bpy.data.objects[name])

    bm = bmesh.new()

    for s in (1, -1):
        c = o + Vector((s * FACE["eye_x"], depth, FACE["eye_z"]))
        bmesh.ops.create_uvsphere(bm, u_segments=24, v_segments=16, radius=radius, matrix=Matrix.Translation(c))

    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.data.collections[f"{character}_v3"].objects.link(obj)

    for p in mesh.polygons:
        p.use_smooth = True

    # Iris : là où la normale regarde droit devant (-Y).
    mat = bpy.data.materials.get("RM_BD_Oeil") or bpy.data.materials.new("RM_BD_Oeil")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    dot = nt.nodes.new("ShaderNodeVectorMath")
    dot.operation = 'DOT_PRODUCT'
    dot.inputs[1].default_value = (0, -1, 0)
    nt.links.new(dot.inputs[0], geo.outputs["Normal"])
    iris = nt.nodes.new("ShaderNodeMath")
    iris.operation = 'GREATER_THAN'
    iris.inputs[1].default_value = 0.86
    nt.links.new(iris.inputs[0], dot.outputs["Value"])
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = 'RGBA'
    nt.links.new(mix.inputs[0], iris.outputs[0])
    mix.inputs[6].default_value = (0.78, 0.75, 0.68, 1)
    mix.inputs[7].default_value = (*INK, 1)
    em = nt.nodes.new("ShaderNodeEmission")
    nt.links.new(em.inputs[0], mix.outputs[2])
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(out.inputs[0], em.outputs[0])
    mesh.materials.append(mat)
    group = obj.vertex_groups.new(name="head")
    group.add(range(len(mesh.vertices)), 1.0, 'REPLACE')
    obj.modifiers.new("Armature", 'ARMATURE').object = rig
    return obj


def apply_bd(character):
    """Matériaux BD sur tout le personnage refait (corps, vêtements, poings, cheveux) + contour pinceau."""
    coll = bpy.data.collections[f"{character}_v3"]
    folder = bpy.path.abspath("//Textures/Nathan_M0_v3")
    textures = {"RM_Hoodie": "T_Hoodie_" + character, "RM_Cargo": "T_Cargo_" + character, "RM_Basket": "T_Basket_" + character}

    for obj in coll.objects:
        if obj.type != 'MESH' or obj.name.endswith(("_Yeux",)) or obj.hide_render:
            continue

        for i, slot in enumerate(obj.material_slots):
            m = slot.material

            if m is None or m.name.startswith(("RM_Outline", "RM_BD")):
                continue

            key = next((k for k in textures if m.name.startswith(k)), None)
            image = bpy.data.images.get(textures[key]) if key else None
            rgb = next((n.outputs[0].default_value[:3] for n in m.node_tree.nodes if n.type == 'RGB'), (0.5, 0.5, 0.5))
            obj.material_slots[i].material = bd_material(f"RM_BD_{m.name}", color=rgb, image=image)

        brush_outline(obj, 0.011 if obj.name.endswith(("_Corps", "_Hoodie", "_Pantalon", "_Cheveux")) else 0.006)


# ------------------------------------------------------------------ Rendu au cadrage des sprites

def render_bd(objects, feet, out_png, size=1024, ortho=2.6, center_z=1.0):
    """Rendu BD d'un groupe d'objets au cadrage des sprites (3/4 18°/30°, 1024 px pour 2,6 m) : passe couleur (soleil
    seul, ombres portées nettes) puis passe de normales pour les traits intérieurs (ink de redmet_hand_hbm.py)."""
    scene = bpy.context.scene
    layer = bpy.context.view_layer
    keep = set(objects)
    hidden = {o: o.hide_render for o in scene.objects}

    for o in scene.objects:
        if o not in keep and o.type != 'LIGHT':
            o.hide_render = True

    cam_data = bpy.data.cameras.get("RM_BD_Cam") or bpy.data.cameras.new("RM_BD_Cam")
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = ortho
    cam = bpy.data.objects.get("RM_BD_Cam") or bpy.data.objects.new("RM_BD_Cam", cam_data)

    if cam.name not in scene.collection.objects:
        scene.collection.objects.link(cam)

    e, y = math.radians(18.0), math.radians(30.0)
    d = Vector((math.cos(y), math.sin(y), 0))
    target = Vector((feet.x, feet.y, center_z))
    cam.location = target - d * 6 * math.cos(e) + Vector((0, 0, 6 * math.sin(e)))
    cam.rotation_euler = (math.radians(72), 0, math.radians(-60))
    sun = bpy.data.objects["RM_Still_Sun"]
    bg = next(n for n in scene.world.node_tree.nodes if n.type == 'BACKGROUND')
    saved = (scene.camera, scene.render.resolution_x, scene.render.resolution_y, scene.render.film_transparent,
             scene.render.filepath, bg.inputs[1].default_value, scene.view_settings.view_transform,
             sun.rotation_euler.copy(), sun.data.use_shadow, sun.data.energy)
    bg.inputs[1].default_value = 0.0
    scene.view_settings.view_transform = 'Standard'
    scene.camera = cam
    scene.render.resolution_x = scene.render.resolution_y = size
    scene.render.film_transparent = True
    # Lumière dure de côté et d'en haut (source à droite de l'image, un peu vers la caméra) : une moitié éclairée, l'autre
    # dans l'ombre (noir et hachures), façon roman graphique.
    side = Vector((-math.sin(y), math.cos(y), 0))
    source = -side * 0.7 - d * 0.6 + Vector((0, 0, 0.9))   # côté du visage (le personnage regarde vers la droite)
    sun.rotation_euler = (-source).to_track_quat('-Z', 'Y').to_euler()
    sun.data.use_shadow = True
    sun.data.energy = 3.0
    scene.render.filepath = out_png
    bpy.ops.render.render(write_still=True)

    # Passe de normales (sans contour) pour les traits intérieurs.
    lines = [m for o in objects if o.type == 'MESH' for m in o.modifiers if m.name == "BD_Contour"]

    for m in lines:
        m.show_render = False

    layer.material_override = normal_material()
    fmt = (scene.render.image_settings.file_format, scene.render.image_settings.color_depth)
    scene.render.image_settings.file_format = 'OPEN_EXR'
    scene.render.image_settings.color_depth = '32'
    normals = out_png.replace(".png", "_n.exr")
    scene.render.filepath = normals
    bpy.ops.render.render(write_still=True)
    layer.material_override = None
    scene.render.image_settings.file_format, scene.render.image_settings.color_depth = fmt

    for m in lines:
        m.show_render = True

    (scene.camera, scene.render.resolution_x, scene.render.resolution_y, scene.render.film_transparent,
     scene.render.filepath, bg.inputs[1].default_value, scene.view_settings.view_transform,
     sun.rotation_euler, sun.data.use_shadow, sun.data.energy) = saved

    for o, h in hidden.items():
        o.hide_render = h

    ink(out_png, normals, out_png, angle=38, width=1, ink_rgb=INK)
    return out_png
