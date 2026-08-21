# UI Centering and Complete Concept Set Report

Date: 2026-08-21

## Implemented layout behavior

- Inventory panel center X is `0` on the 720 x 760 reference canvas.
- Inventory panel bounds are X `102..618`, giving equal 102-pixel margins.
- Inventory is placed below the persistent status HUD rather than overlapping it.
- Harbor outer panel and service board use the same center axis; the former 28-pixel service-board offset is removed.
- HULL, GOLD, CREW, and LOAD remain visible while Inventory, Harbor Services, or Shipyard is open.
- Navigation-only elements still hide while these panels are open to preserve sea and panel readability.

## Verification

- Unity EditMode: 113 passed, 0 failed.
- Windows player build: successful.
- Runtime captures:
  - `outputs/RuntimeCapture-Inventory-Centered-Hud.png`
  - `outputs/RuntimeCapture-Port-Centered-Hud.png`
- Static inventory layout proof: `outputs/inventory-v04-layout-proof.png`

## Concept coverage (local only)

The locally retained 20-image set under `ArtSource/ConceptArt/2026-08-21-complete-set` covers tutorial, normal voyage, inventory, harbor, shipyard, crew, chart, salvage, combat, sinking, bosses, perk rewards, captain log, menu circle, mutations, and automatic docking. Concept-art sources are intentionally excluded from Git; the local directory's `README.md` contains the complete matrix and shared visual rules.
