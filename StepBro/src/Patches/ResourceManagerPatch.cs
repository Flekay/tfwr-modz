using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StepBro.Patches;

[HarmonyPatch(typeof(ResourceManager))]
public class ResourceManagerPatch
{
    private static KeyBindOptionSO stepOverOption = null;
    private static KeyBindOptionSO stepOutOption = null;
    private static KeyBindOptionSO stepToFunctionOption = null;

    // Inject our custom keybind option into the options list
    [HarmonyPostfix]
    [HarmonyPatch("GetAllOptions")]
    static void GetAllOptions_Postfix(ref IEnumerable<OptionSO> __result)
    {
        // Create our custom options only once
        if (stepOverOption == null)
        {
            CreateStepOverOption();
        }
        if (stepOutOption == null)
        {
            CreateStepOutOption();
        }
        if (stepToFunctionOption == null)
        {
            CreateStepToFunctionOption();
        }

        // Add our options to the result
        var optionsList = __result.ToList();
        optionsList.Add(stepOverOption);
        optionsList.Add(stepOutOption);
        optionsList.Add(stepToFunctionOption);
        __result = optionsList;
    }

    private static void CreateStepOverOption()
    {
        // Create a KeyBindOptionSO instance for our option
        stepOverOption = ScriptableObject.CreateInstance<KeyBindOptionSO>();
        stepOverOption.name = "Step Over";  // Display name (capitalized)
        stepOverOption.optionName = "Step Over";  // Internal name (lowercase for GetKeyCombination)
        stepOverOption.tooltip = "Step over the current line without entering function calls (debugger step over)";
        stepOverOption.defaultValue = "F10";
        stepOverOption.category = "controls";
        stepOverOption.importance = 1000f; // High importance to appear near top
        stepOverOption.canBeMouseButton = false; // Only keyboard keys allowed

        // Find and use the KeyBindOptionUI prefab from existing options
        var existingOptions = Resources.LoadAll<OptionSO>("Options/");
        var keyBindOption = existingOptions.FirstOrDefault(o => o is KeyBindOptionSO);

        if (keyBindOption != null && keyBindOption.optionUI != null)
        {
            stepOverOption.optionUI = keyBindOption.optionUI;
        }

        var existingValue = OptionHolder.GetOption("Step Over", null);
        if (existingValue == null)
        {
            OptionHolder.SetOption("Step Over", "F10");
        }
    }

    private static void CreateStepOutOption()
    {
        // Create a KeyBindOptionSO instance for our option
        stepOutOption = ScriptableObject.CreateInstance<KeyBindOptionSO>();
        stepOutOption.name = "Step Out";  // Display name (capitalized)
        stepOutOption.optionName = "Step Out";  // Internal name (for GetKeyCombination)
        stepOutOption.tooltip = "Step out of the current function and return to the caller (debugger step out)";
        stepOutOption.defaultValue = "Shift F11";
        stepOutOption.category = "controls";
        stepOutOption.importance = 999f; // Slightly lower than step over to appear next to it
        stepOutOption.canBeMouseButton = false;

        var existingOptions = Resources.LoadAll<OptionSO>("Options/");
        var keyBindOption = existingOptions.FirstOrDefault(o => o is KeyBindOptionSO);

        if (keyBindOption != null && keyBindOption.optionUI != null)
        {
            stepOutOption.optionUI = keyBindOption.optionUI;
        }

        var existingValue = OptionHolder.GetOption("Step Out", null);
        if (existingValue == null)
        {
            OptionHolder.SetOption("Step Out", "Shift F11");
        }
    }

    private static void CreateStepToFunctionOption()
    {
        stepToFunctionOption = ScriptableObject.CreateInstance<KeyBindOptionSO>();
        stepToFunctionOption.name = "Step to Function";
        stepToFunctionOption.optionName = "Step to Function";
        stepToFunctionOption.tooltip = "Step to the next function call from the configured list";
        stepToFunctionOption.defaultValue = "F9";
        stepToFunctionOption.category = "controls";
        stepToFunctionOption.importance = 998f;
        stepToFunctionOption.canBeMouseButton = false;

        var existingOptions = Resources.LoadAll<OptionSO>("Options/");
        var keyBindOption = existingOptions.FirstOrDefault(o => o is KeyBindOptionSO);

        if (keyBindOption != null && keyBindOption.optionUI != null)
        {
            stepToFunctionOption.optionUI = keyBindOption.optionUI;
        }

        var existingValue = OptionHolder.GetOption("Step to Function", null);
        if (existingValue == null)
        {
            OptionHolder.SetOption("Step to Function", "F9");
        }
    }
}
