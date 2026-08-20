# Inventory v04 source art

Built-in ImageGen production pass based on `ArtSource/ConceptArt/2026-08-20/04-map-inventory-log.png`.

- `inventory_reference_v04.png` is the isolated implementation target.
- `inventory_{item}_source_v04.png` are the ten transparent item masters.
- `inventory_frame_source_v04.png`, `inventory_title_source_v04.png`, and the scrollbar sources are the dedicated inventory chrome masters.
- Runtime item copies are 256px. The frame is 512x640, title is 512x128, track is 32x512, and handle is 48x160 under `Assets/Desktopirates/Resources/Textures/UI/ConceptV04/Inventory`.

Shared constraints: crisp 32-bit nautical pixel art, one object per texture, transparent background, no circular medallion, no per-item brass frame, no text, and a strong silhouette at 48–56 UI pixels.

The runtime list is deliberately data-driven: ten persisted cargo values, uniform 62px rows, a 520px viewport, a permanent authored brass scrollbar, and only the panel perimeter/title use brass framing.
