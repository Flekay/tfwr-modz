using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace BetterWorkspace.Patches;

[HarmonyPatch(typeof(CodeInputField))]
public static class MoveLineDownPatch
{
    // Block Shift+Ctrl+DownArrow from extending selection
    [HarmonyPrefix]
    [HarmonyPatch("OnUpdateSelected")]
    static bool OnUpdateSelected_Prefix(CodeInputField __instance, BaseEventData eventData)
    {
        // Check if our move keybind is pressed
        var moveLineDownKeyCombination = OptionHolder.GetKeyCombination("Move Line Down");
        if (moveLineDownKeyCombination.IsKeyPressed(false)) // Check without consume
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

        string[] lines = text.Split('\n');

        if (endLine >= lines.Length - 1)
        {
            return; // Already at bottom
        }

        // Get the line below that we'll swap with
        string lineBelow = lines[endLine + 1];

        // Move the line below to BEFORE the selected block
        // This is simpler than moving the whole block down
        List<string> newLines = new List<string>(lines);
        newLines.RemoveAt(endLine + 1);
        newLines.Insert(startLine, lineBelow);

        string newText = string.Join("\n", newLines);
        inputField.text = newText;

        // Calculate new selection positions
        // The selected lines moved down by the length of the line below + 1 (for \n)
        int offset = lineBelow.Length + 1;

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
