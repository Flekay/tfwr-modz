using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using System.Reflection;

namespace Debug3.Patches
{
    [HarmonyPatch(typeof(CodeWindow))]
    public static class CodeWindowPatch
    {
        /// <summary>
        /// Postfix patch for GetSubWordList to add support for colors.member autocomplete
        /// </summary>
        [HarmonyPatch("GetSubWordList", typeof(string))]
        [HarmonyPostfix]
        public static void GetSubWordList_Postfix(string domain, ref List<string> __result)
        {
            if (domain == "colors")
            {
                // Return all color names, custom function, and wrap method
                __result = new List<string>
                {
                    "red",
                    "green",
                    "blue",
                    "yellow",
                    "cyan",
                    "magenta",
                    "white",
                    "black",
                    "orange",
                    "purple",
                    "pink",
                    "lime",
                    "teal",
                    "navy",
                    "maroon",
                    "olive",
                    "silver",
                    "gray",
                    "custom",
                    "wrap"
                };
            }
        }
    }
}
