# CLAUDE.md

ONI mod（総質量センサー）。共通事項は HQ（親フォルダ）の `CLAUDE.md`、機能の概要は `README.md`。Build: `dotnet build MassSensorWithSignal.sln`（Debug は `Local.Build.props` の `ModInstallPath` へ配備。コンパイル確認だけなら `-p:ModInstallPath=`。Release は `release/MassSensorWithSignal` に配布用フォルダを作る）。

- ソースは `MassSensorWithSignal/`。`SignalMassSensor.cs`（センサー本体。`IThresholdSwitch` を実装し、200 ms ごとに同じ元素でつながる領域〔4 近傍〕の総質量を数える）、`SignalMassSensorConfig.cs`（建物定義）、`MassSensorWithSignalPatches.cs`（Harmony パッチ 2 つ）。
- パッチは (1) `GeneratedBuildings.LoadGeneratedBuildings` の Prefix で自動化メニューに建物を追加、(2) `ThresholdSwitchSideScreen.SetTarget` の Postfix で、このセンサーのときだけ入力欄の文字数上限を 6 から 10 に広げる。後者は `Traverse` でゲームの private フィールド（`numberInput.inputField`）に触るので、ゲームの更新で壊れやすい。例外は握ってログに出す。
- 設定（しきい値、以上／以下）は `[Serialize]` でセーブに入る。フィールド名を変えると既存セーブの設定が消える。
- PLib を使い、DLL に取り込んで配布する（別インストール不要）。翻訳は `translations/ja.po`（`strings/` に `.po` を置かない。置くと言語に関係なく読まれる）。
- 版と要求ビルドは `MassSensorWithSignal/mod_info.yaml`。U60 対応で 1.1.0、`minimumSupportedBuild` 763796。
- Workshop 用の説明文・変更履歴・サムネイルは `Workshop/`（英日）。更新時は版、README、変更履歴をそろえる。ID は 3835993950。

## 実機検証
`MassSensorWithSignal/HarnessTests.cs`（`#if DEBUG`、タグ `masssensor`、7 件）: 自動化メニューへの登録、連結した同じ元素の質量の合算（斜めと岩を挟んだ別の水は数えない）、真空での 0、「以上」「以下」でしきい値を跨ぐと信号が切り替わる（等しいときは切り替わらない）、質量そのものが変わったときの切り替え、入力欄の文字数上限（このセンサーは 10、バニラは 6）。
場面は、固い岩の中に水 100 + 200 + 300 kg の小部屋を作ったもの（セーブに自然の岩が少ないので、空きの 10 × 4 セルをテストが花崗岩に置き換え、終わると戻す）。
実行: `GRAVITAS_EXCLUSIVE=1 python C:/Users/dha_1/ONI-Modding/GravitasTestHarness/Gravitas.TestProbe/probe.py run --project . --summary --tests '{"tags":["masssensor"]}'`
ゲームを起動し本番の mods.json を書き換えるので、実行前にユーザーに確認する。ほかの実行がゲームを使っていると `stage=busy` になるので、待って再試行する。
テストが見ていないもの: セーブ・ロードでの設定の保持、センサーの見た目（on/off のアニメーション以外）、ゲーム内の実際のオートメーション配線への接続。
`csproj` に Debug 専用のハーネス参照（`HarnessDllPath`、`Local.Build.props` に書く）がある。ILRepack の `/lib` にもその場所を足してある（Debug のみ。足さないとハーネスの参照が解決できず結合に失敗する）。Release には入らない。
