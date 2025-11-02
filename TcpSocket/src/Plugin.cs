using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace TcpSocket;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; } = null!;
    public static ManualLogSource Log { get; private set; } = null!;

    private readonly Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

    public TcpServerManager ServerManager { get; private set; }

    private ConfigEntry<int> configPort;
    private ConfigEntry<bool> configAutoStart;

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        // Setup configuration
        configPort = Config.Bind("Server", "Port", 9999, "TCP server port");
        configAutoStart = Config.Bind("Server", "AutoStart", true, "Automatically start server on game launch");

        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        // Apply Harmony patches
        harmony.PatchAll();

        // Initialize TCP server
        ServerManager = new TcpServerManager(configPort.Value, Log);

        if (configAutoStart.Value)
        {
            ServerManager.Start();
        }
    }

    private void OnDestroy()
    {
        ServerManager?.Stop();
    }

    private void OnApplicationQuit()
    {
        ServerManager?.Stop();
    }
}
