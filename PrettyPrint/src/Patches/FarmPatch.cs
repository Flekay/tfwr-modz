using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace PrettyPrint.Patches
{
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
                if (!startUnlocks.Contains("pretty_print"))
                {
                    startUnlocks.Add("pretty_print");
                }
            }
        }
    }
}
