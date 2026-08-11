# 実装・検証メモ

## 構成

- `GameBootstrap.cs`: 空のシーンからゲーム全体を構築する起点。
- `OceanDisc.cs`: 円形メッシュ生成、外周スカート、低ポリ波アニメーション。
- `ProceduralSceneFactory.cs`: 船、帆、ランタン、敵船、残骸、宝をコード生成。
- `BoatController.cs`: 航行、旋回、論理マップ座標。
- `CameraRigController.cs`: Q/Eによる45度刻み8方向回転。
- `PoiSystem.cs`: 視界外POIと接近時の実物表示。
- `TagRingController.cs`: POI方向、距離連動サイズ、円周配置。
- `DayCycle.cs`: 朝、昼、夕方、夜の色と時刻判定。
- `TimeOrbController.cs`: 実時間オーブ生成とドラッグ入力。
- `WindowsOverlayController.cs`: 背景透過、枠なし、常に手前、モニター内位置補正、位置保存。

## Windows表示方式

Unity Playerの背景をカラーキー `#ff00ff` で描画し、Win32のLayered Windowでその色だけを透明化する。Flip Model Swapchainはカラーキー透過と競合するため無効化し、Direct3D 11を使用する。

## 検証結果

- Unityコンパイル: 成功
- EditModeテスト: 9/9成功
- Windows x64ビルド: 成功
- 実行プロセス: 応答正常
- 実行時例外: 0
- ウィンドウ: 720×760、枠なし、Layered、ToolWindow
- 透過: 円盤とUI以外のデスクトップが表示されることを確認
- マルチモニター: 現在のモニターの作業領域へ位置を補正

詳細なNUnit XMLはプロジェクト直下の `TestResults.xml` に保存している。
