using HarmonyLib;
using UnityEngine;

namespace BetterLeaderboards.Patches;

[HarmonyPatch(typeof(LeaderboardManager))]
public class LeaderboardManagerPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("OkPressed")]
    static bool OkPressed_Prefix(LeaderboardManager __instance)
    {
        // Check if we opened the leaderboard from our overview
        if (LeaderboardOverviewUI.IsViewingFromOverview)
        {
            Plugin.Log.LogInfo("Closing leaderboard and returning to menu (from overview)");

            // Use reflection to access the leaderboardScreen field
            var leaderboardManagerType = typeof(LeaderboardManager);
            var leaderboardScreenField = leaderboardManagerType.GetField("leaderboardScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (leaderboardScreenField != null)
            {
                var leaderboardScreen = leaderboardScreenField.GetValue(__instance) as GameObject;
                if (leaderboardScreen != null)
                {
                    leaderboardScreen.SetActive(false);
                    Plugin.Log.LogInfo("Leaderboard screen closed");
                }
            }

            // Get the stored Menu instance
            var menu = MenuPatch.MenuInstance;
            if (menu != null)
            {
                Plugin.Log.LogInfo("Found Menu instance, calling Open()");

                // Call Menu.Open() to properly restore menu state
                menu.Open();

                Plugin.Log.LogInfo("Menu.Open() called");
            }
            else
            {
                Plugin.Log.LogWarning("MenuPatch.MenuInstance is null!");
            }

            // Show our overview UI again
            var overviewUI = GameObject.FindObjectOfType<LeaderboardOverviewUI>();
            if (overviewUI != null)
            {
                Plugin.Log.LogInfo("Showing overview UI");
                overviewUI.Show();
            }
            else
            {
                Plugin.Log.LogWarning("Could not find LeaderboardOverviewUI instance!");
            }

            // Reset the flag
            LeaderboardOverviewUI.IsViewingFromOverview = false;
            Plugin.Log.LogInfo("IsViewingFromOverview flag reset");

            // Prevent the original method from running
            return false;
        }

        // Let the original method run for normal leaderboard completion
        return true;
    }
}
