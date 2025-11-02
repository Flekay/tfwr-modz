using HarmonyLib;
using UnityEngine;

namespace BetterLeaderboards.Patches;

[HarmonyPatch(typeof(Menu))]
public class MenuPatch
{
    private static LeaderboardDataManager dataManager;
    private static LeaderboardOverviewUI overviewUI;
    private static bool isInitialized = false;

    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    static void Start_Postfix(Menu __instance)
    {
        if (isInitialized) return;

        Plugin.Log.LogInfo("Menu.Start postfix - initializing leaderboard overview");

        // Create data manager GameObject
        var managerObj = new GameObject("LeaderboardDataManager");
        GameObject.DontDestroyOnLoad(managerObj);
        dataManager = managerObj.AddComponent<LeaderboardDataManager>();

        // Find the Canvas in the menu
        Canvas menuCanvas = __instance.GetComponentInChildren<Canvas>();
        if (menuCanvas == null)
        {
            // Try to find any canvas in the scene
            menuCanvas = GameObject.FindObjectOfType<Canvas>();
        }

        if (menuCanvas == null)
        {
            Plugin.Log.LogError("Could not find Canvas to attach leaderboard UI!");
            return;
        }

        Plugin.Log.LogInfo($"Found canvas: {menuCanvas.name}");

        // Create UI manager GameObject
        var uiObj = new GameObject("LeaderboardOverviewUI");
        GameObject.DontDestroyOnLoad(uiObj);
        overviewUI = uiObj.AddComponent<LeaderboardOverviewUI>();

        // Create the UI on the menu canvas - this shows immediately
        overviewUI.CreateUI(menuCanvas);

        // Hook up reload button
        overviewUI.OnReloadRequested += () =>
        {
            Plugin.Log.LogInfo("Reload requested - reloading leaderboard data");
            dataManager.LoadAllLeaderboards();
        };

        // Subscribe to loading progress
        dataManager.OnLoadingProgress += (progress) =>
        {
            overviewUI.UpdateLoadingProgress(progress);
        };

        // Subscribe to data loaded event
        dataManager.OnDataLoaded += (data) =>
        {
            Plugin.Log.LogInfo($"Data loaded event received: {data.Count} leaderboards");

            if (data.Count == 0)
            {
                Plugin.Log.LogWarning("No leaderboards found!");
                return;
            }

            overviewUI.PopulateLeaderboards(data);
            // Don't call Show() here - UI is already visible
        };

        // Start loading leaderboard data
        Plugin.Log.LogInfo("Starting to load leaderboard data...");
        overviewUI.ShowLoadingBar(true);
        dataManager.LoadAllLeaderboards();

        isInitialized = true;
        Plugin.Log.LogInfo("Leaderboard overview initialized successfully");
    }

    // Hide leaderboard when entering Options submenu
    [HarmonyPostfix]
    [HarmonyPatch("Options")]
    static void Options_Postfix()
    {
        if (overviewUI != null)
        {
            overviewUI.Hide();
        }
    }

    // Hide leaderboard when entering Save Chooser submenu
    [HarmonyPostfix]
    [HarmonyPatch("ChooseSave")]
    static void ChooseSave_Postfix()
    {
        if (overviewUI != null)
        {
            overviewUI.Hide();
        }
    }

    // Show leaderboard when returning to title page
    [HarmonyPostfix]
    [HarmonyPatch("Open")]
    static void Open_Postfix()
    {
        if (overviewUI != null)
        {
            overviewUI.Show();
        }
    }
}
