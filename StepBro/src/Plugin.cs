using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace StepBro;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.flekay.stepbro";
    public const string PluginName = "StepBro";
    public const string PluginVersion = "1.0.0";

    public static Plugin Instance { get; private set; } = null!;
    public static ManualLogSource Log { get; private set; } = null!;

    private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        // Initialize configuration
        ConfigManager.Initialize(Config);

        // Plugin startup logic
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        harmony.PatchAll();
    }
}
