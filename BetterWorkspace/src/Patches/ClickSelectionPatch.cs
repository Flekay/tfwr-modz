using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterWorkspace.Patches;

// MonoBehaviour to check for mouse release every frame
public class ClickSelectionMouseChecker : MonoBehaviour
{
    private bool wasMouseDown = false;

    void Update()
    {
        bool isMouseDown = Input.GetMouseButton(0);

        // Detect transition from down to up
        if (wasMouseDown && !isMouseDown)
        {
            ClickSelectionPatch.OnMouseButtonReleased();
        }

        wasMouseDown = isMouseDown;
    }
}

[HarmonyPatch(typeof(CodeInputField))]
public static class ClickSelectionPatch
{
    private enum SelectionMode
    {
        None,           // Normal single-click selection
        Word,           // Double-click word selection
        Line,           // Triple-click line selection
        All             // Quadruple-click select all
    }

    private static SelectionMode currentMode = SelectionMode.None;
    private static float lastClickTime = 0f;
    private static int clickCount = 0;
    private static readonly float multiClickDelay = 0.5f;
    private static int lastClickPosition = -1;
    private static readonly int maxClickDistanceForMultiClick = 3; // Allow up to 3 characters distance

    // For word selection
    private static int initialWordStart = -1;
    private static int initialWordEnd = -1;

    // For line selection
    private static int initialLineStart = -1;

    private static GameObject checkerObject = null;

    private static bool IsWordChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    /// <summary>
    /// Forces an immediate visual caret update for the input field.
    /// This is needed when programmatically setting the cursor position.
    /// </summary>
    public static void ForceCaretUpdate(CodeInputField inputField)
    {
        // Force the text component to update its geometry
        var textComponent = inputField.textComponent;
        if (textComponent != null)
        {
            textComponent.SetVerticesDirty();
            textComponent.SetLayoutDirty();
        }
    }

    // Coroutine to update caret in the next frame
    private static System.Collections.IEnumerator UpdateCaretNextFrame(CodeInputField inputField, int position)
    {
        // Wait for end of frame to ensure all Unity updates are done
        yield return new UnityEngine.WaitForEndOfFrame();

        var stringPositionField = AccessTools.Property(typeof(CodeInputField), "stringPositionInternal");
        var stringSelectPositionField = AccessTools.Property(typeof(CodeInputField), "stringSelectPositionInternal");

        // Set positions again to ensure they stick
        stringPositionField.SetValue(inputField, position);
        stringSelectPositionField.SetValue(inputField, position);

        // Force all the update methods
        inputField.ForceLabelUpdate();

        // Try to call UpdateGeometry to force caret redraw
        var updateGeometryMethod = AccessTools.Method(typeof(TMP_InputField), "UpdateGeometry");
        if (updateGeometryMethod != null)
        {
            updateGeometryMethod.Invoke(inputField, null);
        }

        Plugin.Log.LogInfo($"UpdateCaretNextFrame: Caret updated to position {position}");
    }

    public static void OnMouseButtonReleased()
    {
        // Reset mode on mouse release
        // This ensures that after a multi-click, the next single click starts fresh
        currentMode = SelectionMode.None;
        initialWordStart = -1;
        initialWordEnd = -1;
        initialLineStart = -1;
    }

    private static void EnsureCheckerExists()
    {
        if (checkerObject == null)
        {
            checkerObject = new GameObject("ClickSelectionChecker");
            checkerObject.AddComponent<ClickSelectionMouseChecker>();
            UnityEngine.Object.DontDestroyOnLoad(checkerObject);
        }
    }

    // BLOCK the original OnPointerDown completely and handle everything ourselves
    [HarmonyPrefix]
    [HarmonyPatch("OnPointerDown")]
    [HarmonyPriority(Priority.First)]
    static bool OnPointerDown_Prefix(CodeInputField __instance, PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            clickCount = 0;
            currentMode = SelectionMode.None;
            return true; // Let non-left clicks through
        }

        float currentTime = Time.unscaledTime;

        // Activate input field if needed
        if (!__instance.isFocused)
        {
            __instance.ActivateInputField();
        }

        // Get click position using improved cursor detection
        int stringIndex = ImprovedCursorDetectionPatch.GetCursorPosition(__instance, eventData);
        string text = __instance.text;

        Plugin.Log.LogInfo($"OnPointerDown: stringIndex from GetCursorPosition = {stringIndex}, lastClickPosition = {lastClickPosition}");

        var stringPositionField = AccessTools.Property(typeof(CodeInputField), "stringPositionInternal");
        var stringSelectPositionField = AccessTools.Property(typeof(CodeInputField), "stringSelectPositionInternal");

        // Determine click count - only increment if clicking in the same area
        if (currentTime - lastClickTime <= multiClickDelay &&
            Mathf.Abs(stringIndex - lastClickPosition) <= maxClickDistanceForMultiClick)
        {
            clickCount++;
        }
        else
        {
            // New click sequence - reset everything
            clickCount = 1;
            currentMode = SelectionMode.None;
        }

        lastClickTime = currentTime;

        // Determine mode based on click count
        if (clickCount == 1)
        {
            // Single click - handle it ourselves with improved cursor detection
            currentMode = SelectionMode.None;

            // Set the internal fields directly
            stringPositionField.SetValue(__instance, stringIndex);
            stringSelectPositionField.SetValue(__instance, stringIndex);

            // Force immediate visual caret update
            ForceCaretUpdate(__instance);

            // Update lastClickPosition AFTER setting the position
            lastClickPosition = stringIndex;

            return false; // Block original to use our improved cursor position
        }

        // Update lastClickPosition for multi-click scenarios
        lastClickPosition = stringIndex;

        if (clickCount == 2)
        {
            // Double click - select word
            currentMode = SelectionMode.Word;
            SelectWordAtPosition(__instance, stringIndex);

            int start = (int)stringPositionField.GetValue(__instance);
            int end = (int)stringSelectPositionField.GetValue(__instance);
            initialWordStart = Mathf.Min(start, end);
            initialWordEnd = Mathf.Max(start, end);

            EnsureCheckerExists();
        }
        else if (clickCount == 3)
        {
            // Triple click - select line
            currentMode = SelectionMode.Line;
            SelectLineAtPosition(__instance, stringIndex);

            int start = (int)stringPositionField.GetValue(__instance);
            initialLineStart = Mathf.Min(start, (int)stringSelectPositionField.GetValue(__instance));

            EnsureCheckerExists();
        }
        else if (clickCount >= 4)
        {
            // Quadruple click - select all
            currentMode = SelectionMode.All;
            SelectAll(__instance);
            clickCount = 0; // Reset

            EnsureCheckerExists();
        }

        __instance.ForceLabelUpdate();

        return false; // BLOCK original OnPointerDown completely
    }

    // Handle dragging based on current mode
    [HarmonyPrefix]
    [HarmonyPatch("OnDrag")]
    [HarmonyPriority(Priority.First)]
    static bool OnDrag_Prefix(CodeInputField __instance, PointerEventData eventData)
    {
        // Block ALL original drag logic if we're in any special mode
        if (currentMode == SelectionMode.All)
        {
            // Block all dragging for select all mode
            return false;
        }

        // Declare shared variables at the top
        var stringPositionField = AccessTools.Property(typeof(CodeInputField), "stringPositionInternal");
        var stringSelectPositionField = AccessTools.Property(typeof(CodeInputField), "stringSelectPositionInternal");

        if (currentMode == SelectionMode.None)
        {
            // Normal drag mode - but use our improved cursor detection
            int stringIndex = ImprovedCursorDetectionPatch.GetCursorPosition(__instance, eventData);

            // Update selection to reflect drag (stringPosition is the anchor point)
            stringSelectPositionField.SetValue(__instance, stringIndex);

            __instance.ForceLabelUpdate();

            return false; // Block original drag to use our improved detection
        }

        // We're in Word or Line mode - handle drag ourselves and block original
        // Get current mouse position using improved cursor detection
        int dragStringIndex = ImprovedCursorDetectionPatch.GetCursorPosition(__instance, eventData);
        string text = __instance.text;

        if (currentMode == SelectionMode.Word)
        {
            // Word-by-word selection
            int wordStart = dragStringIndex;
            int wordEnd = dragStringIndex;

            while (wordStart > 0 && IsWordChar(text[wordStart - 1]))
            {
                wordStart--;
            }

            while (wordEnd < text.Length && IsWordChar(text[wordEnd]))
            {
                wordEnd++;
            }

            int newStart, newEnd;

            if (dragStringIndex >= initialWordEnd)
            {
                newStart = initialWordStart;
                newEnd = wordEnd;
            }
            else if (dragStringIndex <= initialWordStart)
            {
                newStart = wordStart;
                newEnd = initialWordEnd;
            }
            else
            {
                newStart = initialWordStart;
                newEnd = initialWordEnd;
            }

            stringPositionField.SetValue(__instance, newStart);
            stringSelectPositionField.SetValue(__instance, newEnd);
        }
        else if (currentMode == SelectionMode.Line)
        {
            // Line-by-line selection
            int currentLineStart = dragStringIndex;
            while (currentLineStart > 0 && text[currentLineStart - 1] != '\n')
            {
                currentLineStart--;
            }

            int currentLineEnd = dragStringIndex;
            while (currentLineEnd < text.Length && text[currentLineEnd] != '\n')
            {
                currentLineEnd++;
            }
            if (currentLineEnd < text.Length && text[currentLineEnd] == '\n')
            {
                currentLineEnd++;
            }

            int selectionStart, selectionEnd;

            if (currentLineStart < initialLineStart)
            {
                // Dragging upwards
                selectionStart = currentLineStart;

                // Find the end of the initial line
                int initialLineEnd = initialLineStart;
                while (initialLineEnd < text.Length && text[initialLineEnd] != '\n')
                {
                    initialLineEnd++;
                }
                if (initialLineEnd < text.Length && text[initialLineEnd] == '\n')
                {
                    initialLineEnd++;
                }

                selectionEnd = initialLineEnd;
            }
            else
            {
                // Dragging downwards or on same line
                selectionStart = initialLineStart;
                selectionEnd = currentLineEnd;
            }

            stringPositionField.SetValue(__instance, selectionStart);
            stringSelectPositionField.SetValue(__instance, selectionEnd);
        }

        __instance.ForceLabelUpdate();

        // Mark event as used
        eventData.Use();

        return false; // ALWAYS block original OnDrag when in special mode
    }

    // Block OnBeginDrag when in special modes
    [HarmonyPrefix]
    [HarmonyPatch("OnBeginDrag")]
    [HarmonyPriority(Priority.First)]
    static bool OnBeginDrag_Prefix()
    {
        if (currentMode != SelectionMode.None)
        {
            return false;
        }
        return true;
    }

    // Block OnEndDrag when in special modes
    [HarmonyPrefix]
    [HarmonyPatch("OnEndDrag")]
    [HarmonyPriority(Priority.First)]
    static bool OnEndDrag_Prefix()
    {
        if (currentMode != SelectionMode.None)
        {
            return false;
        }
        return true;
    }

    // Block LateUpdate's selection updates when in special modes
    [HarmonyPrefix]
    [HarmonyPatch("LateUpdate")]
    [HarmonyPriority(Priority.First)]
    static void LateUpdate_Prefix(CodeInputField __instance, ref bool __state)
    {
        // Store whether we should skip the original LateUpdate selection logic
        __state = (currentMode != SelectionMode.None && __instance.isFocused);
    }

    [HarmonyPostfix]
    [HarmonyPatch("LateUpdate")]
    [HarmonyPriority(Priority.Last)]
    static void LateUpdate_Postfix(CodeInputField __instance, bool __state)
    {
        // If we were in a special mode, make sure the selection stays correct
        if (__state && currentMode != SelectionMode.None)
        {
            // The original LateUpdate might have changed the selection, so we don't need to do anything
            // The drag events handle the selection updates
        }
    }

    private static void SelectWordAtPosition(CodeInputField inputField, int position)
    {
        string text = inputField.text;
        if (string.IsNullOrEmpty(text))
            return;

        int wordStart = position;
        int wordEnd = position;

        while (wordStart > 0 && IsWordChar(text[wordStart - 1]))
        {
            wordStart--;
        }

        while (wordEnd < text.Length && IsWordChar(text[wordEnd]))
        {
            wordEnd++;
        }

        var stringPositionField = AccessTools.Property(typeof(CodeInputField), "stringPositionInternal");
        var stringSelectPositionField = AccessTools.Property(typeof(CodeInputField), "stringSelectPositionInternal");

        stringPositionField.SetValue(inputField, wordStart);
        stringSelectPositionField.SetValue(inputField, wordEnd);

        inputField.ForceLabelUpdate();
    }

    private static void SelectLineAtPosition(CodeInputField inputField, int position)
    {
        string text = inputField.text;
        if (string.IsNullOrEmpty(text))
            return;

        int lineStart = position;
        while (lineStart > 0 && text[lineStart - 1] != '\n')
        {
            lineStart--;
        }

        int lineEnd = position;
        while (lineEnd < text.Length && text[lineEnd] != '\n')
        {
            lineEnd++;
        }

        // Include the newline character if present (VSCode behavior)
        if (lineEnd < text.Length && text[lineEnd] == '\n')
        {
            lineEnd++;
        }

        var stringPositionField = AccessTools.Property(typeof(CodeInputField), "stringPositionInternal");
        var stringSelectPositionField = AccessTools.Property(typeof(CodeInputField), "stringSelectPositionInternal");

        stringPositionField.SetValue(inputField, lineStart);
        stringSelectPositionField.SetValue(inputField, lineEnd);

        inputField.ForceLabelUpdate();
    }

    private static void SelectAll(CodeInputField inputField)
    {
        string text = inputField.text;

        var stringPositionField = AccessTools.Property(typeof(CodeInputField), "stringPositionInternal");
        var stringSelectPositionField = AccessTools.Property(typeof(CodeInputField), "stringSelectPositionInternal");

        stringPositionField.SetValue(inputField, 0);
        stringSelectPositionField.SetValue(inputField, text.Length);

        inputField.ForceLabelUpdate();
    }

    [HarmonyPostfix]
    [HarmonyPatch("DeactivateInputField")]
    static void DeactivateInputField_Postfix()
    {
        currentMode = SelectionMode.None;
        clickCount = 0;
        lastClickPosition = -1;
        initialWordStart = -1;
        initialWordEnd = -1;
        initialLineStart = -1;

        if (checkerObject != null)
        {
            UnityEngine.Object.Destroy(checkerObject);
            checkerObject = null;
        }
    }
}
