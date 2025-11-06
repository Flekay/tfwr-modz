using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace NotEnoughLeaderboards;

[BepInPlugin("com.flekay.notenoughleaderboards", "NotEnoughLeaderboards", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    public static ManualLogSource Log;
    public static Dictionary<string, LeaderboardConfig> CustomBoards = new Dictionary<string, LeaderboardConfig>();

    void Awake()
    {
        Log = Logger;

        // Configure custom leaderboards here
        CustomBoards["weird_substance"] = new LeaderboardConfig
        {
            displayName = "weird_substance",
            startItems = new Dictionary<string, double> { { "power", 1000000000 } },
            goalItems = new Dictionary<string, double> { { "weird_substance", 10000000000 } },
            leaderboardType = LeaderboardType.farm_resources,
            everythingUnlocked = true
        };

        CustomBoards["weird_substance_single"] = new LeaderboardConfig
        {
            displayName = "weird_substance_single",
            startItems = new Dictionary<string, double> { { "power", 1000000000 } },
            goalItems = new Dictionary<string, double> { { "weird_substance", 500000000 } },
            leaderboardType = LeaderboardType.farm_resources,
            everythingUnlocked = true,
            singleDrone = true
        };

        CustomBoards["polyculture"] = new LeaderboardConfig
        {
            displayName = "polyculture",
            startItems = new Dictionary<string, double> { { "power", 1000000000 } },
            goalItems = new Dictionary<string, double>
            {
                { "hay", 2000000000 },
                { "wood", 10000000000 },
                { "carrot", 2000000000 }
            },
            leaderboardType = LeaderboardType.farm_resources,
            everythingUnlocked = true
        };

        CustomBoards["polyculture_single"] = new LeaderboardConfig
        {
            displayName = "polyculture_single",
            startItems = new Dictionary<string, double> { { "power", 1000000000 } },
            goalItems = new Dictionary<string, double>
            {
                { "hay", 100000000 },
                { "wood", 500000000 },
                { "carrot", 100000000 }
            },
            leaderboardType = LeaderboardType.farm_resources,
            everythingUnlocked = true,
            singleDrone = true
        };

        CustomBoards["one_hay"] = new LeaderboardConfig
        {
            displayName = "one_hay",
            startItems = new Dictionary<string, double>(),
            goalItems = new Dictionary<string, double> { { "hay", 1 } },
            leaderboardType = LeaderboardType.farm_resources,
            everythingUnlocked = true
        };

        Log.LogInfo($"NotEnoughLeaderboards loaded with {CustomBoards.Count} boards");
        new Harmony("com.flekay.notenoughleaderboards").PatchAll();
    }
}

public class LeaderboardConfig
{
    public string displayName;
    public Dictionary<string, double> startItems;
    public Dictionary<string, double> goalItems;
    public Dictionary<string, double> unlockRequirements = new Dictionary<string, double>();
    public LeaderboardType leaderboardType = LeaderboardType.simulation;
    public bool everythingUnlocked = false;
    public bool singleDrone = false;
}
