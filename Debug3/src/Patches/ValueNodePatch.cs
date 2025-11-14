using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Debug3.Patches
{
    [HarmonyPatch(typeof(ValueNode))]
    public static class ValueNodePatch
    {
        /// <summary>
        /// Prefix patch for ValueNode.Execute to handle attribute access on PyColor objects
        /// Intercepts evaluation when trying to access members like "wrap" on PyColor instances
        /// </summary>
        [HarmonyPatch("Execute")]
        [HarmonyPrefix]
        public static bool Execute_Prefix(ValueNode __instance, ProgramState state, Execution execution, int depth, ref IEnumerable<double> __result)
        {
            // Get the value string from the ValueNode
            var valueField = typeof(ValueNode).GetField("value", BindingFlags.Public | BindingFlags.Instance);
            if (valueField == null) return true;

            string valueName = valueField.GetValue(__instance) as string;
            if (string.IsNullOrEmpty(valueName)) return true;

            // Handle colors.member access (e.g. "colors.blue", "colors.red.wrap")
            if (valueName.StartsWith("colors."))
            {
                var parts = valueName.Split('.');

                // Get colors object from constants
                var colorsObj = Scope.EvaluateConstant("colors");
                if (colorsObj is PyColors pyColors)
                {
                    IPyObject currentObj = pyColors;

                    // Navigate through the chain: colors.red.wrap
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string memberName = parts[i];

                        if (currentObj is PyColors colors && colors.TryGetMember(memberName, out IPyObject colorMember))
                        {
                            currentObj = colorMember;
                        }
                        else if (currentObj is PyColor pyColor && pyColor.TryGetMember(memberName, out IPyObject pyColorMember))
                        {
                            currentObj = pyColorMember;
                        }
                        else
                        {
                            // Member not found, let original method handle error
                            return true;
                        }
                    }

                    // Set the result
                    state.ReturnValue = currentObj;
                    state.IsExpressionStatic = true;

                    // Return empty enumerable to skip original execution
                    __result = EmptyEnumerable();
                    return false;
                }
            }

            // Check if we're trying to access a member on an object in the current scope
            // This handles cases like accessing "wrap" when evaluating the expression after "colors.red"
            if (valueName == "wrap" || valueName == "custom")
            {
                // Try to get the scope and evaluate to see if there's a PyColor in context
                // We need to check previous execution results
                // Actually, let's check if state.ReturnValue is already a PyColor from previous evaluation
                if (state.ReturnValue is PyColor pyColor)
                {
                    // This is attribute access on a PyColor
                    if (pyColor.TryGetMember(valueName, out IPyObject memberValue))
                    {
                        // Set the result directly
                        state.ReturnValue = memberValue;
                        state.IsExpressionStatic = true;

                        // Return empty enumerable to skip original execution
                        __result = EmptyEnumerable();
                        return false;
                    }
                }
            }

            // Let original method execute
            return true;
        }

        private static IEnumerable<double> EmptyEnumerable()
        {
            yield break;
        }
    }
}
