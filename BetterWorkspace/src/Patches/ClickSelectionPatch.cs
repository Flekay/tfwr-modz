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

        // Get click position
        var textComponentField = AccessTools.Field(typeof(CodeInputField), "m_TextComponent");
        var textComponent = (TMP_Text)textComponentField.GetValue(__instance);

        CaretPosition caretPos = CaretPosition.Left;
        Vector3 position = eventData.position;
        object[] parameters = new object[] { textComponent, position, eventData.pressEventCamera, caretPos };
        var getCursorMethod = typeof(TMP_TextUtilities).GetMethod(
            "GetCursorIndexFromPosition",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            null,
            new System.Type[] { typeof(TMP_Text), typeof(Vector3), typeof(Camera), typeof(CaretPosition).MakeByRefType() },
            null
        );
        int cursorIndexFromPosition = (int)getCursorMethod.Invoke(null, parameters);
        caretPos = (CaretPosition)parameters[3];

        string text = __instance.text;
        int stringIndex;

        if (cursorIndexFromPosition >= 0 && cursorIndexFromPosition < textComponent.textInfo.characterCount)
        {
            if (caretPos == CaretPosition.Left)
            {
                stringIndex = textComponent.textInfo.characterInfo[cursorIndexFromPosition].index;
            }
            else
            {
                stringIndex = textComponent.textInfo.characterInfo[cursorIndexFromPosition].index +
                             textComponent.textInfo.characterInfo[cursorIndexFromPosition].stringLength;
            }
        }
        else
        {
            stringIndex = text.Length;
        }

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
        lastClickPosition = stringIndex;

        // Determine mode based on click count
        if (clickCount == 1)
        {
            // Single click - let original handler deal with it
            currentMode = SelectionMode.None;
            return true; // Allow original OnPointerDown to handle single clicks
        }
        else if (clickCount == 2)
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

        if (currentMode == SelectionMode.None)
        {
            return true; // Let normal drag through
        }

        // We're in Word or Line mode - handle drag ourselves and block original
        // Get current mouse position
        var textComponentField = AccessTools.Field(typeof(CodeInputField), "m_TextComponent");
        var textComponent = (TMP_Text)textComponentField.GetValue(__instance);

        CaretPosition caretPos = CaretPosition.Left;
        Vector3 position = eventData.position;
        object[] parameters = new object[] { textComponent, position, eventData.pressEventCamera, caretPos };
        var getCursorMethod = typeof(TMP_TextUtilities).GetMethod(
            "GetCursorIndexFromPosition",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            null,
            new System.Type[] { typeof(TMP_Text), typeof(Vector3), typeof(Camera), typeof(CaretPosition).MakeByRefType() },
            null
        );
        int cursorIndexFromPosition = (int)getCursorMethod.Invoke(null, parameters);
        caretPos = (CaretPosition)parameters[3];

        string text = __instance.text;
        int stringIndex;

        if (cursorIndexFromPosition >= 0 && cursorIndexFromPosition < textComponent.textInfo.characterCount)
        {
            if (caretPos == CaretPosition.Left)
            {
                stringIndex = textComponent.textInfo.characterInfo[cursorIndexFromPosition].index;
            }
            else
            {
                stringIndex = textComponent.textInfo.characterInfo[cursorIndexFromPosition].index +
                             textComponent.textInfo.characterInfo[cursorIndexFromPosition].stringLength;
            }
        }
        else
        {
            stringIndex = text.Length;
        }

        var stringPositionField = AccessTools.Property(typeof(CodeInputField), "stringPositionInternal");
        var stringSelectPositionField = AccessTools.Property(typeof(CodeInputField), "stringSelectPositionInternal");

        if (currentMode == SelectionMode.Word)
        {
            // Word-by-word selection
            int wordStart = stringIndex;
            int wordEnd = stringIndex;

            while (wordStart > 0 && IsWordChar(text[wordStart - 1]))
            {
                wordStart--;
            }

            while (wordEnd < text.Length && IsWordChar(text[wordEnd]))
            {
                wordEnd++;
            }

            int newStart, newEnd;

            if (stringIndex >= initialWordEnd)
            {
                newStart = initialWordStart;
                newEnd = wordEnd;
            }
            else if (stringIndex <= initialWordStart)
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
            int currentLineStart = stringIndex;
            while (currentLineStart > 0 && text[currentLineStart - 1] != '\n')
            {
                currentLineStart--;
            }

            int currentLineEnd = stringIndex;
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
