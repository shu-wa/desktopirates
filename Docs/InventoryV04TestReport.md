# Inventory v04 verification

Date: 2026-08-21

## Visual target and comparison

- Isolated ImageGen target: `ArtSource/Generated/UI/v04-inventory/inventory_reference_v04.png`
- Exact 720x760 layout proof rendered from the shipped textures and controller geometry: `outputs/inventory-v04-layout-proof.png`
- Side-by-side reference comparison: `outputs/inventory-v04-reference-comparison.png`

The comparison checks the intended hierarchy: one tall brass perimeter, dark navy list surface, title cartouche, ten uniform data-backed rows, frameless icons, left rarity strips, right-aligned quantities, and a permanent brass scrollbar. The computed panel bounds are x=192..708 and y=102..748 on the 720x760 reference canvas, leaving 12 px at the right and bottom.

## Automated checks

- Runtime assembly compiled with Unity 6000.1.6f1 Roslyn response files: pass.
- EditMode test assembly compiled with Unity 6000.1.6f1 Roslyn response files: pass.
- Fourteen runtime PNGs each have a matching `.meta`: pass.
- Fourteen importer GUIDs are unique: pass.
- Ten item textures are 256x256 RGBA with transparent corners: pass.
- Dedicated frame/title/scrollbar textures have transparent corners: pass.
- Import settings specify Point filtering, Clamp wrapping, no mipmaps, alpha transparency, and bounded sizes: pass.
- Layout invariants cover canvas bounds, scrolling, icon-to-copy spacing, and copy-to-quantity spacing.

## Unity runner status

The Unity Test Runner and runtime screenshot step could not start on this machine because Unity reported `No valid Unity Editor license found. Please activate your license.` No test failure or compiler error was reported before that licensing gate. Once the editor license is activated, run the EditMode suite and launch with `--inventory-preview` to replace the deterministic layout proof with an engine-rendered screenshot.
