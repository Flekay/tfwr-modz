using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace BetterWorkspace.Patches;

[HarmonyPatch(typeof(CodeInputField))]
public static class MoveLineUpPatch
{
    // Block Shift+Ctrl+UpArrow from extending selection
    [HarmonyPrefix]
    [HarmonyPatch("OnUpdateSelected")]
    static bool OnUpdateSelected_Prefix(CodeInputField __instance, BaseEventData eventData)
    {
        // Check if our move keybind is pressed
        var moveLineUpKeyCombination = OptionHolder.GetKeyCombination("Move Line Up");
        if (moveLineUpKeyCombination.IsKeyPressed(false)) // Check without consume
        {
            // Block OnUpdateSelected so it doesn't process Shift+Arrow navigation
            return false;
        }
        return true;
    }

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

        int selectionStart = Mathf.Min(stringPosition, stringSelectPosition);
        int selectionEnd = Mathf.Max(stringPosition, stringSelectPosition);

        // Get all selected lines
        int startLine = GetLineNumber(text, selectionStart);
        int endLine = GetLineNumber(text, selectionEnd);

        // If selection ends at the start of a line (just after \n), don't include that line
        if (selectionEnd > 0 && selectionEnd < text.Length &&
            text[selectionEnd - 1] == '\n' && selectionStart != selectionEnd)
        {
            endLine = Mathf.Max(startLine, endLine - 1);
        }

        if (startLine == 0)
        {
            return; // Already at top
        }

        string[] lines = text.Split('\n');
        if (endLine >= lines.Length) return;

        // Get the line above that we'll swap with
        string lineAbove = lines[startLine - 1];

        // Move the line above to AFTER the selected block
        // This is simpler than moving the whole block up
        List<string> newLines = new List<string>(lines);
        newLines.RemoveAt(startLine - 1);
        newLines.Insert(endLine, lineAbove);

        string newText = string.Join("\n", newLines);
        inputField.text = newText;

        // Calculate new selection positions
        // The selected lines moved up by the length of the line above + 1 (for \n)
        int offset = -(lineAbove.Length + 1);

        int newStart = selectionStart + offset;
        int newEnd = selectionEnd + offset;

        newStart = Mathf.Max(0, Mathf.Min(newText.Length, newStart));
        newEnd = Mathf.Max(0, Mathf.Min(newText.Length, newEnd));

        stringPositionField.SetValue(inputField, stringPosition < stringSelectPosition ? newStart : newEnd);
        stringSelectPositionField.SetValue(inputField, stringPosition < stringSelectPosition ? newEnd : newStart);

        inputField.ForceLabelUpdate();
        ClickSelectionPatch.ForceCaretUpdate(inputField);
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
