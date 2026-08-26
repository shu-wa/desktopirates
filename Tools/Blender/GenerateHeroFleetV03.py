"""Build the v03 player fleet as reproducible Blender-authored low-poly assets.

The silhouettes are informed by historical Caribbean age-of-sail vessels: a raft,
sloop, brigantine, corvette, frigate, galleon and a heavily decorated flagship.
Everything is generated with Blender's bundled mesh/curve tools; no paid add-on or
machine-specific dependency is required.

Run:
  blender --background --python Tools/Blender/GenerateHeroFleetV03.py -- <project-root>
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix


PROJECT_ROOT = Path(sys.argv[sys.argv.index("--") + 1]).resolve() if "--" in sys.argv else Path.cwd()
SOURCE_ROOT = PROJECT_ROOT / "ArtSource" / "Models" / "Blender" / "ShipsV03"
EXPORT_ROOT = PROJECT_ROOT / "Assets" / "Desktopirates" / "Resources" / "Models" / "ShipsV03"

COLORS = {
    "HullEbony_v03": (0.105, 0.040, 0.018, 1.0),
    "DeckOak_v03": (0.50, 0.225, 0.055, 1.0),
    "SailCanvas_v03": (0.80, 0.68, 0.45, 1.0),
    "EnemySail_v03": (0.19, 0.025, 0.020, 1.0),
    "Rope_v03": (0.27, 0.125, 0.035, 1.0),
    "Iron_v03": (0.045, 0.052, 0.052, 1.0),
    "Brass_v03": (0.88, 0.43, 0.045, 1.0),
    "GlassGlow_v03": (1.0, 0.35, 0.025, 1.0),
}


def reset_scene() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for name, color in COLORS.items():
        mat = bpy.data.materials.new(name)
        mat.diffuse_color = color
        mat.roughness = 0.82
        if name == "GlassGlow_v03":
            mat.use_nodes = True
            principled = next((node for node in mat.node_tree.nodes if node.type == "BSDF_PRINCIPLED"), None)
            if principled:
                principled.inputs["Base Color"].default_value = color
                principled.inputs["Emission Color"].default_value = color
                principled.inputs["Emission Strength"].default_value = 1.4


def finish(obj, material_name: str, bevel=0.0):
    if obj.data and hasattr(obj.data, "materials"):
        obj.data.materials.append(bpy.data.materials[material_name])
    if bevel > 0.0 and obj.type == "MESH":
        mod = obj.modifiers.new("Low-poly edge bevel", "BEVEL")
        mod.width = bevel
        mod.segments = 1
    if obj.type == "MESH":
        for polygon in obj.data.polygons:
            polygon.use_smooth = False
    return obj


def cube(name, location, scale, material_name, rotation=(0.0, 0.0, 0.0), bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = tuple(value * 0.5 for value in scale)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, material_name, bevel)


def cylinder(name, location, radius, depth, material_name, vertices=8, rotation=(0.0, 0.0, 0.0), bevel=0.0):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    return finish(obj, material_name, bevel)


def sphere(name, location, scale, material_name):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1, radius=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, material_name)


def rope(name: str, points, radius=0.012, material_name="Rope_v03"):
    curve = bpy.data.curves.new(name + "_Curve", type="CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 1
    curve.bevel_depth = radius
    curve.bevel_resolution = 0
    spline = curve.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for item, point in zip(spline.points, points):
        item.co = (*point, 1.0)
    obj = bpy.data.objects.new(name, curve)
    bpy.context.collection.objects.link(obj)
    return finish(obj, material_name)


def create_lofted_hull(name: str, length: float, width: float, depth: float):
    # Each longitudinal station carries port gunwale, port chine, keel,
    # starboard chine and starboard gunwale. The stern is broad and tall;
    # the bow resolves to a narrow raked stem.
    stations = [
        (-0.50, 0.72, 0.18),
        (-0.37, 0.98, 0.08),
        (-0.10, 1.00, 0.00),
        (0.20, 0.91, 0.04),
        (0.39, 0.63, 0.13),
        (0.53, 0.16, 0.25),
    ]
    vertices = []
    for y_ratio, beam_ratio, sheer in stations:
        y = length * y_ratio
        half = width * 0.5 * beam_ratio
        top = depth * (0.48 + sheer)
        chine = -depth * (0.20 + 0.18 * (1.0 - beam_ratio))
        keel = -depth * (0.56 - 0.10 * abs(y_ratio))
        vertices.extend([
            (-half, y, top), (-half * 0.72, y, chine), (0.0, y, keel),
            (half * 0.72, y, chine), (half, y, top),
        ])
    faces = []
    for station in range(len(stations) - 1):
        a = station * 5
        b = (station + 1) * 5
        for strip in range(4):
            faces.append((a + strip, b + strip, b + strip + 1, a + strip + 1))
    faces += [(0, 1, 2, 3, 4), (25, 29, 28, 27, 26)]
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    finish(obj, "HullEbony_v03", min(width, depth) * 0.025)

    # Cambered deck follows the same shear instead of floating as a rectangular box.
    deck_vertices = []
    # Cap every station, including the narrow raked bow. Omitting the final station
    # left an open wedge through which Unity's animated ocean could be seen.
    for y_ratio, beam_ratio, sheer in stations:
        y = length * y_ratio
        # Slightly overlap the gunwale and keep the entire cambered surface above
        # the hull rim. The former inset left a narrow seam where the animated sea
        # could show through, especially along the forward quarter.
        half = width * 0.505 * beam_ratio
        z = depth * (0.57 + sheer)
        deck_vertices.extend([(-half, y, z), (0.0, y, z + depth * 0.065), (half, y, z)])
    deck_faces = []
    for station in range(len(stations) - 1):
        a = station * 3
        b = (station + 1) * 3
        deck_faces.extend([(a, b, b + 1, a + 1), (a + 1, b + 1, b + 2, a + 2)])
    deck_mesh = bpy.data.meshes.new(name + "_DeckMesh")
    deck_mesh.from_pydata(deck_vertices, [], deck_faces)
    deck_mesh.update()
    deck = bpy.data.objects.new(name.replace("Hull", "Deck"), deck_mesh)
    bpy.context.collection.objects.link(deck)
    deck_solidify = deck.modifiers.new("Sealed deck thickness", "SOLIDIFY")
    deck_solidify.thickness = depth * 0.055
    deck_solidify.offset = -1.0
    finish(deck, "DeckOak_v03")
    return obj


def create_billow_sail(name, mast_y, center_z, width, height, sail_material, triangular=False):
    columns, rows = (3, 3)
    vertices = []
    for row in range(rows + 1):
        v = row / rows
        z = center_z + height * (0.5 - v)
        for column in range(columns + 1):
            u = column / columns
            if triangular:
                usable = max(0.06, v)
                x = width * (u - 0.5) * usable
            else:
                taper = 0.88 + 0.12 * v
                x = width * (u - 0.5) * taper
            # Wind fills the canvas toward the bow. The former negative offset made
            # every sail read as if the ship were travelling stern-first in Unity.
            billow = 0.055 + math.sin(math.pi * u) * math.sin(math.pi * v) * 0.090
            vertices.append((x, mast_y + billow, z))
    faces = []
    stride = columns + 1
    for row in range(rows):
        for column in range(columns):
            a = row * stride + column
            faces.append((a, a + stride, a + stride + 1, a + 1))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    solidify = obj.modifiers.new("Sail cloth thickness", "SOLIDIFY")
    solidify.thickness = 0.018
    # Extrude equally to both sides; the default one-sided extrusion pushed the
    # smallest upper sails back across the mast despite the forward billow.
    solidify.offset = 0.0
    return finish(obj, sail_material)


def add_railing(prefix, length, width, deck_z, tier):
    rail_z = deck_z + 0.19
    for side in (-1, 1):
        x = side * width * 0.49
        cube(f"{prefix}_{'Port' if side < 0 else 'Starboard'}_TopRail", (x, -length * 0.03, rail_z),
             (0.045, length * 0.78, 0.045), "Brass_v03", bevel=0.012)
        count = 5 + tier * 2
        for index in range(count):
            y = -length * 0.39 + (length * 0.76) * index / max(1, count - 1)
            cylinder(f"{prefix}_RailBaluster_{side}_{index}", (x, y, deck_z + 0.095), 0.018, 0.19,
                     "Rope_v03", vertices=6)


def add_stern_architecture(length, width, depth, tier):
    stern_y = -length * 0.41
    base_z = depth * 0.70
    cabin_height = 0.34 + tier * 0.065
    cube("Stern_GreatCabin", (0.0, stern_y, base_z + cabin_height * 0.30),
         (width * 0.82, length * 0.21, cabin_height), "HullEbony_v03", bevel=0.035)
    cube("Stern_Quarterdeck", (0.0, stern_y + length * 0.015, base_z + cabin_height * 0.57),
         (width * 0.91, length * 0.25, 0.075), "DeckOak_v03", bevel=0.018)
    window_count = min(2 + tier, 6)
    for index in range(window_count):
        x = width * 0.62 * (index / max(1, window_count - 1) - 0.5)
        cube(f"Stern_Gallery_Window_{index + 1}", (x, stern_y - length * 0.108, base_z + cabin_height * 0.30),
             (width * 0.10, 0.025, cabin_height * 0.31), "GlassGlow_v03", bevel=0.012)
    cube("Stern_Gallery_BrassRail", (0.0, stern_y - length * 0.121, base_z + cabin_height * 0.53),
         (width * 0.78, 0.026, 0.035), "Brass_v03", bevel=0.01)
    if tier >= 4:
        cube("Upper_Stern_Castle", (0.0, stern_y + length * 0.035, base_z + cabin_height * 0.77),
             (width * 0.68, length * 0.16, cabin_height * 0.36), "HullEbony_v03", bevel=0.03)
        for side in (-1, 1):
            sphere(f"Stern_Lantern_{side}", (side * width * 0.39, stern_y - length * 0.12, base_z + cabin_height * 0.78),
                   (0.055, 0.055, 0.08), "GlassGlow_v03")


def add_bow_details(length, width, depth, tier):
    bow_y = length * 0.52
    deck_z = depth * 0.60
    cylinder("Bowsprit_Main", (0.0, bow_y + length * 0.18, deck_z + 0.10), 0.035 + tier * 0.003,
             length * 0.52, "Rope_v03", vertices=8, rotation=(math.radians(-77), 0.0, 0.0))
    sphere("Prow_Figurehead", (0.0, bow_y + length * 0.08, deck_z - 0.05),
           (0.085 + tier * 0.006, 0.12, 0.14 + tier * 0.012), "Brass_v03")
    if tier >= 3:
        cube("Forecastle_Deck", (0.0, length * 0.31, deck_z + 0.12),
             (width * 0.62, length * 0.22, 0.10), "DeckOak_v03", bevel=0.02)


def add_gun_battery(length, width, depth, tier):
    port_count = min(1 + tier, 6)
    z = depth * 0.28
    for side in (-1, 1):
        for index in range(port_count):
            y = -length * 0.30 + length * 0.57 * index / max(1, port_count - 1)
            cube(f"{'Port' if side < 0 else 'Starboard'}_Gunport_{index + 1}",
                 (side * width * 0.505, y, z), (0.035, length * 0.075, depth * 0.22), "Iron_v03", bevel=0.009)
            if tier >= 2 and index % 2 == 0:
                cylinder(f"{'Port' if side < 0 else 'Starboard'}_Cannon_{index + 1}",
                         (side * width * 0.55, y, z), 0.027 + tier * 0.0015, width * 0.22,
                         "Iron_v03", vertices=8, rotation=(0.0, math.radians(90), 0.0))


def add_mast_rig(mast_index, mast_y, base_z, height, width, sail_material, sail_rows, tier):
    mast_radius = 0.035 + tier * 0.003
    cylinder(f"Mast_{mast_index}_Lower", (0.0, mast_y, base_z + height * 0.45), mast_radius, height * 0.90,
             "Rope_v03", vertices=8)
    cylinder(f"Mast_{mast_index}_Top", (0.0, mast_y, base_z + height * 0.96), mast_radius * 0.72, height * 0.40,
             "Rope_v03", vertices=8)
    cube(f"Mast_{mast_index}_FightingTop", (0.0, mast_y, base_z + height * 0.71),
         (width * 0.30, width * 0.18, 0.045), "DeckOak_v03", bevel=0.012)
    for row in range(sail_rows):
        row_width = width * (0.86 - row * 0.16)
        row_height = height * (0.34 - row * 0.035)
        center_z = base_z + height * (0.48 + row * 0.28)
        cube(f"Yard_{mast_index}_{row + 1}", (0.0, mast_y - 0.012, center_z + row_height * 0.48),
             (row_width * 1.18, 0.045, 0.045), "Rope_v03", bevel=0.008)
        create_billow_sail(f"Sail_{mast_index}_{row + 1}", mast_y, center_z, row_width, row_height, sail_material)
    # Shrouds and stays make the silhouette read as a working ship rather than a toy.
    for side in (-1, 1):
        rope(f"Rigging_Shroud_{mast_index}_{side}", [
            (side * width * 0.47, mast_y - width * 0.14, base_z + 0.05),
            (side * width * 0.10, mast_y, base_z + height * 0.70),
        ], radius=0.009)


def create_raft():
    for index in range(-3, 4):
        cylinder(f"Raft_Log_{index + 4}", (index * 0.16, 0.0, 0.03), 0.105, 1.62,
                 "HullEbony_v03", vertices=8, rotation=(math.radians(90), 0.0, 0.0), bevel=0.01)
    for y in (-0.55, 0.0, 0.55):
        cube(f"Raft_Lashed_Crossbeam_{y}", (0.0, y, 0.15), (1.18, 0.10, 0.10), "DeckOak_v03", bevel=0.018)
        for x in (-0.42, 0.42):
            rope(f"Raft_Lashing_{x}_{y}", [(x - 0.05, y, 0.21), (x + 0.05, y, 0.21)], radius=0.018)
    cylinder("Raft_Mast", (0.0, 0.12, 0.91), 0.045, 1.66, "Rope_v03", vertices=8)
    cube("Raft_Yard", (0.0, 0.10, 1.36), (0.92, 0.055, 0.055), "Rope_v03")
    create_billow_sail("Raft_Patched_Sail", 0.08, 1.03, 0.80, 0.86, "SailCanvas_v03", triangular=True)
    cube("Raft_Supply_Crate", (-0.30, -0.48, 0.25), (0.30, 0.30, 0.25), "DeckOak_v03", bevel=0.025)


def create_ship(tier: int, enemy=False):
    length = 2.15 + tier * 0.27
    width = 0.78 + tier * 0.105
    depth = 0.60 + tier * 0.055
    mast_count = 1 if tier == 1 else 2 if tier <= 2 else 3
    sail_rows = 1 if tier == 1 else 2 if tier <= 4 else 3
    sail_material = "EnemySail_v03" if enemy else "SailCanvas_v03"

    create_lofted_hull("Enemy_Corsair_Hull" if enemy else f"Tier_{tier}_Lofted_Hull", length, width, depth)
    deck_z = depth * 0.55
    add_railing(f"Tier_{tier}", length, width, deck_z, tier)
    add_stern_architecture(length, width, depth, tier)
    add_bow_details(length, width, depth, tier)
    add_gun_battery(length, width, depth, tier)

    if mast_count == 1:
        positions = [length * 0.04]
    elif mast_count == 2:
        positions = [length * 0.24, -length * 0.20]
    else:
        positions = [length * 0.28, 0.0, -length * 0.29]
    for index, mast_y in enumerate(positions, 1):
        height = 1.45 + tier * 0.13 - (0.15 if index == mast_count else 0.0)
        add_mast_rig(index, mast_y, deck_z, height, width, sail_material, sail_rows if index < 3 else max(1, sail_rows - 1), tier)

    # Longitudinal stays visually bind the rig together.
    mast_tops = [(0.0, y, deck_z + (1.45 + tier * 0.13) * 1.05) for y in positions]
    rope("Rigging_Forestay", [mast_tops[0], (0.0, length * 0.72, deck_z + 0.12)], radius=0.008)
    if len(mast_tops) > 1:
        for index in range(len(mast_tops) - 1):
            rope(f"Rigging_Stay_{index + 1}", [mast_tops[index], mast_tops[index + 1]], radius=0.008)
    rope("Rigging_Backstay", [mast_tops[-1], (0.0, -length * 0.53, deck_z + 0.18)], radius=0.008)

    if tier >= 4:
        cylinder("Capstan", (0.0, -length * 0.08, deck_z + 0.10), 0.09, 0.18, "Brass_v03", vertices=8)
        for side in (-1, 1):
            cylinder(f"Anchor_{side}", (side * width * 0.58, length * 0.34, deck_z - 0.10), 0.035, 0.30,
                     "Iron_v03", vertices=8, rotation=(math.radians(90), 0.0, 0.0))
    if tier == 5:
        cube("Flagship_Gilded_Stern_Crest", (0.0, -length * 0.525, deck_z + 0.67),
             (width * 0.24, 0.045, 0.42), "Brass_v03", rotation=(0.0, 0.0, math.radians(45)), bevel=0.025)
        cube("Flagship_Gilded_Keel_Stripe", (0.0, -length * 0.02, -depth * 0.34),
             (0.045, length * 0.82, 0.045), "Brass_v03", bevel=0.012)


def prepare() -> None:
    # Curves are converted to mesh so Unity/FBX sees the exact rigging geometry.
    for obj in list(bpy.context.scene.objects):
        if obj.type == "CURVE":
            bpy.context.view_layer.objects.active = obj
            obj.select_set(True)
            bpy.ops.object.convert(target="MESH")
            obj.select_set(False)
    bpy.ops.object.select_all(action="SELECT")
    for obj in list(bpy.context.selected_objects):
        if obj.type != "MESH":
            continue
        bpy.context.view_layer.objects.active = obj
        for modifier in list(obj.modifiers):
            try:
                bpy.ops.object.modifier_apply(modifier=modifier.name)
            except RuntimeError:
                pass
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        try:
            bpy.ops.uv.smart_project(island_margin=0.025)
        except RuntimeError:
            pass
        bpy.ops.object.mode_set(mode="OBJECT")


def rotate_for_runtime() -> None:
    rotation = Matrix.Rotation(math.pi, 4, "Z")
    for obj in bpy.context.scene.objects:
        obj.matrix_world = rotation @ obj.matrix_world


def save_and_export(name: str) -> None:
    SOURCE_ROOT.mkdir(parents=True, exist_ok=True)
    EXPORT_ROOT.mkdir(parents=True, exist_ok=True)
    prepare()
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE_ROOT / f"{name}.blend"), compress=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(EXPORT_ROOT / f"{name}.fbx"),
        use_selection=True,
        object_types={"MESH", "EMPTY"},
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="STRIP",
    )


def build_all() -> None:
    reset_scene()
    create_raft()
    rotate_for_runtime()
    save_and_export("player_tier_0_v03")
    for tier in range(1, 6):
        reset_scene()
        create_ship(tier)
        rotate_for_runtime()
        save_and_export(f"player_tier_{tier}_v03")


if __name__ == "__main__":
    build_all()
    print(f"desktopirates v03 hero fleet generated under {EXPORT_ROOT}")
