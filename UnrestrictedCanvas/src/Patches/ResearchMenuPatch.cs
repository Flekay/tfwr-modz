using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace UnrestrictedCanvas.Patches;

[HarmonyPatch(typeof(ResearchMenu))]
public class ResearchMenuPatch
{
    private static ScrollRect cachedScrollRect = null;
    private static bool isDragging = false;
    private static Vector2 lastMousePosition;

    // Fix dragging restrictions and add drag support for background
    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    static void Start_Postfix(ResearchMenu __instance)
    {
        // Get the ScrollRect component using reflection
        var scrollRectField = __instance.GetType()
            .GetField("scrollRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var scrollRect = scrollRectField?.GetValue(__instance) as ScrollRect;

        if (scrollRect != null)
        {
            cachedScrollRect = scrollRect;

            // Set movementType to Unrestricted to allow dragging outside boundaries
            scrollRect.movementType = ScrollRect.MovementType.Unrestricted;

            // Disable the ScrollRect's built-in drag handling to prevent conflicts
            scrollRect.enabled = false;

            Plugin.Log.LogInfo("Research menu ScrollRect disabled - using manual drag control");
        }
        else
        {
            Plugin.Log.LogWarning("Could not find scrollRect in ResearchMenu");
        }
    }

    // Add HOME key functionality and manual drag handling
    [HarmonyPostfix]
    [HarmonyPatch("Update")]
    static void Update_Postfix(ResearchMenu __instance)
    {
        // Only process if research menu is open
        if (!__instance.IsOpen) return;

        // Check for HOME key press
        if (OptionHolder.GetKeyCombination("center camera").IsKeyPressed(true))
        {
            if (cachedScrollRect != null && cachedScrollRect.content != null)
            {
                // Center the research tree content
                cachedScrollRect.content.anchoredPosition = Vector2.zero;

                // Reset zoom to 1.0
                __instance.transform.localScale = Vector3.one;

                Plugin.Log.LogInfo("Research tree centered and zoom reset");
            }
        }

        // Manual drag handling using raw input
        if (cachedScrollRect != null && cachedScrollRect.content != null)
        {
            // Start dragging on left mouse button press
            if (Input.GetMouseButtonDown(0))
            {
                isDragging = true;
                lastMousePosition = Input.mousePosition;
            }

            // Stop dragging on left mouse button release
            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }

            // While dragging, move the content
            if (isDragging)
            {
                Vector2 currentMousePosition = Input.mousePosition;
                Vector2 delta = currentMousePosition - lastMousePosition;

                // Get current zoom level (stored in transform.localScale)
                float zoomLevel = __instance.transform.localScale.x;

                // Get the Canvas scaler to account for screen resolution
                UnityEngine.Canvas canvas = __instance.GetComponentInParent<UnityEngine.Canvas>();
                float canvasScale = 1f;
                if (canvas != null && canvas.scaleFactor > 0)
                {
                    canvasScale = canvas.scaleFactor;
                }

                // Divide by zoom level and canvas scale, then multiply by speed factor
                // canvasScale compensates for screen resolution changes
                cachedScrollRect.content.anchoredPosition += (delta / (zoomLevel * canvasScale));

                lastMousePosition = currentMousePosition;
            }
        }
    }
}
