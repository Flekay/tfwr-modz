using HarmonyLib;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BetterWorkspace.Patches;

[HarmonyPatch(typeof(CodeInputField))]
public static class MoveLineDownPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("LateUpdate")]
    static bool LateUpdate_Prefix(CodeInputField __instance)
    {
        if (!__instance.isFocused)
            return true;

        var moveLineDownKeyCombination = OptionHolder.GetKeyCombination("Move Line Down");

        if (moveLineDownKeyCombination.IsKeyPressed(true))
        {
            MoveLineDown(__instance);
            return false;
        }

        return true;
    }

    private static void MoveLineDown(CodeInputField inputField)
    {
        var stringPositionField = AccessTools.Property(typeof(CodeInputField), "stringPositionInternal");
        var stringSelectPositionField = AccessTools.Property(typeof(CodeInputField), "stringSelectPositionInternal");

        string text = inputField.text;
        int stringPosition = (int)stringPositionField.GetValue(inputField);
        int stringSelectPosition = (int)stringSelectPositionField.GetValue(inputField);

        int actualPosition = stringPosition; // The "real" cursor position

        int selectionStart = actualPosition;
        int selectionEnd = actualPosition;

        int startLine = GetLineNumber(text, selectionStart);
        int endLine = GetLineNumber(text, selectionEnd);

        // If selectionEnd is at the start of a line, don't include that line
        if (selectionEnd > 0 && selectionEnd < text.Length && text[selectionEnd - 1] == '\n' && selectionStart != selectionEnd)
        {
            endLine = Mathf.Max(startLine, endLine - 1);
        }

        string[] lines = text.Split('\n');

        if (endLine >= lines.Length - 1)
        {
            return; // Already at bottom
        }

        // Calculate offset BEFORE swapping
        string lineBelow = lines[endLine + 1];
        int lineBelowLength = lineBelow.Length + 1; // +1 for newline

        // Move all selected lines down by swapping with the line below
        string temp = lines[endLine + 1];

        for (int i = endLine + 1; i > startLine; i--)
        {
            lines[i] = lines[i - 1];
        }
        lines[startLine] = temp;

        string newText = string.Join("\n", lines);
        inputField.text = newText;

        // Adjust caret position
        int offset = lineBelowLength;
        int newStart = Mathf.Min(newText.Length, selectionStart + offset);
        int newEnd = Mathf.Min(newText.Length, selectionEnd + offset);

        stringPositionField.SetValue(inputField, newStart);
        stringSelectPositionField.SetValue(inputField, newStart);

        // Force caret update to make it visible
        inputField.ForceLabelUpdate();

        // Reset caret blink to make it visible immediately
        var caretBlinkRateField = AccessTools.Field(typeof(TMP_InputField), "m_CaretBlinkRate");
        if (caretBlinkRateField != null)
        {
            float blinkRate = (float)caretBlinkRateField.GetValue(inputField);
            caretBlinkRateField.SetValue(inputField, 0f);
            caretBlinkRateField.SetValue(inputField, blinkRate);
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
public static class MoveLineDownKeybindPatch
{
    private static KeyBindOptionSO moveLineDownOption = null;

    [HarmonyPostfix]
    [HarmonyPatch("GetAllOptions")]
    static void GetAllOptions_Postfix(ref IEnumerable<OptionSO> __result)
    {
        if (moveLineDownOption == null)
        {
            CreateMoveLineDownOption();
        }

        var optionsList = __result.ToList();
        if (moveLineDownOption != null) optionsList.Add(moveLineDownOption);
        __result = optionsList;
    }

    private static void CreateMoveLineDownOption()
    {
        moveLineDownOption = ScriptableObject.CreateInstance<KeyBindOptionSO>();
        moveLineDownOption.name = "Move Line Down";
        moveLineDownOption.optionName = "Move Line Down";
        moveLineDownOption.tooltip = "Move the current line down";
        moveLineDownOption.defaultValue = "Ctrl Shift DownArrow";
        moveLineDownOption.category = "controls";
        moveLineDownOption.importance = 898f;
        moveLineDownOption.canBeMouseButton = false;

        var existingOptions = Resources.LoadAll<OptionSO>("Options/");
        var keyBindOption = existingOptions.FirstOrDefault(o => o is KeyBindOptionSO);
        if (keyBindOption != null && keyBindOption.optionUI != null)
        {
            moveLineDownOption.optionUI = keyBindOption.optionUI;
            Plugin.Log.LogInfo("Created 'Move Line Down' keybind option");
        }

        var existingValue = OptionHolder.GetOption("Move Line Down", null);
        if (existingValue == null)
        {
            OptionHolder.SetOption("Move Line Down", "Ctrl Shift DownArrow");
        }
    }
}
