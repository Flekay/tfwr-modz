using HarmonyLib;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BetterTooltips.Patches;

[HarmonyPatch(typeof(MainSim))]
public class MainSimPatch
{
    // Patch the GetHoveredTooltip method to add additional information
    [HarmonyPostfix]
    [HarmonyPatch("GetHoveredTooltip", MethodType.Normal)]
    static void GetHoveredTooltip_Postfix(MainSim __instance, ref TooltipInfo __result)
    {
        // Check if tooltips are enabled
        if (OptionHolder.GetString("Show Tooltips", "Enabled") == "Disabled")
        {
            __result = null;
            return;
        }

        if (__result == null) return;

        // Access the private hoveredCell field
        var hoveredCellField = AccessTools.Field(typeof(MainSim), "hoveredCell");
        Vector2Int hoveredCell = (Vector2Int)hoveredCellField.GetValue(__instance);

        // Access the private sim field
        var simField = AccessTools.Field(typeof(MainSim), "sim");
        Simulation sim = (Simulation)simField.GetValue(__instance);

        if (sim?.farm?.grid == null || !sim.farm.grid.IsWithinBounds(hoveredCell))
        {
            return;
        }

        StringBuilder additionalInfo = new StringBuilder();
        FarmObject farmObject;

        // Check if there's an entity on this tile
        if (sim.farm.grid.entities.TryGetValue(hoveredCell, out farmObject))
        {
            // Add measure() information - call the actual Measure() method
            IPyObject measureResult = farmObject.Measure();
            if (measureResult != null && !(measureResult is PyNone))
            {
                additionalInfo.Append("\n\n`measure():`");

                // Convert the measure result to a readable string
                string measureStr = CodeUtilities.ToNiceString(measureResult, 0, null, false);
                additionalInfo.Append($"\n`{measureStr}`");
            }

            // Add companion information for plants that can have companions
            if (farmObject is Growable growableWithCompanion && growableWithCompanion.objectSO.canHaveCompanion)
            {
                IPyObject companionInfo = growableWithCompanion.GetCompanion();

                if (companionInfo is PyTuple tuple && tuple.Count == 2)
                {
                    additionalInfo.Append("\n\n`Companion Info:`");

                    // Get companion type (first element of tuple)
                    if (tuple[0] is FarmObjectSO companionType)
                    {
                        additionalInfo.Append($"\n`Type`: {companionType.objectName}");
                    }

                    // Get companion position (second element is a tuple of (x, y))
                    if (tuple[1] is PyTuple posTuple && posTuple.Count == 2)
                    {
                        if (posTuple[0] is PyNumber xNum && posTuple[1] is PyNumber yNum)
                        {
                            int compX = (int)xNum.num;
                            int compY = (int)yNum.num;
                            additionalInfo.Append($"\n`Position`: ({compX}, {compY})");

                            // Check if companion is actually there
                            Vector2Int companionPos = new Vector2Int(compX, compY);
                            if (sim.farm.grid.entities.TryGetValue(companionPos, out FarmObject actualCompanion))
                            {
                                bool matches = actualCompanion.objectSO.objectName == tuple[0].ToString();
                                additionalInfo.Append(matches ? "\n`Companion present`" : "\n`Wrong plant at position`");
                            }
                            else
                            {
                                additionalInfo.Append("\n`Companion missing`");
                            }
                        }
                    }
                }
            }
        }

        // Add custom tile info if any
        string customInfo = TileDataManager.Instance.GetTileInfo(hoveredCell.x, hoveredCell.y);
        if (!string.IsNullOrEmpty(customInfo))
        {
            additionalInfo.Append("\n\n`Custom Info:`");
            additionalInfo.Append($"\n{customInfo}");
        }

        // Append the additional information to the tooltip text
        if (additionalInfo.Length > 0)
        {
            __result = new TooltipInfo(
                __result.text + additionalInfo.ToString(),
                __result.delay,
                Vector3.zero,
                __result.anchor,
                __result.docs
            )
            {
                itemBlock = __result.itemBlock
            };
        }
    }
}
