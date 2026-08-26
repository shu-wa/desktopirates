"""Headless structural checks for the generated v03 fleet Blender sources.

Run:
  blender --background --python Tools/Blender/ValidateHeroFleetV03.py -- <project-root>
"""

from __future__ import annotations

import sys
from pathlib import Path

import bpy
from mathutils import Vector


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
        hull = next(obj for obj in meshes if "lofted_hull" in obj.name.lower())
        deck = next(obj for obj in meshes if "lofted_deck" in obj.name.lower())

        def longitudinal_span(obj):
            values = [(obj.matrix_world @ Vector(corner)).y for corner in obj.bound_box]
            return max(values) - min(values)

        assert longitudinal_span(deck) >= longitudinal_span(hull) * 0.98, \
            f"tier {tier} deck does not close the bow: deck={longitudinal_span(deck):.3f}, hull={longitudinal_span(hull):.3f}"

        hull_rims = {}
        deck_lows = {}
        for vertex in hull.data.vertices:
            world = hull.matrix_world @ vertex.co
            key = round(world.y, 4)
            hull_rims[key] = max(hull_rims.get(key, -1e9), world.z)
        for vertex in deck.data.vertices:
            world = deck.matrix_world @ vertex.co
            key = round(world.y, 4)
            deck_lows[key] = min(deck_lows.get(key, 1e9), world.z)
        for key, deck_z in deck_lows.items():
            hull_key = min(hull_rims, key=lambda value: abs(value - key))
            assert deck_z > hull_rims[hull_key], \
                f"tier {tier} deck intersects the gunwale at y={key}: deck={deck_z:.3f}, hull={hull_rims[hull_key]:.3f}"

        bow = next(obj for obj in meshes if "prow_figurehead" in obj.name.lower())
        stern = next(obj for obj in meshes if "stern_greatcabin" in obj.name.lower())
        forward = bow.matrix_world.translation - stern.matrix_world.translation
        forward.z = 0.0
        forward.normalize()

        bowsprit = next(obj for obj in meshes if "bowsprit_main" in obj.name.lower())
        local_axis = bowsprit.matrix_world.to_3x3() @ Vector((0.0, 0.0, 1.0))
        forward_end_axis = local_axis if local_axis.dot(forward) > 0.0 else -local_axis
        assert forward_end_axis.z > 0.0, \
            f"tier {tier} bowsprit points downward toward the bow: z={forward_end_axis.z:.3f}"
        for sail in (obj for obj in meshes if obj.name.lower().startswith("sail_")):
            mast_index = sail.name.split("_")[1]
            mast = next(obj for obj in meshes if obj.name.lower().startswith(f"mast_{mast_index}_lower"))
            world_center = sum((sail.matrix_world @ vertex.co for vertex in sail.data.vertices), Vector()) / len(sail.data.vertices)
            forward_offset = (world_center - mast.matrix_world.translation).dot(forward)
            print(f"tier {tier} {sail.name} forward billow offset: {forward_offset:.4f}")
            assert forward_offset > 0.01, f"tier {tier} {sail.name} billows toward the stern"
    return len(meshes)


if __name__ == "__main__":
    counts = [validate_file(tier) for tier in range(6)]
    assert counts[5] > counts[3] > counts[1], f"fleet detail does not grow by ship level: {counts}"
    print(f"Hero Fleet v03 validation passed. Mesh counts by tier: {counts}")
