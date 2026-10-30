"""RED MET — méthode A des poings (2026-10-30) : main stylisée des Human Base Meshes v1.4.1 (Blender Studio, CC0,
docs/LICENCES_TIERS.md), avec sa vraie topologie de doigts, munie d'un squelette et repliée en poing.

La main du paquet n'a pas de squelette. Les os sont placés automatiquement : pour chaque doigt, on part du bout et on
suit les anneaux de sommets à distance géodésique croissante ; le centre de chaque anneau donne l'axe du doigt, et
les articulations sont posées à des proportions anatomiques de la longueur du doigt (jusqu'à la palmure + 1,8 cm).

    exec(open(r"<dépôt>/tools/blender/redmet_hand_hbm.py", encoding="utf-8").read())
    hand, rig = rig_hand("Hand - Stylized ")
    fist_pose(rig)
"""
import heapq
import math

import bmesh
import bpy
from mathutils import Matrix, Vector

BUNDLE = r"C:/RED MET/ai/downloads/human_base_meshes/human-base-meshes-bundle-v1.4.1/human_base_meshes_bundle.blend"
# La paume de la main stylisée regarde +Y, les doigts pendent vers -Z, le pouce est du côté +X (main droite).
PALM = Vector((0, 1, 0))
FINGERS = ("thumb", "index", "middle", "ring", "pinky")


def _geodesic(adj, start, n):
    dist = [1e9] * n
    dist[start] = 0.0
    heap = [(0.0, start)]

    while heap:
        d, i = heapq.heappop(heap)

        if d > dist[i]:
            continue

        for j, length in adj[i]:
            if d + length < dist[j]:
                dist[j] = d + length
                heapq.heappush(heap, (d + length, j))

    return dist


def _tips(world, bm):
    """Bouts des doigts : minima locaux en hauteur (doigts pendants), triés du pouce (+X) à l'auriculaire."""
    def ring2(v):
        out = set()
        for e in v.link_edges:
            u = e.other_vert(v)
            out.add(u)
            for e2 in u.link_edges:
                out.add(e2.other_vert(u))
        out.discard(v)
        return out

    tips = [v.index for v in bm.verts if all(world[v.index].z <= world[u.index].z for u in ring2(v))]
    tips.sort(key=lambda i: -world[i].x)
    return tips[:5]


def _axis(world, dist, step=0.006, upto=0.13):
    """Centres et rayons des anneaux géodésiques depuis le bout du doigt."""
    out = []

    for k in range(1, int(upto / step) + 1):
        d = k * step
        ring = [world[i] for i in range(len(world)) if abs(dist[i] - d) < 0.0035]

        if not ring:
            break

        c = sum(ring, Vector()) / len(ring)
        out.append((d, c, sum((p - c).length for p in ring) / len(ring)))

    return out


def joints(hand):
    """Articulations de chaque doigt en coordonnées du monde : [base, ..., bout] (4 points)."""
    bm = bmesh.new()
    bm.from_mesh(hand.data)
    bm.verts.ensure_lookup_table()
    mw = hand.matrix_world
    world = [mw @ v.co for v in bm.verts]
    adj = [[(e.other_vert(v).index, (world[v.index] - world[e.other_vert(v).index]).length) for e in v.link_edges]
           for v in bm.verts]
    result = {}

    for name, tip in zip(FINGERS, _tips(world, bm)):
        prof = _axis(world, _geodesic(adj, tip, len(world)))
        base_r = sorted(r for _, _, r in prof[3:10])[3]
        # Palmure : premier anneau nettement plus large que le doigt.
        web = next(i for i, (_, _, r) in enumerate(prof) if i > 4 and r > base_r * 1.25)
        d_web, c_web, _ = prof[web - 1]
        c_before = prof[max(0, web - 6)][1]
        direction = (c_web - c_before).normalized()              # du bout vers la base
        at = lambda d: min(prof, key=lambda p: abs(p[0] - d))[1]

        if name == "thumb":
            # Pouce : IP, MCP visibles, CMC (trapèze) enfoncé dans la paume.
            total = d_web + 0.035
            base = c_web + direction * 0.035
            pts = [base, at(0.062), at(0.028), prof[0][1]]
        else:
            total = d_web + 0.018
            base = c_web + direction * 0.018
            pts = [base, at(0.52 * total), at(0.24 * total), prof[0][1]]

        result[name] = pts

    bm.free()
    return result


def rig_hand(name, location=(70.0, 0.0, 1.0)):
    """Squelette de la main (poignet + 3 os par doigt), poids automatiques ; renvoie (main, squelette)."""
    hand = bpy.data.objects[name]
    hand.location = location
    bpy.context.view_layer.update()
    J = joints(hand)
    top = max((hand.matrix_world @ v.co).z for v in hand.data.vertices)
    wrist = Vector((sum(J[f][0].x for f in FINGERS[1:]) / 4, J["middle"][0].y, top - 0.01))

    old = bpy.data.objects.get(name.strip() + "_Rig")

    if old is not None:
        bpy.data.objects.remove(old)

    arm = bpy.data.armatures.new(name.strip() + "_Rig")
    rig = bpy.data.objects.new(arm.name, arm)
    hand.users_collection[0].objects.link(rig)
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    root = arm.edit_bones.new("hand")
    root.head, root.tail = wrist, J["middle"][0]
    root.roll = 0

    for f in FINGERS:
        parent = root

        for i in range(3):
            b = arm.edit_bones.new(f"{f}_0{i + 1}")
            b.head, b.tail = J[f][i], J[f][i + 1]
            # Rôle : l'axe X local de chaque os est l'axe de pliage (perpendiculaire au doigt et à la paume).
            b.align_roll(PALM)
            b.parent = parent
            b.use_connect = i > 0
            parent = b

    bpy.ops.object.mode_set(mode='OBJECT')
    hand.modifiers.clear()
    bpy.ops.object.select_all(action='DESELECT')
    hand.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')

    for pb in rig.pose.bones:
        pb.rotation_mode = 'XYZ'

    return hand, rig


def fist_pose(rig, curl=(85, 100, 70), thumb=((25, 0, -35), (0, 0, 0), (0, 0, 0)), thumb_curl=(30, 45, 45)):
    """Poing : chaque doigt plie autour de son axe X local (vers la paume) ; le pouce tourne d'abord vers la paume
    (opposition, thumb), puis plie (thumb_curl)."""
    for f in FINGERS[1:]:
        for i, a in enumerate(curl):
            rig.pose.bones[f"{f}_0{i + 1}"].rotation_euler = (math.radians(a), 0, 0)

    for i in range(3):
        x, y, z = thumb[i]
        rig.pose.bones[f"thumb_0{i + 1}"].rotation_euler = (math.radians(x + thumb_curl[i]), math.radians(y), math.radians(z))


def render_object(obj, eyes, out_dir, prefix, ortho=0.17, size=900, outline=0.0022):
    """Rendu d'un seul objet en aplats et contour, une image par direction d'œil (repère du monde)."""
    scene = bpy.context.scene
    bpy.context.view_layer.update()
    hidden = {o: o.hide_render for o in scene.objects}

    for o in scene.objects:
        o.hide_render = o != obj and o.type != 'LIGHT'

    if "Lisse" not in obj.modifiers:
        obj.modifiers.new("Lisse", 'SUBSURF').levels = 2

    if "Outline" not in obj.modifiers:
        if "RM_Outline_Clay" not in obj.data.materials:
            obj.data.materials.append(bpy.data.materials["RM_Outline_Clay"])
        line = obj.modifiers.new("Outline", 'SOLIDIFY')
        line.thickness = outline
        line.offset = 1.0
        line.use_flip_normals = True
        line.material_offset = len(obj.data.materials) - 1

    cam_data = bpy.data.cameras.get("RM_Main_Cam") or bpy.data.cameras.new("RM_Main_Cam")
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = ortho
    cam = bpy.data.objects.get("RM_Main_Cam") or bpy.data.objects.new("RM_Main_Cam", cam_data)

    if cam.name not in scene.collection.objects:
        scene.collection.objects.link(cam)

    sun = bpy.data.objects["RM_Still_Sun"]
    bg = next(n for n in scene.world.node_tree.nodes if n.type == 'BACKGROUND')
    saved = (scene.camera, scene.render.resolution_x, scene.render.resolution_y, scene.render.film_transparent,
             scene.render.filepath, bg.inputs[1].default_value, scene.view_settings.view_transform, sun.rotation_euler.copy())
    bg.inputs[1].default_value = 0.0
    scene.view_settings.view_transform = 'Standard'
    scene.camera = cam
    scene.render.resolution_x = scene.render.resolution_y = size
    scene.render.film_transparent = True
    dg = bpy.context.evaluated_depsgraph_get()
    mesh = obj.evaluated_get(dg).to_mesh()
    pts = [obj.matrix_world @ v.co for v in mesh.vertices]
    obj.evaluated_get(dg).to_mesh_clear()
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    target = (lo + hi) / 2
    paths = []

    for label, eye in eyes:
        d = Vector(eye).normalized()
        cam.location = target + d * 2.0
        cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
        sun.rotation_euler = (d + Vector((0.4, 0.0, 1.2))).to_track_quat('Z', 'Y').to_euler()
        scene.render.filepath = f"{out_dir}/{prefix}_{label}.png"
        bpy.ops.render.render(write_still=True)
        paths.append(scene.render.filepath)

    (scene.camera, scene.render.resolution_x, scene.render.resolution_y, scene.render.film_transparent,
     scene.render.filepath, bg.inputs[1].default_value, scene.view_settings.view_transform, sun.rotation_euler) = saved

    for o, h in hidden.items():
        o.hide_render = h

    return paths


def normal_material(name="RM_Normales"):
    """Couleur = normale de la surface (repère de la caméra) : sert à trouver les plis et les séparations."""
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    geo = nt.nodes.new("ShaderNodeNewGeometry")
    vt = nt.nodes.new("ShaderNodeVectorTransform")
    vt.vector_type = 'NORMAL'
    vt.convert_from = 'WORLD'
    vt.convert_to = 'CAMERA'
    # Codée en 0..1 (n × 0,5 + 0,5) : les valeurs négatives ne survivent pas au rendu.
    enc = nt.nodes.new("ShaderNodeVectorMath")
    enc.operation = 'MULTIPLY_ADD'
    enc.inputs[1].default_value = (0.5, 0.5, 0.5)
    enc.inputs[2].default_value = (0.5, 0.5, 0.5)
    em = nt.nodes.new("ShaderNodeEmission")
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(vt.inputs[0], geo.outputs["Normal"])
    nt.links.new(enc.inputs[0], vt.outputs[0])
    nt.links.new(em.inputs[0], enc.outputs[0])
    nt.links.new(out.inputs[0], em.outputs[0])
    return mat


def render_normals(obj, eyes, out_dir, prefix, **kw):
    """Mêmes vues que render_object, en normales (sans contour), images <prefix>_<vue>_n.png en EXR flottant."""
    scene = bpy.context.scene
    layer = bpy.context.view_layer
    line = obj.modifiers.get("Outline")
    shown = line.show_render if line else None

    if line:
        line.show_render = False

    layer.material_override = normal_material()
    fmt = (scene.render.image_settings.file_format, scene.render.image_settings.color_depth)
    scene.render.image_settings.file_format = 'OPEN_EXR'
    scene.render.image_settings.color_depth = '32'

    try:
        return render_object(obj, eyes, out_dir, prefix + "N", **kw)
    finally:
        layer.material_override = None
        scene.render.image_settings.file_format, scene.render.image_settings.color_depth = fmt

        if line:
            line.show_render = shown


def ink(color_png, normal_exr, out_png, angle=32.0, width=1, ink_rgb=(0.17, 0.11, 0.13)):
    """Traits d'encre façon anime : là où la normale tourne brusquement d'un pixel à l'autre (séparation des
    doigts, plis des phalanges, pouce posé sur les doigts), on trace une ligne sur le rendu en aplats."""
    import numpy as np

    def load(path):
        img = bpy.data.images.load(path, check_existing=False)
        w, h = img.size
        a = np.empty(w * h * 4, np.float32)
        img.pixels.foreach_get(a)
        bpy.data.images.remove(img)
        return a.reshape(h, w, 4)

    col = load(color_png)
    nrm = load(normal_exr)
    n = nrm[..., :3] * 2.0 - 1.0
    n = n / np.maximum(np.linalg.norm(n, axis=2, keepdims=True), 1e-6)
    inside = nrm[..., 3] > 0.5
    cos = math.cos(math.radians(angle))
    edge = np.zeros(inside.shape, bool)

    for dy, dx in ((0, 1), (1, 0), (1, 1), (1, -1)):
        a = n[max(0, -dy):n.shape[0] - max(0, dy), max(0, -dx):n.shape[1] - max(0, dx)]
        b = n[max(0, dy):, max(0, dx):][:a.shape[0], :a.shape[1]]
        both = inside[max(0, -dy):inside.shape[0] - max(0, dy), max(0, -dx):inside.shape[1] - max(0, dx)] & \
            inside[max(0, dy):, max(0, dx):][:a.shape[0], :a.shape[1]]
        e = ((a * b).sum(axis=2) < cos) & both
        edge[max(0, -dy):max(0, -dy) + e.shape[0], max(0, -dx):max(0, -dx) + e.shape[1]] |= e

    for _ in range(width):   # épaissir le trait
        grow = edge.copy()
        grow[1:] |= edge[:-1]
        grow[:-1] |= edge[1:]
        grow[:, 1:] |= edge[:, :-1]
        grow[:, :-1] |= edge[:, 1:]
        edge = grow

    col[edge, :3] = ink_rgb
    h, w = col.shape[:2]
    out = bpy.data.images.new("ink_tmp", w, h, alpha=True)
    out.pixels.foreach_set(col.ravel())
    out.filepath_raw = out_png
    out.file_format = 'PNG'
    out.save()
    bpy.data.images.remove(out)
    return out_png
