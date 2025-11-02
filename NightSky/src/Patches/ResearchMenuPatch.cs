using HarmonyLib;
using UnityEngine;

namespace NightSky.Patches;

[HarmonyPatch(typeof(ResearchMenu))]
public class ResearchMenuPatch
{
    // Patch Setup which is called when the research menu is initialized
    [HarmonyPatch("Setup")]
    [HarmonyPostfix]
    public static void SetupPostfix(ResearchMenu __instance)
    {
        ApplyBackgroundColor(__instance);
    }

    private static void ApplyBackgroundColor(ResearchMenu instance)
    {
        // Search all Image components in the research menu hierarchy
        var allComponents = instance.GetComponentsInChildren<Component>(true);
        foreach (var component in allComponents)
        {
            if (component.GetType().Name == "Image")
            {
                var name = component.gameObject.name.ToLower();
                if (name.Contains("background") || name.Contains("panel"))
                {
                    var colorProp = component.GetType().GetProperty("color");
                    if (colorProp != null)
                    {
                        colorProp.SetValue(component, Plugin.GetSkyColor());
                        Plugin.Log.LogInfo($"Applied sky color to research menu background: {component.gameObject.name}");
                        return;
                    }
                }
            }
        }
    }
}
