using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace BetterTooltips.Patches;

[HarmonyPatch(typeof(Farm))]
public static class FarmPatch
{
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new[] { typeof(Simulation), typeof(IEnumerable<string>), typeof(ItemBlock), typeof(List<SFO>), typeof(List<SFO>), typeof(bool) })]
    [HarmonyPrefix]
    public static void Constructor_Prefix()
    {
        var startUnlocksField = typeof(Farm).GetField("startUnlocks", BindingFlags.Public | BindingFlags.Static);

        if (startUnlocksField?.GetValue(null) is List<string> startUnlocks)
        {
            if (!startUnlocks.Contains("set_tile_info"))
            {
                startUnlocks.Add("set_tile_info");
                Plugin.Log.LogInfo("Added set_tile_info to startUnlocks");
            }

            if (!startUnlocks.Contains("clear_all_tile_info"))
            {
                startUnlocks.Add("clear_all_tile_info");
                Plugin.Log.LogInfo("Added clear_all_tile_info to startUnlocks");
            }
        }
    }
}
