using HarmonyLib;
using System.Reflection;

namespace Reset.Patches
{
    [HarmonyPatch(typeof(MainSim))]
    public static class MainSimPatch
    {
        private static FieldInfo simField = typeof(MainSim).GetField("sim", BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPatch("SetupSim")]
        [HarmonyPostfix]
        public static void SetupSim_Postfix(MainSim __instance)
        {
            var sim = (Simulation)simField.GetValue(__instance);
            if (sim?.farm != null)
            {
                int goldenPiggyId = StringIds.GetItemId("piggy");
                if (goldenPiggyId >= 0)
                {
                    double goldenPiggies = sim.farm.Items.GetNumber(goldenPiggyId);
                    __instance.harvestFactor = 1 + (int)goldenPiggies;
                }
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void Update_Postfix(MainSim __instance)
        {
            var sim = (Simulation)simField.GetValue(__instance);
            if (sim?.farm != null)
            {
                int goldenPiggyId = StringIds.GetItemId("piggy");
                if (goldenPiggyId >= 0)
                {
                    double goldenPiggies = sim.farm.Items.GetNumber(goldenPiggyId);
                    int expectedFactor = 1 + (int)goldenPiggies;
                    if (__instance.harvestFactor != expectedFactor)
                    {
                        __instance.harvestFactor = expectedFactor;
                    }
                }
            }
        }
    }
}
