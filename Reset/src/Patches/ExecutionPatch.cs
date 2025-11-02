using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Reset.Patches
{
    [HarmonyPatch(typeof(Execution))]
    public static class ExecutionPatch
    {
        [HarmonyPatch("ApplySideEffect", typeof(int))]
        [HarmonyPrefix]
        public static bool ApplySideEffect_Prefix(Execution __instance, int droneId, ref double __result)
        {
            try
            {
                ProgramState state = __instance.States[droneId];

                // Block RunLeaderboard side effect
                if (state.currentSideEffect == SideEffect.RunLeaderboard)
                {
                    Plugin.Log.LogWarning("Blocked run_leaderboard() - leaderboard submissions are disabled in this mod");
                    Logger.Log("Warning: Leaderboard submissions are disabled while using the Reset mod");
                    state.ReturnValue = new PyNone();
                    __result = 0.0;
                    return false;
                }

                if ((int)state.currentSideEffect == 999)
                {
                    Plugin.Log.LogInfo("Detected custom reset SideEffect (999), performing reset...");
                    PerformReset(__instance.sim);
                    state.ReturnValue = new PyNone();
                    __result = 0.0;
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error in ApplySideEffect_Prefix: {ex.Message}\n{ex.StackTrace}");
                throw;
            }

            return true;
        }

        public static void PerformReset(Simulation sim)
        {
            try
            {
                Plugin.Log.LogInfo("PerformReset started");

                var simField = typeof(MainSim).GetField("sim", BindingFlags.NonPublic | BindingFlags.Instance);
                var mainSimulation = (Simulation)simField.GetValue(MainSim.Inst);
                Farm farm = mainSimulation.farm;

                Plugin.Log.LogInfo("Got farm reference");

                var resetUnlocksField = typeof(BuiltinFunctions).GetField("resetUnlocks", BindingFlags.NonPublic | BindingFlags.Static);
                var resetUnlocksList = (List<string>)resetUnlocksField.GetValue(null);
                HashSet<string> programmingUnlocks = new HashSet<string>(resetUnlocksList);

                // Add resource unlocks (keep at max level)
                string[] resourceUnlocks = { "speed", "expand", "grass", "carrots", "trees", "watering", "fertilizer", "mazes", "cactus", "polyculture", "pumpkins", "dinosaurs", "megafarm" };

                // Add hat unlocks
                string[] hatUnlocks = { "hats", "top_hat", "the_farmers_remains" };

                Plugin.Log.LogInfo("Starting unlock reset");

                var unlocksField = typeof(Farm).GetField("unlocks", BindingFlags.NonPublic | BindingFlags.Instance);
                var currentUnlocks = (Dictionary<string, int>)unlocksField.GetValue(farm);
                Dictionary<string, int> allCurrentUnlocks = new Dictionary<string, int>(currentUnlocks);

                Dictionary<string, int> preservedUnlocks = new Dictionary<string, int>();

                // Preserve programming unlocks
                foreach (string unlock in programmingUnlocks)
                {
                    if (allCurrentUnlocks.ContainsKey(unlock))
                    {
                        preservedUnlocks[unlock] = allCurrentUnlocks[unlock];
                    }
                }

                // Preserve resource unlocks at their current level
                foreach (string unlock in resourceUnlocks)
                {
                    if (allCurrentUnlocks.ContainsKey(unlock))
                    {
                        UnlockSO unlockSO = farm.GetUnlockOf(unlock);
                        if (unlockSO != null)
                        {
                            unlockSO.maxUnlockLevel = 999999;
                        }
                        // preservedUnlocks[unlock] = allCurrentUnlocks[unlock];
                    }
                }

                // Preserve hat unlocks
                foreach (string unlock in hatUnlocks)
                {
                    if (allCurrentUnlocks.ContainsKey(unlock))
                    {
                        preservedUnlocks[unlock] = allCurrentUnlocks[unlock];
                    }
                }

                currentUnlocks.Clear();

                foreach (var kvp in preservedUnlocks)
                {
                    currentUnlocks[kvp.Key] = kvp.Value;
                }

                foreach (string unlock in Farm.startUnlocks)
                {
                    if (!currentUnlocks.ContainsKey(unlock))
                    {
                        currentUnlocks[unlock] = 1;
                    }
                }

                Plugin.Log.LogInfo("Unlocks reset complete, starting inventory reset");

                int goldenPiggyId = StringIds.GetItemId("piggy");
                double currentPiggies = 0;

                // Reset inventory, preserve only piggies - items is a public field!
                if (farm.Items.items != null)
                {
                    Plugin.Log.LogInfo($"Items array length: {farm.Items.items.Length}");

                    // Save current piggy count
                    if (goldenPiggyId >= 0 && goldenPiggyId < farm.Items.items.Length)
                    {
                        currentPiggies = farm.Items.items[goldenPiggyId];
                        Plugin.Log.LogInfo($"Current piggies: {currentPiggies}");
                    }

                    Plugin.Log.LogInfo("Clearing items array");
                    // Clear all items manually
                    for (int i = 0; i < farm.Items.items.Length; i++)
                    {
                        farm.Items.items[i] = 0.0;
                    }

                    Plugin.Log.LogInfo("Adding back piggies");
                    // Add back piggies + 1
                    if (goldenPiggyId >= 0 && goldenPiggyId < farm.Items.items.Length)
                    {
                        farm.Items.items[goldenPiggyId] = currentPiggies + 1.0;
                    }
                }

                Plugin.Log.LogInfo("Calling GenerateWorld");

                // Regenerate the world grid to match the new (smaller) world size
                farm.grid.GenerateWorld(true);

                Plugin.Log.LogInfo("GenerateWorld complete");

                Plugin.Log.LogInfo($"Reset complete. Preserved {preservedUnlocks.Count} unlocks, reset {allCurrentUnlocks.Count - preservedUnlocks.Count}");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error in PerformReset: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
    }
}
