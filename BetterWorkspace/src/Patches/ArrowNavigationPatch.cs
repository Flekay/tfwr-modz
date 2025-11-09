using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterWorkspace.Patches;

[HarmonyPatch(typeof(CodeInputField))]
    public static class ArrowNavigationPatch
    {
        private static int? rememberedCaretPosition = null;
        private static int lastCaretStringPosition = -1;
        private static bool wasShiftPressed = false;
        private static int? savedSelectPosition = null;  // Save the anchor position when using Shift    // Block Ctrl+Up/Down and Shift+Up/Down from Unity's default handling
    [HarmonyPrefix]
    [HarmonyPatch("OnUpdateSelected")]
    [HarmonyPriority(Priority.First)]
    static bool OnUpdateSelected_BlockCtrlArrow(CodeInputField __instance, BaseEventData eventData)
    {
        if (!__instance.isFocused)
            return true;

        bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool upPressed = Input.GetKeyDown(KeyCode.UpArrow);
        bool downPressed = Input.GetKeyDown(KeyCode.DownArrow);

        // Block Ctrl+Up/Down (without Shift) - we'll handle scrolling in postfix
        if (ctrlPressed && !shiftPressed && (upPressed || downPressed))
        {
            return false; // Block the entire OnUpdateSelected
        }

        // Block Shift+Up/Down - we'll handle it completely in postfix with remembered X
        if (shiftPressed && !ctrlPressed && (upPressed || downPressed))
        {
            return false; // Block Unity's handling, we'll do it ourselves
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("OnUpdateSelected")]
    static void OnUpdateSelected_Prefix(CodeInputField __instance, BaseEventData eventData)
    {
        if (!__instance.isFocused)
            return;

        bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        // Detect if Up/Down arrow is pressed
        bool upPressed = Input.GetKeyDown(KeyCode.UpArrow);
        bool downPressed = Input.GetKeyDown(KeyCode.DownArrow);

        // Don't interfere with Ctrl+Shift (move lines)
        if (ctrlPressed && shiftPressed)
            return;

        var stringPositionField = AccessTools.Property(typeof(CodeInputField), "stringPositionInternal");
        var stringSelectPositionField = AccessTools.Property(typeof(CodeInputField), "stringSelectPositionInternal");

        int currentPosition = (int)stringPositionField.GetValue(__instance);
        int currentSelectPosition = (int)stringSelectPositionField.GetValue(__instance);

        if (upPressed || downPressed)
        {
            // Only remember X position on the FIRST Up/Down press
            // After that, keep the remembered position even if we land on a shorter line
            if (rememberedCaretPosition == null)
            {
                // First Up/Down - remember current X position
                rememberedCaretPosition = GetCaretXPosition(__instance, currentPosition);
                Plugin.Log.LogInfo($"Arrow: Remembered X position = {rememberedCaretPosition}");
            }
            else
            {
                Plugin.Log.LogInfo($"Arrow: Keeping remembered X position = {rememberedCaretPosition}");
            }

            // IMPORTANT: Save anchor BEFORE Unity's code runs!
            // If using Shift and we haven't saved the anchor yet, save it now
            // We need the anchor position BEFORE Unity modifies it
            if (shiftPressed && !savedSelectPosition.HasValue)
            {
                // When starting selection with Shift+Arrow:
                // - currentPosition and currentSelectPosition are equal (no selection yet)
                // - We want the anchor to stay at the CURRENT position
                // - The caret will move in the Postfix
                savedSelectPosition = currentPosition;
                Plugin.Log.LogInfo($"Arrow: Saved anchor position = {savedSelectPosition} (will stay here while caret moves)");
            }
        }
        else if (Input.anyKeyDown)
        {
            // Check if it's NOT just a modifier key being pressed/released
            bool isModifierOnly = (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) ||
                                   Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl) ||
                                   Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt));

            if (!isModifierOnly)
            {
                // Any other key was pressed - reset remembered position
                if (rememberedCaretPosition.HasValue)
                {
                    Plugin.Log.LogInfo($"Arrow: Reset remembered position (other key pressed)");
                }
                rememberedCaretPosition = null;
                savedSelectPosition = null;
            }
        }

        lastCaretStringPosition = currentPosition;
        wasShiftPressed = shiftPressed;
    }

    [HarmonyPostfix]
    [HarmonyPatch("OnUpdateSelected")]
    static void OnUpdateSelected_Postfix(CodeInputField __instance, BaseEventData eventData)
    {
        if (!__instance.isFocused)
            return;

        bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool upPressed = Input.GetKeyDown(KeyCode.UpArrow);
        bool downPressed = Input.GetKeyDown(KeyCode.DownArrow);

        // Handle Ctrl+Up/Down for scrolling without moving caret
        if (ctrlPressed && !shiftPressed && (upPressed || downPressed))
        {
            // Just scroll - caret movement was already blocked in prefix
            var scrollRect = __instance.GetComponentInParent<UnityEngine.UI.ScrollRect>();
            if (scrollRect != null)
            {
                float scrollAmount = 0.1f; // 10% of viewport
                if (upPressed)
                    scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + scrollAmount);
                else
                    scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition - scrollAmount);
            }
            return;
        }

        // Don't interfere with Ctrl+Shift (move lines)
        if (ctrlPressed && shiftPressed)
            return;

        // Handle Shift+Up/Down - we blocked Unity's processing, now do it ourselves
        if (shiftPressed && !ctrlPressed && (upPressed || downPressed))
        {
            var stringPositionField = AccessTools.Property(typeof(CodeInputField), "stringPositionInternal");
            var stringSelectPositionField = AccessTools.Property(typeof(CodeInputField), "stringSelectPositionInternal");

            int currentPosition = (int)stringPositionField.GetValue(__instance);
            int currentSelectPosition = (int)stringSelectPositionField.GetValue(__instance);
            string text = __instance.text;

            // Save anchor on first Shift+Arrow
            if (!savedSelectPosition.HasValue)
            {
                savedSelectPosition = currentSelectPosition;
                Plugin.Log.LogInfo($"Arrow Shift: Saved anchor = {savedSelectPosition}");
            }

            // Find current line bounds
            int currentLineStart = currentPosition;
            while (currentLineStart > 0 && text[currentLineStart - 1] != '\n')
                currentLineStart--;

            // Remember X position if not already remembered
            if (!rememberedCaretPosition.HasValue)
            {
                rememberedCaretPosition = currentPosition - currentLineStart;
                Plugin.Log.LogInfo($"Arrow Shift: Remembered X = {rememberedCaretPosition}");
            }

            // Find target line
            int targetLineStart;
            int targetLineEnd;

            if (upPressed)
            {
                // Move to previous line
                if (currentLineStart > 0)
                {
                    targetLineEnd = currentLineStart - 1; // Skip the \n
                    targetLineStart = targetLineEnd;
                    while (targetLineStart > 0 && text[targetLineStart - 1] != '\n')
                        targetLineStart--;
                }
                else
                {
                    // Already at first line
                    targetLineStart = 0;
                    targetLineEnd = currentLineStart;
                    while (targetLineEnd < text.Length && text[targetLineEnd] != '\n')
                        targetLineEnd++;
                }
            }
            else // downPressed
            {
                // Move to next line
                int currentLineEnd = currentPosition;
                while (currentLineEnd < text.Length && text[currentLineEnd] != '\n')
                    currentLineEnd++;

                if (currentLineEnd < text.Length)
                {
                    targetLineStart = currentLineEnd + 1; // Skip the \n
                    targetLineEnd = targetLineStart;
                    while (targetLineEnd < text.Length && text[targetLineEnd] != '\n')
                        targetLineEnd++;
                }
                else
                {
                    // Already at last line
                    targetLineStart = currentLineStart;
                    targetLineEnd = currentLineEnd;
                }
            }

            // Calculate target position with remembered X
            int targetPosition = Mathf.Min(targetLineStart + rememberedCaretPosition.Value, targetLineEnd);

            Plugin.Log.LogInfo($"Arrow Shift: Moving from {currentPosition} to {targetPosition}, anchor={savedSelectPosition}");

            // Set positions
            stringPositionField.SetValue(__instance, targetPosition);
            stringSelectPositionField.SetValue(__instance, savedSelectPosition.Value);

            ClickSelectionPatch.ForceCaretUpdate(__instance);
            return;
        }

        // If we have a remembered position and just moved Up/Down (without Shift), adjust to that X position
        if (rememberedCaretPosition.HasValue && !shiftPressed && (upPressed || downPressed))
        {
            var stringPositionField = AccessTools.Property(typeof(CodeInputField), "stringPositionInternal");
            var stringSelectPositionField = AccessTools.Property(typeof(CodeInputField), "stringSelectPositionInternal");

            int currentPosition = (int)stringPositionField.GetValue(__instance);
            int currentSelectPosition = (int)stringSelectPositionField.GetValue(__instance);
            string text = __instance.text;

            // After Unity's processing:
            // - stringPosition is ALWAYS the "moving" end (where the visual caret is)
            // - stringSelectPosition is the "anchor" end (where selection started)
            // We need to adjust stringPosition to use the remembered X position

            // Find the line that stringPosition is on (the position that just moved)
            int positionToAdjust = currentPosition;

            int lineStart = positionToAdjust;
            while (lineStart > 0 && text[lineStart - 1] != '\n')
            {
                lineStart--;
            }

            int lineEnd = positionToAdjust;
            while (lineEnd < text.Length && text[lineEnd] != '\n')
            {
                lineEnd++;
            }

            // Calculate target position using remembered X position
            int targetPosition = Mathf.Min(lineStart + rememberedCaretPosition.Value, lineEnd);

            Plugin.Log.LogInfo($"Arrow Postfix: lineStart={lineStart}, lineEnd={lineEnd}, remembered X={rememberedCaretPosition.Value}, target={targetPosition}, currentPos={currentPosition}, currentSelect={currentSelectPosition}");

            // ALWAYS set stringPosition (the moving caret) to the target
            stringPositionField.SetValue(__instance, targetPosition);

            // Handle selection position
            if (shiftPressed && savedSelectPosition.HasValue)
            {
                // Restore the saved anchor position (don't let Unity move it!)
                stringSelectPositionField.SetValue(__instance, savedSelectPosition.Value);
                Plugin.Log.LogInfo($"Arrow: Restored anchor to saved position = {savedSelectPosition.Value}");
            }
            else if (!shiftPressed)
            {
                // No selection - move anchor to match caret
                stringSelectPositionField.SetValue(__instance, targetPosition);
            }
            // If using Shift but no saved position, keep Unity's value (shouldn't happen)

            ClickSelectionPatch.ForceCaretUpdate(__instance);

            lastCaretStringPosition = targetPosition;
        }
        else
        {
            // Track the caret position even when not using our adjustment
            var stringPositionField = AccessTools.Property(typeof(CodeInputField), "stringPositionInternal");
            lastCaretStringPosition = (int)stringPositionField.GetValue(__instance);
        }

        // Update wasShiftPressed for next time
        wasShiftPressed = shiftPressed;
    }

    private static int GetCaretXPosition(CodeInputField inputField, int stringPosition)
    {
        string text = inputField.text;

        // Find the start of the current line
        int lineStart = stringPosition;
        while (lineStart > 0 && text[lineStart - 1] != '\n')
        {
            lineStart--;
        }

        // Return the offset from the line start
        return stringPosition - lineStart;
    }
}
