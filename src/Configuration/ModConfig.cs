using BepInEx.Configuration;

namespace ElinQoL.Configuration;

internal sealed class ModConfig
{
    internal ConfigEntry<bool> Enabled { get; }
    internal ConfigEntry<bool> MineOnMove { get; }

    internal ModConfig(ConfigFile config)
    {
        Enabled = config.Bind("General", "Enabled", true,
            "ElinQoL のパッチを有効にします。変更後はゲームを再起動してください。");
        MineOnMove = config.Bind("Mining", "MineOnMove", true,
            "つるはしを手に持って壁の方向へ移動入力すると、通常の採掘を開始します。");
    }
}
