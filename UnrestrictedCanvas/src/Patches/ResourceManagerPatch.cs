using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnrestrictedCanvas.Patches;

[HarmonyPatch(typeof(ResourceManager))]
public class ResourceManagerPatch
{
    private static KeyBindOptionSO centerCameraOption = null;

    // Inject our custom keybind option into the options list
    [HarmonyPostfix]
    [HarmonyPatch("GetAllOptions")]
    static void GetAllOptions_Postfix(ref IEnumerable<OptionSO> __result)
    {
        // Create our custom option only once
        if (centerCameraOption == null)
        {
            CreateCenterCameraOption();
        }

        // Add our option to the result
        var optionsList = __result.ToList();
        optionsList.Add(centerCameraOption);
        __result = optionsList;
    }

    private static void CreateCenterCameraOption()
    {
        // Create a KeyBindOptionSO instance for our option
        centerCameraOption = ScriptableObject.CreateInstance<KeyBindOptionSO>();
        centerCameraOption.name = "Center Camera";
        centerCameraOption.optionName = "Center Camera";
        centerCameraOption.tooltip = "Center the camera back to 0,0 position";
        centerCameraOption.defaultValue = "Home";
        centerCameraOption.category = "controls";
        centerCameraOption.importance = 1000f; // High importance to appear near top
        centerCameraOption.canBeMouseButton = false; // Only keyboard keys allowed

        // Find and use the KeyBindOptionUI prefab
        var existingOptions = Resources.LoadAll<OptionSO>("Options/");
        var keyBindOption = existingOptions.FirstOrDefault(o => o is KeyBindOptionSO);

        if (keyBindOption != null && keyBindOption.optionUI != null)
        {
            centerCameraOption.optionUI = keyBindOption.optionUI;
            Plugin.Log.LogInfo("Created 'Center Camera' keybind option with HOME key default");
        }
        else
        {
            Plugin.Log.LogWarning("Could not find KeyBindOptionUI prefab - keybind option may not display correctly");
        }

        // Set the default keybind in OptionHolder if not already set
        var existingValue = OptionHolder.GetOption("Center Camera", null);
        if (existingValue == null)
        {
            OptionHolder.SetOption("Center Camera", "Home");
        }
    }
}
