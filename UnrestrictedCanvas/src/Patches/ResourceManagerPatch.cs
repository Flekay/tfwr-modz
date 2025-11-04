using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnrestrictedCanvas.Patches;

[HarmonyPatch(typeof(ResourceManager))]
public class ResourceManagerPatch
{
    private static KeyBindOptionSO centerCameraOption = null;
    private static KeyBindOptionSO zoomInOption = null;
    private static KeyBindOptionSO zoomOutOption = null;

    // Inject our custom keybind options into the options list
    [HarmonyPostfix]
    [HarmonyPatch("GetAllOptions")]
    static void GetAllOptions_Postfix(ref IEnumerable<OptionSO> __result)
    {
        // Create our custom options only once
        if (centerCameraOption == null)
        {
            CreateCenterCameraOption();
        }
        if (zoomInOption == null)
        {
            CreateZoomInOption();
        }
        if (zoomOutOption == null)
        {
            CreateZoomOutOption();
        }

        // Add our options to the result
        var optionsList = __result.ToList();
        optionsList.Add(centerCameraOption);
        optionsList.Add(zoomInOption);
        optionsList.Add(zoomOutOption);
        __result = optionsList;
    }

    private static void CreateCenterCameraOption()
    {
        // Create a KeyBindOptionSO instance for our option
        centerCameraOption = ScriptableObject.CreateInstance<KeyBindOptionSO>();
        centerCameraOption.name = "Center Camera";
        centerCameraOption.optionName = "Center Camera";
        centerCameraOption.tooltip = "Center the camera back to 0,0 position and reset zoom";
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

    private static void CreateZoomInOption()
    {
        // Create a KeyBindOptionSO instance for zoom in
        zoomInOption = ScriptableObject.CreateInstance<KeyBindOptionSO>();
        zoomInOption.name = "Zoom In";
        zoomInOption.optionName = "Zoom In";
        zoomInOption.tooltip = "Zoom in on the workspace canvas";
        zoomInOption.defaultValue = "KeypadPlus";
        zoomInOption.category = "controls";
        zoomInOption.importance = 999f;
        zoomInOption.canBeMouseButton = true; // Allow scroll wheel

        // Find and use the KeyBindOptionUI prefab
        var existingOptions = Resources.LoadAll<OptionSO>("Options/");
        var keyBindOption = existingOptions.FirstOrDefault(o => o is KeyBindOptionSO);

        if (keyBindOption != null && keyBindOption.optionUI != null)
        {
            zoomInOption.optionUI = keyBindOption.optionUI;
            Plugin.Log.LogInfo("Created 'Zoom In' keybind option with KeypadPlus default");
        }

        // Set the default keybind in OptionHolder if not already set
        var existingValue = OptionHolder.GetOption("Zoom In", null);
        if (existingValue == null)
        {
            OptionHolder.SetOption("Zoom In", "KeypadPlus");
        }
    }

    private static void CreateZoomOutOption()
    {
        // Create a KeyBindOptionSO instance for zoom out
        zoomOutOption = ScriptableObject.CreateInstance<KeyBindOptionSO>();
        zoomOutOption.name = "Zoom Out";
        zoomOutOption.optionName = "Zoom Out";
        zoomOutOption.tooltip = "Zoom out on the workspace canvas";
        zoomOutOption.defaultValue = "KeypadMinus";
        zoomOutOption.category = "controls";
        zoomOutOption.importance = 998f;
        zoomOutOption.canBeMouseButton = true; // Allow scroll wheel

        // Find and use the KeyBindOptionUI prefab
        var existingOptions = Resources.LoadAll<OptionSO>("Options/");
        var keyBindOption = existingOptions.FirstOrDefault(o => o is KeyBindOptionSO);

        if (keyBindOption != null && keyBindOption.optionUI != null)
        {
            zoomOutOption.optionUI = keyBindOption.optionUI;
            Plugin.Log.LogInfo("Created 'Zoom Out' keybind option with KeypadMinus default");
        }

        // Set the default keybind in OptionHolder if not already set
        var existingValue = OptionHolder.GetOption("Zoom Out", null);
        if (existingValue == null)
        {
            OptionHolder.SetOption("Zoom Out", "KeypadMinus");
        }
    }
}
