"""RED MET — poing fermé modélisé à part (2026-10-30, après le deuxième refus des mains MPFB pliées).

Un seul poing pour tous les humains (main gauche ; la droite est son miroir, Blender gère l'échelle négative), à l'échelle d'un adulte un peu enrobé
style anime (un peu plus gros que nature). Repère du poing : Y vers l'avant (du poignet vers les phalanges), Z vers
le dos de la main, X du côté du pouce. Construction :
- paume et dos de la main : bloc arrondi, phalanges (articulations) alignées en une ligne nette sur le dessus ;
- 4 doigts repliés à plat : première phalange qui forme la face avant, deuxième phalange à plat sous le poing,
  bout du doigt rentré dans la paume (rien ne dépasse, aucun creux) ;
- pouce replié en travers devant l'index et le majeur, éminence du pouce pleine ;
- moignon de poignet dans l'axe de l'avant-bras.
Les pièces sont fondues en un seul maillage (remaillage en voxels) puis lissées.

    exec(open(r"<dépôt>/tools/blender/redmet_fist_v3.py", encoding="utf-8").read())
    fist = build_fist("RM_Poing_v3")
"""
import bmesh
import bpy
from mathutils import Vector

# Doigts : (nom, x, avancée de l'articulation, hauteur de l'articulation, rayon).
FINGERS = (("index", 0.033, 0.086, 0.013, 0.0112),
           ("middle", 0.011, 0.089, 0.014, 0.0118),
           ("ring", -0.011, 0.085, 0.012, 0.0112),
           ("pinky", -0.031, 0.077, 0.006, 0.0098))


def _tube(name, points, radii, coll):
    """Tube lisse le long de points 3D, rayon par point (phalanges, pouce)."""
    curve = bpy.data.curves.new(name, 'CURVE')
    curve.dimensions = '3D'
    curve.bevel_depth = 1.0
    curve.bevel_resolution = 6
    curve.use_fill_caps = True
    spline = curve.splines.new('BEZIER')
    spline.bezier_points.add(len(points) - 1)

    for bp, p, r in zip(spline.bezier_points, points, radii):
        bp.co = p
        bp.handle_left_type = bp.handle_right_type = 'AUTO'
        bp.radius = r

    obj = bpy.data.objects.new(name, curve)
    coll.objects.link(obj)
    return obj


def _blob(name, centre, size, coll, bevel=0.012):
    """Bloc arrondi (paume, poignet)."""
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=size, verts=bm.verts)
    bmesh.ops.translate(bm, vec=centre, verts=bm.verts)
    bmesh.ops.bevel(bm, geom=bm.edges[:], offset=bevel, segments=5, affect='EDGES', profile=0.5)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    coll.objects.link(obj)
    return obj


def _sphere(name, centre, scale, coll):
    mesh = bpy.data.meshes.new(name)
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=24, v_segments=16, radius=1.0)
    bmesh.ops.scale(bm, vec=scale, verts=bm.verts)
    bmesh.ops.translate(bm, vec=centre, verts=bm.verts)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    coll.objects.link(obj)
    return obj


def parts(coll):
    """Pièces du poing, avant fusion."""
    made = []
    # Paume et dos de la main : du poignet (y = 0) jusque sous la ligne des articulations.
    made.append(_blob("paume", Vector((0.001, 0.042, -0.004)), Vector((0.086, 0.084, 0.036)), coll, 0.013))
    # Poignet, dans l'axe de l'avant-bras.
    made.append(_tube("poignet", [Vector((0, -0.03, -0.006)), Vector((0, -0.005, -0.006)), Vector((0, 0.03, -0.006))],
                      [0.035, 0.038, 0.042], coll))
    made[-1].scale = (1.0, 1.0, 0.6)   # dessus du poignet au niveau du dos de la main (pas de marche)

    for name, x, y, z, r in FINGERS:
        # Articulation (dessus) → première phalange vers le bas (face avant du poing) → deuxième phalange à plat
        # sous le poing, vers le poignet → bout rentré contre la paume.
        knuckle = Vector((x, y - 0.006, z))
        front_top = Vector((x, y + 0.004, z - 0.004))
        front_bottom = Vector((x, y + 0.006, z - 0.036))
        under = Vector((x, y - 0.016, z - 0.046))
        tip = Vector((x, y - 0.034, z - 0.036))
        made.append(_tube(name, [knuckle, front_top, front_bottom, under, tip], [r * 1.05, r, r * 0.98, r * 0.92, r * 0.82], coll))

    # Pouce : de l'éminence (côté de la paume) vers l'avant, puis en travers devant l'index et le majeur, sous les
    # deuxièmes phalanges.
    made.append(_sphere("eminence", Vector((0.036, 0.03, -0.016)), Vector((0.02, 0.03, 0.02)), coll))
    made.append(_tube("pouce", [Vector((0.042, 0.018, -0.012)), Vector((0.052, 0.06, -0.024)), Vector((0.044, 0.096, -0.029)),
                                Vector((0.022, 0.109, -0.028)), Vector((0.004, 0.107, -0.026))],
                      [0.015, 0.0135, 0.0125, 0.0115, 0.0105], coll))
    return made


def build_fist(name="RM_Poing_v3", voxel=0.0011):
    """Fusionne les pièces en un seul maillage propre et le renvoie (origine au poignet)."""
    coll = bpy.data.collections.get("RM_Poing_v3") or bpy.data.collections.new("RM_Poing_v3")

    if coll.name not in bpy.context.scene.collection.children:
        bpy.context.scene.collection.children.link(coll)

    for o in list(coll.objects):
        bpy.data.objects.remove(o)

    pieces = parts(coll)
    bpy.ops.object.select_all(action='DESELECT')

    for o in pieces:
        o.select_set(True)

    bpy.context.view_layer.objects.active = pieces[0]
    bpy.ops.object.convert(target='MESH')
    bpy.ops.object.join()
    fist = bpy.context.active_object
    fist.name = name
    remesh = fist.modifiers.new("Fusion", 'REMESH')
    remesh.mode = 'VOXEL'
    remesh.voxel_size = voxel
    bpy.ops.object.modifier_apply(modifier=remesh.name)
    smooth = fist.modifiers.new("Lisse", 'SMOOTH')
    smooth.factor = 0.8
    smooth.iterations = 6
    bpy.ops.object.modifier_apply(modifier=smooth.name)
    # Allègement pour le jeu (moins de 10 000 faces) sans perdre la forme.
    dec = fist.modifiers.new("Allege", 'DECIMATE')
    dec.ratio = min(1.0, 9000 / max(1, len(fist.data.polygons)))
    bpy.ops.object.modifier_apply(modifier=dec.name)

    for p in fist.data.polygons:
        p.use_smooth = True

    return fist


def render_views(fist, out_dir, size=900):
    """Le poing seul en aplats et contour (même rendu que le personnage) : face (articulations), 3/4, profil (pouce)."""
    import math

    scene = bpy.context.scene
    bpy.context.view_layer.update()   # matrix_world à jour si le poing vient d'être déplacé
    hidden = {o: o.hide_render for o in scene.objects}

    for o in scene.objects:
        o.hide_render = o != fist and o.type != 'LIGHT'

    if "Outline" not in fist.modifiers:
        line = fist.modifiers.new("Outline", 'SOLIDIFY')
        line.thickness = 0.0022
        line.offset = 1.0
        line.use_flip_normals = True
        fist.data.materials.append(bpy.data.materials["RM_Outline_Clay"])
        line.material_offset = len(fist.data.materials) - 1

    cam_data = bpy.data.cameras.get("RM_Poing_Cam") or bpy.data.cameras.new("RM_Poing_Cam")
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = 0.2
    cam = bpy.data.objects.get("RM_Poing_Cam") or bpy.data.objects.new("RM_Poing_Cam", cam_data)

    if cam.name not in scene.collection.objects:
        scene.collection.objects.link(cam)

    sun = bpy.data.objects["RM_Still_Sun"]
    world = scene.world
    bg = next(n for n in world.node_tree.nodes if n.type == 'BACKGROUND')
    saved = (scene.camera, scene.render.resolution_x, scene.render.resolution_y, scene.render.film_transparent,
             scene.render.filepath, bg.inputs[1].default_value, scene.view_settings.view_transform, sun.rotation_euler.copy())
    bg.inputs[1].default_value = 0.0
    scene.view_settings.view_transform = 'Standard'
    scene.camera = cam
    scene.render.resolution_x = scene.render.resolution_y = size
    scene.render.film_transparent = True
    target = fist.matrix_world @ Vector((0.0, 0.035, -0.01))
    paths = []

    # Direction de l'œil (repère du poing) : face = devant les articulations, profil = côté du pouce.
    for label, eye in (("face", Vector((0.0, 1.0, 0.25))), ("34", Vector((0.75, 0.75, 0.35))), ("profil", Vector((1.0, 0.0, 0.12)))):
        d = (fist.matrix_world.to_3x3() @ eye).normalized()
        cam.location = target + d * 2.0
        cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
        # Lumière en haut, un peu de côté par rapport à la vue.
        sun.rotation_euler = (d + Vector((0.4, 0.0, 1.2))).to_track_quat('Z', 'Y').to_euler()
        scene.render.filepath = f"{out_dir}/poing_{label}.png"
        bpy.ops.render.render(write_still=True)
        paths.append(scene.render.filepath)

    (scene.camera, scene.render.resolution_x, scene.render.resolution_y, scene.render.film_transparent,
     scene.render.filepath, bg.inputs[1].default_value, scene.view_settings.view_transform, sun.rotation_euler) = saved

    for o, h in hidden.items():
        o.hide_render = h

    return paths


def attach_fists(character, fist_name="RM_Poing_v3", scale_up=1.12):
    """Remplace les mains MPFB par le poing modélisé : main du corps masquée, un poing par côté (le modèle est une
    main gauche, la droite est son miroir) attaché à l'os de la main, moignon du poignet caché dans l'avant-bras."""
    from mathutils import Matrix

    body = bpy.data.objects[f"{character}_v3_Corps"]
    rig = bpy.data.objects[f"{character}_v3_Rig"]
    src = bpy.data.objects[fist_name]
    hand = {g.index for g in body.vertex_groups if g.name.split("_")[0] in ("hand", "thumb", "index", "middle", "ring", "pinky")}
    group = body.vertex_groups.get("Main_MPFB") or body.vertex_groups.new(name="Main_MPFB")
    group.remove(range(len(body.data.vertices)))
    group.add([v.index for v in body.data.vertices if sum(x.weight for x in v.groups if x.group in hand) > 0.5], 1.0, 'REPLACE')
    mask = body.modifiers.get("Sans main") or body.modifiers.new("Sans main", 'MASK')
    mask.vertex_group = group.name
    mask.invert_vertex_group = True

    with bpy.context.temp_override(object=body, active_object=body):
        bpy.ops.object.modifier_move_to_index(modifier=mask.name, index=len(body.modifiers) - 2)

    bpy.context.view_layer.update()
    mw = rig.matrix_world
    made = []

    for side in "lr":
        P = lambda n, tail=False: mw @ (rig.pose.bones[f"{n}_{side}"].tail if tail else rig.pose.bones[f"{n}_{side}"].head)
        wrist = P("hand")
        fwd = (P("middle_01") - wrist).normalized()
        palm = P("middle_03", True) - P("middle_01")              # les doigts pliés vont vers la paume
        back = -(palm - fwd * palm.dot(fwd)).normalized()
        thumb = P("index_01") - P("pinky_01")
        thumb = (thumb - fwd * thumb.dot(fwd) - back * thumb.dot(back)).normalized()
        knuckles = (P("index_01") - P("pinky_01")).length
        k = knuckles / 0.064 * scale_up                           # 0,064 m : largeur des articulations du modèle
        name = f"{character}_v3_Poing_{side}"

        if name in bpy.data.objects:
            bpy.data.objects.remove(bpy.data.objects[name])

        obj = src.copy()
        obj.data = src.data.copy()
        obj.name = name
        obj.modifiers.clear()
        bpy.data.collections[f"{character}_v3"].objects.link(obj)
        # Colonnes : X modèle → côté du pouce, Y → avant, Z → dos de la main (le miroir de la main droite en découle).
        basis = Matrix((thumb, fwd, back)).transposed() * k
        world = Matrix.Translation(wrist - fwd * 0.004 * k) @ basis.to_4x4()
        obj.parent = rig
        obj.parent_type = 'BONE'
        obj.parent_bone = f"hand_{side}"
        bpy.context.view_layer.update()
        obj.matrix_world = world

        line = obj.modifiers.new("Outline", 'SOLIDIFY')
        line.thickness = 0.0042 / k
        line.offset = 1.0
        line.use_flip_normals = True
        line.material_offset = 1
        made.append(obj)

    return made
