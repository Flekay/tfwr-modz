using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

namespace Debug3.Patches
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
                if (!startUnlocks.Contains("arrow"))
                {
                    startUnlocks.Add("arrow");
                }
                if (!startUnlocks.Contains("colors"))
                {
                    startUnlocks.Add("colors");
                }
                if (!startUnlocks.Contains("custom"))
                {
                    startUnlocks.Add("custom");
                }
                if (!startUnlocks.Contains("reset_arrows"))
                {
                    startUnlocks.Add("reset_arrows");
                }
            }
        }
    }
}
