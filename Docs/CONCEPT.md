# desktopirates コンセプト v0.3

## 画面構成

- 長方形のゲームウィンドウは見せず、円盤状の海、船、円周タグだけをデスクトップ上へ表示する。
- 約45度上方からの正投影に近い視点を使う。
- 船の少し上に実時間連動の時刻オーブを置く。
- オーブの中は朝、昼、夕方、夜で空・水平線・太陽・月・星が変化する。
- オーブ外周には一日の色の推移を示す小さな色環を持たせる。
- 時刻オーブをドラッグすると、円盤の海を含むゲーム全体が移動する。
- 敵、残骸、宝は視界外では実物を見せず、円周上のタグだけを表示する。
- 対象へ近づくほどタグが大きくなり、円盤内へ入ると実物へ切り替わる。

## 基準コンセプトアート

`Assets/Art/Concept/desktopirates-concept-v03-time-orb.png`

## 画像生成情報

生成方式: Codex内蔵 ImageGen（既存コンセプト v0.2への編集）

```text
Use case: precise-object-edit
Asset type: final desktop game concept art for implementation
Input images: Image 1: edit target and visual style anchor
Primary request: keep the circular ocean game exactly as established, and add a small circular real-time day-cycle orb slightly above the ocean disc. The orb is also the drag handle used to move the entire game overlay.
Time orb: place one compact round medallion about 64–76 pixels wide, centered a short distance above the upper edge of the ocean disc. Inside it show the current late-night state as a tiny pixel-art sky vignette: deep indigo gradient, crescent moon, two or three stars, and a dark sea horizon. Give its rim a very subtle four-part color progression suggesting dawn peach, noon cyan, sunset amber, and night indigo. Add a short minimal stem or bead connector between the orb and the ocean disc so it visually belongs to the game and clearly looks grabbable, but do not add text or a hand cursor.
Invariants: preserve the desktop background and taskbar, circular low-poly pixel-textured ocean, centered wooden ship, 45-degree orthographic view, perimeter tags, tiny arc compass, dark teal and amber palette, and all object placements as closely as possible.
Constraints: only add the time orb and connector; no rectangular window, no rectangular panel, no title, no clock digits, no readable text, no extra HUD, no visible off-disc destinations, no logos, no watermark. The orb must not cover the enemy tag at the top; shift that tag slightly around the circumference if necessary.
Avoid: analog clock face, modern glossy UI, large sun icon, floating tooltip, text labels, mobile UI, photorealism.
```
