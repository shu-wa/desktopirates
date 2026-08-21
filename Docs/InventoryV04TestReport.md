# Inventory v04 verification

Date: 2026-08-21

## Visual target and comparison

- Isolated ImageGen target: `ArtSource/Generated/UI/v04-inventory/inventory_reference_v04.png`
- Exact 720x760 layout proof rendered from the shipped textures and controller geometry: `outputs/inventory-v04-layout-proof.png`
- Side-by-side reference comparison: `outputs/inventory-v04-reference-comparison.png`
- Unity Windows runtime capture: `outputs/RuntimeCapture-Inventory-v04-final.png`
- Concept-versus-engine comparison: `outputs/inventory-v04-engine-reference-comparison.png`

The comparison checks the intended hierarchy: one tall brass perimeter, dark navy list surface, title cartouche, ten uniform data-backed rows, frameless icons, left rarity strips, right-aligned quantities, and a permanent brass scrollbar. The final computed panel bounds are x=192..708 and y=150..750 on the 720x760 reference canvas. This leaves 12 px at the right, 10 px at the bottom, and an 8 px minimum gap below the menu circle.

## Automated checks

- Runtime assembly compiled with Unity 6000.1.6f1 Roslyn response files: pass.
- EditMode test assembly compiled with Unity 6000.1.6f1 Roslyn response files: pass.
- Unity 6000.1.6f1 EditMode Test Runner: 112/112 passed, 0 failed.
- Windows player build: succeeded, 122,420,203 bytes.
- `--inventory-preview` runtime launch and desktop capture: pass.
- Fourteen runtime PNGs each have a matching `.meta`: pass.
- Fourteen importer GUIDs are unique: pass.
- Ten item textures are 256x256 RGBA with transparent corners: pass.
- Dedicated frame/title/scrollbar textures have transparent corners: pass.
- Import settings specify Point filtering, Clamp wrapping, no mipmaps, alpha transparency, and bounded sizes: pass.
- Layout invariants cover canvas bounds, scrolling, icon-to-copy spacing, and copy-to-quantity spacing.

## Unity runner status

The first batch launch happened while Unity Hub was not running. Editor 6000.1.6f1 therefore launched its bundled Licensing Client 1.16.2 without an access token; the stored legacy machine binding also differed from the current network adapter identity. Starting Unity Hub 3.12.1 launched Licensing Client 1.17.0, refreshed `UnityEntitlementLicense.xml`, resolved the Personal entitlement, and restored the editor license. The final Test Runner, player build, and runtime screenshot all completed successfully.
