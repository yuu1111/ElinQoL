using System;
using BepInEx;
using ElinQoL.Configuration;
using HarmonyLib;

namespace ElinQoL;

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
public sealed class Plugin : BaseUnityPlugin
{
    private Harmony? harmony;

    private void Awake()
    {
        var settings = new ModConfig(Config);
        if (!settings.Enabled.Value)
        {
            Logger.LogInfo($"{ModInfo.Name} is disabled by configuration.");
            return;
        }

        harmony = new Harmony(ModInfo.Guid);
        try
        {
            // このアセンブリの [HarmonyPatch] クラスだけを登録する。
            // 現段階ではパッチはなく、ゲームの挙動は変更しない。
            harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo($"{ModInfo.Name} v{ModInfo.Version} loaded (scaffold).");
        }
        catch (Exception exception)
        {
            Logger.LogError($"Failed to initialize {ModInfo.Name}: {exception}");
            RemovePatches();
        }
    }

    private void OnDestroy()
    {
        RemovePatches();
    }

    private void RemovePatches()
    {
        if (harmony == null)
            return;

        try
        {
            // 他の Mod のパッチは解除しない。
            harmony.UnpatchSelf();
            harmony = null;
        }
        catch (Exception exception)
        {
            Logger.LogError($"Failed to remove {ModInfo.Name} patches: {exception}");
        }
    }
}
