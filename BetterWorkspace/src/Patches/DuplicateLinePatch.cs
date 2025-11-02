using HarmonyLib;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BetterWorkspace.Patches;

[HarmonyPatch(typeof(CodeInputField))]
public static class DuplicateLinePatch
{
    [HarmonyPrefix]
    [HarmonyPatch("LateUpdate")]
    static bool LateUpdate_Prefix(CodeInputField __instance)
    {
        if (!__instance.isFocused)
            return true;

        var duplicateLineKeyCombination = OptionHolder.GetKeyCombination("Duplicate Line");

        if (duplicateLineKeyCombination.IsKeyPressed(true))
        {
            DuplicateLine(__instance);
            return false;
        }

        return true;
    }

    private static void DuplicateLine(CodeInputField inputField)
    {
        var stringPositionField = AccessTools.Property(typeof(CodeInputField), "stringPositionInternal");
        var stringSelectPositionField = AccessTools.Property(typeof(CodeInputField), "stringSelectPositionInternal");

        string text = inputField.text;
        int stringPosition = (int)stringPositionField.GetValue(inputField);
        int stringSelectPosition = (int)stringSelectPositionField.GetValue(inputField);

        int selectionStart = Mathf.Min(stringPosition, stringSelectPosition);
        int selectionEnd = Mathf.Max(stringPosition, stringSelectPosition);

        int startLine = GetLineNumber(text, selectionStart);
        int endLine = GetLineNumber(text, selectionEnd);

        // If selectionEnd is at the start of a line, don't include that line
        if (selectionEnd > 0 && selectionEnd < text.Length && text[selectionEnd - 1] == '\n' && selectionStart != selectionEnd)
        {
            endLine = Mathf.Max(startLine, endLine - 1);
        }

        string[] lines = text.Split('\n');
        if (endLine >= lines.Length) return;

        // Collect lines to duplicate
        var linesToDuplicate = new System.Collections.Generic.List<string>();
        for (int i = startLine; i <= endLine && i < lines.Length; i++)
        {
            linesToDuplicate.Add(lines[i]);
        }

        // Insert duplicated lines below
        var newLines = new System.Collections.Generic.List<string>(lines);
        newLines.InsertRange(endLine + 1, linesToDuplicate);

        string newText = string.Join("\n", newLines);
        inputField.text = newText;

        // Move selection to duplicated lines
        int offset = linesToDuplicate.Sum(line => line.Length + 1); // +1 for each newline
        int newStart = selectionStart + offset;
        int newEnd = selectionEnd + offset;

        stringPositionField.SetValue(inputField, stringPosition < stringSelectPosition ? newStart : newEnd);
        stringSelectPositionField.SetValue(inputField, stringPosition < stringSelectPosition ? newEnd : newStart);
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
public static class DuplicateLineKeybindPatch
{
    private static KeyBindOptionSO duplicateLineOption = null;

    [HarmonyPostfix]
    [HarmonyPatch("GetAllOptions")]
    static void GetAllOptions_Postfix(ref IEnumerable<OptionSO> __result)
    {
        if (duplicateLineOption == null)
        {
            CreateDuplicateLineOption();
        }

        var optionsList = __result.ToList();
        if (duplicateLineOption != null) optionsList.Add(duplicateLineOption);
        __result = optionsList;
    }

    private static void CreateDuplicateLineOption()
    {
        duplicateLineOption = ScriptableObject.CreateInstance<KeyBindOptionSO>();
        duplicateLineOption.name = "Duplicate Line";
        duplicateLineOption.optionName = "Duplicate Line";
        duplicateLineOption.tooltip = "Duplicate the current line or selection";
        duplicateLineOption.defaultValue = "Ctrl D";
        duplicateLineOption.category = "controls";
        duplicateLineOption.importance = 897f;
        duplicateLineOption.canBeMouseButton = false;

        var existingOptions = Resources.LoadAll<OptionSO>("Options/");
        var keyBindOption = existingOptions.FirstOrDefault(o => o is KeyBindOptionSO);
        if (keyBindOption != null && keyBindOption.optionUI != null)
        {
            duplicateLineOption.optionUI = keyBindOption.optionUI;
            Plugin.Log.LogInfo("Created 'Duplicate Line' keybind option (Ctrl+D)");
        }

        var existingValue = OptionHolder.GetOption("Duplicate Line", null);
        if (existingValue == null)
        {
            OptionHolder.SetOption("Duplicate Line", "Ctrl D");
        }
    }
}
