using HarmonyLib;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BetterWorkspace.Patches;

[HarmonyPatch(typeof(CodeInputField))]
public static class MoveLineUpPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("LateUpdate")]
    static bool LateUpdate_Prefix(CodeInputField __instance)
    {
        if (!__instance.isFocused)
            return true;

        var moveLineUpKeyCombination = OptionHolder.GetKeyCombination("Move Line Up");

        if (moveLineUpKeyCombination.IsKeyPressed(true))
        {
            MoveLineUp(__instance);
            return false;
        }

        return true;
    }

    private static void MoveLineUp(CodeInputField inputField)
    {
        var stringPositionField = AccessTools.Property(typeof(CodeInputField), "stringPositionInternal");
        var stringSelectPositionField = AccessTools.Property(typeof(CodeInputField), "stringSelectPositionInternal");

        string text = inputField.text;
        int stringPosition = (int)stringPositionField.GetValue(inputField);
        int stringSelectPosition = (int)stringSelectPositionField.GetValue(inputField);

        // Reset selection to ONLY the cursor position
        stringSelectPositionField.SetValue(inputField, stringPosition);

        int selectionStart = stringPosition;
        int selectionEnd = stringPosition;

        int startLine = GetLineNumber(text, selectionStart);
        int endLine = startLine; // Only move ONE line

        if (startLine == 0)
        {
            return; // Already at top
        }

        string[] lines = text.Split('\n');
        if (endLine >= lines.Length) return;

        // Calculate offset BEFORE swapping (using original line positions)
        string lineAbove = lines[startLine - 1];
        int lineAboveLength = lineAbove.Length + 1; // +1 for newline

        // Move all selected lines up by swapping with the line above
        string temp = lines[startLine - 1];

        for (int i = startLine - 1; i < endLine; i++)
        {
            lines[i] = lines[i + 1];
        }
        lines[endLine] = temp;

        string newText = string.Join("\n", lines);

        inputField.text = newText;

        // Calculate new positions based on line positions in the new text
        int newStartLinePos = 0;
        for (int i = 0; i < startLine - 1; i++)
        {
            newStartLinePos += lines[i].Length + 1; // +1 for newline
        }

        // Calculate offset from start of original startLine to selectionStart
        int oldStartLinePos = 0;
        for (int i = 0; i < startLine; i++)
        {
            oldStartLinePos += text.Split('\n')[i].Length + 1;
        }
        int offsetInStartLine = selectionStart - oldStartLinePos;
        int offsetInEndLine = selectionEnd - oldStartLinePos;

        // Calculate how many characters into the moved block each position is
        int charsFromBlockStart_Start = selectionStart - oldStartLinePos;
        int charsFromBlockStart_End = selectionEnd - oldStartLinePos;

        int newStart = newStartLinePos + charsFromBlockStart_Start;
        int newEnd = newStartLinePos + charsFromBlockStart_End;

        newStart = Mathf.Max(0, Mathf.Min(newText.Length, newStart));
        newEnd = Mathf.Max(0, Mathf.Min(newText.Length, newEnd));

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
public static class MoveLineUpKeybindPatch
{
    private static KeyBindOptionSO moveLineUpOption = null;

    [HarmonyPostfix]
    [HarmonyPatch("GetAllOptions")]
    static void GetAllOptions_Postfix(ref IEnumerable<OptionSO> __result)
    {
        if (moveLineUpOption == null)
        {
            CreateMoveLineUpOption();
        }

        var optionsList = __result.ToList();
        if (moveLineUpOption != null) optionsList.Add(moveLineUpOption);
        __result = optionsList;
    }

    private static void CreateMoveLineUpOption()
    {
        moveLineUpOption = ScriptableObject.CreateInstance<KeyBindOptionSO>();
        moveLineUpOption.name = "Move Line Up";
        moveLineUpOption.optionName = "Move Line Up";
        moveLineUpOption.tooltip = "Move the current line up";
        moveLineUpOption.defaultValue = "Ctrl Shift UpArrow";
        moveLineUpOption.category = "controls";
        moveLineUpOption.importance = 899f;
        moveLineUpOption.canBeMouseButton = false;

        var existingOptions = Resources.LoadAll<OptionSO>("Options/");
        var keyBindOption = existingOptions.FirstOrDefault(o => o is KeyBindOptionSO);
        if (keyBindOption != null && keyBindOption.optionUI != null)
        {
            moveLineUpOption.optionUI = keyBindOption.optionUI;
            Plugin.Log.LogInfo("Created 'Move Line Up' keybind option");
        }

        var existingValue = OptionHolder.GetOption("Move Line Up", null);
        if (existingValue == null)
        {
            OptionHolder.SetOption("Move Line Up", "Ctrl Shift UpArrow");
        }
    }
}
