# ElinQoL

Elin を遊びやすくするための QoL Mod です。

## 機能

- **移動採掘**：つるはしを手に持って壁の方向へ移動すると採掘します。

## 導入

1. ゲームを終了します。
2. ビルドで生成される `artifacts/ElinQoL/` を、ゲームの `Package/Mod_ElinQoL/` へコピーします。
3. ゲームを起動し、Mod 一覧で有効にします。

**ゲーム内動作は未確認です。バックアップしたセーブで試してください。**

## 設定

初回読み込み後、ゲームの `BepInEx/config/com.github.yuu1111.elinqol.cfg` で変更できます。変更後はゲームを再起動してください。

```ini
[General]
Enabled = true

[Mining]
MineOnMove = true
```

`Enabled` は Mod 全体、`MineOnMove` は移動採掘の有効・無効です。

## 開発

.NET SDK 8 以降と、Elin のインストールが必要です。対象は `netstandard2.0`。ゲーム同梱の DLL を直接参照します。

導入先が既定の Steam フォルダと異なる場合は、[Local.props.example](Local.props.example) を `Local.props` にコピーし、`ElinGamePath` を指定してください。

```powershell
dotnet build ElinQoL.csproj -c Release
dotnet run --project tests/ElinQoL.Tests.csproj -c Release
```

ビルド時のゲームへの自動配置は行いません。Elin 0.23.355 の DLL でビルドと条件テストを確認済みです。テストには実際のゲーム内動作の確認を含みません。

参考：[Elin Modding Docs](https://elin-modding.net/ja/articles/2_Getting%20Started/Script%20Mods/script_mod) / [Elin Plugin Template](https://github.com/gottyduke/Elin.Plugins/tree/master/PluginTemplate)
