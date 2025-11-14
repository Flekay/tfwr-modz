using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Debug3.Patches
{
    [HarmonyPatch(typeof(Scope))]
    public static class ScopePatch
    {
        private static PyModule colorsModule = null;
        private static Type valueTupleType;

        /// <summary>
        /// Prefix patch for Scope.Evaluate to check if a name is 'colors' before throwing error
        /// </summary>
        [HarmonyPatch("Evaluate")]
        [HarmonyPrefix]
        public static bool Evaluate_Prefix(string s, string currentFileName, ref object __result, Scope __instance)
        {
            // Check if it's the 'colors' constant
            if (s == "colors")
            {
                IPyObject constantValue = Scope.EvaluateConstant(s);
                if (constantValue != null)
                {
                    // Create ValueTuple<IPyObject, bool> via reflection
                    if (valueTupleType == null)
                    {
                        valueTupleType = typeof(Scope).GetMethod("Evaluate").ReturnType;
                    }

                    __result = Activator.CreateInstance(valueTupleType, constantValue, true);
                    return false; // Skip original method
                }
            }

            return true;
        }

        /// <summary>
        /// Postfix patch for Scope.EvaluateConstant to add the "colors" constant as PyModule
        /// </summary>
        [HarmonyPatch("EvaluateConstant")]
        [HarmonyPostfix]
        public static void EvaluateConstant_Postfix(string s, ref IPyObject __result)
        {
            // If the result is already set (not null), return early
            if (__result != null) return;

            // Check if the constant being evaluated is "colors"
            if (s == "colors")
            {
                if (colorsModule == null)
                {
                    try
                    {
                        var colorsObj = new PyColors();

                        // Build the dictionary dynamically using reflection
                        var tupleType = typeof(Scope).GetMethod("Evaluate").ReturnType;
                        var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), tupleType);
                        var dict = Activator.CreateInstance(dictType);
                        var addMethod = dictType.GetMethod("Add");

                        // Add all the color properties
                        var colorNames = new[] { "red", "green", "blue", "yellow", "cyan", "magenta",
                            "white", "black", "orange", "purple", "pink", "lime", "teal", "navy",
                            "maroon", "olive", "silver", "gray" };

                        foreach (var name in colorNames)
                        {
                            if (colorsObj.TryGetMember(name, out IPyObject member))
                            {
                                var tuple = Activator.CreateInstance(tupleType, member, true);
                                addMethod.Invoke(dict, new object[] { name, tuple });
                            }
                        }

                        // Add custom function
                        if (colorsObj.TryGetMember("custom", out IPyObject customFunc))
                        {
                            var tuple = Activator.CreateInstance(tupleType, customFunc, true);
                            addMethod.Invoke(dict, new object[] { "custom", tuple });
                        }

                        // Create PyModule with the members dictionary
                        colorsModule = (PyModule)Activator.CreateInstance(typeof(PyModule), dict, "colors");
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogError($"Failed to create colors module: {ex}");
                    }
                }
                __result = colorsModule;
            }
        }

        /// <summary>
        /// Postfix patch for Scope.IsConstant to recognize "colors" as a constant
        /// </summary>
        [HarmonyPatch("IsConstant")]
        [HarmonyPostfix]
        public static void IsConstant_Postfix(string s, ref bool __result)
        {
            // If already identified as a constant, don't override
            if (__result) return;

            // Check if this is the colors constant
            if (s == "colors")
            {
                __result = true;
            }
        }
    }
}
