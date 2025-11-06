using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Steamworks;

namespace NotEnoughLeaderboards.Patches;

[HarmonyPatch(typeof(ResourceManager), "LoadAll")]
class ResourceManagerPatch
{
    static void Postfix()
    {
        var leaderboardsField = typeof(ResourceManager).GetField("leaderboards", BindingFlags.NonPublic | BindingFlags.Static);
        var leaderboards = (Dictionary<string, LeaderboardSO>)leaderboardsField.GetValue(null);

        foreach (var kvp in Plugin.CustomBoards)
        {
            var config = kvp.Value;
            var leaderboardName = kvp.Key;

            var leaderboardSO = ScriptableObject.CreateInstance<LeaderboardSO>();
            leaderboardSO.leaderboardName = leaderboardName;
            leaderboardSO.steamLeaderboardName = leaderboardName;
            leaderboardSO.leaderboardType = config.leaderboardType;
            leaderboardSO.everythingUnlocked = config.everythingUnlocked;
            leaderboardSO.singleDrone = config.singleDrone;

            // Create start items
            leaderboardSO.startItems = new ItemBlock();
            leaderboardSO.startItems.items = new double[ResourceManager.GetAllItems().Count()];
            foreach (var item in config.startItems)
            {
                int itemId = StringIds.GetItemId(item.Key);
                if (itemId >= 0) leaderboardSO.startItems.items[itemId] = item.Value;
            }

            // Create goal items
            leaderboardSO.goalItems = new ItemBlock();
            leaderboardSO.goalItems.items = new double[ResourceManager.GetAllItems().Count()];
            foreach (var item in config.goalItems)
            {
                int itemId = StringIds.GetItemId(item.Key);
                if (itemId >= 0) leaderboardSO.goalItems.items[itemId] = item.Value;
            }

            leaderboards[leaderboardName] = leaderboardSO;
            Plugin.Log.LogInfo($"Added custom leaderboard: {leaderboardName}");
        }
    }
}

[HarmonyPatch(typeof(LeaderboardManager), "StartLeaderboardRun")]
class LeaderboardStartPatch
{
    static void Prefix(ref bool showAverageText)
    {
        // Force show average text for all leaderboard runs
        showAverageText = true;
    }
}

// Patch Steam leaderboard upload
[HarmonyPatch(typeof(SteamUserStats), "UploadLeaderboardScore")]
class SteamUploadPatch
{
    static bool Prefix()
    {
        return true;
    }
}

[HarmonyPatch(typeof(LeaderboardManager), "StopLeaderboardRun")]
class LeaderboardStopPatch
{
    private static Dictionary<string, TimeSpan> pendingSubmissions = new Dictionary<string, TimeSpan>();

    static void Postfix(bool finished, string leaderboardName, string steamLeaderboardName, TimeSpan timeSpan)
    {
        // Track ALL calls for custom boards
        if (Plugin.CustomBoards.ContainsKey(steamLeaderboardName))
        {
            if (timeSpan.TotalMilliseconds > 0)
            {
                pendingSubmissions[steamLeaderboardName] = timeSpan;
            }

            if (finished && pendingSubmissions.ContainsKey(steamLeaderboardName))
            {
                var finalTime = pendingSubmissions[steamLeaderboardName];

                try
                {
                    string steamId = SteamUser.GetSteamID().ToString();
                    string playerName = SteamFriends.GetPersonaName();

                    long time = (long)finalTime.TotalMilliseconds;

                    var go = new GameObject("ApiSubmitter");
                    var submitter = go.AddComponent<ApiSubmitter>();
                    submitter.Submit(steamLeaderboardName, playerName, steamId, time);

                    pendingSubmissions.Remove(steamLeaderboardName);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"Submission error: {ex.Message}");
                }
            }
        }
    }
}

class ApiSubmitter : MonoBehaviour
{
    public void Submit(string board, string playerName, string steamId, long timeMs)
    {
        StartCoroutine(SubmitCoroutine(board, playerName, steamId, timeMs));
    }

    private IEnumerator SubmitCoroutine(string board, string playerName, string steamId, long timeMs)
    {
        bool success = false;
        int rank = 0;

        yield return Api.Submit(board, steamId, playerName, timeMs, (s, r) =>
        {
            success = s;
            rank = r;
        });

        if (success)
        {
            Plugin.Log.LogInfo($"Submission successful! Rank: #{rank}");
        }

        Destroy(gameObject);
    }
}
