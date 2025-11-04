using HarmonyLib;
using System;
using UnityEngine;

namespace BetterWorkspace.Patches;

[HarmonyPatch(typeof(CodeWindow))]
public static class VariableTooltipPatch
{
    private static string lastTooltipValue = "";
    private static bool isVariableTooltip = false;

    // Patch GetTooltipInfo to add "Right-click to copy" text
    [HarmonyPostfix]
    [HarmonyPatch("GetTooltipInfo")]
    static void GetTooltipInfo_Postfix(CodeWindow __instance, ref TooltipInfo __result)
    {
        if (__result == null)
        {
            isVariableTooltip = false;
            lastTooltipValue = "";
            return;
        }

        // Access the private field hoverWordStart and hoverWordEnd
        var hoverWordStartField = AccessTools.Field(typeof(CodeWindow), "hoverWordStart");
        var hoverWordEndField = AccessTools.Field(typeof(CodeWindow), "hoverWordEnd");
        
        int hoverWordStart = (int)hoverWordStartField.GetValue(__instance);
        int hoverWordEnd = (int)hoverWordEndField.GetValue(__instance);

        if (hoverWordStart < 0 || hoverWordEnd >= __instance.CodeInput.text.Length)
        {
            isVariableTooltip = false;
            lastTooltipValue = "";
            return;
        }

        string text = __instance.CodeInput.text.Substring(hoverWordStart, hoverWordEnd - hoverWordStart + 1);
        
        // Check if this is a variable tooltip (starts with backtick)
        if (__result.text.StartsWith("`") && __result.text.EndsWith("`"))
        {
            // Extract the value (remove backticks)
            lastTooltipValue = __result.text.Substring(1, __result.text.Length - 2);
            isVariableTooltip = true;

            // Add "Right-click to copy" instruction
            __result = new TooltipInfo(
                __result.text + "\n\n`Right-click to copy`",
                __result.delay,
                __result.fixedPosition,
                __result.anchor,
                __result.docs
            )
            {
                itemBlock = __result.itemBlock
            };
        }
        else
        {
            isVariableTooltip = false;
            lastTooltipValue = "";
        }
    }

    // Patch Update to detect right-click and copy value to clipboard
    [HarmonyPostfix]
    [HarmonyPatch("Update")]
    static void Update_Postfix(CodeWindow __instance)
    {
        // Check if tooltip is showing and right mouse button was clicked
        if (isVariableTooltip && !string.IsNullOrEmpty(lastTooltipValue) && Input.GetKeyDown(KeyCode.Mouse1))
        {
            // Check if mouse is over the code window
            if (__instance.IsPointerOverCodeInput())
            {
                // Verify the tooltip is actually showing
                var workspace = __instance.workspace;
                if (workspace != null && workspace.tooltip != null && workspace.tooltip.Info != null)
                {
                    // Copy to clipboard
                    GUIUtility.systemCopyBuffer = lastTooltipValue;
                    Plugin.Log.LogInfo($"Copied variable value to clipboard: {lastTooltipValue}");

                    // Close the tooltip after copying
                    workspace.tooltip.CloseTooltip();
                    
                    // Reset state
                    isVariableTooltip = false;
                    lastTooltipValue = "";
                }
            }
        }
    }
    
    // Reset state when tooltip is gone
    [HarmonyPostfix]
    [HarmonyPatch("TooltipGone")]
    static void TooltipGone_Postfix()
    {
        isVariableTooltip = false;
        lastTooltipValue = "";
    }
}
