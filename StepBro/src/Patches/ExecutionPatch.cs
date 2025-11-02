using HarmonyLib;

namespace StepBro.Patches;

[HarmonyPatch(typeof(Execution))]
public class ExecutionPatch
{
    // Static fields to track step over state
    private static bool isStepOverMode = false;
    private static int stepOverTargetDepth = -1;

    // Static fields to track step out state
    private static bool isStepOutMode = false;
    private static int stepOutTargetDepth = -1;

    // Static fields to track step to function state
    private static bool isStepToFunctionMode = false;

    public static void SetStepOverMode(bool enabled)
    {
        isStepOverMode = enabled;
        if (!enabled)
        {
            stepOverTargetDepth = -1;
        }
    }

    public static bool IsStepOverMode()
    {
        return isStepOverMode;
    }

    public static void SetStepOverTargetDepth(int depth)
    {
        stepOverTargetDepth = depth;
    }

    public static int GetStepOverTargetDepth()
    {
        return stepOverTargetDepth;
    }

    public static void SetStepOutMode(bool enabled)
    {
        isStepOutMode = enabled;
        if (!enabled)
        {
            stepOutTargetDepth = -1;
        }
    }

    public static bool IsStepOutMode()
    {
        return isStepOutMode;
    }

    public static void SetStepOutTargetDepth(int depth)
    {
        stepOutTargetDepth = depth;
    }

    public static int GetStepOutTargetDepth()
    {
        return stepOutTargetDepth;
    }

    public static void SetStepToFunctionMode(bool enabled)
    {
        isStepToFunctionMode = enabled;
    }

    public static bool IsStepToFunctionMode()
    {
        return isStepToFunctionMode;
    }

    [HarmonyPrefix]
    [HarmonyPatch("StopExecution")]
    static void StopExecution_Prefix()
    {
        if (isStepOverMode)
        {
            SetStepOverMode(false);
        }
        if (isStepOutMode)
        {
            SetStepOutMode(false);
        }
        if (isStepToFunctionMode)
        {
            SetStepToFunctionMode(false);
        }
    }
}
