using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace Debug3.Patches
{
    [HarmonyPatch(typeof(BuiltinFunctions))]
    public static class BuiltinFunctionsPatch
    {
        private static bool hasAddedFunction = false;

        [HarmonyPatch("Functions", MethodType.Getter)]
        [HarmonyPrefix]
        public static void Functions_Prefix()
        {
            if (hasAddedFunction) return;

            var functionListField = typeof(BuiltinFunctions).GetField("functionList",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (functionListField?.GetValue(null) is List<PyFunction> functionList)
            {
                // Add arrow() function
                if (!functionList.Any(f => f.functionName == "arrow"))
                {
                    functionList.Add(new PyFunction(
                        "arrow",
                        new Func<List<IPyObject>, Simulation, Execution, int, double>(Arrow),
                        null,
                        false
                    ));
                }

                // Add reset_arrows() function
                if (!functionList.Any(f => f.functionName == "reset_arrows"))
                {
                    functionList.Add(new PyFunction(
                        "reset_arrows",
                        new Func<List<IPyObject>, Simulation, Execution, int, double>(ResetArrows),
                        null,
                        false
                    ));
                }

                hasAddedFunction = true;
            }
        }

        // Patch quick_print to support colors
        [HarmonyPatch("QuickPrint", MethodType.Normal)]
        [HarmonyPrefix]
        public static bool QuickPrint_Prefix(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
        {
            if (parameters.Count == 0)
            {
                throw new ExecuteException("error_empty_print", -1, -1);
            }

            // Check if any parameters are colors or colored text
            bool hasColorArguments = parameters.Any(p => p is PyColor || p is PyColoredText);

            if (!hasColorArguments)
            {
                // Let original function handle it
                return true;
            }

            // Process colored output
            StringBuilder output = new StringBuilder();
            PyColor currentColor = null;

            foreach (IPyObject param in parameters)
            {
                if (param is PyColor color)
                {
                    // Set color for subsequent parameters
                    currentColor = color;
                }
                else if (param is PyColoredText coloredText)
                {
                    // Append colored text
                    if (output.Length > 0) output.Append(' ');
                    output.Append(coloredText.ToColoredString());
                }
                else
                {
                    // Regular parameter
                    string text = CodeUtilities.ToNiceString(param, 0, null, false);
                    if (output.Length > 0) output.Append(' ');

                    if (currentColor != null)
                    {
                        // Wrap in color tags
                        string hexColor = UnityEngine.ColorUtility.ToHtmlStringRGB(currentColor.color);
                        output.Append($"<color=#{hexColor}>{text}</color>");
                    }
                    else
                    {
                        output.Append(text);
                    }
                }
            }

            Logger.Log(output.ToString());
            exec.States[droneId].ReturnValue = new PyNone();

            // Skip original function
            return false;
        }

        // Patch the existing clear() function to also clear debug arrows
        [HarmonyPatch("Clear")]
        [HarmonyPostfix]
        public static void Clear_Postfix()
        {
            MainSimPatch.ClearDebugArrows();
        }

        private static double Arrow(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
        {
            // arrow(x, y, direction) or arrow(x, y, direction, color)
            // direction can be None (points down), a direction, or an iterable of directions
            if (parameters.Count < 3)
            {
                exec.States[droneId].ReturnValue = new PyNone();
                return 0.0;
            }

            if (!(parameters[0] is PyNumber xNum) || !(parameters[1] is PyNumber yNum))
            {
                exec.States[droneId].ReturnValue = new PyNone();
                return 0.0;
            }

            int x = (int)xNum.num;
            int y = (int)yNum.num;

            // Default color
            UnityEngine.Color color = UnityEngine.Color.white;

            // Check if fourth parameter is color
            if (parameters.Count >= 4 && parameters[3] is PyColor pyColor)
            {
                color = pyColor.color;
            }

            // Third parameter is direction (required)
            if (parameters[2] is PyNone)
            {
                // None points arrow down (no direction indicator)
                var arrow = new DebugArrow(x, y, false, 0, color);
                MainSimPatch.AddDebugArrow(arrow);
            }
            else if (parameters[2] is PyGridDirection pyDir)
            {
                // Single direction
                GridDirection direction = pyDir;
                var arrow = new DebugArrow(x, y, true, (int)direction, color);
                MainSimPatch.AddDebugArrow(arrow);
            }
            else if (parameters[2] is PyList pyList)
            {
                // List of directions - make them overlap
                for (int i = 0; i < pyList.Count; i++)
                {
                    if (pyList[i] is PyGridDirection itemDir)
                    {
                        GridDirection direction = itemDir;
                        var arrow = new DebugArrow(x, y, true, (int)direction, color);
                        MainSimPatch.AddDebugArrowOverlapping(arrow);
                    }
                }
            }
            else if (parameters[2] is PyTuple pyTuple)
            {
                // Tuple of directions - make them overlap
                for (int i = 0; i < pyTuple.Count; i++)
                {
                    if (pyTuple[i] is PyGridDirection itemDir)
                    {
                        GridDirection direction = itemDir;
                        var arrow = new DebugArrow(x, y, true, (int)direction, color);
                        MainSimPatch.AddDebugArrowOverlapping(arrow);
                    }
                }
            }
            else
            {
                // Invalid direction parameter
                exec.States[droneId].ReturnValue = new PyNone();
                return 0.0;
            }

            exec.States[droneId].ReturnValue = new PyNone();
            return 0.0;
        }

        private static double ResetArrows(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
        {
            // reset_arrows() to clear all arrows, or reset_arrows(x, y) to clear arrow at specific position
            if (parameters.Count == 0)
            {
                // Clear all arrows
                MainSimPatch.ClearDebugArrows();
            }
            else if (parameters.Count >= 2)
            {
                if (parameters[0] is PyNumber xNum && parameters[1] is PyNumber yNum)
                {
                    int x = (int)xNum.num;
                    int y = (int)yNum.num;
                    MainSimPatch.RemoveDebugArrow(x, y);
                }
            }

            exec.States[droneId].ReturnValue = new PyNone();
            return 0.0;
        }
    }
}
