using HarmonyLib;
using UnityEngine;

namespace BetterWorkspace.Patches
{
    [HarmonyPatch(typeof(Tooltip))]
    public static class TooltipSetTooltipPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("SetTooltip")]
        [HarmonyPriority(Priority.First)]
        static bool SetTooltip_Prefix(Tooltip __instance, GameObject tooltipObject)
        {
            if (Input.GetMouseButton(0))
            {
                return false;
            }
            return true;
        }
    }
}
