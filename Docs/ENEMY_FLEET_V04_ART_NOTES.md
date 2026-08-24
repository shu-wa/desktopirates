# Enemy Fleet v04 — art notes

## Goal

通常敵8船種を、色だけでなく船体比率・帆装・武装・甲板装備の輪郭で識別できるようにする。映画や既存作品の固有船・紋章は複製せず、歴史的なカリブ海帆船時代の材質感を基礎にする。

## Runtime assets

- `Resources/Models/EnemyShipsV04/enemy_*_v04.fbx`
- `Resources/Textures/Models/v04/EnemyHull_BurgundyIron_Pixel_v04.png`
- `Resources/Textures/Models/v04/EnemySail_RaggedPatch_Pixel_v04.png`

Unityでは Point / Repeat / No MipMap / Max 512 px で読み込み、船種別の控えめな色を乗算する。生成原画はテクスチャとして保持し、Blender側の`.blend`を制作元とする。

## Image generation

Built-in image generation modeを使用。生成日: 2026-08-24。

### Enemy hull

`seamless tileable low-poly pixel-art ship hull texture; salt-worn near-black age-of-sail timber; deep burgundy tar stains; narrow iron straps; nail heads; subtle battle scars; square orthographic swatch; charcoal, oxidized iron and muted brass; no symbols, text, logos, franchise imagery, perspective, hotspot or directional shadow`

### Enemy sail

`seamless tileable low-poly pixel-art enemy sailcloth; stitched patchwork; dark repairs; old rope seams; subtle soot; neutral dirty parchment for runtime tinting; high readability at small desktop-game scale; square orthographic swatch; no symbols, text, logos, franchise imagery, perspective, hotspot or directional shadow`

## Blender generation and QA

- Generate: `Tools/Blender/GenerateEnemyFleetV04.py`
- Validate: `Tools/Blender/ValidateEnemyFleetV04.py`
- Preview: `Tools/Blender/RenderEnemyFleetPreviewV04.py`
- Preview outputs: `outputs/EnemyFleetV04/`（Git対象外）
