using BepInEx.Configuration;

namespace ElinQoL.Configuration;

internal sealed class ModConfig
{
    internal ConfigEntry<bool> Enabled { get; }

    internal ModConfig(ConfigFile config)
    {
        Enabled = config.Bind("General", "Enabled", true,
            "ElinQoL のパッチを有効にします。変更後はゲームを再起動してください。");
    }
}
