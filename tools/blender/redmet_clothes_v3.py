"""RED MET — vêtements modélisés des personnages 3D refaits (étape 2, 2026-10-30).

Chaque vêtement part d'une copie du corps MPFB au repos (mêmes sommets, donc mêmes poids du squelette) :
on garde la zone couverte, on coupe proprement aux poignets, à la taille et aux chevilles, on lisse les détails du
corps (nombril, clavicules), puis on gonfle vers l'extérieur (tissu ample). Les pièces ajoutées (capuche, cordons,
poches) reçoivent les poids de l'os qui les porte.

    exec(open(r"<dépôt>/tools/blender/redmet_characters_v3.py", encoding="utf-8").read())
    exec(open(r"<dépôt>/tools/blender/redmet_clothes_v3.py", encoding="utf-8").read())
    dress("Nathan_M0")
"""
import math

import bmesh
import bpy
from mathutils import Vector

# Bas du hoodie, haut du pantalon, bas du pantalon et haut des chaussures, en mètres au-dessus du sol.
HOODIE_HEM = 0.90
NECK_LINE = 1.45
PANTS_TOP = 1.00
PANTS_HEM = 0.07
SHOE_TOP = 0.115

ARM_BONES = ("clavicle", "upperarm", "lowerarm")
HAND_BONES = ("hand", "thumb", "index", "middle", "ring", "pinky")


def rest_copy(character, name):
    """Copie du corps au repos (cibles de morphologie appliquées, géométrie d'aide retirée), avec ses groupes."""
    body = bpy.data.objects[f"{character}_v3_Corps"]
    rig = bpy.data.objects[f"{character}_v3_Rig"]
    old = bpy.data.objects.get(name)

    if old is not None:
        bpy.data.objects.remove(old)

    states = {m.name: m.show_viewport for m in body.modifiers}

    for m in body.modifiers:
        m.show_viewport = m.type == 'MASK'

    dg = bpy.context.evaluated_depsgraph_get()
    mesh = bpy.data.meshes.new_from_object(body.evaluated_get(dg), preserve_all_data_layers=True, depsgraph=dg)

    for m in body.modifiers:
        m.show_viewport = states[m.name]

    mesh.name = name
    obj = bpy.data.objects.new(name, mesh)
    obj.matrix_world = body.matrix_world
    body.users_collection[0].objects.link(obj)

    for g in body.vertex_groups:
        obj.vertex_groups.new(name=g.name)

    mesh.materials.clear()
    return obj, body, rig


def weights(obj, bm, prefixes):
    """Somme des poids des groupes dont le nom commence par un des préfixes, par sommet."""
    layer = bm.verts.layers.deform.active
    ids = [g.index for g in obj.vertex_groups if g.name.startswith(prefixes)]
    return {v: sum(v[layer].get(i, 0.0) for i in ids) for v in bm.verts}


def cut(bm, co, no):
    """Coupe le maillage le long d'un plan (arêtes nettes au bord du vêtement)."""
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=no, dist=1e-5)


def bone_line(rig, name):
    bone = rig.data.bones[name]
    return rig.matrix_world @ bone.head_local, rig.matrix_world @ bone.tail_local


def closest_on_segment(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / ab.length_squared))
    return a + ab * t, t


def smooth(bm, verts, factor=0.5, repeat=8):
    for _ in range(repeat):
        bmesh.ops.smooth_vert(bm, verts=verts, factor=factor, use_axis_x=True, use_axis_y=True, use_axis_z=True)


def inflate(bm, offset_of):
    """Pousse chaque sommet le long de sa normale, de la distance donnée par offset_of(v)."""
    bm.normal_update()
    moves = [(v, v.normal.copy() * offset_of(v)) for v in bm.verts]

    for v, d in moves:
        v.co += d


def widen(bm, obj, rig, bones, radius):
    """Manches et jambes amples : chaque sommet est éloigné de l'axe de son os jusqu'à un rayon minimal."""
    mw, inv = obj.matrix_world, obj.matrix_world.inverted()
    layer = bm.verts.layers.deform.active
    index = {g.name: g.index for g in obj.vertex_groups}

    for v in bm.verts:
        best, bone = 0.0, None

        for name in bones:
            w = v[layer].get(index.get(name, -1), 0.0)

            if w > best:
                best, bone = w, name

        if bone is None or best < 0.3:
            continue

        a, b = bone_line(rig, bone)
        p = mw @ v.co
        c, t = closest_on_segment(p, a, b)
        d = p - c
        r = radius(bone, t)

        if d.length < r and d.length > 1e-6:
            v.co = inv @ (c + d.normalized() * (d.length + (r - d.length) * best))


def flat_material(name, color):
    """Aplat de couleur en 4 tons nets (ombre portée, ombre, demi-teinte, lumière) ; la texture des plis vient après."""
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    n = nt.nodes.new
    diffuse = n("ShaderNodeBsdfDiffuse")
    to_rgb = n("ShaderNodeShaderToRGB")
    ramp = n("ShaderNodeValToRGB")
    ramp.color_ramp.interpolation = 'CONSTANT'

    while len(ramp.color_ramp.elements) < 4:
        ramp.color_ramp.elements.new(1.0)

    for element, position, tone in zip(ramp.color_ramp.elements, (0.0, 0.05, 0.28, 0.6), (0.42, 0.6, 0.8, 1.0)):
        element.position = position
        element.color = (tone * 0.95, tone * 0.93, min(1.0, tone * 1.06), 1)

    rgb = n("ShaderNodeRGB")
    rgb.outputs[0].default_value = (*color, 1)
    mix = n("ShaderNodeMix"); mix.data_type = 'RGBA'; mix.blend_type = 'MULTIPLY'; mix.inputs[0].default_value = 1.0
    emission = n("ShaderNodeEmission")
    out = n("ShaderNodeOutputMaterial")
    link = nt.links.new
    link(to_rgb.inputs[0], diffuse.outputs[0])
    link(ramp.inputs[0], to_rgb.outputs[0])
    link(mix.inputs[6], rgb.outputs[0])
    link(mix.inputs[7], ramp.outputs[0])
    link(emission.inputs[0], mix.outputs[2])
    link(out.inputs[0], emission.outputs[0])
    return mat


def keep_largest(bm):
    """Supprime les petits morceaux détachés laissés par les coupes."""
    seen, parts = set(), []

    for v in bm.verts:
        if v in seen:
            continue

        stack, part = [v], []
        seen.add(v)

        while stack:
            x = stack.pop()
            part.append(x)

            for e in x.link_edges:
                y = e.other_vert(x)

                if y not in seen:
                    seen.add(y)
                    stack.append(y)

        parts.append(part)

    parts.sort(key=len, reverse=True)
    bmesh.ops.delete(bm, geom=[v for p in parts[1:] for v in p], context='VERTS')


def bind(obj, rig, bone=None):
    """Squelette sur le vêtement ; une pièce ajoutée prend tout le poids de l'os qui la porte."""
    if bone is not None:
        group = obj.vertex_groups.get(bone) or obj.vertex_groups.new(name=bone)
        group.add(range(len(obj.data.vertices)), 1.0, 'REPLACE')

    mod = obj.modifiers.new("Armature", 'ARMATURE')
    mod.object = rig
    return mod


def outline(obj, thickness=0.006):
    """Contour (coque inversée), comme le corps."""
    mat = bpy.data.materials.get("RM_Outline_Clay")
    obj.data.materials.append(mat)
    mod = obj.modifiers.new("Outline", 'SOLIDIFY')
    mod.thickness = thickness
    mod.offset = 1.0
    mod.use_flip_normals = True
    mod.use_rim = False
    mod.material_offset = len(obj.data.materials) - 1
    return mod


def make_hoodie(character):
    """Hoodie noir trop grand du concept R0 : corps et manches longues, bord-côte en bas et aux poignets, capuche
    baissée en bourrelet derrière la nuque, deux cordons blancs."""
    obj, body, rig = rest_copy(character, f"{character}_v3_Hoodie")
    mw, inv = obj.matrix_world, obj.matrix_world.inverted()
    cx = body.matrix_world.translation.x
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    wrists = []

    for s in "lr":
        a, b = bone_line(rig, f"lowerarm_{s}")
        d = (b - a).normalized()
        wrists.append((b - d * 0.015, d, b))

    cut(bm, inv @ Vector((cx, 0, HOODIE_HEM)), Vector((0, 0, 1)))
    cut(bm, inv @ Vector((cx, 0, NECK_LINE)), Vector((0, 0, 1)))

    for co, d, _ in wrists:
        cut(bm, inv @ co, d)

    kill = []

    for v in bm.verts:
        p = mw @ v.co

        if p.z < HOODIE_HEM - 1e-4 or (abs(p.x - cx) < 0.2 and p.z > NECK_LINE + 1e-4):
            kill.append(v)
        elif any((p - co).dot(d) > 1e-4 and (p - b).length < 0.3 for co, d, b in wrists):
            kill.append(v)

    bmesh.ops.delete(bm, geom=kill, context='VERTS')
    keep_largest(bm)

    # Les détails du corps disparaissent sous le tissu ; les bords coupés restent en place.
    inner = [v for v in bm.verts if not v.is_boundary]
    smooth(bm, inner, 0.5, 10)

    def sleeve(bone, t):
        if bone.startswith("upperarm"):
            return 0.078 - 0.006 * t
        return 0.072 - 0.022 * max(0.0, (t - 0.75) / 0.25)   # resserré au poignet (bord-côte)

    widen(bm, obj, rig, [f"{b}_{s}" for b in ("upperarm", "lowerarm") for s in "lr"], sleeve)

    def offset(v):
        z = (mw @ v.co).z
        hem = max(0.0, 1.0 - (z - HOODIE_HEM) / 0.06)           # bord-côte du bas plus près du corps
        return 0.03 - 0.012 * hem

    inflate(bm, offset)
    smooth(bm, [v for v in bm.verts if not v.is_boundary], 0.4, 4)
    bm.to_mesh(obj.data)
    bm.free()

    black = flat_material("RM_Hoodie", (0.05, 0.055, 0.08))
    rib = flat_material("RM_Hoodie_Rib", (0.035, 0.04, 0.06))
    obj.data.materials.append(black)
    obj.data.materials.append(rib)

    for poly in obj.data.polygons:
        p = mw @ poly.center
        cuff = any((p - co).dot(d) > -0.06 and (p - b).length < 0.15 for co, d, b in wrists)
        poly.material_index = 1 if p.z < HOODIE_HEM + 0.055 or cuff else 0
        poly.use_smooth = True

    cloth = obj.modifiers.new("Epaisseur", 'SOLIDIFY')
    cloth.thickness = 0.008
    cloth.offset = -1.0
    bind(obj, rig)
    obj.modifiers.new("Lisse", 'SUBSURF').levels = 1
    outline(obj)
    return obj


def region(character, name, bottom, top, keep):
    """Copie du corps coupée entre deux hauteurs ; keep(p, v) décide des sommets gardés dans la tranche."""
    obj, body, rig = rest_copy(character, name)
    mw, inv = obj.matrix_world, obj.matrix_world.inverted()
    cx = body.matrix_world.translation.x
    bm = bmesh.new()
    bm.from_mesh(obj.data)

    if bottom is not None:
        cut(bm, inv @ Vector((cx, 0, bottom)), Vector((0, 0, 1)))

    cut(bm, inv @ Vector((cx, 0, top)), Vector((0, 0, 1)))
    arms = weights(obj, bm, tuple(b + "_" for b in ARM_BONES + HAND_BONES))
    kill = []

    for v in bm.verts:
        p = mw @ v.co

        if (bottom is not None and p.z < bottom - 1e-4) or p.z > top + 1e-4 or arms[v] > 0.5 or not keep(p - Vector((cx, 0, 0)), v):
            kill.append(v)

    bmesh.ops.delete(bm, geom=kill, context='VERTS')
    return obj, body, rig, bm


def make_pants(character):
    """Cargo gris ample : jambes droites et larges, poches à rabat sur le côté des cuisses."""
    obj, body, rig, bm = region(character, f"{character}_v3_Pantalon", PANTS_HEM, PANTS_TOP, lambda p, v: True)
    mw = obj.matrix_world
    keep_largest(bm)
    smooth(bm, [v for v in bm.verts if not v.is_boundary], 0.5, 8)

    def leg(bone, t):
        return 0.092 - 0.012 * t if bone.startswith("thigh") else 0.08

    widen(bm, obj, rig, [f"{b}_{s}" for b in ("thigh", "calf") for s in "lr"], leg)
    inflate(bm, lambda v: 0.02)
    smooth(bm, [v for v in bm.verts if not v.is_boundary], 0.4, 4)
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.materials.append(flat_material("RM_Cargo", (0.47, 0.46, 0.44)))

    for poly in obj.data.polygons:
        poly.use_smooth = True

    cloth = obj.modifiers.new("Epaisseur", 'SOLIDIFY')
    cloth.thickness = 0.008
    cloth.offset = -1.0
    bind(obj, rig)
    obj.modifiers.new("Lisse", 'SUBSURF').levels = 1
    outline(obj)
    pockets = [cargo_pocket(character, rig, obj, s) for s in "lr"]
    return obj, pockets


def cargo_pocket(character, rig, pants, side):
    """Poche à soufflet sur le côté de la cuisse : pièce découpée dans le pantalon lui-même (elle épouse la jambe),
    décollée et épaissie, avec un rabat en haut."""
    name = f"{character}_v3_Poche_{side}"

    for old in (name, name + "_Rabat"):
        if old in bpy.data.objects:
            bpy.data.objects.remove(bpy.data.objects[old])

    a, b = bone_line(rig, f"thigh_{side}")
    sign = 1 if side == "l" else -1
    axis = a.lerp(b, 0.62)
    mw = pants.matrix_world
    pieces = []

    for label, bottom, top, lift, depth in (("", axis.z - 0.09, axis.z + 0.075, 0.004, 0.014),
                                            ("_Rabat", axis.z + 0.035, axis.z + 0.085, 0.02, 0.005)):
        mesh = pants.data.copy()
        obj = bpy.data.objects.new(name + label, mesh)
        obj.matrix_world = mw
        bpy.data.collections[f"{character}_v3"].objects.link(obj)

        for g in pants.vertex_groups:
            obj.vertex_groups.new(name=g.name)

        bm = bmesh.new()
        bm.from_mesh(mesh)
        bm.normal_update()
        kill = []

        for f in bm.faces:
            p = mw @ f.calc_center_median()
            n = mw.to_3x3() @ f.normal
            inside = bottom < p.z < top and abs(p.y - axis.y) < 0.058 and (p.x - axis.x) * sign > 0 and n.x * sign > 0.3
            if not inside:
                kill.append(f)

        bmesh.ops.delete(bm, geom=kill, context='FACES')
        keep_largest(bm)
        inflate(bm, lambda v: lift)
        bm.to_mesh(mesh)
        bm.free()
        mesh.materials.clear()
        mesh.materials.append(bpy.data.materials["RM_Cargo"])
        thick = obj.modifiers.new("Epaisseur", 'SOLIDIFY')
        thick.thickness = depth
        thick.offset = 1.0
        bind(obj, rig)
        obj.modifiers.new("Lisse", 'SUBSURF').levels = 1
        outline(obj, 0.004)
        pieces.append(obj)

    return pieces[0]


def make_shoes(character):
    """Baskets sombres usées : semelle et bout blancs, lacets blancs (peints)."""
    obj, body, rig, bm = region(character, f"{character}_v3_Baskets", None, SHOE_TOP, lambda p, v: True)
    mw = obj.matrix_world
    smooth(bm, [v for v in bm.verts if not v.is_boundary], 0.6, 30)   # orteils fondus en un bout rond
    inflate(bm, lambda v: 0.02)
    # Semelle : le dessous est aplati au sol et un peu débordant.
    inv = mw.inverted()

    for v in bm.verts:
        p = mw @ v.co

        if p.z < 0.03:
            p.z = max(p.z - 0.012, 0.0)
            v.co = inv @ p

    bm.to_mesh(obj.data)
    bm.free()
    obj.data.materials.append(flat_material("RM_Basket", (0.13, 0.14, 0.2)))
    obj.data.materials.append(flat_material("RM_Semelle", (0.9, 0.9, 0.88)))

    for poly in obj.data.polygons:
        p = mw @ poly.center
        foot = rig.matrix_world @ rig.data.bones["ball_" + ("l" if p.x > body.matrix_world.translation.x else "r")].head_local
        toe = p.y < foot.y - 0.045 and p.z < 0.05
        poly.material_index = 1 if p.z < 0.024 or toe else 0
        poly.use_smooth = True

    bind(obj, rig)
    obj.modifiers.new("Lisse", 'SUBSURF').levels = 1
    outline(obj)
    return obj


def _fresh(name):
    old = bpy.data.objects.get(name)

    if old is not None:
        bpy.data.objects.remove(old)


def _to_collection(obj, character):
    for c in obj.users_collection:
        c.objects.unlink(obj)

    bpy.data.collections[f"{character}_v3"].objects.link(obj)


def surface_y(obj, x, z, start_y=-0.6):
    """Avant d'un vêtement à la hauteur z : rayon lancé de face vers l'arrière."""
    dg = bpy.context.evaluated_depsgraph_get()
    inv = obj.matrix_world.inverted()
    hit, loc, _, _ = obj.evaluated_get(dg).ray_cast(inv @ Vector((x, start_y, z)), inv.to_3x3() @ Vector((0, 1, 0)))
    return (obj.matrix_world @ loc).y if hit else None


def make_hood(character, hoodie):
    """Capuche baissée : bourrelet épais derrière la nuque qui retombe sur le haut du dos, fin et croisé devant."""
    rig = bpy.data.objects[f"{character}_v3_Rig"]
    name = f"{character}_v3_Capuche"
    _fresh(name)
    neck, _ = bone_line(rig, "neck_01")
    centre = Vector((neck.x, neck.y + 0.012, NECK_LINE + 0.004))
    bpy.ops.mesh.primitive_torus_add(major_radius=0.1, minor_radius=0.03, major_segments=48, minor_segments=12,
                                     location=(0, 0, 0))
    obj = bpy.context.active_object
    obj.name = name

    for v in obj.data.vertices:
        ring = Vector((v.co.x, v.co.y, 0))
        phi = math.atan2(ring.y, ring.x)                 # +90° : derrière la nuque
        back = max(0.0, math.sin(phi))
        front = max(0.0, -math.sin(phi))
        local = v.co - ring.normalized() * 0.1           # écart au cercle central
        thick = 1.0 + 0.55 * back - 0.45 * front
        ring_scaled = Vector((ring.x * 1.08, ring.y * (1.0 + 0.05 * back), 0))
        lift = -0.095 * back ** 1.5 - 0.035 * front ** 2  # retombe dans le dos, plonge en V devant
        v.co = ring_scaled.normalized() * ring_scaled.length + local * thick + Vector((0, -0.012 * back, lift))

    obj.location = centre
    obj.data.materials.append(bpy.data.materials["RM_Hoodie"])

    for poly in obj.data.polygons:
        poly.use_smooth = True

    _to_collection(obj, character)
    bind(obj, rig, "spine_03")
    obj.modifiers.new("Lisse", 'SUBSURF').levels = 1
    outline(obj, 0.005)
    return obj


def make_drawstrings(character, hoodie):
    """Deux cordons blancs qui sortent de la capuche et pendent sur la poitrine, posés sur le tissu."""
    rig = bpy.data.objects[f"{character}_v3_Rig"]
    cx = hoodie.matrix_world.translation.x
    made = []

    for side, sx in (("l", 1), ("r", -1)):
        name = f"{character}_v3_Cordon_{side}"
        _fresh(name)
        curve = bpy.data.curves.new(name, 'CURVE')
        curve.dimensions = '3D'
        curve.bevel_depth = 0.0035
        curve.bevel_resolution = 2
        spline = curve.splines.new('POLY')
        zs = [NECK_LINE - 0.02 - i * 0.025 for i in range(10)]
        spline.points.add(len(zs) - 1)

        for p, z in zip(spline.points, zs):
            x = cx + sx * (0.035 + 0.006 * (zs[0] - z) / 0.22)
            y = surface_y(hoodie, x, z)
            p.co = (x, (y if y is not None else -0.15) - 0.006, z, 1)

        obj = bpy.data.objects.new(name, curve)
        bpy.data.collections[f"{character}_v3"].objects.link(obj)
        obj.data.materials.append(flat_material("RM_Cordon", (0.86, 0.86, 0.84)))
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.convert(target='MESH')
        obj = bpy.data.objects[name]
        bind(obj, rig, "spine_03")
        outline(obj, 0.002)
        made.append(obj)

    return made


def dress(character):
    hoodie = make_hoodie(character)
    pants, pockets = make_pants(character)
    shoes = make_shoes(character)
    hood = make_hood(character, hoodie)
    strings = make_drawstrings(character, hoodie)
    return [hoodie, pants, shoes, hood, *pockets, *strings]


# ------------------------------------------------------------------ Image fixe pour l'auteur

SKIN = (0.93, 0.72, 0.6)


def skin(character):
    """Peau en aplats (même rampe à 4 tons que les vêtements) à la place de la glaise grise de l'étape 1."""
    body = bpy.data.objects[f"{character}_v3_Corps"]
    body.data.materials[0] = flat_material("RM_Peau_v3", SKIN)


def still(character, out_dir, size=1200, close=None):
    """Rendus de face et en 3/4 de jeu (18° de hauteur, 30° de côté), seulement la collection du personnage."""
    scene = bpy.context.scene
    coll = bpy.data.collections[f"{character}_v3"]
    keep = set(coll.objects)
    hidden = {o: o.hide_render for o in scene.objects}

    for o in scene.objects:
        o.hide_render = o not in keep and o.type != 'LIGHT'

    body = bpy.data.objects[f"{character}_v3_Corps"]
    target = body.matrix_world.translation + Vector((0, 0, 0.86))
    cam_data = bpy.data.cameras.get("RM_Still") or bpy.data.cameras.new("RM_Still")
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = 1.95
    cam = bpy.data.objects.get("RM_Still") or bpy.data.objects.new("RM_Still", cam_data)

    if cam.name not in scene.collection.objects:
        scene.collection.objects.link(cam)

    sun = bpy.data.objects.get("RM_Still_Sun")

    if sun is None:
        sun = bpy.data.objects.new("RM_Still_Sun", bpy.data.lights.new("RM_Still_Sun", 'SUN'))
        scene.collection.objects.link(sun)

    sun.data.energy = 3.0
    sun.data.angle = 0.0
    sun.data.use_shadow = False   # pas d'ombres portées tachetées : les tons suivent la forme
    sun.rotation_euler = (math.radians(50), 0, math.radians(-35))   # lumière en haut à gauche, de face
    previous = (scene.camera, scene.render.resolution_x, scene.render.resolution_y, scene.render.film_transparent,
                scene.render.filepath)
    # Seul le soleil éclaire (pas de lumière d'ambiance) et les couleurs sortent telles quelles : aplats nets.
    world = scene.world
    background = next((n for n in world.node_tree.nodes if n.type == 'BACKGROUND'), None) if world and world.use_nodes else None
    strength = background.inputs[1].default_value if background else None
    view = scene.view_settings.view_transform

    if background:
        background.inputs[1].default_value = 0.0

    scene.view_settings.view_transform = 'Standard'
    scene.camera = cam
    scene.render.resolution_x = scene.render.resolution_y = size
    scene.render.film_transparent = True
    paths = []

    shots = [("face", 0.0, 90.0, target, 1.95), ("34", 18.0, 60.0, target, 1.95)]

    if close is not None:   # gros plan (os, nom) : par exemple le poing gauche, vu de 3/4 côté extérieur
        rig = bpy.data.objects[f"{character}_v3_Rig"]
        bone, label = close
        shots.append((label, 18.0, 20.0, rig.matrix_world @ rig.pose.bones[bone].head, 0.32))

    for label, elevation, yaw, target, scale in shots:
        cam_data.ortho_scale = scale
        e, y = math.radians(elevation), math.radians(yaw)
        d = Vector((math.cos(y), math.sin(y), 0))
        cam.location = target - d * 6 * math.cos(e) + Vector((0, 0, 6 * math.sin(e)))
        cam.rotation_euler = (math.radians(90 - elevation), 0, math.radians(-90 + yaw))
        scene.render.filepath = f"{out_dir}/{character}_{label}.png"
        bpy.ops.render.render(write_still=True)
        paths.append(scene.render.filepath)

    scene.camera, scene.render.resolution_x, scene.render.resolution_y, scene.render.film_transparent, \
        scene.render.filepath = previous

    for o, h in hidden.items():
        o.hide_render = h

    if background:
        background.inputs[1].default_value = strength

    scene.view_settings.view_transform = view

    return paths


# ------------------------------------------------------------------ Plis peints

def uv_unwrap(obj):
    """Dépliage automatique (îlots) : la texture est un aplat, seules les lignes des plis y sont peintes."""
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.01)
    bpy.ops.object.mode_set(mode='OBJECT')


class Painter:
    """Peint des traits dans la texture d'un vêtement à partir de courbes données en 3D (repère du monde) :
    chaque point est ramené sur la surface au repos, puis converti en coordonnées de texture."""

    def __init__(self, obj, base, size=2048, scale=1.8):
        import numpy as np
        from mathutils.bvhtree import BVHTree
        self.np = np
        self.obj, self.size, self.scale = obj, size, scale
        mesh = obj.data
        mesh.calc_loop_triangles()
        self.tris = list(mesh.loop_triangles)
        self.uv = mesh.uv_layers.active.data
        self.verts = [v.co.copy() for v in mesh.vertices]
        self.bvh = BVHTree.FromPolygons(self.verts, [t.vertices[:] for t in self.tris])
        self.inv = obj.matrix_world.inverted()
        self.img = np.empty((size, size, 4), np.float32)
        self.img[:] = (*base, 1.0)

    def to_uv(self, p):
        from mathutils.geometry import barycentric_transform
        co, _, index, dist = self.bvh.find_nearest(self.inv @ p)

        if co is None or dist > 0.06:
            return None

        t = self.tris[index]
        a, b, c = (self.verts[i] for i in t.vertices)
        ua, ub, uc = (self.uv[l].uv for l in t.loops)
        q = barycentric_transform(co, a, b, c, ua.to_3d(), ub.to_3d(), uc.to_3d())
        return q.x * self.size, q.y * self.size

    def stroke(self, points, width=4.0, color=(0, 0, 0), taper=True, samples=48):
        """Trait épais au milieu et fin aux bouts (taper), comme un trait d'encre."""
        np = self.np
        pts = [Vector(p) for p in points if p is not None]

        if len(pts) < 2:
            return

        dense = []

        for i in range(samples + 1):
            u = i / samples * (len(pts) - 1)
            k = min(int(u), len(pts) - 2)
            dense.append(pts[k].lerp(pts[k + 1], u - k))

        uvs = [self.to_uv(p) for p in dense]

        for i in range(len(uvs) - 1):
            a, b = uvs[i], uvs[i + 1]

            if a is None or b is None or math.dist(a, b) > self.size * 0.03:   # saut d'îlot : on lève le pinceau
                continue

            f = i / (len(uvs) - 1)
            r = width * self.scale * 0.5 * ((math.sin(math.pi * f) * 0.8 + 0.2) if taper else 1.0)
            steps = max(1, int(math.dist(a, b) * 2))

            for s in range(steps + 1):
                x = a[0] + (b[0] - a[0]) * s / steps
                y = a[1] + (b[1] - a[1]) * s / steps
                x0, x1 = int(max(0, x - r - 1)), int(min(self.size, x + r + 2))
                y0, y1 = int(max(0, y - r - 1)), int(min(self.size, y + r + 2))

                if x0 >= x1 or y0 >= y1:
                    continue

                yy, xx = np.mgrid[y0:y1, x0:x1]
                cover = np.clip(r + 0.5 - np.hypot(xx - x, yy - y), 0, 1)[..., None]
                patch = self.img[y0:y1, x0:x1, :3]
                self.img[y0:y1, x0:x1, :3] = patch * (1 - cover) + np.array(color, np.float32) * cover

    def save(self, name, folder):
        image = bpy.data.images.get(name) or bpy.data.images.new(name, self.size, self.size, alpha=False)

        if tuple(image.size) != (self.size, self.size):
            image.scale(self.size, self.size)

        image.pixels.foreach_set(self.img.ravel())
        image.filepath_raw = f"{folder}/{name}.png"
        image.file_format = 'PNG'
        image.save()
        return image


def textured(obj, slot, image):
    """Remplace l'aplat de la couleur du matériau par la texture peinte (même rampe à 4 tons)."""
    old = obj.data.materials[slot]
    mat = old.copy()
    mat.name = image.name.replace("T_", "RM_")
    nt = mat.node_tree
    rgb = next(n for n in nt.nodes if n.type == 'RGB')
    mix = next(n for n in nt.nodes if n.type == 'MIX')
    uv = nt.nodes.new("ShaderNodeUVMap")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = image
    tex.interpolation = 'Cubic'
    nt.links.new(tex.inputs[0], uv.outputs[0])
    nt.links.new(mix.inputs[6], tex.outputs[0])
    nt.nodes.remove(rgb)
    obj.data.materials[slot] = mat
    return mat


def _arc(a, b, bulge, n=5):
    """Courbe de a à b, bombée de « bulge » (vecteur) au milieu."""
    return [a.lerp(b, i / (n - 1)) + bulge * math.sin(math.pi * i / (n - 1)) for i in range(n)]


def srgb(c):
    """Couleur linéaire (matériaux) vers sRGB (pixels d'une image 8 bits)."""
    return tuple(x * 12.92 if x <= 0.0031308 else 1.055 * x ** (1 / 2.4) - 0.055 for x in c)


def _front(obj, x, z, inset=0.0):
    y = surface_y(obj, x, z)
    return None if y is None else Vector((x, y + inset, z))


def _ring(centre, axis, radius, angles, side_ref):
    """Points autour d'un axe (manche, jambe) : angle 0 du côté de side_ref."""
    axis = axis.normalized()
    u = (side_ref - axis * side_ref.dot(axis)).normalized()
    v = axis.cross(u)
    return [centre + (u * math.cos(math.radians(a)) + v * math.sin(math.radians(a))) * radius for a in angles]


def paint_hoodie(character, folder):
    obj = bpy.data.objects[f"{character}_v3_Hoodie"]
    rig = bpy.data.objects[f"{character}_v3_Rig"]
    cx = obj.matrix_world.translation.x
    uv_unwrap(obj)
    base = (0.05, 0.055, 0.08)
    p = Painter(obj, srgb(base))
    ink = srgb((0.006, 0.006, 0.012))
    shine = srgb((0.11, 0.12, 0.17))
    F = lambda x, z: _front(obj, cx + x, z)

    # Poche kangourou : les deux fentes en biais et le haut de la poche.
    for s in (1, -1):
        p.stroke([F(s * 0.055, 1.07), F(s * 0.09, 1.02), F(s * 0.125, 0.965)], 5, ink)
    p.stroke([F(x, 1.072) for x in (-0.055, -0.02, 0.02, 0.055)], 3, ink, taper=False)

    # Ventre : petits plis tirés au-dessus du bord-côte, et le bord-côte lui-même (ligne et côtes).
    for x in (-0.14, -0.08, -0.02, 0.05, 0.11, 0.16):
        p.stroke([F(x, HOODIE_HEM + 0.06), F(x * 1.05 + 0.006, HOODIE_HEM + 0.1), F(x * 1.1, HOODIE_HEM + 0.13)], 3.5, ink)

    for x in [i * 0.018 - 0.2 for i in range(23)]:
        a, b = F(x, HOODIE_HEM + 0.008), F(x, HOODIE_HEM + 0.05)
        if a and b:
            p.stroke([a, b], 1.6, ink, taper=False, samples=6)

    # Aisselles et poitrine : plis en biais vers le milieu.
    for s in (1, -1):
        for dz, dx in ((0.0, 0.0), (-0.05, 0.015)):
            p.stroke([F(s * (0.2 - dx), 1.27 + dz), F(s * (0.16 - dx), 1.22 + dz), F(s * (0.11 - dx), 1.19 + dz)], 4, ink)
        p.stroke([F(s * 0.21, 1.2), F(s * 0.17, 1.15)], 3, shine)

    # Manches : plis au creux du coude, fronces au-dessus du poignet, pli au pli de l'épaule.
    for s in "lr":
        up_a, up_b = bone_line(rig, f"upperarm_{s}")
        lo_a, lo_b = bone_line(rig, f"lowerarm_{s}")
        inside = Vector((cx, 0, 0)) - Vector((up_b.x, 0, 0)) + Vector((0, -0.6, 0))
        for k, t in enumerate((-0.03, 0.0, 0.035)):
            c = lo_a + (lo_b - lo_a).normalized() * t
            p.stroke(_ring(c, lo_b - up_a, 0.075, (-60 + k * 8, -20, 25 - k * 8), inside), 4, ink)
        for k, t in enumerate((0.72, 0.8, 0.88)):
            c = lo_a.lerp(lo_b, t)
            ang = -150 + k * 95
            p.stroke(_ring(c, lo_b - lo_a, 0.066, (ang, ang + 35), inside) + [_ring(lo_a.lerp(lo_b, t + 0.05), lo_b - lo_a, 0.066, (ang + 55,), inside)[0]], 3, ink)
        c = up_a.lerp(up_b, 0.5)
        p.stroke(_ring(c, up_b - up_a, 0.08, (-90, -60, -30), inside), 3, shine)

    # Dos : deux plis souples au-dessus des reins.
    back = lambda x, z: Vector((cx + x, 0.3, z))
    for z in (1.0, 1.06):
        p.stroke([back(-0.12, z), back(-0.04, z + 0.02), back(0.05, z + 0.01), back(0.13, z - 0.015)], 3.5, ink)

    image = p.save("T_Hoodie_" + character, folder)
    textured(obj, 0, image)
    return image


def paint_pants(character, folder):
    obj = bpy.data.objects[f"{character}_v3_Pantalon"]
    rig = bpy.data.objects[f"{character}_v3_Rig"]
    cx = obj.matrix_world.translation.x
    uv_unwrap(obj)
    base = (0.47, 0.46, 0.44)
    p = Painter(obj, srgb(base))
    ink = srgb((0.16, 0.155, 0.15))
    light = srgb((0.62, 0.61, 0.59))
    F = lambda x, z: _front(obj, cx + x, z)

    # Braguette (couture en J) et plis de l'entrejambe.
    p.stroke([F(0.0, 0.99), F(0.022, 0.95), F(0.024, 0.88), F(0.01, 0.845)], 3, ink, taper=False)
    for s in (1, -1):
        p.stroke([F(s * 0.03, 0.82), F(s * 0.07, 0.85), F(s * 0.12, 0.875)], 4, ink)
        p.stroke([F(s * 0.035, 0.79), F(s * 0.08, 0.80), F(s * 0.13, 0.83)], 3.5, ink)

    for s in "lr":
        th_a, th_b = bone_line(rig, f"thigh_{s}")
        ca_a, ca_b = bone_line(rig, f"calf_{s}")
        sign = 1 if s == "l" else -1
        front = Vector((0, -1, 0))
        # Genou : deux plis en zigzag devant.
        for dz, w in ((0.03, 4), (-0.025, 3.5)):
            c = ca_a + Vector((0, 0, dz))
            p.stroke(_ring(c, Vector((0, 0, 1)), 0.1, (-50, -10, 15, 45), front), w, ink)
        # Bas de jambe : le tissu s'accumule sur la chaussure (fronces en zigzag).
        for k, z in enumerate((0.13, 0.17, 0.215)):
            c = Vector((ca_b.x, ca_b.y, z))
            a0 = -70 + k * 25
            p.stroke(_ring(c, Vector((0, 0, 1)), 0.095, (a0, a0 + 25, a0 + 45), front) +
                     [_ring(c + Vector((0, 0, 0.02)), Vector((0, 0, 1)), 0.095, (a0 + 70,), front)[0]], 3.5, ink)
        # Cuisse : long pli tiré de la hanche vers le genou, reflet le long du devant de la jambe.
        p.stroke([th_a.lerp(th_b, t) + Vector((sign * 0.03, -0.11, 0)) for t in (0.25, 0.5, 0.8)], 3, ink)
        p.stroke([th_a.lerp(ca_b, t) + Vector((-sign * 0.02, -0.1, 0)) for t in (0.35, 0.55, 0.75)], 3, light)
        # Couture de côté.
        p.stroke([Vector((th_a.x + sign * 0.12, th_a.y, z)) for z in (0.95, 0.7, 0.45, 0.2, 0.08)], 2, ink, taper=False)

    image = p.save("T_Cargo_" + character, folder)
    textured(obj, 0, image)
    return image


def paint_shoes(character, folder):
    obj = bpy.data.objects[f"{character}_v3_Baskets"]
    rig = bpy.data.objects[f"{character}_v3_Rig"]
    uv_unwrap(obj)
    p = Painter(obj, srgb((0.13, 0.14, 0.2)), size=1024)
    lace = srgb((0.92, 0.92, 0.9))

    for s in "lr":
        a = rig.matrix_world @ rig.data.bones[f"foot_{s}"].head_local
        b = rig.matrix_world @ rig.data.bones[f"ball_{s}"].head_local
        for k in range(4):
            t0 = 0.15 + k * 0.17
            c = a.lerp(b, t0)
            c.z = 0.105 - k * 0.012
            side = Vector((0.025, 0, 0))
            p.stroke([c - side + Vector((0, 0.012, 0)), c + side - Vector((0, 0.012, 0))], 3.5, lace, taper=False, samples=8)
            p.stroke([c + side + Vector((0, 0.012, 0)), c - side - Vector((0, 0.012, 0))], 3.5, lace, taper=False, samples=8)

    image = p.save("T_Basket_" + character, folder)
    textured(obj, 0, image)
    return image


def paint(character, folder):
    import os
    os.makedirs(folder, exist_ok=True)
    return [paint_hoodie(character, folder), paint_pants(character, folder), paint_shoes(character, folder)]
