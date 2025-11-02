using HarmonyLib;

namespace Reset.Patches
{
    [HarmonyPatch(typeof(Helper))]
    public static class HelperPatch
    {
        [HarmonyPatch("WorldSizeScale")]
        [HarmonyPrefix]
        public static bool WorldSizeScale_Prefix(int numExpandUpgrades, ref int __result)
        {
            // Remove the 32x32 cap and continue scaling exponentially
            switch (numExpandUpgrades)
            {
                case 0: __result = 1; return false;
                case 1: __result = 2; return false;
                case 2: __result = 3; return false;
                case 3: __result = 4; return false;
                case 4: __result = 6; return false;
                case 5: __result = 8; return false;
                case 6: __result = 12; return false;
                case 7: __result = 16; return false;
                case 8: __result = 22; return false;
                case 9: __result = 32; return false;
                default:
                    // Continue scaling: 32, 44, 64, 88, 128, 176, 256, etc.
                    // Pattern: multiply by ~1.4 (alternating between *1.375 and *1.45)
                    int size = 32;
                    for (int i = 10; i <= numExpandUpgrades; i++)
                    {
                        size = (i % 2 == 0) ? (size * 145) / 100 : (size * 1375) / 1000;
                    }
                    __result = size;
                    return false;
            }
        }
    }
}
