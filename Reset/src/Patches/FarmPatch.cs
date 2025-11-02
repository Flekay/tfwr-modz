using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace Reset.Patches
{
    [HarmonyPatch(typeof(Farm))]
    public static class FarmPatch
    {
        [HarmonyPatch(MethodType.Constructor, typeof(Simulation), typeof(IEnumerable<string>), typeof(ItemBlock), typeof(List<SFO>), typeof(List<SFO>), typeof(bool))]
        [HarmonyPrefix]
        public static void Constructor_Prefix()
        {
            // Get the startUnlocks field via reflection
            var startUnlocksField = typeof(Farm).GetField("startUnlocks", BindingFlags.Public | BindingFlags.Static);
            var startUnlocks = (List<string>)startUnlocksField.GetValue(null);

            // Add reset function to startUnlocks if leaderboard unlock is present
            if (!startUnlocks.Contains("reset"))
            {
                // reset() is unlocked with the leaderboard unlock
                // We'll check for this in the unlock logic
                startUnlocks.Add("reset");
            }
        }

        [HarmonyPatch("IsUnlocked", typeof(string))]
        [HarmonyPrefix]
        public static bool IsUnlocked_Prefix(Farm __instance, string s, ref bool __result)
        {
            if (s == "reset")
            {
                __result = __instance.NumUnlocked("leaderboard") > 0;
                return false;
            }
            return true;
        }
    }
}
