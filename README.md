# ElinQoL

Elin 用の QoL Mod の開発用プロジェクトです。現在は読み込み・設定・ログ・Harmony パッチの登録と解除だけを備え、ゲームの挙動やセーブデータは変更しません。具体的な QoL 機能はまだ実装していません。

## 採用した構成

[Elin Modding Docs: Script Mods](https://elin-modding.net/ja/articles/2_Getting%20Started/Script%20Mods/script_mod) が案内する [Elin Plugin Template](https://github.com/gottyduke/Elin.Plugins/tree/master/PluginTemplate) の構成を参考にしています。

- SDK 形式の C# プロジェクト、対象フレームワークは `netstandard2.0`。
- ゲームに同梱された BepInEx 6 系と Harmony を使用します。別バージョンを NuGet で導入・上書きしません。
- ゲーム・Unity・BepInEx の DLL はインストール先を直接参照し、ビルド出力に含めません。
- テンプレートと異なり、ビルド時にゲームへ自動配置しません。配布用のファイルは作業フォルダ内に生成します。
- C# 12、nullable 有効、警告をエラーとして扱います。プレビュー言語機能は使いません。

.NET SDK のバージョンと、Mod が実行される対象フレームワークは別です。.NET 10 SDK でビルドしても、Mod 自体を .NET 10 向けにはしません。

## ビルド

.NET SDK 8 以降と、Elin のインストールが必要です。この環境では .NET SDK 10.0.303 で検証しました。

以下はこのプロジェクトのフォルダで実行します。

```powershell
dotnet build ElinQoL.csproj -c Release
```

既定では次のインストール先を参照します。

```text
C:/Program Files (x86)/Steam/steamapps/common/Elin
```

導入先が異なる場合は、[Local.props.example](Local.props.example) を `Local.props` にコピーし、`ElinGamePath` を指定してください。環境変数またはコマンドラインでも指定できます。

```powershell
dotnet build ElinQoL.csproj -c Release "-p:ElinGamePath=D:/SteamLibrary/steamapps/common/Elin"
```

初回の復元では .NET Standard のコンパイル用パッケージを NuGet から取得します。ゲームの DLL が不足している場合は、ビルドをエラーで止めます。

## ローカル導入

ビルドが生成する `artifacts/ElinQoL/` を、フォルダごと `<ElinGamePath>/Package/Mod_ElinQoL/` へコピーしてください。ゲームを終了した状態で配置し、起動後に Mod 一覧で有効にします。Workshop 版などを追加した場合は、同じ Mod の二重配置を避けてください。

配布用フォルダには Mod の DLL、デバッグ情報、[package.xml](package/package.xml) が入ります。ゲーム本体や BepInEx の DLL は同梱しません。Workshop 用のプレビュー画像は未作成です。

初回読み込み時に、次の設定ファイルが生成されます。

```text
<ElinGamePath>/BepInEx/config/com.github.yuu1111.elinqol.cfg
```

```ini
[General]
Enabled = true
```

`Enabled = false` でパッチの登録を停止します。変更後はゲームを再起動してください。起動確認では BepInEx のログ、または `%USERPROFILE%/AppData/LocalLow/Lafrontier/Elin/Player.log` の `ElinQoL v0.1.0 loaded (scaffold).` を確認します。

## QoL 機能の追加

- [src/Plugin.cs](src/Plugin.cs): プラグインの初期化、ログ、パッチの登録と解除。
- [src/Configuration/ModConfig.cs](src/Configuration/ModConfig.cs): 設定項目。機能ごとの有効・無効設定もここへ追加します。
- [src/ModInfo.cs](src/ModInfo.cs): Mod ID・表示名・Mod 自体のバージョン。アセンブリ情報もこの定数を使用します。

機能を追加するときは `src/Features/<機能名>/` に実装を置きます。対象メソッドを実ゲームの `Elin_Data/Managed/Elin.dll` で確認してから、`[HarmonyPatch]` を付けたクラスを追加してください。このアセンブリ内のパッチは起動時に自動登録されます。現時点ではダミーのゲームパッチは入れていません。

`Awake` は設定やパッチ登録に使用し、プレイヤーやワールドの存在を前提にしないでください。ガイドにある `OnStartCore` は全プラグインの読み込み後、`Start` はデータ読み込み後の処理に使います。個々の処理でセーブ読み込み済みかどうかの確認も必要です。

機能別のパッチでは、対応する有効・無効設定を確認してから処理してください。ゲームのデータを恒久変更する機能は、表示だけの機能と分離し、バックアップしたセーブで検証します。

[package.xml](package/package.xml) の `version` は **Mod 自体ではなく Elin 本体のバージョン**です。現在の値は参照 DLL の `0.23.355` に合わせた仮値で、ゲーム内での動作確認済みという意味ではありません。公開前に実機確認して更新してください。`visibility` は開発中のため `Private` にしています。

## 検証範囲

導入済みの Elin DLL 0.23.355.0、BepInEx DLL 6.0.0.0、Harmony DLL 2.10.0.0 を参照して Release ビルドを確認しました。これは手元の導入環境であり、最新のゲーム配信版であることまでは確認していません。

ゲーム内での読み込み・設定生成・パッチの適用と解除は未検証です。今回はゲーム側へのコピーやゲーム起動は行っていません。QoL 機能が未実装のため、自動テスト用プロジェクトはまだ追加していません。
