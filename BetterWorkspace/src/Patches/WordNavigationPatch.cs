using HarmonyLib;
using TMPro;
using UnityEngine;

namespace BetterWorkspace.Patches;

[HarmonyPatch(typeof(CodeInputField))]
public static class WordNavigationPatch
{
    private static bool patchesInitialized = false;

    private static void EnsureInitialized()
    {
        if (!patchesInitialized)
        {
            Plugin.Log.LogInfo("Word navigation patches loaded");
            patchesInitialized = true;
        }
    }

    private static bool IsWordChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    // Patch FindNextWordBegin for better word jumping (like VSCode)
    [HarmonyPrefix]
    [HarmonyPatch("FindNextWordBegin", MethodType.Normal)]
    static bool FindNextWordBegin_Prefix(CodeInputField __instance, ref int __result)
    {
        EnsureInitialized();

        var stringSelectPositionField = AccessTools.Property(typeof(CodeInputField), "stringSelectPositionInternal");
        int stringSelectPosition = (int)stringSelectPositionField.GetValue(__instance);

        string text = __instance.text;

        if (stringSelectPosition >= text.Length)
        {
            __result = text.Length;
            return false;
        }

        int pos = stringSelectPosition;

        // VSCode behavior (Ctrl+Right):
        // 1. If on whitespace, skip to start of next word/punctuation
        // 2. If on word char, skip to end of word (then stop)
        // 3. If on punctuation, skip to end of punctuation (then stop)
        // The key: we stop AFTER the current word/punctuation, not AT the next one

        char currentChar = pos < text.Length ? text[pos] : '\0';

        // If we're on whitespace, skip it first
        if (char.IsWhiteSpace(currentChar))
        {
            // Skip spaces/tabs
            while (pos < text.Length && char.IsWhiteSpace(text[pos]) && text[pos] != '\n')
            {
                pos++;
            }
            // If we hit a newline, move past it AND any following whitespace (indentation)
            if (pos < text.Length && text[pos] == '\n')
            {
                pos++;
                // Skip indentation on the new line
                while (pos < text.Length && char.IsWhiteSpace(text[pos]) && text[pos] != '\n')
                {
                    pos++;
                }
            }
        }

        // Now we're on either a word char or punctuation (or EOF)
        currentChar = pos < text.Length ? text[pos] : '\0';

        // If we're on a word, skip to the end of it
        if (pos < text.Length && IsWordChar(currentChar))
        {
            while (pos < text.Length && IsWordChar(text[pos]))
            {
                pos++;
            }
        }
        // If we're on punctuation, skip to end of punctuation
        else if (pos < text.Length && !char.IsWhiteSpace(currentChar))
        {
            while (pos < text.Length && !IsWordChar(text[pos]) && !char.IsWhiteSpace(text[pos]))
            {
                pos++;
            }
        }

        __result = pos;
        return false; // Skip original method
    }

    // Patch FindPrevWordBegin for better word jumping (like VSCode)
    [HarmonyPrefix]
    [HarmonyPatch("FindPrevWordBegin", MethodType.Normal)]
    static bool FindPrevWordBegin_Prefix(CodeInputField __instance, ref int __result)
    {
        var stringSelectPositionField = AccessTools.Property(typeof(CodeInputField), "stringSelectPositionInternal");
        int stringSelectPosition = (int)stringSelectPositionField.GetValue(__instance);

        string text = __instance.text;

        if (stringSelectPosition <= 0)
        {
            __result = 0;
            return false;
        }

        int pos = stringSelectPosition;

        // VSCode behavior (Ctrl+Left):
        // 1. If on whitespace, skip backwards to end of previous word/punctuation
        // 2. Then skip to start of that word/punctuation
        // The key: we stop at the START of the previous word/punctuation

        // Move back one position to look at what's before the cursor
        pos--;

        char currentChar = pos >= 0 && pos < text.Length ? text[pos] : '\0';

        // If we're on whitespace, skip it backwards
        if (char.IsWhiteSpace(currentChar))
        {
            // Skip spaces/tabs backwards
            while (pos > 0 && char.IsWhiteSpace(text[pos]) && text[pos] != '\n')
            {
                pos--;
            }

            // If we hit a newline, stop at it
            if (pos >= 0 && text[pos] == '\n')
            {
                __result = pos;
                return false;
            }

            currentChar = pos >= 0 && pos < text.Length ? text[pos] : '\0';
        }

        // Now we should be at the end of a word or punctuation
        // Skip backwards to find the start of it
        if (pos >= 0 && IsWordChar(currentChar))
        {
            while (pos > 0 && IsWordChar(text[pos - 1]))
            {
                pos--;
            }
        }
        else if (pos >= 0 && !char.IsWhiteSpace(currentChar))
        {
            while (pos > 0 && !IsWordChar(text[pos - 1]) && !char.IsWhiteSpace(text[pos - 1]))
            {
                pos--;
            }
        }

        __result = Mathf.Max(0, pos);
        return false; // Skip original method
    }

    // Intercept Ctrl+Backspace in OnUpdateSelected (where text input is processed)
    [HarmonyPrefix]
    [HarmonyPatch("OnUpdateSelected")]
    [HarmonyPriority(Priority.First)]
    static bool OnUpdateSelected_CtrlBackspace(CodeInputField __instance, UnityEngine.EventSystems.BaseEventData eventData)
    {
        if (!__instance.isFocused)
            return true;

        // Check for Ctrl+Backspace
        bool ctrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool backspacePressed = Input.GetKeyDown(KeyCode.Backspace);

        if (ctrlPressed && backspacePressed)
        {
            DeletePreviousWord(__instance);
            return false; // Block the default behavior completely
        }

        return true;
    }

    private static void DeletePreviousWord(CodeInputField inputField)
    {
        var stringPositionField = AccessTools.Property(typeof(CodeInputField), "stringPositionInternal");
        var stringSelectPositionField = AccessTools.Property(typeof(CodeInputField), "stringSelectPositionInternal");

        int stringPosition = (int)stringPositionField.GetValue(inputField);
        int stringSelectPosition = (int)stringSelectPositionField.GetValue(inputField);

        string text = inputField.text;

        // If there's a selection, just delete it (standard behavior)
        if (stringPosition != stringSelectPosition)
        {
            int selectionStart = Mathf.Min(stringPosition, stringSelectPosition);
            int selectionEnd = Mathf.Max(stringPosition, stringSelectPosition);

            string newText = text.Substring(0, selectionStart) + text.Substring(selectionEnd);
            inputField.text = newText;

            stringPositionField.SetValue(inputField, selectionStart);
            stringSelectPositionField.SetValue(inputField, selectionStart);

            inputField.ForceLabelUpdate();
            ClickSelectionPatch.ForceCaretUpdate(inputField);
            return;
        }

        // No selection - delete previous word
        if (stringPosition <= 0)
            return;

        int pos = stringPosition;

        // VSCode Ctrl+Backspace behavior:
        // 1. If cursor is after whitespace, delete the whitespace first
        // 2. Then delete the word/punctuation before it
        // This matches VSCode's two-phase deletion

        // Move back one position to look at what's before the cursor
        pos--;

        char currentChar = pos >= 0 && pos < text.Length ? text[pos] : '\0';

        // Check if we're directly after a newline (no whitespace between)
        if (pos >= 0 && text[pos] == '\n')
        {
            // Delete the newline
            string newText = text.Substring(0, pos) + text.Substring(stringPosition);
            inputField.text = newText;

            stringPositionField.SetValue(inputField, pos);
            stringSelectPositionField.SetValue(inputField, pos);

            inputField.ForceLabelUpdate();
            ClickSelectionPatch.ForceCaretUpdate(inputField);
            return;
        }

        // Phase 1: Skip trailing whitespace (spaces/tabs only, not newlines)
        while (pos >= 0 && (text[pos] == ' ' || text[pos] == '\t'))
        {
            pos--;
        }

        // If we're at the start of file, delete just the whitespace
        if (pos < 0)
        {
            string newText = text.Substring(stringPosition);
            inputField.text = newText;

            stringPositionField.SetValue(inputField, 0);
            stringSelectPositionField.SetValue(inputField, 0);

            inputField.ForceLabelUpdate();
            ClickSelectionPatch.ForceCaretUpdate(inputField);
            return;
        }

        // If we hit a newline after skipping whitespace, stop at the whitespace (don't delete newline)
        if (pos >= 0 && text[pos] == '\n')
        {
            // Delete only from after the newline to cursor (just the whitespace)
            int deleteStart = pos + 1;
            string newText = text.Substring(0, deleteStart) + text.Substring(stringPosition);
            inputField.text = newText;

            stringPositionField.SetValue(inputField, deleteStart);
            stringSelectPositionField.SetValue(inputField, deleteStart);

            inputField.ForceLabelUpdate();
            ClickSelectionPatch.ForceCaretUpdate(inputField);
            return;
        }

        // Phase 2: Now delete the word or punctuation
        currentChar = text[pos];

        if (IsWordChar(currentChar))
        {
            // Delete word characters
            while (pos > 0 && IsWordChar(text[pos - 1]))
            {
                pos--;
            }
        }
        else
        {
            // Delete punctuation characters
            while (pos > 0 && !IsWordChar(text[pos - 1]) && !char.IsWhiteSpace(text[pos - 1]))
            {
                pos--;
            }
        }

        int deleteStartPos = Mathf.Max(0, pos);

        // Delete from deleteStartPos to stringPosition
        string resultText = text.Substring(0, deleteStartPos) + text.Substring(stringPosition);
        inputField.text = resultText;

        stringPositionField.SetValue(inputField, deleteStartPos);
        stringSelectPositionField.SetValue(inputField, deleteStartPos);

        inputField.ForceLabelUpdate();
        ClickSelectionPatch.ForceCaretUpdate(inputField);
    }
}
