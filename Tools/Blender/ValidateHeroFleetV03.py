"""Headless structural checks for the generated v03 fleet Blender sources.

Run:
  blender --background --python Tools/Blender/ValidateHeroFleetV03.py -- <project-root>
"""

from __future__ import annotations

import sys
from pathlib import Path

import bpy


ROOT = Path(sys.argv[sys.argv.index("--") + 1]).resolve() if "--" in sys.argv else Path.cwd()
SOURCE = ROOT / "ArtSource" / "Models" / "Blender" / "ShipsV03"
EXPORT = ROOT / "Assets" / "Desktopirates" / "Resources" / "Models" / "ShipsV03"


def validate_file(tier: int):
    blend = SOURCE / f"player_tier_{tier}_v03.blend"
    fbx = EXPORT / f"player_tier_{tier}_v03.fbx"
    assert blend.is_file() and blend.stat().st_size > 50_000, blend
    assert fbx.is_file() and fbx.stat().st_size > 50_000, fbx
    bpy.ops.wm.open_mainfile(filepath=str(blend))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    assert meshes, f"tier {tier} has no mesh objects"
    names = [obj.name.lower() for obj in meshes]
    if tier == 0:
        for token in ("raft_log", "mast", "sail", "crate", "lashing"):
            assert any(token in name for name in names), f"tier {tier} missing {token}"
    else:
        for token in ("hull", "deck", "stern_greatcabin", "sail", "rigging", "gunport", "figurehead"):
            assert any(token in name for name in names), f"tier {tier} missing {token}"
    return len(meshes)


if __name__ == "__main__":
    counts = [validate_file(tier) for tier in range(6)]
    assert counts[5] > counts[3] > counts[1], f"fleet detail does not grow by ship level: {counts}"
    print(f"Hero Fleet v03 validation passed. Mesh counts by tier: {counts}")
