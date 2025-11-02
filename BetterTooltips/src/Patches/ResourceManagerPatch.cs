using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterTooltips.Patches;

[HarmonyPatch(typeof(ResourceManager))]
public class ResourceManagerPatch
{
    private static object showTooltipsOption = null;

    // Inject our custom option into the options list
    [HarmonyPostfix]
    [HarmonyPatch("GetAllOptions")]
    static void GetAllOptions_Postfix(ref IEnumerable<OptionSO> __result)
    {
        // Create our custom option only once
        if (showTooltipsOption == null)
        {
            CreateShowTooltipsOption();
        }

        // Add our option to the result if successfully created
        if (showTooltipsOption != null)
        {
            var optionsList = __result.ToList();
            optionsList.Add((OptionSO)showTooltipsOption);
            __result = optionsList;
        }
    }

    private static void CreateShowTooltipsOption()
    {
        // Find an existing CycleOptionSO to use as template and get its type
        var existingOptions = Resources.LoadAll<OptionSO>("Options/");

        Plugin.Log.LogInfo($"Found {existingOptions.Length} existing options");

        // Find a CycleOptionSO template (like "autosave", "code highlights", etc.)
        OptionSO templateOption = null;
        foreach (var opt in existingOptions)
        {
            if (opt.GetType().Name == "CycleOptionSO")
            {
                templateOption = opt;
                Plugin.Log.LogInfo($"Found CycleOptionSO template: {opt.name} (Type: {opt.GetType().FullName})");
                break;
            }
        }

        if (templateOption == null)
        {
            Plugin.Log.LogWarning("Could not find CycleOptionSO template");
            return;
        }

        // Create an instance of CycleOptionSO using the template's type
        var cycleOptionType = templateOption.GetType();
        showTooltipsOption = ScriptableObject.CreateInstance(cycleOptionType);

        if (showTooltipsOption == null)
        {
            Plugin.Log.LogError("Failed to create CycleOptionSO instance");
            return;
        }

        // Set the ScriptableObject name
        ((ScriptableObject)showTooltipsOption).name = "Show Tooltips";

        // Set basic properties that all OptionSO have
        AccessTools.Field(cycleOptionType, "optionName")?.SetValue(showTooltipsOption, "Show Tooltips");
        AccessTools.Field(cycleOptionType, "tooltip")?.SetValue(showTooltipsOption, "Show or hide enhanced tooltips when hovering over tiles");
        AccessTools.Field(cycleOptionType, "defaultValue")?.SetValue(showTooltipsOption, "Enabled");
        AccessTools.Field(cycleOptionType, "category")?.SetValue(showTooltipsOption, "general");
        AccessTools.Field(cycleOptionType, "importance")?.SetValue(showTooltipsOption, 50f);

        // Copy the UI prefab from template
        if (templateOption.optionUI != null)
        {
            AccessTools.Field(cycleOptionType, "optionUI")?.SetValue(showTooltipsOption, templateOption.optionUI);
        }

        // Find all possible field names for the cycle values
        var allFields = AccessTools.GetDeclaredFields(cycleOptionType);
        bool valuesSet = false;
        foreach (var field in allFields)
        {
            Plugin.Log.LogInfo($"CycleOptionSO has field: {field.Name} (Type: {field.FieldType.Name})");

            // Try to find the string array field for values
            if (field.FieldType == typeof(string[]))
            {
                field.SetValue(showTooltipsOption, new string[] { "Enabled", "Disabled" });
                Plugin.Log.LogInfo($"Set toggle values using string[] field: {field.Name}");
                valuesSet = true;
            }
            // Also check for List<string>
            else if (field.FieldType == typeof(List<string>))
            {
                field.SetValue(showTooltipsOption, new List<string> { "Enabled", "Disabled" });
                Plugin.Log.LogInfo($"Set toggle values using List<string> field: {field.Name}");
                valuesSet = true;
            }
        }

        if (!valuesSet)
        {
            Plugin.Log.LogWarning("Could not find suitable field to set toggle values!");
        }

        Plugin.Log.LogInfo("Successfully created 'Show Tooltips' toggle option");

        // Set the default value in OptionHolder
        var existingValue = OptionHolder.GetOption("Show Tooltips", null);
        if (existingValue == null)
        {
            OptionHolder.SetOption("Show Tooltips", "Enabled");
            Plugin.Log.LogInfo("Set default value to 'Enabled' in OptionHolder");
        }
    }
}
