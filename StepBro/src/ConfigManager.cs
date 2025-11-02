using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;

namespace StepBro;

public static class ConfigManager
{
    private static ConfigEntry<string> stepOnFunctionsConfig;
    private static HashSet<string> functionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static void Initialize(ConfigFile config)
    {
        stepOnFunctionsConfig = config.Bind(
            "Step to Function",
            "FunctionList",
            "move,till,plant,harvest,swap,use_item,clear",
            "Comma-separated list of function names that should trigger a step when called. Case-insensitive."
        );

        LoadFunctionNames();
    }

    private static void LoadFunctionNames()
    {
        functionNames.Clear();

        if (string.IsNullOrWhiteSpace(stepOnFunctionsConfig.Value))
        {
            return;
        }

        var names = stepOnFunctionsConfig.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var name in names)
        {
            var trimmed = name.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                functionNames.Add(trimmed);
            }
        }
    }

    public static bool ShouldStepOnFunction(string functionName)
    {
        return functionNames.Contains(functionName);
    }
}
