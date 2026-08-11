# desktopirates 操作・開発・Git手順

## 1. すぐにゲームを起動する

1. エクスプローラーで `C:\dev\desktop pirates\Builds\Windows` を開く。
2. `desktopirates.exe` をダブルクリックする。
3. Windows SmartScreenが表示された場合は、内容を確認して「詳細情報」→「実行」を選ぶ。
4. 円盤状の海と時刻オーブがデスクトップ上へ表示されたら起動成功。

終了するときはゲームを選択して `Alt+F4` を押す。終了時にウィンドウ位置が保存される。

## 2. ゲーム操作

- `W/S` または `↑/↓`: 前進・後退
- `A/D` または `←/→`: 船を旋回
- `Q/E`: カメラを45度ずつ回転
- 時刻オーブをドラッグ: ゲーム全体を移動
- `F2`: 時刻表現を6時間進めて確認

時刻オーブは通常、Windowsの現在時刻を使用する。朝、昼、夕方、夜で空、太陽、月、星、海の色が変わる。

## 3. Unityでプロジェクトを開く

1. Unity Hubを起動する。
2. Projects画面の「Add」または「Open」を選ぶ。
3. `C:\dev\desktop pirates` フォルダを指定する。
4. Editor Versionは `6000.1.6f1` を選ぶ。
5. 初回はLibraryの再生成とアセットインポートが行われるので完了まで待つ。
6. `Assets/Scenes/Main.unity` を開く。
7. Unity上部のPlayボタンで実行する。

Unity Editor内ではWindowsのカラーキー透過を適用しないため、背景がマゼンタ色に見える。これは正常で、Windowsビルドでは透明になる。

## 4. Windows版を再ビルドする

1. Unityでプロジェクトを開く。
2. メニューバーから `desktopirates` → `Build Windows` を選ぶ。
3. ビルド完了まで待つ。
4. `Builds/Windows/desktopirates.exe` が更新されたことを確認する。

`desktopirates` → `Prepare Project` はシーン、Player設定、必須マテリアルだけを再生成したい場合に使う。

## 5. テストを実行する

### Unity画面から実行

1. `Window` → `General` → `Test Runner` を開く。
2. `EditMode` タブを選ぶ。
3. `Run All` を押す。
4. 9件すべて緑色になれば成功。

### コマンドで実行

PowerShellで次を実行する。

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.1.6f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'C:\dev\desktop pirates' `
  -runTests `
  -testPlatform EditMode `
  -testResults 'C:\dev\desktop pirates\TestResults.xml' `
  -logFile 'C:\dev\desktop pirates\unity-tests.log'
```

テスト終了後、`TestResults.xml` の先頭に `result="Passed"`、`passed="9"`、`failed="0"` があることを確認する。

## 6. ローカルGitの基本操作

このフォルダはすでにGitリポジトリとして初期化され、`main` ブランチに初回コミットが作られている。

状態確認:

```powershell
cd 'C:\dev\desktop pirates'
git status
git log --oneline --decorate -5
```

変更内容の確認:

```powershell
git diff
git diff --stat
```

変更をコミット:

```powershell
git add Assets Packages ProjectSettings Docs README.md .gitignore
git status
git commit -m "変更内容を短く説明"
```

`Library`、`Temp`、`Logs`、`Builds`、テスト結果などは自動生成物なので `.gitignore` で除外している。

## 7. Gitの名前・メールを確認する

このプロジェクトの設定を確認:

```powershell
git config user.name
git config user.email
```

変更する場合:

```powershell
git config user.name "Git上で使用する名前"
git config user.email "正しいメールアドレス"
```

これは `desktop pirates` リポジトリだけに適用される。PC全体の設定を変えたい場合だけ `--global` を付ける。

## 8. GitHubへ登録する

公開範囲が未決定なので、GitHubリポジトリの作成とpushはまだ行っていない。

### GitHub画面から作る場合

1. GitHubへログインする。
2. New repositoryを選ぶ。
3. Repository nameを `desktopirates` にする。
4. PublicまたはPrivateを選ぶ。迷う場合はPrivateを推奨。
5. README、`.gitignore`、Licenseは追加せず、空のリポジトリとして作る。
6. GitHubに表示されたURLを使い、PowerShellで次を実行する。

```powershell
cd 'C:\dev\desktop pirates'
git remote add origin https://github.com/ユーザー名/desktopirates.git
git push -u origin main
```

### GitHub CLIを使う場合

GitHub CLIへログイン済みなら、Privateリポジトリは次の一行で作成できる。

```powershell
cd 'C:\dev\desktop pirates'
gh repo create desktopirates --private --source . --remote origin --push
```

公開する場合は `--private` を `--public` に変更する。Publicにするとソースコード、設計書、コンセプトアートを誰でも閲覧できるため、公開前に必ず内容を確認する。

## 9. 普段の安全な開発手順

1. 作業前に `git status` を確認する。
2. Unityで変更する。
3. Test Runnerでテストする。
4. 必要ならWindowsビルドを起動確認する。
5. `git diff` で変更内容を読む。
6. 必要なファイルだけ `git add` する。
7. `git commit` する。
8. GitHubを設定済みなら `git push` する。

問題が起きたとき、内容を確認せず `git reset --hard` を使わない。まず `git status` と `git diff` で未保存の変更を確認する。

## 10. 次の実装候補

1. 港への入港とショップ
2. 船体・帆・砲・観測設備の改造
3. 船員雇用と配置
4. 敵船とのリアルタイム戦闘
5. 航跡図と海図断片
6. セーブ／ロード画面
7. タスクトレイからの終了・設定
