using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace BetterTooltips.Patches;

[HarmonyPatch(typeof(BuiltinFunctions))]
public static class BuiltinFunctionsPatch
{
    private static bool hasAddedFunctions = false;

    [HarmonyPatch("Functions", MethodType.Getter)]
    [HarmonyPrefix]
    public static void Functions_Prefix()
    {
        if (hasAddedFunctions) return;

        var functionListField = typeof(BuiltinFunctions).GetField("functionList",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (functionListField?.GetValue(null) is List<PyFunction> functionList)
        {
            // Add set_tile_info() function
            if (!functionList.Any(f => f.functionName == "set_tile_info"))
            {
                functionList.Add(new PyFunction(
                    "set_tile_info",
                    new Func<List<IPyObject>, Simulation, Execution, int, double>(SetTileInfo),
                    null,
                    false
                ));
                Plugin.Log.LogInfo("Added set_tile_info() builtin function");
            }

            // Add clear_all_tile_info() function
            if (!functionList.Any(f => f.functionName == "clear_all_tile_info"))
            {
                functionList.Add(new PyFunction(
                    "clear_all_tile_info",
                    new Func<List<IPyObject>, Simulation, Execution, int, double>(ClearAllTileInfo),
                    null,
                    false
                ));
                Plugin.Log.LogInfo("Added clear_all_tile_info() builtin function");
            }

            hasAddedFunctions = true;
        }
    }

    /// <summary>
    /// set_tile_info(x, y, info) - Set custom info text for a specific tile
    /// </summary>
    private static double SetTileInfo(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
    {
        if (parameters.Count < 2 || parameters.Count > 3)
        {
            exec.States[droneId].ReturnValue = new PyNone();
            Plugin.Log.LogWarning("set_tile_info requires 2 or 3 parameters: x, y, [info]");
            return 0.0;
        }

        if (!(parameters[0] is PyNumber xNum) || !(parameters[1] is PyNumber yNum))
        {
            exec.States[droneId].ReturnValue = new PyNone();
            Plugin.Log.LogWarning("set_tile_info: x and y must be numbers");
            return 0.0;
        }

        int x = (int)xNum.num;
        int y = (int)yNum.num;

        string info = "";
        if (parameters.Count >= 3)
        {
            if (parameters[2] is PyString pyStr)
            {
                info = pyStr.str;
            }
            else if (parameters[2] is PyNone)
            {
                info = null; // Clear the info
            }
            else
            {
                // Convert to string representation
                info = CodeUtilities.ToNiceString(parameters[2], 0, null, false);
            }
        }

        TileDataManager.Instance.SetTileInfo(x, y, info);
        exec.States[droneId].ReturnValue = new PyNone();
        return 0.0;
    }

    /// <summary>
    /// clear_all_tile_info() - Clear all custom tile information
    /// </summary>
    private static double ClearAllTileInfo(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
    {
        if (parameters.Count > 0)
        {
            Plugin.Log.LogWarning("clear_all_tile_info takes no parameters");
        }

        TileDataManager.Instance.ClearAllTileInfo();
        exec.States[droneId].ReturnValue = new PyNone();
        return 0.0;
    }
}
