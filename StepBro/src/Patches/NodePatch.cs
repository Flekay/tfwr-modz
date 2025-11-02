using HarmonyLib;

namespace StepBro.Patches;

[HarmonyPatch(typeof(Node))]
public class NodePatch
{
    private static Node stepOverStartNode = null;

    public static void SetStepOverStartNode(Node node)
    {
        stepOverStartNode = node;
    }

    public static void ClearStepOverStartNode()
    {
        stepOverStartNode = null;
    }

    [HarmonyPrefix]
    [HarmonyPatch("ErrorsAndBreakpoints")]
    static void ErrorsAndBreakpoints_Prefix(Node __instance, ProgramState state, Execution execution, int depth, ref bool __state)
    {
        __state = false;

        if (ExecutionPatch.IsStepOverMode())
        {
            if (execution.sim.stepByStepMode && execution.MainState == state)
            {
                var executionStackIndexField = AccessTools.Field(typeof(ProgramState), "executionStackIndex");
                int currentDepth = (int)executionStackIndexField.GetValue(state);
                int targetDepth = ExecutionPatch.GetStepOverTargetDepth();

                if (currentDepth > targetDepth)
                {
                    __state = true;
                }
            }
        }

        if (ExecutionPatch.IsStepOutMode())
        {
            if (execution.sim.stepByStepMode && execution.MainState == state)
            {
                var executionStackIndexField = AccessTools.Field(typeof(ProgramState), "executionStackIndex");
                int currentDepth = (int)executionStackIndexField.GetValue(state);
                int targetDepth = ExecutionPatch.GetStepOutTargetDepth();

                if (currentDepth > targetDepth)
                {
                    __state = true;
                }
            }
        }

        if (ExecutionPatch.IsStepToFunctionMode())
        {
            if (execution.sim.stepByStepMode && execution.MainState == state)
            {
                if (__instance.NodeName != "call")
                {
                    __state = true;
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("ErrorsAndBreakpoints")]
    static void ErrorsAndBreakpoints_Postfix(Node __instance, ProgramState state, Execution execution, int depth, bool __state)
    {
        if (__state)
        {
            bool wasStepToFunctionAndFoundMatch = !ExecutionPatch.IsStepToFunctionMode() &&
                                                   !ExecutionPatch.IsStepOverMode() &&
                                                   !ExecutionPatch.IsStepOutMode();

            if (!wasStepToFunctionAndFoundMatch)
            {
                state.hitStoppingPoint = false;
            }
        }

        if (ExecutionPatch.IsStepOverMode())
        {
            if (execution.sim.stepByStepMode && execution.MainState == state)
            {
                var executionStackIndexField = AccessTools.Field(typeof(ProgramState), "executionStackIndex");
                int currentDepth = (int)executionStackIndexField.GetValue(state);
                int targetDepth = ExecutionPatch.GetStepOverTargetDepth();

                // If we're back at the target depth and on a different line, complete the step over
                if (currentDepth <= targetDepth && !IsOnSameLine(__instance, stepOverStartNode))
                {
                    state.hitStoppingPoint = true;
                    ExecutionPatch.SetStepOverMode(false);
                    ClearStepOverStartNode();
                }
            }
        }

        // Handle step out completion
        if (ExecutionPatch.IsStepOutMode())
        {
            if (execution.sim.stepByStepMode && execution.MainState == state)
            {
                var executionStackIndexField = AccessTools.Field(typeof(ProgramState), "executionStackIndex");
                int currentDepth = (int)executionStackIndexField.GetValue(state);
                int targetDepth = ExecutionPatch.GetStepOutTargetDepth();

                if (currentDepth <= targetDepth)
                {
                    state.hitStoppingPoint = true;
                    ExecutionPatch.SetStepOutMode(false);
                }
            }
        }
    }

    private static bool IsOnSameLine(Node current, Node other)
    {
        if (current == null || other == null)
        {
            return false;
        }

        CodeWindow currentCodeWindow = current.boxedParams.codeWindow;
        CodeWindow otherCodeWindow = other.boxedParams.codeWindow;

        if (currentCodeWindow != otherCodeWindow)
        {
            return false;
        }

        if (currentCodeWindow == null)
        {
            return false;
        }

        int currentLine = currentCodeWindow.CodeInput.textComponent.textInfo.characterInfo[current.boxedParams.wordStart].lineNumber;
        int otherLine = currentCodeWindow.CodeInput.textComponent.textInfo.characterInfo[other.boxedParams.wordStart].lineNumber;

        return currentLine == otherLine;
    }
}
