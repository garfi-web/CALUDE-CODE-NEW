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
