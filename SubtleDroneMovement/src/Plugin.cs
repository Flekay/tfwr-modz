using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SubtleDroneMovement;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; } = null!;
    public static ManualLogSource Log { get; private set; } = null!;

    // Configuration entries
    public static ConfigEntry<float> MovementAmplitude { get; private set; } = null!;
    public static ConfigEntry<float> MovementSpeed { get; private set; } = null!;
    public static ConfigEntry<float> RotationAmplitude { get; private set; } = null!;
    public static ConfigEntry<bool> EnableVerticalBob { get; private set; } = null!;

    private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        // Setup configuration
        MovementAmplitude = Config.Bind("Movement",
            "Amplitude",
            0.05f,
            new ConfigDescription("How far the drone moves when idle (0.0 = no movement, 0.1 = noticeable)", new AcceptableValueRange<float>(0f, 0.3f)));

        MovementSpeed = Config.Bind("Movement",
            "Speed",
            1.0f,
            new ConfigDescription("How fast the organic movement is (0.5 = slow, 1.0 = normal, 2.0 = fast)", new AcceptableValueRange<float>(0.1f, 5f)));

        RotationAmplitude = Config.Bind("Rotation",
            "Amplitude",
            2.0f,
            new ConfigDescription("How much the drone rotates when idle in degrees (0.0 = no rotation, 5.0 = noticeable)", new AcceptableValueRange<float>(0f, 15f)));

        EnableVerticalBob = Config.Bind("Movement",
            "Vertical Bobbing",
            true,
            "Enable subtle up/down bobbing motion");

        // Plugin startup logic
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        harmony.PatchAll();
    }
}
