"""Generate eight silhouette-driven enemy ships for desktopirates.

The base construction reuses the reproducible v03 historical ship tools, then
adds role-specific hull proportions, weapons and deck silhouettes. No external
Blender add-on is required, keeping the art pipeline deterministic.

Run:
  blender --background --python Tools/Blender/GenerateEnemyFleetV04.py -- <project-root>
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bpy


PROJECT_ROOT = Path(sys.argv[sys.argv.index("--") + 1]).resolve() if "--" in sys.argv else Path.cwd()
TOOLS_ROOT = PROJECT_ROOT / "Tools" / "Blender"
if str(TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(TOOLS_ROOT))

import GenerateHeroFleetV03 as hero


SOURCE_ROOT = PROJECT_ROOT / "ArtSource" / "Models" / "Blender" / "EnemyShipsV04"
EXPORT_ROOT = PROJECT_ROOT / "Assets" / "Desktopirates" / "Resources" / "Models" / "EnemyShipsV04"


def scale_scene(x: float, y: float, z: float) -> None:
    for obj in bpy.context.scene.objects:
        obj.scale.x *= x
        obj.scale.y *= y
        obj.scale.z *= z


def gun(name: str, x: float, y: float, z: float, length=0.42, radius=0.045) -> None:
    hero.cylinder(name, (x, y, z), radius, length, "Iron_v03", vertices=8,
                  rotation=(0.0, math.radians(90.0), 0.0))


def pointed_crystal(name: str, x: float, y: float, z: float, scale, material="GlassGlow_v03") -> None:
    hero.cube(name, (x, y, z), scale, material,
              rotation=(math.radians(18.0), math.radians(45.0), math.radians(14.0)), bevel=0.008)


def add_identity_flag(prefix: str, y: float, z: float, width=0.42) -> None:
    hero.cube(f"{prefix}_Identity_Flag", (width * 0.48, y, z), (width, 0.025, 0.22),
              "EnemySail_v03", rotation=(0.0, math.radians(-7.0), 0.0), bevel=0.008)


def build_corsair() -> None:
    hero.create_ship(3, enemy=True)
    scale_scene(1.02, 1.04, 1.00)
    add_identity_flag("Corsair", -0.82, 2.18, 0.48)
    hero.cube("Corsair_Brass_Stern_Badge", (0.0, -1.38, 0.88), (0.30, 0.05, 0.30),
              "Brass_v03", rotation=(0.0, 0.0, math.radians(45.0)), bevel=0.018)


def build_skirmisher() -> None:
    hero.create_ship(1, enemy=True)
    scale_scene(0.76, 1.28, 0.78)
    # The long bowsprit and swept pennants give an unmistakably fast silhouette.
    hero.cylinder("Skirmisher_Extended_Bowsprit", (0.0, 1.68, 0.55), 0.026, 1.05,
                  "Rope_v03", vertices=8, rotation=(math.radians(90.0), 0.0, 0.0))
    for side in (-1, 1):
        hero.cube(f"Skirmisher_Swept_Pennant_{side}", (side * 0.26, 0.05, 1.70),
                  (0.34, 0.025, 0.14), "EnemySail_v03",
                  rotation=(0.0, math.radians(side * 16.0), math.radians(side * 9.0)), bevel=0.006)


def build_gunboat() -> None:
    hero.create_ship(2, enemy=True)
    scale_scene(1.12, 0.90, 0.90)
    for side in (-1, 1):
        for index, y in enumerate((-0.72, -0.24, 0.24, 0.72), 1):
            gun(f"Gunboat_Heavy_Cannon_{side}_{index}", side * 0.67, y, 0.26, length=0.50, radius=0.055)
    hero.cylinder("Gunboat_Bow_Chaser", (0.0, 1.25, 0.48), 0.058, 0.58, "Iron_v03", vertices=8,
                  rotation=(math.radians(90.0), 0.0, 0.0))
    hero.cube("Gunboat_Armored_Battery_Roof", (0.0, 0.0, 0.67), (0.86, 1.16, 0.10),
              "Iron_v03", bevel=0.025)


def build_fire_raider() -> None:
    hero.create_ship(2, enemy=True)
    scale_scene(0.94, 1.06, 0.94)
    for index, x in enumerate((-0.34, 0.0, 0.34), 1):
        hero.sphere(f"FireRaider_Fire_Pot_{index}", (x, 0.03, 0.78), (0.11, 0.11, 0.14), "GlassGlow_v03")
        pointed_crystal(f"FireRaider_Flame_{index}", x, 0.03, 0.96, (0.08, 0.08, 0.26))
    hero.cube("FireRaider_Burning_Ram", (0.0, 1.48, 0.15), (0.20, 0.82, 0.18),
              "Iron_v03", rotation=(math.radians(8.0), 0.0, 0.0), bevel=0.015)


def build_plague_raider() -> None:
    hero.create_ship(2, enemy=True)
    scale_scene(1.00, 1.00, 1.02)
    for side in (-1, 1):
        for index, y in enumerate((-0.32, 0.24), 1):
            hero.cylinder(f"PlagueRaider_Toxic_Cask_{side}_{index}", (side * 0.34, y, 0.74), 0.11, 0.28,
                          "DeckOak_v03", vertices=8, rotation=(0.0, math.radians(90.0), 0.0))
    for side in (-1, 1):
        hero.cylinder(f"PlagueRaider_Vent_{side}", (side * 0.22, -0.55, 0.95), 0.052, 0.56,
                      "Iron_v03", vertices=8)
        hero.sphere(f"PlagueRaider_Glow_{side}", (side * 0.22, -0.55, 1.27),
                    (0.10, 0.10, 0.10), "GlassGlow_v03")


def build_frost_cutter() -> None:
    hero.create_ship(2, enemy=True)
    scale_scene(0.80, 1.20, 0.86)
    for index, (x, y, height) in enumerate(((0.0, 1.48, 0.48), (-0.34, 0.66, 0.35),
                                            (0.34, 0.66, 0.35), (-0.39, -0.22, 0.28),
                                            (0.39, -0.22, 0.28)), 1):
        pointed_crystal(f"FrostCutter_Ice_Crystal_{index}", x, y, 0.56 + height * 0.5,
                        (0.14, 0.14, height))
    hero.cube("FrostCutter_Ice_Prow", (0.0, 1.58, 0.18), (0.18, 0.94, 0.20),
              "GlassGlow_v03", rotation=(math.radians(11.0), 0.0, math.radians(45.0)), bevel=0.01)


def build_ironclad() -> None:
    hero.create_ship(4, enemy=True)
    scale_scene(1.22, 0.96, 1.06)
    for side in (-1, 1):
        for index, y in enumerate((-0.92, -0.46, 0.0, 0.46, 0.92), 1):
            hero.cube(f"Ironclad_Armor_Plate_{side}_{index}", (side * 0.76, y, 0.18),
                      (0.065, 0.39, 0.52), "Iron_v03", bevel=0.018)
            hero.cylinder(f"Ironclad_Armor_Rivet_{side}_{index}", (side * 0.80, y, 0.24), 0.025, 0.04,
                          "Brass_v03", vertices=8, rotation=(0.0, math.radians(90.0), 0.0))
    hero.cylinder("Ironclad_Deck_Turret", (0.0, 0.22, 0.92), 0.25, 0.18, "Iron_v03", vertices=12)
    gun("Ironclad_Turret_Cannon_Port", -0.30, 0.22, 0.93, length=0.62, radius=0.064)
    gun("Ironclad_Turret_Cannon_Starboard", 0.30, 0.22, 0.93, length=0.62, radius=0.064)


def build_hunter() -> None:
    hero.create_ship(3, enemy=True)
    scale_scene(0.86, 1.16, 0.94)
    hero.cylinder("Hunter_Harpoon_Launcher", (0.0, 1.12, 0.76), 0.052, 0.72, "Iron_v03", vertices=8,
                  rotation=(math.radians(90.0), 0.0, 0.0))
    for side in (-1, 1):
        hero.sphere(f"Hunter_Tracking_Lantern_{side}", (side * 0.45, 0.54, 1.03),
                    (0.115, 0.115, 0.15), "GlassGlow_v03")
        hero.rope(f"Hunter_Harpoon_Line_{side}", [(0.0, 1.14, 0.76),
                  (side * 0.40, -0.45, 0.68)], radius=0.013)
    hero.cube("Hunter_Aft_Fin", (0.0, -1.38, 0.78), (0.08, 0.54, 0.58),
              "EnemySail_v03", rotation=(math.radians(-9.0), 0.0, 0.0), bevel=0.01)


BUILDERS = {
    "enemy_corsair_v04": build_corsair,
    "enemy_skirmisher_v04": build_skirmisher,
    "enemy_gunboat_v04": build_gunboat,
    "enemy_fire_raider_v04": build_fire_raider,
    "enemy_plague_raider_v04": build_plague_raider,
    "enemy_frost_cutter_v04": build_frost_cutter,
    "enemy_ironclad_v04": build_ironclad,
    "enemy_hunter_v04": build_hunter,
}


def save_and_export(name: str) -> None:
    SOURCE_ROOT.mkdir(parents=True, exist_ok=True)
    EXPORT_ROOT.mkdir(parents=True, exist_ok=True)
    hero.rotate_for_runtime()
    hero.prepare()
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE_ROOT / f"{name}.blend"), compress=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=str(EXPORT_ROOT / f"{name}.fbx"), use_selection=True,
        object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", add_leaf_bones=False, bake_anim=False, path_mode="STRIP")


def build_all() -> None:
    for name, builder in BUILDERS.items():
        hero.reset_scene()
        builder()
        save_and_export(name)
        print(f"Generated {name}")


if __name__ == "__main__":
    build_all()
    print(f"desktopirates v04 enemy fleet generated under {EXPORT_ROOT}")
