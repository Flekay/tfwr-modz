using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Reset.Patches
{
    [HarmonyPatch(typeof(BuiltinFunctions))]
    public static class BuiltinFunctionsPatch
    {
        private static FieldInfo functionsField;
        private static MethodInfo noParamsMethod;

        [HarmonyPatch("Functions", MethodType.Getter)]
        [HarmonyPrefix]
        public static void Functions_Prefix()
        {
            if (functionsField == null)
            {
                functionsField = typeof(BuiltinFunctions).GetField("functions", BindingFlags.NonPublic | BindingFlags.Static);
                noParamsMethod = typeof(BuiltinFunctions).GetMethod("NoParams", BindingFlags.NonPublic | BindingFlags.Static);
            }

            var functions = (Dictionary<string, PyFunction>)functionsField.GetValue(null);

            if (functions != null)
            {
                // Add reset function
                if (!functions.ContainsKey("reset"))
                {
                    var resetFunc = new PyFunction("reset", new Func<List<IPyObject>, Simulation, Execution, int, double>(Reset), null, false);
                    functions["reset"] = resetFunc;
                }
            }
        }

        private static double Reset(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
        {
            // Use reflection to call NoParams
            noParamsMethod.Invoke(null, new object[] { parameters, "reset" });

            Plugin.Log.LogInfo("reset() function called, setting side effect");

            ProgramState programState = exec.States[droneId];
            programState.currentSideEffect = (SideEffect)999; // Custom SideEffect value
            return 0.0;
        }
    }
}
