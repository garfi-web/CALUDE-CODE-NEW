"""RED MET — accessoires modélisés directement dans Blender (exécuté par le MCP Blender, après redmet_animation.py).

Mannequin d'entraînement : socle en croix et poteau en bois, sac de frappe en toile rapiécée serré par
des cordes, tête en sac nouée, chiffon rouge en bandeau, deux chevilles en bois pour les bras.
Formes simples, couleurs en aplats par face (attribut « Col »), rendu toon et contour comme les personnages.
Le mannequin regarde vers -Y, comme les personnages : de profil, l'avant est à droite de l'image.
"""
import math
import os

import bmesh
import bpy
from mathutils import Matrix, Vector

WOOD = (0.36, 0.22, 0.12)
WOOD_DARK = (0.24, 0.14, 0.08)
CANVAS = (0.62, 0.52, 0.36)
CANVAS_LIGHT = (0.72, 0.63, 0.46)
PATCH_DARK = (0.44, 0.35, 0.23)
PATCH_GREY = (0.48, 0.47, 0.42)
ROPE = (0.46, 0.33, 0.17)
RAG = (0.72, 0.05, 0.05)


def _part(bm, build, color, matrix=Matrix()):
    """Ajoute une primitive au bmesh commun et colore ses faces ; build(bm) renvoie ses sommets."""
    before = set(bm.faces)
    build(bm)
    faces = [f for f in bm.faces if f not in before]
    verts = {v for f in faces for v in f.verts}
    bmesh.ops.transform(bm, matrix=matrix, verts=list(verts))

    layer = bm.loops.layers.color["Col"]

    for f in faces:
        for loop in f.loops:
            loop[layer] = (*color, 1.0)

    return faces


def _cylinder(radius, depth, segments=24):
    return lambda bm: bmesh.ops.create_cone(bm, cap_ends=True, segments=segments, radius1=radius, radius2=radius, depth=depth)


def _box(x, y, z):
    return lambda bm: bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Diagonal((x, y, z, 1)))


def _torus(major, minor, segments=32, ring=8):
    def build(bm):
        verts = []

        for i in range(segments):
            a = 2 * math.pi * i / segments
            for j in range(ring):
                b = 2 * math.pi * j / ring
                r = major + minor * math.cos(b)
                verts.append(bm.verts.new((r * math.cos(a), r * math.sin(a), minor * math.sin(b))))

        for i in range(segments):
            for j in range(ring):
                a, b = i * ring + j, i * ring + (j + 1) % ring
                c, d = ((i + 1) % segments) * ring + (j + 1) % ring, ((i + 1) % segments) * ring + j
                bm.faces.new((verts[a], verts[b], verts[c], verts[d]))

    return build


def _bag(radius, z0, z1, segments=32, rings=12):
    """Sac de frappe : cylindre légèrement bombé, fermé."""
    def build(bm):
        rows = []

        for k in range(rings + 1):
            t = k / rings
            r = radius * (0.92 + 0.08 * math.sin(math.pi * t))
            z = z0 + (z1 - z0) * t
            rows.append([bm.verts.new((r * math.cos(2 * math.pi * i / segments), r * math.sin(2 * math.pi * i / segments), z))
                         for i in range(segments)])

        for k in range(rings):
            for i in range(segments):
                j = (i + 1) % segments
                bm.faces.new((rows[k][i], rows[k][j], rows[k + 1][j], rows[k + 1][i]))

        bm.faces.new(list(reversed(rows[0])))
        bm.faces.new(rows[-1])

    return build


def build_training_dummy(name="Mannequin_M", location=(42.0, 0.0, 0.0)):
    """Construit (ou reconstruit) le mannequin dans la collection « Mannequin »."""
    old = bpy.data.objects.get(name)

    if old is not None:
        bpy.data.objects.remove(old)

    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bm.loops.layers.color.new("Col")

    # Socle en croix et poteau.
    _part(bm, _box(0.75, 0.12, 0.08), WOOD_DARK, Matrix.Translation((0, 0, 0.04)))
    _part(bm, _box(0.12, 0.75, 0.08), WOOD_DARK, Matrix.Translation((0, 0, 0.04)))
    _part(bm, _cylinder(0.05, 1.72, 16), WOOD, Matrix.Translation((0, 0, 0.86)))

    # Sac en toile, avec ses pièces rapiécées (faces recolorées par zones).
    bag_faces = _part(bm, _bag(0.25, 0.5, 1.36), CANVAS)
    layer = bm.loops.layers.color["Col"]
    patches = [(-0.6, 0.2, 0.7, 0.95, PATCH_DARK), (1.9, 2.6, 1.0, 1.25, PATCH_GREY), (-1.9, -1.3, 0.62, 0.8, CANVAS_LIGHT),
               (0.9, 1.4, 1.12, 1.3, PATCH_DARK), (-2.9, -2.3, 0.95, 1.2, PATCH_GREY)]

    for f in bag_faces:
        c = f.calc_center_median()
        angle = math.atan2(c.y, c.x)

        for a0, a1, z0, z1, color in patches:
            if a0 <= angle <= a1 and z0 <= c.z <= z1:
                for loop in f.loops:
                    loop[layer] = (*color, 1.0)

    # Cordes autour du sac, tête en sac nouée au cou.
    for z in (0.72, 1.16):
        _part(bm, _torus(0.255, 0.022), ROPE, Matrix.Translation((0, 0, z)))

    _part(bm, lambda b: bmesh.ops.create_uvsphere(b, u_segments=24, v_segments=14, radius=0.16), CANVAS_LIGHT,
          Matrix.Translation((0, 0, 1.56)) @ Matrix.Diagonal((1.0, 1.0, 1.12, 1.0)))
    _part(bm, _torus(0.1, 0.03), ROPE, Matrix.Translation((0, 0, 1.39)))

    # Chiffon rouge : bandeau sur la tête et deux pans qui pendent derrière (+Y).
    _part(bm, _torus(0.158, 0.03), RAG, Matrix.Translation((0, 0, 1.62)))

    for side, tilt in ((-0.05, 18), (0.06, 32)):
        _part(bm, _box(0.06, 0.02, 0.32), RAG,
              Matrix.Translation((side, 0.2, 1.48)) @ Matrix.Rotation(math.radians(tilt), 4, "X"))

    # Chevilles en bois pour les bras, vers l'avant (-Y).
    for z, x in ((1.22, 0.1), (1.0, -0.1)):
        _part(bm, _cylinder(0.035, 0.38, 12), WOOD,
              Matrix.Translation((x, -0.22, z)) @ Matrix.Rotation(math.radians(90), 4, "X"))

    bm.to_mesh(mesh)
    bm.free()

    for polygon in mesh.polygons:
        polygon.use_smooth = True

    try:
        mesh.set_sharp_from_angle(angle=math.radians(40))
    except AttributeError:
        pass

    obj = bpy.data.objects.new(name, mesh)
    collection = bpy.data.collections.get("Mannequin") or bpy.data.collections.new("Mannequin")

    if collection.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(collection)

    collection.objects.link(obj)
    obj.location = location
    return obj


# ------------------------------------------------------------- Armes du Flic

GUN_METAL = (0.30, 0.31, 0.34)
GUN_DARK = (0.10, 0.10, 0.11)
BATON_BLACK = (0.07, 0.07, 0.08)
BATON_GRIP = (0.20, 0.20, 0.22)


def _new_prop(name, collection):
    old = bpy.data.objects.get(name)

    if old is not None:
        bpy.data.objects.remove(old)

    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    bm = bmesh.new()
    bm.loops.layers.color.new("Col")
    return obj, bm


def _finish_prop(obj, bm):
    bm.to_mesh(obj.data)
    bm.free()

    for polygon in obj.data.polygons:
        polygon.use_smooth = False

    return obj


def _hand_frame(rig, side="R"):
    """Repère de la main au repos (pose en T, paume vers le bas) : poing, axe du bras vers la main,
    côté du pouce (vers l'avant, -Y). Une arme tenue suit le pouce : canon le long du bras,
    dessus de l'arme côté pouce, crosse côté auriculaire."""
    bone = rig.data.bones[f"hand.{side}"]
    head = rig.matrix_world @ bone.head_local
    tail = rig.matrix_world @ bone.tail_local
    along = (tail - head).normalized()
    fist = head + (tail - head) * 0.55
    return fist, along, Vector((0, -1, 0))


def _frame_matrix(origin, x_axis, y_axis):
    """Matrice d'un repère local : X = x_axis, Y = y_axis, Z = X × Y, origine origin."""
    z_axis = x_axis.cross(y_axis)
    m = Matrix((x_axis, y_axis, z_axis)).transposed().to_4x4()
    m.translation = origin
    return m


def build_pistol(rig, collection, name="Flic_Pistol", side="R"):
    """Pistolet générique (ni marque ni inscription) : culasse, canon, crosse inclinée, pontet.
    Repère local : X = canon (vers l'avant de l'arme), Y = dessus de l'arme."""
    obj, bm = _new_prop(name, collection)
    _part(bm, _box(0.20, 0.034, 0.030), GUN_METAL, Matrix.Translation((0.04, 0.045, 0)))
    _part(bm, _box(0.03, 0.022, 0.022), GUN_DARK, Matrix.Translation((0.15, 0.040, 0)))
    _part(bm, _box(0.12, 0.024, 0.026), GUN_DARK, Matrix.Translation((0.03, 0.018, 0)))
    _part(bm, _box(0.036, 0.11, 0.030), GUN_DARK,
          Matrix.Translation((-0.035, -0.035, 0)) @ Matrix.Rotation(math.radians(-14), 4, "Z"))
    _part(bm, _box(0.05, 0.008, 0.012), GUN_DARK, Matrix.Translation((0.03, -0.012, 0)))
    _finish_prop(obj, bm)

    fist, along, thumb = _hand_frame(rig, side)
    obj.matrix_world = _frame_matrix(fist, along, thumb)
    return obj


def build_baton(rig, collection, name="Flic_Baton", side="R"):
    """Matraque droite de 60 cm : poignée gainée, pommeau, tige noire. Elle sort du poing côté pouce.
    Repère local : Y = la tige."""
    obj, bm = _new_prop(name, collection)
    _part(bm, _cylinder(0.018, 0.16, 12), BATON_GRIP, Matrix.Translation((0, 0.02, 0)) @ Matrix.Rotation(math.radians(-90), 4, "X"))
    _part(bm, _cylinder(0.026, 0.02, 12), BATON_GRIP, Matrix.Translation((0, -0.07, 0)) @ Matrix.Rotation(math.radians(-90), 4, "X"))
    _part(bm, _cylinder(0.014, 0.46, 12), BATON_BLACK, Matrix.Translation((0, 0.33, 0)) @ Matrix.Rotation(math.radians(-90), 4, "X"))
    _finish_prop(obj, bm)

    fist, along, thumb = _hand_frame(rig, side)
    obj.matrix_world = _frame_matrix(fist, along, thumb)
    return obj


def add_prop_bone(rig, bone_name, side="R"):
    """Os sans déformation, enfant de la main : porte une arme. Son échelle permet de la cacher
    dans les animations (opération « H » de redmet_animation)."""
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='EDIT')
    eb = rig.data.edit_bones
    hand = eb[f"hand.{side}"]
    bone = eb.get(bone_name) or eb.new(bone_name)
    bone.head = hand.head.copy()
    bone.tail = hand.tail.copy()
    bone.roll = hand.roll
    bone.parent = hand
    bone.use_connect = False
    bpy.ops.object.mode_set(mode='OBJECT')
    rig.data.bones[bone_name].use_deform = False


def attach_to_bone(obj, rig, bone_name):
    """Accroche l'objet à l'os en gardant sa place au repos."""
    world = obj.matrix_world.copy()
    obj.parent = rig
    obj.parent_type = 'BONE'
    obj.parent_bone = bone_name
    bpy.context.view_layer.update()
    obj.matrix_world = world


def render_still(scene, cam, obj, path):
    """Une image du mêmes cadrage que les sprites des personnages (pieds à feet_from_bottom)."""
    if obj.name not in scene.collection.objects:
        scene.collection.objects.link(obj)

    hidden = [o for o in scene.objects if o.type == 'MESH' and o != obj and not o.hide_render]

    for o in hidden:
        o.hide_render = True

    feet = obj.matrix_world.translation
    cam.location = (feet.x - 6.0, feet.y, scene["center_z"])
    cam.rotation_euler = (math.radians(90), 0, math.radians(-90))
    window = bpy.context.window
    previous = window.scene
    window.scene = scene

    try:
        os.makedirs(os.path.dirname(path), exist_ok=True)
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
    finally:
        window.scene = previous

        for o in hidden:
            o.hide_render = False
