# 生成アセット記録

## OceanSurface_Faceted_v01

- 生成方式：Codex内蔵 ImageGen
- スタイル参照：`Assets/Desktopirates/Art/Concept/desktopirates-concept-v04-menu-world.png`
- 実装先：`Assets/Desktopirates/Resources/Textures/Environment/OceanSurface_Faceted_v01.png`
- 用途：円盤状の海のベース色・波面・泡パターン。専用シェーダーで時間帯着色とスクロールを加える。

### 使用プロンプト

> Use case: stylized-concept. Asset type: seamless tileable game texture for the actual DESKTOPIRATES ocean shader. Image 1 is a style and palette reference only, not an edit target. Create only a close top-down orthographic view of dark teal ocean surface matching the circular sea in the reference: small faceted low-poly wave cells, crisp pixel-art and subtle ordered-dither edges, layered navy/teal/turquoise depth, sparse tiny pale foam flecks, premium handcrafted retro 3D game texture. Uniform material coverage across the entire image, no horizon, no vignette, no large focal wave, no ship, no harbor, no objects, no UI, no text, no border, no logo, no watermark. The left/right and top/bottom edges must tile seamlessly with no visible seam. Neutral even lighting so the Unity shader can tint it for dawn, day, evening and night.

生成画像はそのままUIへ貼らず、テクスチャインポート規則と `OceanLowPoly.shader` を通して実装しています。
