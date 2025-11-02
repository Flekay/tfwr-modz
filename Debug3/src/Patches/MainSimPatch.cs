using HarmonyLib;
using System.Collections.Generic;

namespace Debug3.Patches
{
    [HarmonyPatch(typeof(MainSim))]
    public static class MainSimPatch
    {
        private static List<DebugArrow> debugArrows = new List<DebugArrow>();

        public static void AddDebugArrow(DebugArrow arrow)
        {
            // Remove any existing arrow at this position first
            debugArrows.RemoveAll(a => a.x == arrow.x && a.y == arrow.y);
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
