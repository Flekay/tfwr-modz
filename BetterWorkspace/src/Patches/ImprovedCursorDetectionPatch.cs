using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BetterWorkspace.Patches;

/// <summary>
/// Fixes the frustrating issue where tabs and spaces have extremely narrow (1px) hit detection areas.
/// This patch improves cursor position detection by treating whitespace characters with proper width
/// and using the 50% rule: you need to be over 50% of a character to select it.
/// </summary>
[HarmonyPatch(typeof(CodeInputField))]
public static class ImprovedCursorDetectionPatch
{
    /// <summary>
    /// Gets an improved cursor position that properly handles whitespace character hit detection.
    /// Unlike Unity's default behavior where tabs/spaces are ~1px wide, this treats them with their actual rendered width.
    /// </summary>
    public static int GetImprovedCursorPosition(TMP_Text textComponent, Vector3 worldPosition, Camera camera, out bool isInsideText)
    {
        if (textComponent == null || textComponent.textInfo == null || textComponent.textInfo.characterCount == 0)
        {
            isInsideText = false;
            return 0;
        }

        // Force update to ensure textInfo is current
        textComponent.ForceMeshUpdate();

        // Convert world position to local position relative to the text component
        Vector3 localPosition;
        if (camera == null)
        {
            localPosition = textComponent.transform.InverseTransformPoint(worldPosition);
        }
        else
        {
            localPosition = textComponent.transform.InverseTransformPoint(camera.ScreenToWorldPoint(worldPosition));
        }

        float clickX = localPosition.x;
        float clickY = localPosition.y;

        Plugin.Log.LogInfo($"ImprovedCursor: ClickPos=({clickX:F2}, {clickY:F2})");

        TMP_TextInfo textInfo = textComponent.textInfo;
        int characterCount = textInfo.characterCount;
        string fullText = textComponent.text;

        // Find which line we're on
        int closestLine = -1;
        float closestLineDistance = float.MaxValue;

        for (int i = 0; i < textInfo.lineCount; i++)
        {
            TMP_LineInfo lineInfo = textInfo.lineInfo[i];
            if (lineInfo.characterCount == 0) continue;

            // Get line bounds
            float lineTop = lineInfo.ascender;
            float lineBottom = lineInfo.descender;
            float lineMiddle = (lineTop + lineBottom) / 2f;

            float distanceToLine = Mathf.Abs(clickY - lineMiddle);

            // If click is within line bounds, use this line
            if (clickY <= lineTop && clickY >= lineBottom)
            {
                closestLine = i;
                break;
            }

            // Otherwise track closest line
            if (distanceToLine < closestLineDistance)
            {
                closestLineDistance = distanceToLine;
                closestLine = i;
            }
        }

        if (closestLine == -1)
        {
            isInsideText = false;
            Plugin.Log.LogInfo($"  No line found");
            return 0;
        }

        Plugin.Log.LogInfo($"  Line: {closestLine}");

        // Now find the character within the line - use a simpler approach
        TMP_LineInfo targetLine = textInfo.lineInfo[closestLine];
        int firstChar = targetLine.firstCharacterIndex;
        int lastChar = targetLine.lastCharacterIndex;

        // Build a list of character positions for this line
        float bestDistance = float.MaxValue;
        int bestPosition = firstChar < characterCount ? textInfo.characterInfo[firstChar].index : 0;

        for (int i = firstChar; i <= lastChar && i < characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            // Skip newline characters - we don't want to select them
            if (charInfo.character == '\n' || charInfo.character == '\r')
                continue;

            // Calculate character's X position range
            float charStartX = charInfo.origin;
            float charEndX = charStartX; // Initialize

            // For whitespace, use the next character's origin as the end position
            if (charInfo.character == ' ' || charInfo.character == '\t')
            {
                // Look for next non-newline character on the same line
                int nextCharIndex = i + 1;
                bool foundNext = false;
                while (nextCharIndex <= lastChar && nextCharIndex < characterCount)
                {
                    TMP_CharacterInfo nextChar = textInfo.characterInfo[nextCharIndex];
                    if (nextChar.character != '\n' && nextChar.character != '\r')
                    {
                        charEndX = nextChar.origin;
                        foundNext = true;
                        break;
                    }
                    nextCharIndex++;
                }

                // If we didn't find a next non-newline character, use xAdvance
                if (!foundNext)
                {
                    charEndX = charStartX + Mathf.Max(charInfo.xAdvance, textComponent.fontSize * 0.3f);
                }
            }
            else
            {
                // Use actual character width
                charEndX = charInfo.topRight.x;
            }

            // Find the midpoint
            float charMidX = (charStartX + charEndX) * 0.5f;

            // Position before this character
            float distToBefore = Mathf.Abs(clickX - charStartX);
            if (distToBefore < bestDistance)
            {
                bestDistance = distToBefore;
                bestPosition = charInfo.index;
            }

            // Position after this character (only if we're past the midpoint)
            if (clickX >= charMidX)
            {
                float distToAfter = Mathf.Abs(clickX - charEndX);
                if (distToAfter < bestDistance)
                {
                    bestDistance = distToAfter;
                    // Don't go past newlines - stop at the character position
                    bestPosition = charInfo.index + charInfo.stringLength;
                }
            }
        }

        isInsideText = true;

        // Make sure we don't return a position after a newline on this line
        // Find the newline character position for this line (if any)
        for (int i = firstChar; i <= lastChar && i < characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (charInfo.character == '\n' || charInfo.character == '\r')
            {
                // If bestPosition would be after the newline, clamp it to before the newline
                int newlinePos = charInfo.index;
                if (bestPosition > newlinePos)
                {
                    bestPosition = newlinePos;
                    Plugin.Log.LogInfo($"  -> Clamped to before newline: {bestPosition}");
                }
                break;
            }
        }

        Plugin.Log.LogInfo($"  -> Best position: {bestPosition}");
        return bestPosition;
    }

    /// <summary>
    /// Helper method to get cursor position with improved whitespace detection.
    /// This is designed to be a drop-in replacement for the Unity TMP_TextUtilities.GetCursorIndexFromPosition calls.
    /// </summary>
    public static int GetCursorPosition(CodeInputField inputField, PointerEventData eventData)
    {
        var textComponentField = AccessTools.Field(typeof(CodeInputField), "m_TextComponent");
        var textComponent = (TMP_Text)textComponentField.GetValue(inputField);

        if (textComponent == null)
        {
            Plugin.Log.LogWarning("ImprovedCursorDetection: textComponent is null");
            return 0;
        }

        bool isInsideText;
        int stringIndex = GetImprovedCursorPosition(textComponent, eventData.position, eventData.pressEventCamera, out isInsideText);

        Plugin.Log.LogInfo($"ImprovedCursorDetection: Position {eventData.position}, Result: {stringIndex}, InsideText: {isInsideText}");

        // Clamp to valid range
        return Mathf.Clamp(stringIndex, 0, inputField.text.Length);
    }
}
