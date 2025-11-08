using System.Collections.Generic;
using HarmonyLib;
using System;
using System.Reflection;

namespace Directions.Patches;

[HarmonyPatch(typeof(Scope))]
public static class ScopePatch
{
    private static PyList directionsListCache = null;
    private static Type valueTupleType;

    /// <summary>
    /// Prefix patch for Scope.Evaluate to check if a name is a constant before throwing "never been defined" error
    /// </summary>
    [HarmonyPatch("Evaluate")]
    [HarmonyPrefix]
    public static bool Evaluate_Prefix(string s, string currentFileName, ref object __result, Scope __instance)
    {
        // Check if it's one of our constants FIRST, before checking variables
        if (s == "Directions" || s == "Left" || s == "Right" || 
            s == "Forward" || s == "Backward" || s == "Up" || s == "Down")
        {
            Plugin.Log.LogInfo($"Scope.Evaluate: Intercepting constant lookup for '{s}'");
            
            // Evaluate it as a constant and return as a static value
            IPyObject constantValue = Scope.EvaluateConstant(s);
            if (constantValue != null)
            {
                Plugin.Log.LogInfo($"Scope.Evaluate: Successfully evaluated '{s}' as constant");
                
                // Create ValueTuple<IPyObject, bool> via reflection
                if (valueTupleType == null)
                {
                    valueTupleType = typeof(Scope).GetMethod("Evaluate").ReturnType;
                }
                
                __result = Activator.CreateInstance(valueTupleType, constantValue, true);
                return false; // Skip original method
            }
            else
            {
                Plugin.Log.LogWarning($"Scope.Evaluate: Failed to evaluate '{s}' as constant (returned null)");
            }
        }
        
        // Let original method continue for all other names
        return true;
    }

    /// <summary>
    /// Postfix patch for Scope.EvaluateConstant to add the "Directions" constant and individual direction constants.
    /// This allows users to write: for direction in Directions:
    /// Also adds: Left, Right, Forward, Back, Up, Down as individual constants
    /// </summary>
    [HarmonyPatch("EvaluateConstant")]
    [HarmonyPostfix]
    public static void EvaluateConstant_Postfix(string s, ref IPyObject __result)
    {
        // If the result is already set (not null), return early
        if (__result != null) return;

        // Check if the constant being evaluated is "Directions"
        if (s == "Directions")
        {
            Plugin.Log.LogInfo("EvaluateConstant called for 'Directions'");
            // Create cached list if it doesn't exist
            if (directionsListCache == null)
            {
                Plugin.Log.LogInfo("Creating Directions list cache");
                var directionsList = new List<IPyObject>
                {
                    // Absolute grid directions
                    new PyGridDirection(GridDirection.North),
                    new PyGridDirection(GridDirection.East),
                    new PyGridDirection(GridDirection.South),
                    new PyGridDirection(GridDirection.West),
                    // Relative directions
                    new PyExtendedDirection(DirectionType.Left),
                    new PyExtendedDirection(DirectionType.Right),
                    new PyExtendedDirection(DirectionType.Forward),
                    new PyExtendedDirection(DirectionType.Backward),
                    new PyExtendedDirection(DirectionType.Up),
                    new PyExtendedDirection(DirectionType.Down)
                };
                directionsListCache = new PyList(directionsList);
                Plugin.Log.LogInfo($"Directions list cache created with {directionsList.Count} directions");
            }

            __result = directionsListCache;
            return;
        }

        // Individual direction constants
        switch (s)
        {
            case "Left":
                __result = new PyExtendedDirection(DirectionType.Left);
                break;
            case "Right":
                __result = new PyExtendedDirection(DirectionType.Right);
                break;
            case "Forward":
                __result = new PyExtendedDirection(DirectionType.Forward);
                break;
            case "Backward":
                __result = new PyExtendedDirection(DirectionType.Backward);
                break;
            case "Up":
                __result = new PyExtendedDirection(DirectionType.Up);
                break;
            case "Down":
                __result = new PyExtendedDirection(DirectionType.Down);
                break;
        }
    }

    /// <summary>
    /// Postfix patch for Scope.IsConstant to recognize direction constants.
    /// </summary>
    [HarmonyPatch("IsConstant")]
    [HarmonyPostfix]
    public static void IsConstant_Postfix(string s, ref bool __result)
    {
        // If already identified as a constant, don't override
        if (__result) return;

        // Check if this is one of our direction constants
        if (s == "Directions" || s == "Left" || s == "Right" || 
            s == "Forward" || s == "Backward" || s == "Up" || s == "Down")
        {
            __result = true;
        }
    }
}

