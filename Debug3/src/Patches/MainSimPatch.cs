using HarmonyLib;
using System.Collections.Generic;

namespace Debug3.Patches
{
    [HarmonyPatch(typeof(MainSim))]
    public static class MainSimPatch
    {
        private static List<DebugArrow> debugArrows = new List<DebugArrow>();

        [HarmonyPatch(nameof(MainSim.GetUnlockedKeywords))]
        [HarmonyPostfix]
        public static void GetUnlockedKeywords_Postfix(ref HashSet<string> __result)
        {
            // Add colors constant and color names to autocomplete
            __result.Add("colors");
            __result.Add("red");
            __result.Add("green");
            __result.Add("blue");
            __result.Add("yellow");
            __result.Add("cyan");
            __result.Add("magenta");
            __result.Add("white");
            __result.Add("black");
            __result.Add("orange");
            __result.Add("purple");
            __result.Add("pink");
            __result.Add("lime");
            __result.Add("teal");
            __result.Add("navy");
            __result.Add("maroon");
            __result.Add("olive");
            __result.Add("silver");
            __result.Add("gray");
            __result.Add("custom");
            __result.Add("wrap");
            __result.Add("arrow");
            __result.Add("reset_arrows");
        }

        public static void AddDebugArrow(DebugArrow arrow)
        {
            if (arrow.hasDirection)
            {
                // Remove only arrows at same position with same direction
                debugArrows.RemoveAll(a => a.x == arrow.x && a.y == arrow.y && a.hasDirection && a.direction == arrow.direction);
            }
            else
            {
                // For arrows without direction (None), remove all arrows at that position
                debugArrows.RemoveAll(a => a.x == arrow.x && a.y == arrow.y);
            }
            // Add the new arrow
            debugArrows.Add(arrow);
        }

        public static void AddDebugArrowOverlapping(DebugArrow arrow)
        {
            // For iterable directions, remove only the same direction at same position
            debugArrows.RemoveAll(a => a.x == arrow.x && a.y == arrow.y && a.hasDirection && a.direction == arrow.direction);
            // Add the new arrow
            debugArrows.Add(arrow);
        }

        public static void RemoveDebugArrow(int x, int y)
        {
            debugArrows.RemoveAll(a => a.x == x && a.y == y);
        }

        public static List<DebugArrow> GetDebugArrows()
        {
            return debugArrows;
        }

        public static void ClearDebugArrows()
        {
            debugArrows.Clear();
        }

        // Clear arrows when code execution starts
        [HarmonyPatch("StartMainExecution")]
        [HarmonyPrefix]
        public static void StartMainExecution_Prefix()
        {
            ClearDebugArrows();
        }

        // Clear arrows when execution stops
        [HarmonyPatch("StopMainExecution")]
        [HarmonyPrefix]
        public static void StopMainExecution_Prefix()
        {
            ClearDebugArrows();
        }

        // Clear arrows when MainSim awakens (level load)
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void Awake_Postfix()
        {
            ClearDebugArrows();
        }

        // Add GetDebugArrows method to MainSim instance
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void Update_Postfix(MainSim __instance)
        {
            // This allows FarmRenderer to call MainSim.Inst.GetDebugArrows()
            // We're just ensuring the method exists on the instance
        }
    }

    // Extension method to add GetDebugArrows to MainSim
    public static class MainSimExtensions
    {
        public static List<DebugArrow> GetDebugArrows(this MainSim mainSim)
        {
            return MainSimPatch.GetDebugArrows();
        }
    }
}
