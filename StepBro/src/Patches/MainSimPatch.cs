using HarmonyLib;

namespace StepBro.Patches;

[HarmonyPatch(typeof(MainSim))]
public class MainSimPatch
{
    public static void NextExecutionStepOver(MainSim instance)
    {
        object lockSimulation = AccessTools.Field(typeof(MainSim), "lockSimulation").GetValue(instance);

        lock (lockSimulation)
        {
            Simulation sim = (Simulation)AccessTools.Field(typeof(MainSim), "sim").GetValue(instance);

            if (sim.IsExecuting() && sim.Execution != null)
            {
                int currentDepth = GetExecutionStackDepth(sim.Execution.MainState);
                Node currentNode = sim.Execution.MainState.CurrentExecutingNode;

                NodePatch.SetStepOverStartNode(currentNode);
                ExecutionPatch.SetStepOverTargetDepth(currentDepth);
                ExecutionPatch.SetStepOverMode(true);

                sim.NextExecutionStep();
            }
        }
    }

    public static void NextExecutionStepOut(MainSim instance)
    {
        object lockSimulation = AccessTools.Field(typeof(MainSim), "lockSimulation").GetValue(instance);

        lock (lockSimulation)
        {
            Simulation sim = (Simulation)AccessTools.Field(typeof(MainSim), "sim").GetValue(instance);

            if (sim.IsExecuting() && sim.Execution != null)
            {
                int currentDepth = GetExecutionStackDepth(sim.Execution.MainState);
                int targetDepth = currentDepth - 1;

                if (targetDepth >= 0)
                {
                    ExecutionPatch.SetStepOutTargetDepth(targetDepth);
                    ExecutionPatch.SetStepOutMode(true);
                    sim.NextExecutionStep();
                }
            }
        }
    }

    public static void NextExecutionStepToFunction(MainSim instance)
    {
        object lockSimulation = AccessTools.Field(typeof(MainSim), "lockSimulation").GetValue(instance);

        lock (lockSimulation)
        {
            Simulation sim = (Simulation)AccessTools.Field(typeof(MainSim), "sim").GetValue(instance);

            if (sim.IsExecuting() && sim.Execution != null)
            {
                ExecutionPatch.SetStepToFunctionMode(true);
                sim.NextExecutionStep();
            }
        }
    }

    private static int GetExecutionStackDepth(ProgramState state)
    {
        if (state == null) return 0;

        // Access the private executionStackIndex field
        var executionStackIndexField = AccessTools.Field(typeof(ProgramState), "executionStackIndex");
        int stackIndex = (int)executionStackIndexField.GetValue(state);

        return stackIndex;
    }
}
