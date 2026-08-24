"""Render isometric QA previews for all v04 enemy ship silhouettes."""

from __future__ import annotations

import sys
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(sys.argv[sys.argv.index("--") + 1]).resolve() if "--" in sys.argv else Path.cwd()
SOURCE = ROOT / "ArtSource" / "Models" / "Blender" / "EnemyShipsV04"
OUTPUT = ROOT / "outputs" / "EnemyFleetV04"
TEXTURES = ROOT / "Assets" / "Desktopirates" / "Resources" / "Textures" / "Models"

SHIPS = {
    "enemy_corsair_v04": ((0.48, 0.10, 0.09, 1.0), 5.4),
    "enemy_skirmisher_v04": ((0.92, 0.68, 0.16, 1.0), 4.6),
    "enemy_gunboat_v04": ((0.43, 0.49, 0.52, 1.0), 4.8),
    "enemy_fire_raider_v04": ((0.92, 0.22, 0.06, 1.0), 4.9),
    "enemy_plague_raider_v04": ((0.42, 0.72, 0.15, 1.0), 4.9),
    "enemy_frost_cutter_v04": ((0.42, 0.88, 1.00, 1.0), 4.9),
    "enemy_ironclad_v04": ((0.58, 0.62, 0.64, 1.0), 6.0),
    "enemy_hunter_v04": ((0.62, 0.20, 0.82, 1.0), 5.4),
}


def point_camera(camera, target=(0.0, 0.0, 0.72)):
    camera.rotation_euler = (Vector(target) - camera.location).to_track_quat("-Z", "Y").to_euler()


def configure_flat_materials():
    for material in bpy.data.materials:
        color = tuple(material.diffuse_color)
        material.use_nodes = True
        principled = material.node_tree.nodes.get("Principled BSDF")
        if principled:
            principled.inputs["Base Color"].default_value = color
            principled.inputs["Roughness"].default_value = 0.84


def configure_textured_material(material_name: str, texture_path: Path, tint):
    material = bpy.data.materials.get(material_name)
    if material is None:
        return
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    principled = nodes.get("Principled BSDF")
    image = nodes.new("ShaderNodeTexImage")
    image.interpolation = "Closest"
    image.extension = "REPEAT"
    image.image = bpy.data.images.load(str(texture_path), check_existing=True)
    multiply = nodes.new("ShaderNodeMixRGB")
    multiply.blend_type = "MULTIPLY"
    multiply.inputs[0].default_value = 1.0
    multiply.inputs[2].default_value = tint
    links.new(image.outputs["Color"], multiply.inputs[1])
    links.new(multiply.outputs["Color"], principled.inputs["Base Color"])
    principled.inputs["Roughness"].default_value = 0.90


def render_ship(name: str, tint, ortho_scale: float):
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE / f"{name}.blend"))
    configure_flat_materials()
    configure_textured_material("HullEbony_v03", TEXTURES / "v04" / "EnemyHull_BurgundyIron_Pixel_v04.png", (0.90, 0.90, 0.90, 1.0))
    configure_textured_material("EnemySail_v03", TEXTURES / "v04" / "EnemySail_RaggedPatch_Pixel_v04.png", tint)
    configure_textured_material("DeckOak_v03", TEXTURES / "v03" / "DeckOak_Warm_Pixel_v03.png", (1.0, 1.0, 1.0, 1.0))

    world = bpy.context.scene.world or bpy.data.worlds.new("Enemy Fleet Preview World")
    bpy.context.scene.world = world
    world.color = (0.008, 0.018, 0.027)

    bpy.ops.object.camera_add(location=(4.5, -5.9, 4.3))
    camera = bpy.context.object
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = ortho_scale
    point_camera(camera)
    bpy.context.scene.camera = camera

    bpy.ops.object.light_add(type="AREA", location=(-3.0, -4.0, 6.5))
    bpy.context.object.data.energy = 1150
    bpy.context.object.data.size = 5.0
    bpy.ops.object.light_add(type="AREA", location=(4.0, 2.0, 3.2))
    bpy.context.object.data.energy = 560
    bpy.context.object.data.color = (0.16, 0.44, 0.56)
    bpy.context.object.data.size = 4.0

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 512
    scene.render.resolution_y = 512
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.render.filepath = str(OUTPUT / f"{name}.png")
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for ship_name, (ship_tint, ship_scale) in SHIPS.items():
        render_ship(ship_name, ship_tint, ship_scale)
    print(f"Enemy fleet previews written to {OUTPUT}")
