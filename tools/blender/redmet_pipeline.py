"""RED MET — pipeline personnages dans Blender (exécuté par le MCP Blender).

Étapes, pour un modèle TripoSG en pose en T, debout sur z = 0, face à -Y :
1. clean_mesh      : supprime les fragments, remaille en volume (étanche), réduit, lisse.
2. paint_from_concept : couleurs par sommet en aplats, projetées depuis l'image d'entrée de TripoSG.
   Le concept est réduit à une petite palette (k-moyennes) ; chaque sommet prend la couleur
   dominante autour de sa projection, sans les traits noirs du dessin.
3. add_rig         : squelette Rigify (metarig humain sans visage ni doigts) ajusté à la silhouette,
   peau par poids automatiques. Le metarig sert de squelette FK pour les animations de sprites.

Usage dans Blender :
    exec(open(r"<dépôt>/tools/blender/redmet_pipeline.py", encoding="utf-8").read())
    obj = clean_mesh(bpy.data.objects["Voyou_triposg_s42"], "Voyou")
"""
import bmesh
import bpy
import numpy as np
from mathutils import Vector


# --------------------------------------------------------------------------- Nettoyage

def _islands(bm):
    bm.verts.ensure_lookup_table()
    seen = set()
    parts = []

    for v in bm.verts:
        if v.index in seen:
            continue

        stack, part = [v], []
        seen.add(v.index)

        while stack:
            x = stack.pop()
            part.append(x)

            for e in x.link_edges:
                o = e.other_vert(x)

                if o.index not in seen:
                    seen.add(o.index)
                    stack.append(o)

        parts.append(part)

    return sorted(parts, key=len, reverse=True)


def clean_mesh(source, name, voxel=0.012, faces=14000, collection=None):
    """Copie propre du modèle : un seul volume étanche, ~faces faces, origine aux pieds."""
    obj = source.copy()
    obj.data = source.data.copy()
    obj.name = name
    obj.data.name = name
    (collection or source.users_collection[0]).objects.link(obj)

    # Transformations appliquées : coordonnées monde dans le maillage.
    obj.data.transform(obj.matrix_world)
    obj.matrix_world.identity()

    bm = bmesh.new()
    bm.from_mesh(obj.data)
    parts = _islands(bm)

    # On garde le corps et les morceaux d'au moins 15 % de sa taille (ex. une main détachée).
    keep = {v for part in parts if len(part) >= 0.15 * len(parts[0]) for v in part}
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v not in keep], context='VERTS')
    bm.to_mesh(obj.data)
    bm.free()

    remesh = obj.modifiers.new("Remesh", 'REMESH')
    remesh.mode = 'VOXEL'
    remesh.voxel_size = voxel
    _apply(obj, remesh)

    count = len(obj.data.polygons)

    if count > faces:
        decimate = obj.modifiers.new("Decimate", 'DECIMATE')
        decimate.ratio = faces / count
        _apply(obj, decimate)

    for poly in obj.data.polygons:
        poly.use_smooth = True

    # Origine au centre des pieds.
    xs = [v.co.x for v in obj.data.vertices]
    ys = [v.co.y for v in obj.data.vertices]
    zmin = min(v.co.z for v in obj.data.vertices)
    offset = Vector(((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, zmin))
    obj.data.transform(_translation(-offset))
    obj.location = offset
    return obj


def _apply(obj, modifier):
    with bpy.context.temp_override(object=obj, active_object=obj, selected_objects=[obj]):
        bpy.ops.object.modifier_apply(modifier=modifier.name)


def _translation(offset):
    from mathutils import Matrix
    return Matrix.Translation(offset)


# ------------------------------------------------------------------- Couleurs du concept

def _kmeans(pixels, k, iterations=20, seed=0):
    rng = np.random.default_rng(seed)
    centers = pixels[rng.choice(len(pixels), k, replace=False)].astype(float)

    for _ in range(iterations):
        d = ((pixels[:, None, :] - centers[None, :, :]) ** 2).sum(axis=2)
        labels = d.argmin(axis=1)

        for i in range(k):
            members = pixels[labels == i]

            if len(members):
                centers[i] = members.mean(axis=0)

    return centers, labels


def paint_from_concept(obj, image_path, colors=7, window=9, line_luma=0.12):
    """Couleurs par sommet (attribut « Col ») en aplats, depuis l'image RGBA d'entrée de TripoSG."""
    img = bpy.data.images.load(image_path, check_existing=True)
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)[::-1]  # ligne 0 = haut
    alpha = px[..., 3] > 0.5
    rgb = px[..., :3]

    rows = np.where(alpha.any(axis=1))[0]
    cols = np.where(alpha.any(axis=0))[0]
    top, bottom, left, right = rows.min(), rows.max(), cols.min(), cols.max()

    # Palette des zones du personnage ; les traits (très sombres) forment leur propre famille.
    fg = rgb[alpha]
    sample = fg[np.random.default_rng(1).choice(len(fg), min(20000, len(fg)), replace=False)]
    centers, _ = _kmeans(sample, colors)
    luma = centers @ np.array([0.299, 0.587, 0.114])
    line_label = int(luma.argmin()) if luma.min() < line_luma else -1

    flat = rgb.reshape(-1, 3)
    d = ((flat[:, None, :] - centers[None, :, :]) ** 2).sum(axis=2)
    labels = d.argmin(axis=1).reshape(h, w)
    labels[~alpha] = -2

    mesh = obj.data
    world = [obj.matrix_world @ v.co for v in mesh.vertices]
    xs = [p.x for p in world]
    zs = [p.z for p in world]
    x0, x1, z0, z1 = min(xs), max(xs), min(zs), max(zs)
    half = window // 2
    vertex_colors = np.zeros((len(world), 4), dtype=np.float32)

    for i, p in enumerate(world):
        u = int(round(left + (p.x - x0) / (x1 - x0) * (right - left)))
        v = int(round(bottom - (p.z - z0) / (z1 - z0) * (bottom - top)))
        patch = labels[max(0, v - half):v + half + 1, max(0, u - half):u + half + 1].ravel()
        patch = patch[patch >= 0]

        if len(patch) == 0:
            label = line_label if line_label >= 0 else 0
        else:
            counts = np.bincount(patch, minlength=colors)

            # Les traits ne gagnent que s'ils occupent presque toute la fenêtre (vêtement noir).
            if line_label >= 0 and counts[line_label] < 0.85 * counts.sum():
                counts[line_label] = 0

            label = int(counts.argmax())

        vertex_colors[i, :3] = centers[label]
        vertex_colors[i, 3] = 1.0

    attr = mesh.color_attributes.get("Col") or mesh.color_attributes.new("Col", 'FLOAT_COLOR', 'POINT')
    attr.data.foreach_set("color", vertex_colors.ravel())
    mesh.color_attributes.active_color = attr
    return centers


def _srgb_to_linear(c):
    c = np.asarray(c, dtype=np.float32)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


# Zones du corps en pose en T : (nom, test sur (x relatif à l'envergure, z relatif à la taille), nb de teintes).
# Une zone à 1 teinte devient un aplat (couleur médiane, sans traits ni ombres dessinées) ;
# au-delà, chaque sommet prend la teinte dominante de la zone autour de sa projection.
# « hem » : hauteur relative du bas du haut (sweat, veste), mesurée sur le concept.
DEFAULT_ZONES = [
    ("pieds", lambda xr, zr, hem: zr < 0.09, 3),
    ("jambes", lambda xr, zr, hem: zr < hem, 1),
    ("mains", lambda xr, zr, hem: xr > 0.80, 1),
    # Le dos de la tête prend la couleur du buste (capuche, col) : vue de face, le visage domine.
    ("tete", lambda xr, zr, hem: zr > 0.82 and xr < 0.16, 2, "buste"),
    ("buste", lambda xr, zr, hem: True, 1),
]


def _find_hem(rgb, fg, xr, zr, low=0.30, high=0.75):
    """Hauteur relative du bas du haut : là où la colonne centrale passe de la couleur des jambes à celle du buste."""
    center = fg & (xr < 0.12)
    legs = np.median(rgb[center & (zr > low) & (zr < low + 0.08)], axis=0)
    torso = np.median(rgb[center & (zr > high - 0.08) & (zr < high)], axis=0)
    rows = sorted(set(np.where(center & (zr > low) & (zr < high))[0]), reverse=True)  # du bas vers le haut

    for y in rows:
        row = rgb[y][center[y]]

        if len(row) == 0:
            continue

        color = np.median(row, axis=0)

        if ((color - torso) ** 2).sum() < ((color - legs) ** 2).sum():
            return float(zr[y][center[y]].mean())

    return 0.5


def paint_zones(obj, image_path, zones=None, window=9, line_luma=0.06):
    """Couleurs par sommet (attribut « Col », linéaires) en aplats par zone du corps."""
    zones = zones or DEFAULT_ZONES
    img = bpy.data.images.load(image_path, check_existing=True)
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)[::-1]  # ligne 0 = haut
    alpha = px[..., 3] > 0.5
    rgb = px[..., :3]
    luma = rgb @ np.array([0.299, 0.587, 0.114], dtype=np.float32)

    rows = np.where(alpha.any(axis=1))[0]
    cols = np.where(alpha.any(axis=0))[0]
    top, bottom, left, right = rows.min(), rows.max(), cols.min(), cols.max()

    mesh = obj.data
    world = [obj.matrix_world @ v.co for v in mesh.vertices]
    xs = np.array([p.x for p in world])
    zs = np.array([p.z for p in world])
    x0, x1, z0, z1 = xs.min(), xs.max(), zs.min(), zs.max()
    cx, span, H = (x0 + x1) / 2, (x1 - x0) / 2, z1 - z0

    # Zone de chaque pixel et de chaque sommet, avec les mêmes règles.
    yy, xx = np.mgrid[0:h, 0:w]
    px_x = x0 + (xx - left) / (right - left) * (x1 - x0)
    px_z = z0 + (bottom - yy) / (bottom - top) * (z1 - z0)
    px_xr = np.abs(px_x - cx) / span
    px_zr = (px_z - z0) / H

    hem = _find_hem(rgb, alpha & (luma > line_luma), px_xr, px_zr)

    def zone_of(xr, zr):
        for index, zone in enumerate(zones):
            if zone[1](xr, zr, hem):
                return index
        return len(zones) - 1

    pixel_zone = np.full((h, w), -1, dtype=np.int32)
    ys_fg, xs_fg = np.where(alpha)

    for y, x in zip(ys_fg, xs_fg):
        pixel_zone[y, x] = zone_of(px_xr[y, x], px_zr[y, x])

    vertex_zone = [zone_of(abs(p.x - cx) / span, (p.z - z0) / H) for p in world]
    colors = np.zeros((len(world), 4), dtype=np.float32)
    report = {"ourlet": round(float(hem), 3)}
    normals = [(obj.matrix_world.to_3x3() @ v.normal).normalized() for v in mesh.vertices]

    flat = {}

    for index, (name, _, k, *_) in enumerate(zones):
        # Les traits du dessin ne comptent que pour les zones détaillées (un visage dans l'ombre est noir).
        mask = (pixel_zone == index) & (luma > line_luma) if k <= 1 else pixel_zone == index

        if not mask.any():
            mask = pixel_zone == index

        pixels = rgb[mask]

        if len(pixels) == 0:
            continue

        members = [i for i, z in enumerate(vertex_zone) if z == index]

        if k <= 1:
            color = np.median(pixels, axis=0)
            colors[members, :3] = _srgb_to_linear(color)
            flat[name] = _srgb_to_linear(color)
            report[name] = [round(float(c), 2) for c in color]
            continue

        sample = pixels[np.random.default_rng(index).choice(len(pixels), min(8000, len(pixels)), replace=False)]
        centers, _ = _kmeans(sample, k)
        d = ((rgb[..., None, :] - centers[None, None, :, :]) ** 2).sum(axis=3)
        labels = np.where(pixel_zone == index, d.argmin(axis=2), -1)
        half = window // 2
        main_label = int(np.bincount(labels[labels >= 0], minlength=k).argmax())

        for i in members:
            p = world[i]

            # Face cachée (tournée vers l'arrière) : la projection frontale la traverse, on prend la teinte principale.
            if normals[i].y > 0.15:
                colors[i, :3] = _srgb_to_linear(centers[main_label])
                continue

            u = int(round(left + (p.x - x0) / (x1 - x0) * (right - left)))
            v = int(round(bottom - (p.z - z0) / (z1 - z0) * (bottom - top)))
            patch = labels[max(0, v - half):v + half + 1, max(0, u - half):u + half + 1].ravel()
            patch = patch[patch >= 0]
            label = int(np.bincount(patch, minlength=k).argmax()) if len(patch) else 0
            colors[i, :3] = _srgb_to_linear(centers[label])

        report[name] = [[round(float(c), 2) for c in col] for col in centers]

    # Faces cachées des zones qui empruntent la couleur d'une autre zone.
    for index, zone in enumerate(zones):
        if len(zone) > 3 and zone[3] in flat:
            for i, z in enumerate(vertex_zone):
                if z == index and normals[i].y > 0.15:
                    colors[i, :3] = flat[zone[3]]

    colors[:, 3] = 1.0
    attr = mesh.color_attributes.get("Col") or mesh.color_attributes.new("Col", 'FLOAT_COLOR', 'POINT')
    attr.data.foreach_set("color", colors.ravel())
    mesh.color_attributes.active_color = attr
    return report


# --------------------------------------------------------------------------- Squelette

def _slice_extent(points, zlo, zhi):
    sel = [p for p in points if zlo <= p.z <= zhi]
    return sel


def landmarks(obj):
    """Repères en pose en T, mesurés sur le maillage (mètres, repère du monde)."""
    pts = [obj.matrix_world @ v.co for v in obj.data.vertices]
    H = max(p.z for p in pts)
    cx = (min(p.x for p in pts) + max(p.x for p in pts)) / 2
    cy = sum(p.y for p in pts) / len(pts)

    # Bras : sommets loin de l'axe ; leur hauteur moyenne donne l'épaule et le poignet.
    span = max(abs(p.x - cx) for p in pts)
    arm = [p for p in pts if abs(p.x - cx) > 0.55 * span]
    arm_z = float(np.median([p.z for p in arm]))
    hand_x = 0.93 * span
    wrist_x = 0.80 * span

    # Jambes : centre de chaque jambe à hauteur de genou.
    knee = [p for p in pts if abs(p.z - 0.28 * H) < 0.02 * H]
    left_leg = [p.x - cx for p in knee if p.x > cx]
    leg_x = float(np.median(left_leg)) if left_leg else 0.1 * H

    return {
        "H": H, "cx": cx, "cy": cy, "arm_z": arm_z, "span": span,
        "hand_x": hand_x, "wrist_x": wrist_x, "leg_x": leg_x,
    }


def add_rig(obj, name, marks=None):
    """Metarig humain Rigify ajusté au personnage, puis peau automatique. Renvoie l'armature.

    marks : repères donnés à la main (mêmes clés que landmarks) quand la mesure automatique se trompe,
    par exemple quand des cheveux longs ou une jupe large écartent des sommets loin de l'axe (Mara)."""
    import addon_utils
    addon_utils.enable("rigify", default_set=True, persistent=True)

    # Reprise : on repart d'une peau vierge.
    for modifier in [m for m in obj.modifiers if m.type == 'ARMATURE']:
        obj.modifiers.remove(modifier)

    obj.vertex_groups.clear()
    obj.parent = None

    # matrix_world n'est à jour qu'après une mise à jour de la scène (juste après clean_mesh, par exemple).
    bpy.context.view_layer.update()
    L = marks or landmarks(obj)
    H, cx, cy, az = L["H"], L["cx"], L["cy"], L["arm_z"]
    lx = L["leg_x"]

    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.object.armature_human_metarig_add()
    rig = bpy.context.active_object
    rig.name = name
    rig.data.name = name

    for c in list(rig.users_collection):
        c.objects.unlink(rig)

    obj.users_collection[0].objects.link(rig)

    bpy.ops.object.mode_set(mode='EDIT')
    eb = rig.data.edit_bones

    # Pas de visage ni de doigts : inutiles pour des sprites de cette taille.
    for b in list(eb):
        if b.name.startswith(("face", "nose", "lip", "jaw", "chin", "ear", "brow", "lid", "cheek",
                              "forehead", "temple", "tongue", "teeth", "eye", "thumb", "f_index",
                              "f_middle", "f_ring", "f_pinky", "palm", "breast", "pelvis")):
            eb.remove(b)

    def place(bone, head, tail):
        b = eb[bone]
        b.head = Vector(head)
        b.tail = Vector(tail)

    shoulder_x = 0.11 * H
    elbow_x = (shoulder_x + L["wrist_x"]) / 2

    # Colonne : bassin → tête (spine, spine.001 … spine.006).
    spine_z = [0.53, 0.60, 0.67, 0.74, 0.80, 0.85, 0.89, 1.0]

    for i, bone in enumerate(["spine", "spine.001", "spine.002", "spine.003", "spine.004", "spine.005", "spine.006"]):
        place(bone, (cx, cy, spine_z[i] * H), (cx, cy, spine_z[i + 1] * H))

    for side, s in (("L", 1), ("R", -1)):
        place(f"shoulder.{side}", (cx + s * 0.02 * H, cy, az + 0.02 * H), (cx + s * shoulder_x, cy, az + 0.01 * H))
        place(f"upper_arm.{side}", (cx + s * shoulder_x, cy, az), (cx + s * elbow_x, cy + 0.01, az))
        place(f"forearm.{side}", (cx + s * elbow_x, cy + 0.01, az), (cx + s * L["wrist_x"], cy, az))
        place(f"hand.{side}", (cx + s * L["wrist_x"], cy, az), (cx + s * L["hand_x"], cy, az))
        place(f"thigh.{side}", (cx + s * lx, cy, 0.52 * H), (cx + s * lx, cy - 0.01, 0.28 * H))
        place(f"shin.{side}", (cx + s * lx, cy - 0.01, 0.28 * H), (cx + s * lx, cy + 0.01, 0.06 * H))
        place(f"foot.{side}", (cx + s * lx, cy + 0.01, 0.06 * H), (cx + s * lx, cy - 0.10 * H, 0.015 * H))
        place(f"toe.{side}", (cx + s * lx, cy - 0.10 * H, 0.015 * H), (cx + s * lx, cy - 0.14 * H, 0.015 * H))
        place(f"heel.02.{side}", (cx + s * (lx - 0.03 * H), cy + 0.03 * H, 0.0), (cx + s * (lx + 0.03 * H), cy + 0.03 * H, 0.0))

    bpy.ops.object.mode_set(mode='OBJECT')

    # Le talon (repère Rigify) ne déforme rien.
    for side in ("L", "R"):
        rig.data.bones[f"heel.02.{side}"].use_deform = False

    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    return rig
