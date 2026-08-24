"""Headless structural validation for the eight v04 enemy ship sources."""

from __future__ import annotations

import sys
from pathlib import Path

import bpy


ROOT = Path(sys.argv[sys.argv.index("--") + 1]).resolve() if "--" in sys.argv else Path.cwd()
SOURCE = ROOT / "ArtSource" / "Models" / "Blender" / "EnemyShipsV04"
EXPORT = ROOT / "Assets" / "Desktopirates" / "Resources" / "Models" / "EnemyShipsV04"

REQUIRED = {
    "enemy_corsair_v04": ("corsair", "stern", "gunport", "rigging"),
    "enemy_skirmisher_v04": ("skirmisher", "extended_bowsprit", "sail", "rigging"),
    "enemy_gunboat_v04": ("gunboat", "heavy_cannon", "battery_roof", "gunport"),
    "enemy_fire_raider_v04": ("fireraider", "fire_pot", "burning_ram", "sail"),
    "enemy_plague_raider_v04": ("plagueraider", "toxic_cask", "vent", "sail"),
    "enemy_frost_cutter_v04": ("frostcutter", "ice_crystal", "ice_prow", "sail"),
    "enemy_ironclad_v04": ("ironclad", "armor_plate", "deck_turret", "cannon"),
    "enemy_hunter_v04": ("hunter", "harpoon", "tracking_lantern", "rigging"),
}


def validate(name: str, tokens) -> int:
    blend = SOURCE / f"{name}.blend"
    fbx = EXPORT / f"{name}.fbx"
    assert blend.is_file() and blend.stat().st_size > 70_000, blend
    assert fbx.is_file() and fbx.stat().st_size > 90_000, fbx
    bpy.ops.wm.open_mainfile(filepath=str(blend))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    names = [obj.name.lower() for obj in meshes]
    for common in ("hull", "deck", "sail"):
        assert any(common in item for item in names), f"{name} missing {common}"
    for token in tokens:
        assert any(token in item for item in names), f"{name} missing {token}"
    return len(meshes)


if __name__ == "__main__":
    counts = {name: validate(name, tokens) for name, tokens in REQUIRED.items()}
    assert counts["enemy_ironclad_v04"] > counts["enemy_skirmisher_v04"], counts
    assert len(set(counts.values())) >= 5, f"enemy silhouettes lack structural diversity: {counts}"
    print(f"Enemy Fleet v04 validation passed. Mesh counts: {counts}")
