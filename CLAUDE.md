# CLAUDE.md

ONI mod（総質量センサー）。共通事項は HQ（親フォルダ）の `CLAUDE.md`、機能の概要は `README.md`。Build: `dotnet build MassSensorWithSignal.sln`（Debug は `Local.Build.props` の `ModInstallPath` へ配備。コンパイル確認だけなら `-p:ModInstallPath=`。Release は `release/MassSensorWithSignal` に配布用フォルダを作る）。

- ソースは `MassSensorWithSignal/`。`SignalMassSensor.cs`（センサー本体。`IThresholdSwitch` を実装し、200 ms ごとに同じ元素でつながる領域〔4 近傍〕の総質量を数える）、`SignalMassSensorConfig.cs`（建物定義）、`MassSensorWithSignalPatches.cs`（Harmony パッチ 2 つ）。
- パッチは (1) `GeneratedBuildings.LoadGeneratedBuildings` の Prefix で自動化メニューに建物を追加、(2) `ThresholdSwitchSideScreen.SetTarget` の Postfix で、このセンサーのときだけ入力欄の文字数上限を 6 から 10 に広げる。後者は `Traverse` でゲームの private フィールド（`numberInput.inputField`）に触るので、ゲームの更新で壊れやすい。例外は握ってログに出す。
- 設定（しきい値、以上／以下）は `[Serialize]` でセーブに入る。フィールド名を変えると既存セーブの設定が消える。
- PLib を使い、DLL に取り込んで配布する（別インストール不要）。翻訳は `translations/ja.po`（`strings/` に `.po` を置かない。置くと言語に関係なく読まれる）。
- 版と要求ビルドは `MassSensorWithSignal/mod_info.yaml`。U60 対応で 1.1.0、`minimumSupportedBuild` 763796。
- Workshop 用の説明文・変更履歴・サムネイルは `Workshop/`（英日）。更新時は版、README、変更履歴をそろえる。ID は 3835993950。

## 実機検証
`harness.json` は読み込み確認用（起動、Mod の読み込み、セーブのロード、ログに例外なし）。`HarnessTests.cs` はなく、センサーの機能（質量の合算、信号の出力）の自動テストはない。
実行: `python C:/Users/dha_1/ONI-Modding/GravitasTestHarness/Gravitas.TestProbe/probe.py run --project . --summary`
ゲームを起動し本番の mods.json を書き換えるので、実行前にユーザーに確認する。ほかの実行がゲームを使っていると `stage=busy` になるので、待って再試行する。
U60（buildid 26770249）で読み込み確認に合格済み（2026-11-20）。
