using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

                hasAddedFunction = true;
            }
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
            // arrow(x, y, direction) or arrow(x, y, None) to remove
            if (parameters.Count < 2)
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

            // Check if direction is provided
            if (parameters.Count >= 3)
            {
                if (parameters[2] is PyNone)
                {
                    // Remove arrow at this position
                    MainSimPatch.RemoveDebugArrow(x, y);
                }
                else if (parameters[2] is PyGridDirection pyDir)
                {
                    // Add arrow with direction
                    GridDirection direction = pyDir;
                    var arrow = new DebugArrow(x, y, true, (int)direction);
                    MainSimPatch.AddDebugArrow(arrow);
                }
            }
            else
            {
                // No direction - add white arrow
                var arrow = new DebugArrow(x, y, false, 0);
                MainSimPatch.AddDebugArrow(arrow);
            }

            exec.States[droneId].ReturnValue = new PyNone();
            return 0.0;
        }
    }
}
