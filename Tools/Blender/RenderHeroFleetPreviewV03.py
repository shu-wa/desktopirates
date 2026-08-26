"""Render transparent isometric QA previews for the generated v03 player fleet.

Run:
  blender --background --python Tools/Blender/RenderHeroFleetPreviewV03.py -- <project-root>
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


ROOT = Path(sys.argv[sys.argv.index("--") + 1]).resolve() if "--" in sys.argv else Path.cwd()
SOURCE = ROOT / "ArtSource" / "Models" / "Blender" / "ShipsV03"
OUTPUT = ROOT / "outputs" / "FleetV03"
TEXTURES = ROOT / "Assets" / "Desktopirates" / "Resources" / "Textures" / "Models" / "v03"


def point_camera(camera, target=(0.0, 0.0, 0.75)):
    camera.rotation_euler = (Vector(target) - camera.location).to_track_quat("-Z", "Y").to_euler()


def configure_textured_material(material_name: str, texture_name: str):
    material = bpy.data.materials.get(material_name)
    if material is None:
        return
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    principled = next((node for node in nodes if node.type == "BSDF_PRINCIPLED"), None)
    if principled is None:
        return
    image_node = nodes.new("ShaderNodeTexImage")
    image_node.interpolation = "Closest"
    image_node.extension = "REPEAT"
    image_node.image = bpy.data.images.load(str(TEXTURES / texture_name), check_existing=True)
    links.new(image_node.outputs["Color"], principled.inputs["Base Color"])
    principled.inputs["Roughness"].default_value = 0.88


def configure_flat_materials():
    for material in bpy.data.materials:
        color = tuple(material.diffuse_color)
        material.use_nodes = True
        principled = next((node for node in material.node_tree.nodes if node.type == "BSDF_PRINCIPLED"), None)
        if principled is None:
            continue
        principled.inputs["Base Color"].default_value = color
        principled.inputs["Roughness"].default_value = 0.82


def render_tier(tier: int):
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE / f"player_tier_{tier}_v03.blend"))
    configure_flat_materials()
    configure_textured_material("HullEbony_v03", "HullEbony_SaltWorn_Pixel_v03.png")
    configure_textured_material("DeckOak_v03", "DeckOak_Warm_Pixel_v03.png")
    configure_textured_material("SailCanvas_v03", "SailCanvas_Patched_Pixel_v03.png")

    world = bpy.context.scene.world or bpy.data.worlds.new("Fleet Preview World")
    bpy.context.scene.world = world
    world.color = (0.012, 0.028, 0.045)

    bpy.ops.object.camera_add(location=(4.4, -5.8, 4.2))
    camera = bpy.context.object
    camera.data.type = "ORTHO"
    camera.data.ortho_scale = 4.4 if tier == 0 else 4.8 + tier * 0.18
    point_camera(camera)
    bpy.context.scene.camera = camera

    bpy.ops.object.light_add(type="AREA", location=(-3.0, -4.0, 6.5))
    key = bpy.context.object
    key.data.energy = 1100
    key.data.shape = "DISK"
    key.data.size = 5.0
    bpy.ops.object.light_add(type="AREA", location=(4.0, 2.0, 3.0))
    fill = bpy.context.object
    fill.data.energy = 520
    fill.data.color = (0.18, 0.48, 0.60)
    fill.data.size = 4.0

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 768
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.look = "AgX - Medium High Contrast"
    scene.render.filepath = str(OUTPUT / f"player_tier_{tier}_v03.png")
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for requested_tier in (0, 1, 3, 5):
        render_tier(requested_tier)
    print(f"Fleet preview renders written to {OUTPUT}")
