using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterLeaderboards;

public class LeaderboardDataManager : MonoBehaviour
{
    public class LeaderboardData
    {
        public string LeaderboardName { get; set; } = "";
        public string SteamLeaderboardName { get; set; } = "";
        public int PlayerScore { get; set; }
        public int PlayerRank { get; set; }
        public bool HasPlayerEntry { get; set; }
    }

    private List<LeaderboardData> leaderboardsData = new List<LeaderboardData>();
    private int loadingCount = 0;
    private int totalCount = 0;
    private bool isLoading = false;

    public event Action<List<LeaderboardData>> OnDataLoaded;
    public event Action<float> OnLoadingProgress;

    public void LoadAllLeaderboards()
    {
        if (isLoading)
        {
            Plugin.Log.LogWarning("Already loading leaderboards, skipping...");
            return;
        }

        // Start coroutine to wait for Steam initialization
        StartCoroutine(LoadLeaderboardsWhenReady());
    }

    private IEnumerator LoadLeaderboardsWhenReady()
    {
        Plugin.Log.LogInfo("Waiting for SteamManager initialization...");

        // Wait up to 10 seconds for Steam to initialize
        float timeout = 10f;
        float elapsed = 0f;

        while (!SteamManager.Initialized && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;

            // Update progress bar based on wait time (0 to 50% during wait)
            float waitProgress = (elapsed / timeout) * 0.5f;
            OnLoadingProgress?.Invoke(waitProgress);

            // Plugin.Log.LogInfo($"Still waiting for Steam... ({elapsed}s)");
        }

        if (!SteamManager.Initialized)
        {
            Plugin.Log.LogWarning("SteamManager not initialized - you may be offline. Creating placeholder entries.");

            // Create placeholder entries for all leaderboards
            var leaderboards = ResourceManager.GetAllLeaderboards();
            var placeholderData = new List<LeaderboardData>();
            var offlineBoards = leaderboards.ToArray();
            totalCount = offlineBoards.Length;

            for (int i = 0; i < offlineBoards.Length; i++)
            {
                var leaderboardSO = offlineBoards[i];
                placeholderData.Add(new LeaderboardData
                {
                    LeaderboardName = leaderboardSO.leaderboardName,
                    SteamLeaderboardName = leaderboardSO.steamLeaderboardName,
                    HasPlayerEntry = false,
                    PlayerScore = 0,
                    PlayerRank = 0
                });

                // Update progress for offline mode (50% to 100%)
                float progress = 0.5f + ((float)(i + 1) / totalCount) * 0.5f;
                OnLoadingProgress?.Invoke(progress);

                // Small delay to show progress animation
                yield return new WaitForSeconds(0.05f);
            }

            Plugin.Log.LogInfo($"Created {placeholderData.Count} placeholder entries (Steam offline)");
            // Show all leaderboards even without data when offline
            OnDataLoaded?.Invoke(placeholderData);
            yield break;
        }

        Plugin.Log.LogInfo("SteamManager is now initialized!");

        isLoading = true;
        leaderboardsData.Clear();

        var allLeaderboards = ResourceManager.GetAllLeaderboards();
        var leaderboardArray = allLeaderboards.ToArray();
        loadingCount = leaderboardArray.Length;
        totalCount = leaderboardArray.Length;

        Plugin.Log.LogInfo($"Loading {loadingCount} leaderboards...");

        foreach (var leaderboardSO in leaderboardArray)
        {
            Plugin.Log.LogInfo($"Requesting leaderboard: {leaderboardSO.leaderboardName} (Steam: {leaderboardSO.steamLeaderboardName})");

            var data = new LeaderboardData
            {
                LeaderboardName = leaderboardSO.leaderboardName,
                SteamLeaderboardName = leaderboardSO.steamLeaderboardName
            };

            leaderboardsData.Add(data);

            // Fetch player score (score = 0 means just fetch, not upload)
            SteamLeaderboard.LoadLeaderboard(
                leaderboardSO.steamLeaderboardName,
                0,
                playerData => OnPlayerDataLoaded(data, playerData),
                _ => { } // We don't need top entries, just player data
            );
        }
    }

    private void OnPlayerDataLoaded(LeaderboardData data, LeaderboardEntryData[] playerEntries)
    {
        Plugin.Log.LogInfo($"OnPlayerDataLoaded callback for {data.LeaderboardName}, entries: {playerEntries.Length}");

        if (playerEntries.Length > 0)
        {
            data.HasPlayerEntry = true;
            data.PlayerScore = playerEntries[0].score;
            data.PlayerRank = playerEntries[0].rank;

            // Plugin.Log.LogInfo($"Loaded {data.LeaderboardName}: Rank #{data.PlayerRank}, Score {data.PlayerScore}ms");
        }
        else
        {
            data.HasPlayerEntry = false;
            Plugin.Log.LogInfo($"No entry for {data.LeaderboardName}");
        }

        loadingCount--;
        Plugin.Log.LogInfo($"Loading count remaining: {loadingCount}");

        // Update progress (50% to 100% during data loading)
        if (totalCount > 0)
        {
            float dataProgress = 1.0f - ((float)loadingCount / totalCount);
            float progress = 0.5f + (dataProgress * 0.5f);
            OnLoadingProgress?.Invoke(progress);
        }

        if (loadingCount <= 0)
        {
            isLoading = false;
            Plugin.Log.LogInfo("All leaderboards loaded!");

            // Show ALL leaderboards, not just participated ones
            Plugin.Log.LogInfo($"Found {leaderboardsData.Count} total leaderboards");
            OnDataLoaded?.Invoke(leaderboardsData);
        }
    }
}
