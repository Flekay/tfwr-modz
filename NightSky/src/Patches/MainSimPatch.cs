using HarmonyLib;
using UnityEngine;

namespace NightSky.Patches;

[HarmonyPatch(typeof(MainSim))]
public class MainSimPatch
{
    // Patch the Start method to reapply on level load
    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    public static void Start_Postfix(MainSim __instance)
    {
        ApplySkyColor(__instance);
    }

    private static void ApplySkyColor(MainSim instance)
    {
        if (instance.mainCamera != null)
        {
            Color skyColor = Plugin.GetSkyColor();
            instance.mainCamera.backgroundColor = skyColor;
            instance.mainCamera.clearFlags = CameraClearFlags.SolidColor;

            // Only disable post-processing if the setting is enabled
            if (Plugin.DisablePostProcessing.Value)
            {
                // Disable URP's UniversalAdditionalCameraData which controls post-processing
                var postProcessingComponents = instance.mainCamera.GetComponents<MonoBehaviour>();
                foreach (var component in postProcessingComponents)
                {
                    if (component.GetType().Name == "UniversalAdditionalCameraData")
                    {
                        var renderPostProcessingProp = component.GetType().GetProperty("renderPostProcessing");
                        if (renderPostProcessingProp != null)
                        {
                            renderPostProcessingProp.SetValue(component, false);
                            Plugin.Log.LogInfo("Disabled URP post-processing");
                        }
                    }
                }

                // Disable global volumes (vignette, color grading, etc.)
                var volumes = UnityEngine.Object.FindObjectsOfType(typeof(MonoBehaviour));
                foreach (var vol in volumes)
                {
                    if (vol.GetType().Name == "Volume")
                    {
                        ((MonoBehaviour)vol).enabled = false;
                        Plugin.Log.LogInfo("Disabled post-processing Volume");
                    }
                }
            }

            // Apply light intensity
            ApplyLightIntensity();

            Plugin.Log.LogInfo($"Sky color applied: #{ColorUtility.ToHtmlStringRGB(skyColor)}");
        }
    }

    private static void ApplyLightIntensity()
    {
        var lights = UnityEngine.Object.FindObjectsOfType<Light>();
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.intensity = Plugin.LightIntensity.Value;
                Plugin.Log.LogInfo($"Set directional light intensity to {Plugin.LightIntensity.Value}");
            }
        }
    }
}
