using HarmonyLib;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BetterWorkspace.Patches;

[HarmonyPatch(typeof(CodeInputField))]
public static class CommentTogglePatch
{
    [HarmonyPrefix]
    [HarmonyPatch("LateUpdate")]
    static bool LateUpdate_Prefix(CodeInputField __instance)
    {
        if (!__instance.isFocused)
            return true;

        var commentKeyCombination = OptionHolder.GetKeyCombination("Toggle Comment");

        if (commentKeyCombination.IsKeyPressed(true))
        {
            ToggleComment(__instance);
            return false;
        }

        return true;
    }

    private static void ToggleComment(CodeInputField inputField)
    {
        string text = inputField.text;

        var stringPositionField = AccessTools.Property(typeof(CodeInputField), "stringPositionInternal");
        var stringSelectPositionField = AccessTools.Property(typeof(CodeInputField), "stringSelectPositionInternal");

        int stringPosition = (int)stringPositionField.GetValue(inputField);
        int stringSelectPosition = (int)stringSelectPositionField.GetValue(inputField);

        int selectionStart = Mathf.Min(stringPosition, stringSelectPosition);
        int selectionEnd = Mathf.Max(stringPosition, stringSelectPosition);

        // Get selected lines
        int startLine = GetLineNumber(text, selectionStart);
        int endLine = GetLineNumber(text, selectionEnd);

        string[] lines = text.Split('\n');

        // Check if all selected lines are commented
        bool allCommented = true;
        for (int i = startLine; i <= endLine && i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimStart();
            if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
            {
                allCommented = false;
                break;
            }
        }

        // Track how many characters were added/removed per line
        Dictionary<int, int> lineLengthChanges = new Dictionary<int, int>();

        // Toggle comments
        for (int i = startLine; i <= endLine && i < lines.Length; i++)
        {
            int oldLength = lines[i].Length;

            if (allCommented)
            {
                // Remove comment
                string trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("#"))
                {
                    int leadingSpaces = lines[i].Length - trimmed.Length;
                    string afterHash = trimmed.Substring(1);
                    if (afterHash.StartsWith(" ")) afterHash = afterHash.Substring(1);
                    lines[i] = lines[i].Substring(0, leadingSpaces) + afterHash;
                }
            }
            else
            {
                // Add comment
                if (!string.IsNullOrEmpty(lines[i].Trim()))
                {
                    int leadingSpaces = lines[i].Length - lines[i].TrimStart().Length;
                    lines[i] = lines[i].Substring(0, leadingSpaces) + "# " + lines[i].TrimStart();
                }
            }

            int newLength = lines[i].Length;
            int change = newLength - oldLength;
            lineLengthChanges[i] = change;
        }

        string newText = string.Join("\n", lines);

        // Calculate how position shifts based on line changes
        int CalculateNewPosition(int oldPos)
        {
            if (oldPos < selectionStart)
                return oldPos; // Before modified range, no change

            // Find which line this position is on in the ORIGINAL text
            int currentPos = 0;
            for (int lineNum = 0; lineNum < lines.Length; lineNum++)
            {
                int change = lineLengthChanges.ContainsKey(lineNum) ? lineLengthChanges[lineNum] : 0;
                int originalLineLength = lines[lineNum].Length - change;
                int lineEnd = currentPos + (lineNum < lines.Length - 1 ?
                    (originalLineLength + 1) :
                    originalLineLength);

                if (oldPos <= lineEnd || lineNum >= lines.Length - 1)
                {
                    // Position is on this line
                    if (lineNum < startLine)
                        return oldPos; // Before modified range

                    if (lineNum > endLine)
                    {
                        // After modified range - add all accumulated changes
                        int totalChange = 0;
                        for (int i = startLine; i <= endLine; i++)
                        {
                            if (lineLengthChanges.ContainsKey(i))
                                totalChange += lineLengthChanges[i];
                        }
                        return oldPos + totalChange;
                    }

                    // Within modified range
                    // Add changes from all previous modified lines
                    int accumulatedChange = 0;
                    for (int i = startLine; i < lineNum; i++)
                    {
                        if (lineLengthChanges.ContainsKey(i))
                            accumulatedChange += lineLengthChanges[i];
                    }

                    // If we're on a modified line, we need to include its change too
                    // Comments are added at the START of the line, so any position on that line
                    // should be shifted by the full line change
                    if (lineNum >= startLine && lineNum <= endLine)
                    {
                        if (lineLengthChanges.ContainsKey(lineNum))
                            accumulatedChange += lineLengthChanges[lineNum];
                    }

                    return oldPos + accumulatedChange;
                }

                currentPos = lineEnd + 1; // +1 for newline
            }

            return oldPos;
        }

        inputField.text = newText;

        int newStringPosition = CalculateNewPosition(stringPosition);
        int newStringSelectPosition = CalculateNewPosition(stringSelectPosition);

        stringPositionField.SetValue(inputField, Mathf.Clamp(newStringPosition, 0, newText.Length));
        stringSelectPositionField.SetValue(inputField, Mathf.Clamp(newStringSelectPosition, 0, newText.Length));

        // Force caret update and make it visible even with selection
        inputField.ForceLabelUpdate();

        // Activate caret rendering even with selection
        var caretBlinkRateField = AccessTools.Field(typeof(TMP_InputField), "m_CaretBlinkRate");
        if (caretBlinkRateField != null)
        {
            float blinkRate = (float)caretBlinkRateField.GetValue(inputField);
            caretBlinkRateField.SetValue(inputField, 0f);
            caretBlinkRateField.SetValue(inputField, blinkRate); // Reset to trigger blink
        }
    }

    private static int GetLineNumber(string text, int position)
    {
        int lineNumber = 0;
        for (int i = 0; i < position && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                lineNumber++;
            }
        }
        return lineNumber;
    }
}

[HarmonyPatch(typeof(ResourceManager))]
public static class CommentToggleKeybindPatch
{
    private static KeyBindOptionSO commentToggleOption = null;

    [HarmonyPostfix]
    [HarmonyPatch("GetAllOptions")]
    static void GetAllOptions_Postfix(ref IEnumerable<OptionSO> __result)
    {
        if (commentToggleOption == null)
        {
            CreateCommentToggleOption();
        }

        var optionsList = __result.ToList();
        if (commentToggleOption != null) optionsList.Add(commentToggleOption);
        __result = optionsList;
    }

    private static void CreateCommentToggleOption()
    {
        commentToggleOption = ScriptableObject.CreateInstance<KeyBindOptionSO>();
        commentToggleOption.name = "Toggle Comment";
        commentToggleOption.optionName = "Toggle Comment";
        commentToggleOption.tooltip = "Comment or uncomment selected lines (adds/removes # at the start of each line)";
        commentToggleOption.defaultValue = "Ctrl Q";
        commentToggleOption.category = "controls";
        commentToggleOption.importance = 900f;
        commentToggleOption.canBeMouseButton = false;

        var existingOptions = Resources.LoadAll<OptionSO>("Options/");
        var keyBindOption = existingOptions.FirstOrDefault(o => o is KeyBindOptionSO);
        if (keyBindOption != null && keyBindOption.optionUI != null)
        {
            commentToggleOption.optionUI = keyBindOption.optionUI;
            Plugin.Log.LogInfo("Created 'Toggle Comment' keybind option (Ctrl+Q)");
        }

        var existingValue = OptionHolder.GetOption("Toggle Comment", null);
        if (existingValue == null)
        {
            OptionHolder.SetOption("Toggle Comment", "Ctrl Q");
        }
    }
}
