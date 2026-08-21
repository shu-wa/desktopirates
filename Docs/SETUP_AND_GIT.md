# desktopirates 起動・開発・Git手順

## 1. まずゲームを起動する

1. エクスプローラーで `C:\dev\desktop pirates\Builds\Windows` を開きます。
2. `desktopirates.exe` をダブルクリックします。
3. 初回だけWindows SmartScreenが出た場合は、内容を確認して「詳細情報」→「実行」を選びます。
4. デスクトップ右下付近に円盤状の海とメニューサークルが現れれば起動成功です。

通常はメニューサークルの `EXIT` から終了します。緊急時はゲームを選択して `Alt+F4` でも終了できます。位置、サイズ、音量、航海状態は保存されます。

## 2. 遊び方

1. `W/S` で航行し、`A/D` で船を旋回します。
2. `Q/E` で俯瞰方向を45度ずつ回します。
3. 円周タグは未知のイベント方向を示します。近いイベントほどタグが大きくなります。
4. 残骸・宝・港へは速度を落として接近します。
5. 残骸と宝は `F`、港も `F`、敵は射程内で `Space` です。
6. 港では修理、補給、機関強化、大砲強化ができます。港の周囲は安全地帯です。
7. 撃沈されても終了せず、所持金の一部を失って港へ曳航されます。
8. 残骸を回収すると周囲の海図情報も得られます。

## 3. メニューサークル

- クリック：丸型メニューを開閉します。
- ドラッグ：ウィンドウ全体を移動します。7px以上の移動をドラッグとして扱うため、クリックと誤判定しにくい設計です。
- `VOLUME`：全体音量をバーで変更します。
- `SIZE`：表示サイズを75%～150%で変更します。ドラッグ中は数値だけを確認でき、離した時点でウィンドウへ反映されます。
- `MUTE`：消音／消音解除を切り替えます。
- `MAP`：探索済みチャンクの海図を開きます。
- `BAG`：スクロール式の船倉インベントリを開きます。
- `CAPTAIN LOG`：航海記録、敵・ボス・海域の発見記録を開きます。
- `EXIT`：保存してゲームを終了します。
- `Esc`：航海中はメニューを開き、メニューまたは別画面の表示中は閉じます。

港への入港、イベント解決、アプリ終了時にも自動保存されます。

## 4. Unityで開発する

1. Unity Hubを起動します。
2. Projectsの「Add」または「Open」を選びます。
3. `C:\dev\desktop pirates` を指定します。
4. Unity Editor `6000.1.6f1` で開きます。
5. 初回のインポートが終わるまで待ちます。
6. `Assets/Scenes/Main.unity` を開き、Playで確認します。

Editor内ではWindowsのカラーキー透過を使わないため、透明部分がマゼンタに見えます。Windowsビルドでは透明になります。

## 5. ビルドする

1. Unityメニューの `desktopirates` → `Build Windows` を選びます。
2. 完了後、`Builds/Windows/desktopirates.exe` が更新されたことを確認します。

コマンドから実行する場合：

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.1.6f1\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath 'C:\dev\desktop pirates' `
  -executeMethod Desktopirates.Editor.DesktopiratesBuild.BuildWindows `
  -quit
```

## 6. テストする

Unityでは `Window` → `General` → `Test Runner` → `EditMode` → `Run All` を選びます。現在は13件すべて成功するのが正常です。

コマンドから実行する場合は、Test Runnerが自動終了するため `-quit` を付けません。

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.1.6f1\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath 'C:\dev\desktop pirates' `
  -runTests -testPlatform EditMode `
  -testResults 'C:\dev\desktop pirates\TestResults\editmode.xml' `
  -logFile 'C:\dev\desktop pirates\TestResults\editmode.log'
```

`editmode.xml` の先頭が `result="Passed" total="13" passed="13" failed="0"` なら成功です。

## 7. Gitの日常操作

作業前：

```powershell
cd 'C:\dev\desktop pirates'
git status
git log --oneline --decorate -5
```

変更確認とコミット：

```powershell
git diff --stat
git diff
git add Assets Packages ProjectSettings Docs README.md .gitignore
git status
git commit -m "変更内容を短く説明"
```

`Library`、`Logs`、`Builds`、テスト結果などはUnityが再生成するためGit対象外です。問題が起きても、未保存の変更を消す `git reset --hard` は安易に使わず、まず `git status` と `git diff` を確認してください。

## 8. GitHubへ登録する

このPCには現時点でGitHub CLI (`gh`) がありません。GitHub上で空の非公開リポジトリ `desktopirates` を作成した場合は、次で接続できます。

```powershell
cd 'C:\dev\desktop pirates'
git remote add origin https://github.com/あなたのユーザー名/desktopirates.git
git push -u origin main
```

GitHub側でREADME、`.gitignore`、Licenseを自動追加せず、空のリポジトリとして作るのが安全です。認証画面が出たらGitHubへログインします。
