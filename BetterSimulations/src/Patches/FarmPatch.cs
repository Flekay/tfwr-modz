using HarmonyLib;
using System.Linq;

namespace BetterSimulations.Patches
{
    [HarmonyPatch(typeof(Farm))]
    public static class FarmPatch
    {
        [HarmonyPatch("IsUnlocked")]
        [HarmonyPrefix]
        public static bool IsUnlocked_Prefix(Farm __instance, string s, ref bool __result)
        {
            // Check if this is a free function (like quick_print, set_simulation_speed)
            var functions = BuiltinFunctions.Functions;
            if (functions != null && functions.TryGetValue(s.ToLower(), out PyFunction func))
            {
                if (func.isFree)
                {
                    __result = true;
                    return false; // Skip original method
                }
            }

            return true; // Run original method
        }
    }
}
