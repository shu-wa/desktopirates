# desktopirates concept art v05-v11

Generated with the built-in ImageGen workflow on 2026-08-13. The original reference supplied by the user was used as the visual anchor. These images are design targets, not textures copied directly into the runtime.

## Shared art direction

- Frameless desktop overlay; a real circular sea disc rather than a rectangular window.
- Elevated 45-degree orthographic camera.
- Premium low-poly 3D with crisp pixel-texture treatment and strong small-scale silhouettes.
- Deep navy, blue-green water, salt-worn walnut, aged ivory canvas, antique brass.
- Faceted water remains slightly realistic; movement is shown by current streaks, a bow wave and two stern wakes.
- UI follows physical nautical objects: brass bezels, navy enamel, engraved arrows and mechanical telegraph segments.
- No logos, watermark, modern flat UI, or decorative unreadable text.

## Prompt set and outputs

### v05 — voyage and speed

`Assets/Desktopirates/Art/Concept/desktopirates-concept-v05-voyage-speed.png`

> Create a polished sailing concept for desktopirates matching the supplied dark desktop-overlay reference. Place a salt-worn wooden cutter on a circular faceted ocean, cruising toward the upper-right with clearly readable twin wakes, current streaks and bow foam. Keep the draggable time circle above, perimeter tags outside the disc, and add a compact antique-brass engine telegraph with eight segments and the word “CRUISE”. No rectangular game window.

### v06 — circular cargo inventory

`Assets/Desktopirates/Art/Concept/desktopirates-concept-v06-inventory.png`

> Create the inventory-open state using the same world and materials. Build a circular ship's-hold panel with six radial slots for TIMBER, CANVAS, IRON, GEAR, CHART and RELIC, each with a crisp material icon and quantity. Include a highlighted backpack button, TAB hint and round back arrow. The voyage remains visible behind the circular panel; do not use a rectangular grid window.

### v07 — port and workshop

`Assets/Desktopirates/Art/Concept/desktopirates-concept-v07-port-workshop.png`

> Create a circular harbor workshop screen around a moored cutter and lighthouse. Divide the shipwright table into REPAIR, SUPPLIES, ENGINE and CANNON sectors. Communicate costs with gold and salvage icons. Show the engine upgrade as an eight-notch maximum-speed strip, plus a crew-capacity medallion and round SAIL control. Use warm workshop lamps against cool evening water.

### v08 — infinite exploration chart

`Assets/Desktopirates/Art/Concept/desktopirates-concept-v08-chart.png`

> Create a circular parchment CHART inside an engraved brass compass. Explored infinite-world chunks appear as teal hand-inked tiles that fade into untouched charcoal fog. Use unique silhouettes and colors for the player cutter, lighthouse port, enemy ship, wreck, treasure and unknown signal. Include N/E/S/W and eight camera-direction ticks. Do not imply a finite square map.

### v09 — menu circle and radial controls

`Assets/Desktopirates/Art/Concept/desktopirates-concept-v09-menu-circle.png`

> Create an implementation-ready close view of the draggable time menu circle and its radial controls. The time circle contains a miniature horizon and 24-hour color ring. Surround it with brass buttons for volume, size, chart, cargo backpack, save and quit, plus tactile slider rails and diamond handles. Keep the circular sea and compact telegraph visible below; no menu background rectangle.

### v10 — night encounter and salvage

`Assets/Desktopirates/Art/Concept/desktopirates-concept-v10-night-encounter.png`

> Create a moonlit encounter on the circular sea: the cutter passes a partly submerged wreck while an enemy raider approaches the rim. Use a large coral enemy tag, mint wreck tag and amber treasure tag. Show a restrained cannon flash, one projectile, moonlit wakes and moving water. The telegraph reads “HALF” with five lit segments, and hull damage is visible as one red pip rather than a long number.

### v11 — spacious live voyage and chart

`Assets/Desktopirates/Art/Concept/desktopirates-concept-v11-live-voyage-map.png`

> Create a premium desktop-overlay concept with no rectangular window. Make a broad circular faceted ocean the dominant form, with a continuously sailing customizable pirate cutter, strong wake trails, distant harbor, enemy, wreck and treasure. Keep the compact status strip and thin engine telegraph outside the water. Add a separate circular live-chart inset with north-up grid, explored teal regions, fogged unknown regions, event icons and an off-center moving player arrow. Above the sea, retain the draggable day-to-night menu circle. Use deep navy, petrol teal, aged brass and warm sunset highlights.

This is the current composition target after the sailing-visibility and live-chart pass: gameplay information belongs around the sea, while the ocean and ship remain visually unobstructed.

## Runtime translation

- ImageGen concepts define composition, palette, lighting and material character.
- World materials use authored project textures under `Resources/Textures/Environment` and `Resources/Textures/Materials`.
- Small controls are deterministic pixel-authored PNGs under `Resources/Textures/UI`; this keeps every 32-80 px glyph legible and repeatable.
- `DesktopiratesUiTextureBaker` is the source-of-truth baker for menu, cargo, marker, navigation and UI chrome textures.
- Runtime loaders use baked PNGs and retain procedural generation only as a development fallback.
