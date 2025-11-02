using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace UnrestrictedCanvas.Patches;

[HarmonyPatch(typeof(Workspace))]
public class WorkspacePatch
{
    // Disable automatic camera snap-back by setting ScrollRect to Unrestricted
    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    static void Start_Postfix(Workspace __instance)
    {
        // Get the ScrollRect component using reflection
        var scrollRectField = __instance.GetType()
            .GetField("scrollRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var scrollRect = scrollRectField?.GetValue(__instance) as ScrollRect;

        if (scrollRect != null)
        {
            // Set movementType to Unrestricted to prevent snap-back when outside margins
            scrollRect.movementType = ScrollRect.MovementType.Unrestricted;

            Plugin.Log.LogInfo("Workspace ScrollRect set to Unrestricted movement");
        }
        else
        {
            Plugin.Log.LogWarning("Could not find scrollRect in Workspace");
        }
    }

    // Add HOME key functionality to center camera at 0,0 and reset zoom
    [HarmonyPostfix]
    [HarmonyPatch("Update")]
    static void Update_Postfix(Workspace __instance)
    {
        // Check for HOME key press
        if (OptionHolder.GetKeyCombination("center camera").IsKeyPressed(true))
        {
            // Use the built-in camera movement by creating a temporary invisible window at 0,0
            // First, reset zoom
            __instance.cameraController.zoom = 1f;
            __instance.zoomContainer.localScale = Vector3.one;
            __instance.container.GetComponent<ContainerScaler>()?.UpdateMarginSize();

            // Center to 0,0
            __instance.container.anchoredPosition = Vector2.zero;

            Plugin.Log.LogInfo("Camera centered to 0,0 and zoom reset to 1.0");
        }
    }
}
