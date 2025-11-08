using HarmonyLib;
using System.Text.RegularExpressions;

namespace Directions.Patches
{
    [HarmonyPatch(typeof(CodeUtilities))]
    public class CodeUtilitiesPatch
    {
        [HarmonyPatch(nameof(CodeUtilities.SyntaxColor2))]
        [HarmonyPrefix]
        public static bool SyntaxColor2_Prefix(ref string code, string searchWord, int searchIndex, ref string __result)
        {
            // Modified regex pattern that includes all direction keywords in the builtin group
            string pattern = "(?<comment>#.*)|(?<string>(['\"])(.*?)\\1)|(?<keyword>\\b(?:in|for|while|def|if|else|elif|return|pass|break|continue|and|or|not|global|import|from)\\b)|(?<function>\\b[a-zA-Z]\\w*(?=\\())|(?<builtin>\\b(?:True|False|None|North|East|South|West|Entities|Items|Grounds|Unlocks|Leaderboards|Hats|Directions|Left|Right|Forward|Backward|Up|Down)\\b|(?<=[a-zA-Z]\\.)\\w*)|(?<number>\\b(?:\\d*\\.)?\\d+\\b)";
            
            string searchPattern = string.IsNullOrEmpty(searchWord) 
                ? pattern 
                : ("(?<search>(?i:" + Regex.Escape(searchWord) + ")(?-i))|" + pattern);
            
            __result = Regex.Replace(code, searchPattern, delegate(Match m)
            {
                if (!string.IsNullOrEmpty(searchWord) && m.Groups["search"].Success)
                {
                    int index = m.Index;
                    if (searchIndex >= 0 && index == searchIndex)
                    {
                        return "<mark=#ffffff10>" + m.Value + "</mark>";
                    }
                    return "<mark=#ffffff05>" + m.Value + "</mark>";
                }
                else
                {
                    string text = m.Value;
                    if (!string.IsNullOrEmpty(searchWord))
                    {
                        text = Regex.Replace(text, Regex.Escape(searchWord), delegate(Match match)
                        {
                            int num = m.Index + match.Index;
                            if (searchIndex >= 0 && num == searchIndex)
                            {
                                return "<mark=#ffffff14>" + match.Value + "</mark>";
                            }
                            return "<mark=#ffffff09>" + match.Value + "</mark>";
                        }, RegexOptions.IgnoreCase);
                    }
                    if (m.Groups["comment"].Success)
                    {
                        return "<color=#666666>" + text + "</color>";
                    }
                    if (m.Groups["string"].Success)
                    {
                        return "<color=#8f5d28>" + text + "</color>";
                    }
                    if (m.Groups["keyword"].Success)
                    {
                        return "<color=#9e7124>" + text + "</color>";
                    }
                    if (m.Groups["function"].Success)
                    {
                        return "<color=#e6c87c>" + text + "</color>";
                    }
                    if (m.Groups["builtin"].Success)
                    {
                        return "<color=#7f8a36>" + text + "</color>";
                    }
                    if (m.Groups["number"].Success)
                    {
                        return "<color=#7f8a36>" + text + "</color>";
                    }
                    return text;
                }
            });
            
            return false; // Skip original method
        }
    }
}
