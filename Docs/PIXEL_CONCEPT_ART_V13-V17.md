# desktopirates pixel-art direction v13-v17

Generated with the built-in ImageGen workflow on 2026-08-14. The early v03-v05 concepts and the approved v12 review image are the visual anchors. These images deliberately return to a quiet, compact desktop world instead of painterly full-screen spectacle.

## Non-negotiable art rules

- Render as low-resolution pixel art enlarged with nearest-neighbor scaling.
- Use hard stair-stepped silhouettes, 2-4 pixel clusters and a restrained navy/teal/brass palette.
- Keep the frameless circular sea in the lower-right with generous desktop negative space.
- Put HUD controls around the sea, never over the ship or central sailing area.
- Prefer one readable discovery over a crowded archipelago.
- Avoid painterly water, smooth vector edges, anti-aliasing and glossy mobile-game rendering.

## Scene concepts

- `desktopirates-concept-v13-pixel-morning-voyage.png` — quiet morning idle voyage, long wakes and sparse discovery tags.
- `desktopirates-concept-v14-pixel-night-battle.png` — moonlit one-on-one battle demonstrating directional cannon fire.
- `desktopirates-concept-v15-pixel-live-chart.png` — north-up live chart with an off-center moving player marker.
- `desktopirates-concept-v16-pixel-shipyard.png` — harbor customization with six cannon hardpoints and radial upgrades.
- `desktopirates-concept-v17-pixel-cargo-menu.png` — six-slot cargo wheel and the open draggable menu circle.

## Generated UI texture set

High-resolution ImageGen sources are stored under `Assets/Desktopirates/Art/Generated/UI/Source`. Optimized transparent runtime textures are stored under `Assets/Desktopirates/Resources/Textures/UI/GeneratedPixel`.

| Runtime texture | Size | Role |
| --- | ---: | --- |
| `ui_menu_circle_pixel_v01.png` | 128 x 128 | draggable day/night menu circle |
| `ui_marker_enemy_pixel_v01.png` | 96 x 96 | coral enemy-ship signal and pointer |
| `ui_marker_wreck_pixel_v01.png` | 96 x 96 | mint wreck signal and pointer |
| `ui_marker_treasure_pixel_v01.png` | 96 x 96 | amber treasure signal and pointer |
| `ui_marker_port_pixel_v01.png` | 96 x 96 | teal lighthouse/harbor signal and pointer |
| `ui_button_inventory_pixel_v01.png` | 80 x 80 | cargo inventory launcher |
| `ui_button_back_pixel_v01.png` | 80 x 80 | circular back control |
| `ui_telegraph_pixel_v01.png` | 512 x 128 | eight-step engine order and actual-motion rail |

All runtime files use transparency, Point filtering, Clamp wrapping, no mipmaps and no texture compression. Generated text is intentionally excluded from reusable controls so labels remain code-driven and localizable.

## Prompt summary

Scene prompts shared the early concepts as strict references and requested a frameless desktop overlay, circular tiled ocean, small isometric cutter, restrained perimeter UI, hard square pixels, limited colors and no anti-aliasing. Individual UI prompts requested one centered nautical control with aged-brass rim, navy enamel core, role-specific accent color and no baked text. The menu-circle chroma source was processed with the installed ImageGen background-removal helper; other generated UI sources already contained alpha. Runtime variants were cropped and reduced with nearest-neighbor resampling.
