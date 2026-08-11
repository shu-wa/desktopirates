# desktopirates

デスクトップの片隅に浮かぶ円形の海を探索する、Unity製の常駐型3D海洋冒険RPGプロトタイプです。

## 実装済み

- 枠なし・背景透過・常に手前のWindowsオーバーレイ
- 円盤状のローポリ海面とプロシージャル船
- 現実のローカル時刻に連動する朝・昼・夕・夜の色と時刻オーブ
- 時刻オーブのドラッグによるウィンドウ移動と位置保存
- 航行、8方向カメラ回転、視界外POIタグ
- 敵船、残骸、宝の論理マップと接近時の表示

## 操作

- `W/S` または `↑/↓`: 前進・後退
- `A/D` または `←/→`: 旋回
- `Q/E`: カメラを45度回転
- 時刻オーブをドラッグ: ゲーム全体を移動
- `F2`: 時刻表現のプレビューを6時間進める（次の実時間更新まで）

## 開発環境

- Unity 6000.1.6f1
- Windows x64

## テストとビルド

Unityメニューの `desktopirates/Prepare Project` でシーンと設定を生成します。  
`desktopirates/Build Windows` で `Builds/Windows/desktopirates.exe` を作成します。

コンセプトアートは `Assets/Art/Concept/desktopirates-concept-v03-time-orb.png` にあります。

初回起動、Unityでの編集、テスト、Git/GitHub操作の詳細は `Docs/SETUP_AND_GIT.md` を参照してください。
