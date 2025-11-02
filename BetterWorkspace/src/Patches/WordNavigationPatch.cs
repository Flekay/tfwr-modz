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
}
