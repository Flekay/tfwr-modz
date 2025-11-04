using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace UnrestrictedCanvas.Patches;

[HarmonyPatch(typeof(Workspace))]
public class WorkspacePatch
{
    private static float zoomSpeed = 1.1f; // Default zoom speed multiplier
    private static Vector2 initialCameraPosition;
    private static float initialZoom;
    private static bool hasRecordedInitialState = false;
    
    // For simulating left-click drag when middle/right clicking over windows
    private static bool isSimulatingDrag = false;
    private static ScrollRect cachedScrollRect = null;

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
            cachedScrollRect = scrollRect;

            Plugin.Log.LogInfo("Workspace ScrollRect set to Unrestricted movement");
        }
        else
        {
            Plugin.Log.LogWarning("Could not find scrollRect in Workspace");
        }

        // Get zoomSpeed from the Workspace instance
        var zoomSpeedField = __instance.GetType()
            .GetField("zoomSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (zoomSpeedField != null)
        {
            zoomSpeed = (float)zoomSpeedField.GetValue(__instance);
        }

        // Reset initial state flag when workspace starts (new level/game start)
        hasRecordedInitialState = false;
    }

    // Replace the Zoom method to remove restrictions
    [HarmonyPrefix]
    [HarmonyPatch("Zoom")]
    static bool Zoom_Prefix(Workspace __instance, float zoom)
    {
        if (zoom > 0f)
        {
            // Zoom in - no restrictions
            __instance.cameraController.zoom *= zoomSpeed;
            __instance.zoomContainer.localScale = Vector3.one * __instance.cameraController.zoom;
            __instance.container.GetComponent<ContainerScaler>()?.UpdateMarginSize();
            return false; // Skip original method
        }
        if (zoom < 0f)
        {
            // Zoom out - no restrictions
            __instance.cameraController.zoom /= zoomSpeed;
            __instance.zoomContainer.localScale = Vector3.one * __instance.cameraController.zoom;
            __instance.container.GetComponent<ContainerScaler>()?.UpdateMarginSize();
            return false; // Skip original method
        }
        return false; // Skip original method
    }

    // Add HOME key functionality and custom zoom keybinds
    [HarmonyPostfix]
    [HarmonyPatch("Update")]
    static void Update_Postfix(Workspace __instance)
    {
        // Record initial camera position and zoom after the first frame when everything is set up
        // Wait until there's at least one window open to capture the "default" state
        if (!hasRecordedInitialState && __instance.openWindows.Count > 0)
        {
            // Wait a bit more to ensure camera has moved to initial position
            if (Time.frameCount > 10)
            {
                initialCameraPosition = __instance.container.anchoredPosition;
                initialZoom = __instance.cameraController.zoom;
                hasRecordedInitialState = true;
                Plugin.Log.LogInfo($"Recorded initial camera state - Position: {initialCameraPosition}, Zoom: {initialZoom}");
            }
        }

        // Check for HOME key press to reset camera to initial state
        if (OptionHolder.GetKeyCombination("Center Camera").IsKeyPressed(true))
        {
            // Restore to initial state (or fallback to 0,0 and zoom 1.0 if not recorded yet)
            if (hasRecordedInitialState)
            {
                __instance.cameraController.zoom = initialZoom;
                __instance.zoomContainer.localScale = Vector3.one * initialZoom;
                __instance.container.GetComponent<ContainerScaler>()?.UpdateMarginSize();
                __instance.container.anchoredPosition = initialCameraPosition;
                Plugin.Log.LogInfo($"Camera restored to initial state - Position: {initialCameraPosition}, Zoom: {initialZoom}");
            }
            else
            {
                // Fallback to 0,0 if initial state hasn't been recorded yet
                __instance.cameraController.zoom = 1f;
                __instance.zoomContainer.localScale = Vector3.one;
                __instance.container.GetComponent<ContainerScaler>()?.UpdateMarginSize();
                __instance.container.anchoredPosition = Vector2.zero;
                Plugin.Log.LogInfo("Camera reset to fallback state (0,0, zoom 1.0)");
            }
            return; // Exit early to prevent other keybinds from triggering
        }

        // Handle zoom keybinds - these work EVERYWHERE regardless of cursor position
        // Check for Zoom In keybind
        if (OptionHolder.GetKeyCombination("Zoom In").IsKeyPressed(false))
        {
            __instance.Zoom(1f); // Positive value for zoom in
        }

                // Check for Zoom Out keybind
        if (OptionHolder.GetKeyCombination("Zoom Out").IsKeyPressed(false))
        {
            __instance.Zoom(-1f); // Negative value for zoom out
        }

        // Handle middle/right click over windows - simulate ScrollRect drag
        HandleMiddleRightClickPanning(__instance);
    }

    private static void HandleMiddleRightClickPanning(Workspace workspace)
    {
        if (cachedScrollRect == null) return;

        // Check if middle or right button pressed
        bool middleOrRightDown = Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2);
        bool middleOrRightHeld = Input.GetMouseButton(1) || Input.GetMouseButton(2);
        bool middleOrRightUp = Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2);

        // Check if over a window
        bool isOverWindow = workspace.openWindows.Values
            .Any(w => RectTransformUtility.RectangleContainsScreenPoint(
                w.GetComponent<RectTransform>(), 
                Input.mousePosition, 
                workspace.uiCam));

        // Start simulated drag when middle/right clicked over window
        if (!isSimulatingDrag && middleOrRightDown && isOverWindow)
        {
            var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {
                button = UnityEngine.EventSystems.PointerEventData.InputButton.Left,
                position = Input.mousePosition
            };
            cachedScrollRect.OnBeginDrag(pointerData);
            isSimulatingDrag = true;
        }

        // Continue simulated drag
        if (isSimulatingDrag && middleOrRightHeld)
        {
            var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {
                button = UnityEngine.EventSystems.PointerEventData.InputButton.Left,
                position = Input.mousePosition
            };
            cachedScrollRect.OnDrag(pointerData);
        }

        // End simulated drag
        if (isSimulatingDrag && middleOrRightUp)
        {
            var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {
                button = UnityEngine.EventSystems.PointerEventData.InputButton.Left,
                position = Input.mousePosition
            };
            cachedScrollRect.OnEndDrag(pointerData);
            isSimulatingDrag = false;
        }
    }

    // Replace the Zoom method to remove restrictions
}
