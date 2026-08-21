"""Generate the authored low-poly desktopirates model library with Blender.

Run with:
  blender --background --python Tools/Blender/GenerateLowPolyWorld.py -- <project-root>
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bpy


PROJECT_ROOT = Path(sys.argv[sys.argv.index("--") + 1]).resolve() if "--" in sys.argv else Path.cwd()
SOURCE_ROOT = PROJECT_ROOT / "ArtSource" / "Models" / "Blender"
EXPORT_ROOT = PROJECT_ROOT / "Assets" / "Desktopirates" / "Resources" / "Models"

MATERIAL_COLORS = {
    "HullWood_v02": (0.32, 0.11, 0.035, 1.0),
    "DeckWood_v02": (0.52, 0.25, 0.07, 1.0),
    "SailCanvas_v02": (0.78, 0.65, 0.42, 1.0),
    "EnemySail_v02": (0.18, 0.025, 0.025, 1.0),
    "HarborStone_v02": (0.40, 0.46, 0.43, 1.0),
    "HarborWood_v02": (0.43, 0.20, 0.055, 1.0),
    "Metal_v02": (0.12, 0.13, 0.12, 1.0),
    "Brass_v02": (0.83, 0.43, 0.055, 1.0),
    "Glow_v02": (1.0, 0.42, 0.025, 1.0),
}


def reset_scene() -> None:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for name, color in MATERIAL_COLORS.items():
        material = bpy.data.materials.new(name)
        material.diffuse_color = color


def material(name: str):
    return bpy.data.materials.get(name)


def finish_object(obj, material_name: str, bevel: float = 0.0):
    if obj.data and hasattr(obj.data, "materials"):
        obj.data.materials.append(material(material_name))
    if bevel > 0.0:
        modifier = obj.modifiers.new("Readable edge bevel", "BEVEL")
        modifier.width = bevel
        modifier.segments = 1
    if obj.type == "MESH":
        for polygon in obj.data.polygons:
            polygon.use_smooth = False
    return obj


def cube(name: str, location, scale, material_name: str, rotation=(0.0, 0.0, 0.0), bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = (scale[0] * 0.5, scale[1] * 0.5, scale[2] * 0.5)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish_object(obj, material_name, bevel)


def cylinder(name: str, location, radius: float, depth: float, material_name: str, vertices=8, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    return finish_object(obj, material_name)


def sphere(name: str, location, scale, material_name: str):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish_object(obj, material_name)


def hull(name: str, length: float, width: float, height: float, material_name="HullWood_v02"):
    bow = length * 0.58
    stern = -length * 0.46
    top = height * 0.45
    bottom = -height * 0.55
    vertices = [
        (0.0, bow, top * 0.80), (-width * 0.50, length * 0.25, top), (width * 0.50, length * 0.25, top),
        (-width * 0.44, stern, top * 0.85), (width * 0.44, stern, top * 0.85),
        (0.0, bow * 0.86, bottom * 0.45), (-width * 0.25, length * 0.18, bottom), (width * 0.25, length * 0.18, bottom),
        (-width * 0.28, stern * 0.92, bottom * 0.72), (width * 0.28, stern * 0.92, bottom * 0.72),
    ]
    faces = [
        (0, 1, 2), (1, 3, 4), (1, 4, 2),
        (0, 5, 1), (1, 5, 6), (1, 6, 8), (1, 8, 3),
        (2, 7, 5), (0, 2, 5), (2, 4, 9), (2, 9, 7),
        (3, 8, 9), (3, 9, 4), (5, 7, 6), (6, 7, 9), (6, 9, 8),
    ]
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish_object(obj, material_name, min(width, height) * 0.035)


def sail(name: str, location, width: float, height: float, material_name: str, triangular=False):
    if triangular:
        vertices = [(0.0, 0.0, height * 0.50), (0.0, 0.0, -height * 0.50), (width, 0.0, -height * 0.35)]
        faces = [(0, 1, 2)]
    else:
        vertices = [(-width * 0.48, 0.0, height * 0.50), (-width * 0.42, 0.0, -height * 0.50),
                    (width * 0.48, 0.0, -height * 0.42), (width * 0.42, 0.0, height * 0.36)]
        faces = [(0, 1, 2, 3)]
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    solidify = obj.modifiers.new("Canvas thickness", "SOLIDIFY")
    solidify.thickness = 0.035
    return finish_object(obj, material_name)


def create_ship(tier: int, enemy=False):
    if tier == 0:
        for index in range(-2, 3):
            cylinder(f"Raft_Log_{index + 3}", (index * 0.22, 0.0, 0.08), 0.13, 1.55, "HullWood_v02", vertices=8, rotation=(math.radians(90), 0.0, 0.0))
        cube("Raft_Crossbeam_Fore", (0.0, 0.50, 0.20), (1.18, 0.12, 0.12), "HarborWood_v02", bevel=0.025)
        cube("Raft_Crossbeam_Aft", (0.0, -0.50, 0.20), (1.18, 0.12, 0.12), "HarborWood_v02", bevel=0.025)
        cylinder("Raft_Mast", (0.0, 0.10, 0.88), 0.065, 1.65, "HarborWood_v02", vertices=8)
        sail("Raft_SailCanvas", (0.04, 0.08, 1.25), 0.92, 1.08, "SailCanvas_v02")
        return

    length = 2.30 + tier * 0.24
    width = 0.92 + tier * 0.09
    hull_height = 0.70 + tier * 0.055
    mast_count = 1 if tier <= 2 else 2 if tier <= 4 else 3
    sail_material = "EnemySail_v02" if enemy else "SailCanvas_v02"

    hull("Enemy_Faceted_Hull" if enemy else f"Tier{tier}_Faceted_Hull", length, width, hull_height)
    cube("Raised_Deck", (0.0, -0.04, hull_height * 0.43), (width * 0.82, length * 0.72, 0.14), "DeckWood_v02", bevel=0.035)
    cube("Stern_Cabin", (0.0, -length * 0.33, hull_height * 0.78), (width * 0.72, length * 0.25, 0.45 + tier * 0.04), "HullWood_v02", bevel=0.045)
    cube("Cabin_Roof", (0.0, -length * 0.33, hull_height * 1.08), (width * 0.83, length * 0.30, 0.10), "Brass_v02", bevel=0.025)
    cube("Port_Gunwale", (-width * 0.47, -0.03, hull_height * 0.55), (0.07, length * 0.80, 0.10), "Brass_v02")
    cube("Starboard_Gunwale", (width * 0.47, -0.03, hull_height * 0.55), (0.07, length * 0.80, 0.10), "Brass_v02")

    mast_positions = [0.20] if mast_count == 1 else [0.42, -0.42] if mast_count == 2 else [0.52, 0.0, -0.50]
    for index, ratio in enumerate(mast_positions):
        mast_y = length * ratio
        mast_height = 1.90 if index == 0 else 1.65
        cylinder(f"Mast_{index + 1}", (0.0, mast_y, hull_height + mast_height * 0.46), 0.055, mast_height, "HarborWood_v02", vertices=8)
        cube(f"Yard_{index + 1}", (0.0, mast_y - 0.015, hull_height + mast_height * 0.66), (width * 1.08, 0.07, 0.07), "HarborWood_v02")
        sail(f"SailCanvas_{index + 1}", (0.0, mast_y - 0.04, hull_height + mast_height * 0.50), width * 0.82, mast_height * 0.62, sail_material)

    if tier >= 3:
        cube("Forecastle", (0.0, length * 0.34, hull_height * 0.72), (width * 0.68, length * 0.24, 0.28), "HullWood_v02", bevel=0.035)
    if tier >= 4:
        cube("Upper_Stern_Gallery", (0.0, -length * 0.39, hull_height * 1.18), (width * 0.80, length * 0.17, 0.23), "HullWood_v02", bevel=0.035)
    if tier >= 5:
        cube("Flagship_Quarterdeck", (0.0, -length * 0.15, hull_height * 0.82), (width * 0.86, length * 0.34, 0.12), "Brass_v02", bevel=0.025)

    cannon_rows = max(1, min(3, tier // 2 + 1))
    for side in (-1, 1):
        for row in range(cannon_rows):
            y = (row - (cannon_rows - 1) * 0.5) * 0.52
            cylinder(f"{'Port' if side < 0 else 'Starboard'}_Cannon_{row + 1}", (side * width * 0.52, y, hull_height * 0.50), 0.065, 0.36, "Metal_v02", vertices=8, rotation=(0.0, math.radians(90), 0.0))


def create_harbor():
    cylinder("Harbor_Stone_Island", (-0.6, 0.1, 0.0), 2.25, 0.24, "HarborStone_v02", vertices=12)
    cube("Harbor_Stone_Quay", (0.60, -0.20, 0.24), (3.8, 1.55, 0.32), "HarborStone_v02", bevel=0.08)
    for index in range(8):
        cube(f"Harbor_Pier_Plank_{index + 1}", (1.15, -1.10 - index * 0.22, 0.29), (1.10, 0.18, 0.12), "HarborWood_v02", bevel=0.025)
    for index, (x, y, sx, sy) in enumerate(((-1.15, 0.20, 1.25, 0.90), (0.15, 0.52, 1.05, 0.75), (1.10, 0.44, 0.86, 0.66))):
        cube(f"Harbor_Warehouse_{index + 1}", (x, y, 0.70), (sx, sy, 1.10), "HarborWood_v02", bevel=0.07)
        cube(f"Harbor_Roof_{index + 1}", (x, y, 1.32), (sx * 1.12, sy * 1.12, 0.18), "HullWood_v02", rotation=(0.0, math.radians(8 if index % 2 == 0 else -8), 0.0), bevel=0.035)
    cylinder("Harbor_Lighthouse_Tower", (-1.75, -0.15, 1.05), 0.30, 1.95, "HarborStone_v02", vertices=10)
    cylinder("Harbor_Lantern_Room", (-1.75, -0.15, 2.12), 0.39, 0.25, "Metal_v02", vertices=8)
    cylinder("Harbor_Beacon", (-1.75, -0.15, 2.28), 0.20, 0.18, "Glow_v02", vertices=8)
    cube("Harbor_Crane_Post", (1.30, 0.65, 1.15), (0.16, 0.16, 1.75), "HarborWood_v02")
    cube("Harbor_Crane_Arm", (1.68, 0.65, 1.88), (0.95, 0.14, 0.14), "HarborWood_v02", rotation=(0.0, math.radians(-9), 0.0))


def create_wreck():
    hull("Wreck_Broken_Hull", 2.05, 0.90, 0.62)
    cube("Wreck_Split_Beam", (0.42, 0.12, 0.26), (0.16, 1.85, 0.16), "HarborWood_v02", rotation=(math.radians(18), math.radians(32), math.radians(14)))
    cylinder("Wreck_Floating_Barrel", (-0.72, -0.40, 0.14), 0.18, 0.42, "HarborWood_v02", vertices=8, rotation=(math.radians(72), 0.0, math.radians(18)))


def create_treasure():
    cylinder("Treasure_Shoal", (0.0, 0.0, 0.0), 0.75, 0.15, "HarborStone_v02", vertices=10)
    cube("Treasure_Chest", (0.0, 0.0, 0.34), (0.92, 0.62, 0.54), "HullWood_v02", bevel=0.08)
    cube("Treasure_Gold_Band", (0.0, -0.32, 0.36), (0.18, 0.06, 0.56), "Brass_v02")


def create_gang_admiral():
    create_ship(5, enemy=True)
    cube("Admiral_Second_Gun_Deck", (0.0, -0.05, 0.95), (1.25, 2.25, 0.30), "HullWood_v02", bevel=0.05)
    cylinder("Admiral_Brass_Ram", (0.0, 1.95, 0.42), 0.18, 0.86, "Brass_v02", vertices=8, rotation=(math.radians(90), 0.0, 0.0))


def create_ghost_ship():
    create_ship(4, enemy=True)
    for index in range(-2, 3):
        sphere(f"Ghost_Glow_{index + 3}", (index * 0.26, -0.30, 1.25 + abs(index) * 0.04), (0.10, 0.10, 0.16), "Glow_v02")


def create_kraken():
    sphere("Kraken_Head", (0.0, 0.0, 0.92), (0.88, 0.78, 0.95), "HullWood_v02")
    for index in range(8):
        angle = index * math.tau / 8.0
        x = math.sin(angle) * 1.02
        y = math.cos(angle) * 1.02
        cylinder(f"Kraken_Tentacle_{index + 1}", (x, y, 0.34), 0.20, 1.42, "HullWood_v02", vertices=8,
                 rotation=(math.radians(58), 0.0, -angle))
        sphere(f"Kraken_Tentacle_Tip_{index + 1}", (x * 1.34, y * 1.34, 0.46), (0.24, 0.24, 0.34), "HullWood_v02")
    sphere("Kraken_Glow_Eye_L", (-0.26, -0.68, 1.05), (0.11, 0.08, 0.11), "Glow_v02")
    sphere("Kraken_Glow_Eye_R", (0.26, -0.68, 1.05), (0.11, 0.08, 0.11), "Glow_v02")


def create_poseidon():
    cylinder("Poseidon_Torso", (0.0, 0.0, 1.15), 0.58, 1.45, "HarborStone_v02", vertices=10)
    sphere("Poseidon_Head", (0.0, 0.0, 2.14), (0.46, 0.42, 0.50), "HarborStone_v02")
    cube("Poseidon_Shoulder_L", (-0.62, 0.0, 1.58), (0.62, 0.48, 0.48), "HarborStone_v02", rotation=(0.0, math.radians(-12), math.radians(18)), bevel=0.10)
    cube("Poseidon_Shoulder_R", (0.62, 0.0, 1.58), (0.62, 0.48, 0.48), "HarborStone_v02", rotation=(0.0, math.radians(12), math.radians(-18)), bevel=0.10)
    cylinder("Poseidon_Trident_Staff", (0.98, 0.0, 1.45), 0.065, 3.15, "Brass_v02", vertices=8)
    for offset in (-0.22, 0.0, 0.22):
        cube(f"Poseidon_Trident_Tine_{offset}", (0.98 + offset, 0.0, 3.00), (0.08, 0.10, 0.72 if offset == 0 else 0.55), "Brass_v02", bevel=0.02)
    for index in range(-2, 3):
        cube(f"Poseidon_Beard_{index + 3}", (index * 0.11, -0.28, 1.83 - abs(index) * 0.04), (0.08, 0.10, 0.62), "Glow_v02")


def prepare_meshes() -> None:
    bpy.ops.object.select_all(action="SELECT")
    for obj in bpy.context.selected_objects:
        if obj.type != "MESH":
            continue
        bpy.context.view_layer.objects.active = obj
        try:
            bpy.ops.object.modifier_apply(modifier="Readable edge bevel")
        except Exception:
            pass
        try:
            bpy.ops.object.modifier_apply(modifier="Canvas thickness")
        except Exception:
            pass
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        try:
            bpy.ops.uv.smart_project(island_margin=0.03)
        except Exception:
            pass
        bpy.ops.object.mode_set(mode="OBJECT")


def save_and_export(category: str, name: str) -> None:
    source_directory = SOURCE_ROOT / category
    export_directory = EXPORT_ROOT / category
    source_directory.mkdir(parents=True, exist_ok=True)
    export_directory.mkdir(parents=True, exist_ok=True)
    prepare_meshes()
    bpy.ops.wm.save_as_mainfile(filepath=str(source_directory / f"{name}.blend"), compress=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(export_directory / f"{name}.fbx"),
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
    for tier in range(6):
        reset_scene()
        create_ship(tier)
        save_and_export("Ships", f"player_tier_{tier}_v02")
    reset_scene()
    create_ship(3, enemy=True)
    save_and_export("Ships", "enemy_corsair_v02")
    reset_scene()
    create_harbor()
    save_and_export("World", "harbor_v02")
    reset_scene()
    create_wreck()
    save_and_export("World", "wreck_v02")
    reset_scene()
    create_treasure()
    save_and_export("World", "treasure_v02")
    reset_scene()
    create_gang_admiral()
    save_and_export("Bosses", "gang_admiral_v02")
    reset_scene()
    create_ghost_ship()
    save_and_export("Bosses", "ghost_ship_v02")
    reset_scene()
    create_kraken()
    save_and_export("Bosses", "kraken_v02")
    reset_scene()
    create_poseidon()
    save_and_export("Bosses", "poseidon_v02")


if __name__ == "__main__":
    build_all()
    print(f"desktopirates authored model library generated under {EXPORT_ROOT}")
