# 生成アセット記録

## OceanSurface_Faceted_v01

- 生成方式：Codex内蔵 ImageGen
- スタイル参照：`Assets/Desktopirates/Art/Concept/desktopirates-concept-v04-menu-world.png`
- 実装先：`Assets/Desktopirates/Resources/Textures/Environment/OceanSurface_Faceted_v01.png`
- 用途：円盤状の海のベース色・波面・泡パターン。専用シェーダーで時間帯着色とスクロールを加える。

### 使用プロンプト

> Use case: stylized-concept. Asset type: seamless tileable game texture for the actual DESKTOPIRATES ocean shader. Image 1 is a style and palette reference only, not an edit target. Create only a close top-down orthographic view of dark teal ocean surface matching the circular sea in the reference: small faceted low-poly wave cells, crisp pixel-art and subtle ordered-dither edges, layered navy/teal/turquoise depth, sparse tiny pale foam flecks, premium handcrafted retro 3D game texture. Uniform material coverage across the entire image, no horizon, no vignette, no large focal wave, no ship, no harbor, no objects, no UI, no text, no border, no logo, no watermark. The left/right and top/bottom edges must tile seamlessly with no visible seam. Neutral even lighting so the Unity shader can tint it for dawn, day, evening and night.

生成画像はそのままUIへ貼らず、テクスチャインポート規則と `OceanLowPoly.shader` を通して実装しています。

## 素材テクスチャ v01

- 生成方式：Codex内蔵 ImageGen
- 実装先：`Assets/Desktopirates/Resources/Textures/Materials/`
- Unity設定：Point / Repeat / No MipMap / Uncompressed / Max 512px
- 共用範囲：木材は船体・甲板・港・残骸・宝箱、帆布は通常帆・敵帆・破れた帆、石材は港・岸壁・灯台

### WoodPlanks_SaltWorn_v01

> Use case: stylized-concept. Asset type: seamless tileable game texture for DESKTOPIRATES ship hulls, decks, piers, crates and wreckage. Primary request: dark salt-worn wooden planks matching a premium low-poly pixel-art pirate adventure. Style/medium: top-down orthographic material texture, faceted low-poly shading translated into crisp pixel clusters and subtle ordered dithering. Composition/framing: uniform surface coverage, narrow horizontal planks with irregular seams, small nail heads and restrained chipped edges; no single focal plank. Lighting/mood: neutral even material lighting so Unity can apply dawn, day, evening and night tinting. Color palette: deep walnut brown, burnt umber, muted amber highlights, near-black seams. Materials/textures: readable wood grain at small game scale, salt wear, a few dark water stains, restrained detail. Constraints: left/right and top/bottom edges tile seamlessly; no perspective, no objects, no ship silhouette, no UI, no text, no logo, no border, no vignette, no watermark. Avoid: photorealistic photography, large knots, bright orange wood, moss covering the surface, dramatic shadows.

### SailCanvas_Aged_v01

> Use case: stylized-concept. Asset type: seamless tileable game texture for DESKTOPIRATES low-poly ship sails and torn wreck sails. Primary request: aged woven sailcloth matching a premium pixel-art pirate adventure. Style/medium: flat orthographic material texture, stylized low-poly surface translated into crisp pixel clusters and very subtle ordered dithering. Composition/framing: uniform fine canvas weave across the image, faint stitched panel seams, sparse salt stains and tiny repaired patches, no large focal feature. Lighting/mood: neutral even lighting so Unity can apply day and night tinting. Color palette: warm parchment, muted sand, pale cream highlights, restrained umber stitching. Materials/textures: readable woven canvas at small game scale, slightly weathered edges implied only through color variation. Constraints: left/right and top/bottom edges tile seamlessly; no perspective, no sail silhouette, no skull, no emblem, no objects, no UI, no text, no logo, no border, no vignette, no watermark. Avoid: photorealistic fabric photography, strong folds, deep cast shadows, bright white cloth, large stains, high-frequency noise.

### HarborStone_Damp_v01

> Use case: stylized-concept. Asset type: seamless tileable game texture for DESKTOPIRATES harbor quays, lighthouse masonry and rocky structures. Primary request: damp old coastal stone blocks matching a premium low-poly pixel-art pirate adventure. Style/medium: top-down orthographic material texture, faceted low-poly value changes with crisp pixel clusters and subtle ordered dithering. Composition/framing: uniform small irregular masonry blocks, thin dark mortar, sparse chipped corners and restrained salt deposits, no focal object. Lighting/mood: neutral even lighting so Unity can tint it for dawn, day, evening and night. Color palette: charcoal slate, muted blue-gray, weathered olive-gray, sparse pale salt highlights. Materials/textures: readable masonry at small game scale, damp patches, tiny moss traces only in crevices. Constraints: left/right and top/bottom edges tile seamlessly; no perspective, no architecture silhouette, no water, no plants, no UI, no text, no logo, no border, no vignette, no watermark. Avoid: photorealistic photography, large boulders, repeating obvious symbols, bright green moss, dramatic shadows.
