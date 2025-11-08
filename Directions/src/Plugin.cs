using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace Directions;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.flekay.directions";
    public const string PluginName = "Directions";
    public const string PluginVersion = "1.0.0";

    public static Plugin Instance { get; private set; } = null!;
    public static ManualLogSource Log { get; private set; } = null!;

    private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        // Plugin startup logic
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        try
        {
            harmony.PatchAll();
            Log.LogInfo("All Harmony patches applied successfully");
            
            // Log which patches were applied
            var patchedMethods = harmony.GetPatchedMethods();
            foreach (var method in patchedMethods)
            {
                Log.LogInfo($"Patched method: {method.DeclaringType?.Name}.{method.Name}");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"Failed to apply Harmony patches: {ex}");
        }
    }
}
