using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace NightSky;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; } = null!;
    public static ManualLogSource Log { get; private set; } = null!;

    // Configuration entries
    public static ConfigEntry<int> SkyColorR { get; private set; } = null!;
    public static ConfigEntry<int> SkyColorG { get; private set; } = null!;
    public static ConfigEntry<int> SkyColorB { get; private set; } = null!;
    public static ConfigEntry<float> LightIntensity { get; private set; } = null!;
    public static ConfigEntry<bool> DisablePostProcessing { get; private set; } = null!;

    private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        // Setup configuration
        SkyColorR = Config.Bind("Sky Color",
            "Red",
            26,
            new ConfigDescription("Red color component (0-255)", new AcceptableValueRange<int>(0, 255)));

        SkyColorG = Config.Bind("Sky Color",
            "Green",
            27,
            new ConfigDescription("Green color component (0-255)", new AcceptableValueRange<int>(0, 255)));

        SkyColorB = Config.Bind("Sky Color",
            "Blue",
            38,
            new ConfigDescription("Blue color component (0-255)", new AcceptableValueRange<int>(0, 255)));

        LightIntensity = Config.Bind("Lighting",
            "Main Light Intensity",
            1.0f,
            new ConfigDescription("Brightness of the main directional light (0.0 = dark, 1.0 = default, 2.0 = very bright)", new AcceptableValueRange<float>(0f, 3f)));

        DisablePostProcessing = Config.Bind("Post Processing",
            "Disable Post Processing",
            true,
            "Disable post-processing effects like vignette, color grading, etc.");

        // Plugin startup logic
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Log.LogInfo($"Sky color configured as RGB({SkyColorR.Value}, {SkyColorG.Value}, {SkyColorB.Value})");

        harmony.PatchAll();
    }

    public static Color GetSkyColor()
    {
        // Return the raw configured color without any correction
        return new Color(
            SkyColorR.Value / 255f,
            SkyColorG.Value / 255f,
            SkyColorB.Value / 255f
        );
    }
}
