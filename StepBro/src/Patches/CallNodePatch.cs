using HarmonyLib;

namespace StepBro.Patches;

[HarmonyPatch(typeof(CallNode))]
public class CallNodePatch
{
    [HarmonyPrefix]
    [HarmonyPatch("Execute")]
    static void Execute_Prefix(CallNode __instance, ProgramState state, Execution execution, int depth)
    {
        if (!execution.sim.stepByStepMode || execution.MainState != state || !ExecutionPatch.IsStepToFunctionMode())
        {
            return;
        }

        try
        {
            var boxedParamsField = AccessTools.Field(typeof(Node), "boxedParams");
            var boxedParams = boxedParamsField?.GetValue(__instance);
            if (boxedParams == null) return;

            var codeWindowField = AccessTools.Field(boxedParams.GetType(), "codeWindow");
            var codeWindow = codeWindowField?.GetValue(boxedParams);
            if (codeWindow == null) return;

            var codeInputProperty = AccessTools.Property(codeWindow.GetType(), "CodeInput");
            var codeInput = codeInputProperty?.GetValue(codeWindow);
            if (codeInput == null) return;

            var textProperty = AccessTools.Property(codeInput.GetType(), "text");
            var text = textProperty?.GetValue(codeInput) as string;
            if (text == null) return;

            var wordStartField = AccessTools.Field(boxedParams.GetType(), "wordStart");
            var wordStart = (int)wordStartField.GetValue(boxedParams);

            if (wordStart > 0)
            {
                // Find function name by going backwards from the opening parenthesis
                int nameEnd = wordStart;
                int nameStart = wordStart - 1;

                // Skip whitespace backwards
                while (nameStart >= 0 && char.IsWhiteSpace(text[nameStart]))
                {
                    nameStart--;
                }
                nameEnd = nameStart + 1;

                // Read identifier backwards
                while (nameStart >= 0 && (char.IsLetterOrDigit(text[nameStart]) || text[nameStart] == '_'))
                {
                    nameStart--;
                }
                nameStart++;

                if (nameStart < nameEnd)
                {
                    var functionName = text.Substring(nameStart, nameEnd - nameStart);

                    if (ConfigManager.ShouldStepOnFunction(functionName))
                    {
                        state.hitStoppingPoint = true;
                        ExecutionPatch.SetStepToFunctionMode(false);
                    }
                }
            }
        }
        catch
        {
            // Silently ignore errors during function name extraction
        }
    }
}
