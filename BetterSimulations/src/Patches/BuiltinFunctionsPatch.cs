using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BetterSimulations.Patches
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
                if (!functionList.Any(f => f.functionName == "set_simulation_speed"))
                {
                    functionList.Add(new PyFunction(
                        "set_simulation_speed",
                        new Func<List<IPyObject>, Simulation, Execution, int, double>(SetSimulationSpeed),
                        null,
                        true  // Free function like quick_print
                    ));

                    hasAddedFunction = true;
                }
            }
        }

        private static double SetSimulationSpeed(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
        {
            if (parameters.Count == 0)
            {
                throw new ExecuteException("set_simulation_speed requires 1 parameter (speed)", -1, -1);
            }

            if (!(parameters[0] is PyNumber))
            {
                throw new ExecuteException("set_simulation_speed parameter must be a number", -1, -1);
            }

            double speed = (PyNumber)parameters[0];

            try
            {
                // Access MainSim.Inst and set TimeFactor directly
                // This is the same variable used by simulation() and leaderboard_run() speed parameter
                // TimeFactor controls how fast time passes in the simulation (plants grow, etc.)
                // This does NOT affect drone execution speed (set_execution_speed does that)
                if (MainSim.Inst != null)
                {
                    MainSim.Inst.TimeFactor = speed;
                }
                else
                {
                    Logger.Log("Warning: MainSim not available, cannot set simulation speed");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error setting simulation speed: {ex.Message}");
            }

            exec.States[droneId].ReturnValue = new PyNone();
            return 0.0;
        }
    }
}
