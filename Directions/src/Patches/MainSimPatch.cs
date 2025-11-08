using HarmonyLib;
using System.Collections.Generic;

namespace Directions.Patches
{
    [HarmonyPatch(typeof(MainSim))]
    public class MainSimPatch
    {
        [HarmonyPatch(nameof(MainSim.GetUnlockedKeywords))]
        [HarmonyPostfix]
        public static void GetUnlockedKeywords_Postfix(ref HashSet<string> __result)
        {
            // Add all direction keywords to the unlocked keywords set
            __result.Add("Directions");
            __result.Add("Left");
            __result.Add("Right");
            __result.Add("Forward");
            __result.Add("Backward");
            __result.Add("Up");
            __result.Add("Down");
        }
    }
}
