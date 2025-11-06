using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Steamworks;

namespace NotEnoughLeaderboards.Patches;

[HarmonyPatch(typeof(SteamLeaderboard), "LoadLeaderboard")]
class LoadLeaderboardPatch
{
    static bool Prefix(string leaderboardName, int score,
        Action<LeaderboardEntryData[]> loadPlayerCallback,
        Action<LeaderboardEntryData[]> loadTopCallback,
        ref SteamLeaderboard __result)
    {
        if (!Plugin.CustomBoards.ContainsKey(leaderboardName))
            return true;

        var loader = new GameObject("CustomLeaderboardLoader");
        var component = loader.AddComponent<CustomLeaderboardLoader>();
        component.Load(leaderboardName, loadPlayerCallback, loadTopCallback);

        __result = null;
        return false;
    }
}

class CustomLeaderboardLoader : MonoBehaviour
{
    public void Load(string boardName, Action<LeaderboardEntryData[]> playerCallback, Action<LeaderboardEntryData[]> topCallback)
    {
        StartCoroutine(LoadCoroutine(boardName, playerCallback, topCallback));
    }

    private IEnumerator LoadCoroutine(string boardName, Action<LeaderboardEntryData[]> playerCallback, Action<LeaderboardEntryData[]> topCallback)
    {
        Api.Entry[] apiEntries = null;
        bool loaded = false;

        yield return Api.GetTop(boardName, (entries) =>
        {
            apiEntries = entries;
            loaded = true;
        });

        while (!loaded)
            yield return null;

        var topEntries = new LeaderboardEntryData[apiEntries?.Length ?? 0];
        LeaderboardEntryData? playerEntry = null;

        try
        {
            for (int i = 0; i < topEntries.Length; i++)
            {
                topEntries[i] = new LeaderboardEntryData
                {
                    playerName = apiEntries[i].name,
                    score = (int)apiEntries[i].time,
                    rank = apiEntries[i].rank
                };

                if (apiEntries[i].name == SteamFriends.GetPersonaName())
                {
                    playerEntry = topEntries[i];
                }
            }
        }
        catch
        {
            for (int i = 0; i < topEntries.Length; i++)
            {
                topEntries[i] = new LeaderboardEntryData
                {
                    playerName = apiEntries[i].name,
                    score = (int)apiEntries[i].time,
                    rank = apiEntries[i].rank
                };
            }
        }

        playerCallback?.Invoke(playerEntry.HasValue ? new[] { playerEntry.Value } : new LeaderboardEntryData[0]);
        topCallback?.Invoke(topEntries);

        Destroy(gameObject);
    }
}
