using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using UnityEngine;

namespace LC.Grub4K.YippeeReloaded;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
[BepInDependency("ainavt.lc.lethalconfig")]
public class Plugin : BaseUnityPlugin
{
    public static ManualLogSource Log = null!;
    private ConfigEntry<EnabledState> EnabledConfig = null!;
    private ConfigEntry<string> PathConfig = null!;

    public void Awake()
    {
        Log = Logger;

        PathConfig = Config.Bind(
            "General", "Path", "",
            "The path to the directory containing sfx files to use");
        PathConfig.SettingChanged += (obj, args) => UpdateState();
        EnabledConfig = Config.Bind(
            "General", "Mode", EnabledState.Default,
            "Enable or disable hoarder bug sfx replacement");
        EnabledConfig.SettingChanged += (obj, args) => UpdateState();

        LethalConfigManager.AddConfigItem(new EnumDropDownConfigItem<EnabledState>(EnabledConfig, requiresRestart: false));
        LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(PathConfig, new TextInputFieldOptions()
        {
            RequiresRestart = false,
            CanModifyCallback = () => (EnabledConfig.Value == EnabledState.Custom) switch {
                true => CanModifyResult.True(),
                false => CanModifyResult.False("\"Mode\" must be set to \"Custom\""),
            },
        }));
        LethalConfigManager.AddConfigItem(new GenericButtonConfigItem(
            "General", "Reload custom sfx",
            "Reload custom audio files.\n\nThis button only works if \"Mode\" is set to \"Custom\"",
            "Reload...", () =>
            {
                if (EnabledConfig.Value == EnabledState.Custom)
                {
                    AudioReplacer.Load(PathConfig.Value);
                }
            }
        ));
        LethalConfigManager.SetModDescription("Change the Hoarder Bug sfx to custom sounds inside a folder");

        Harmony patcher = new(PluginInfo.PLUGIN_GUID);
        patcher.PatchAll(typeof(BugHandlerPatches));

        UpdateState();
    }

    private void UpdateState() => AudioReplacer.Load(EnabledConfig.Value switch {
        EnabledState.Default => Path.GetDirectoryName(this.Info.Location),
        EnabledState.Custom => PathConfig.Value,
        _ => "",
    });
}

public enum EnabledState {
    Disabled,
    Default,
    Custom,
}
