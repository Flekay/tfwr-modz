using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Reset.Patches
{
    [HarmonyPatch(typeof(ResourceManager))]
    public static class ItemSOPatch
    {
        private static ItemSO goldenPiggyItem;

        [HarmonyPatch("GetItem")]
        [HarmonyPostfix]
        public static void GetItem_Postfix(int itemId, ref ItemSO __result)
        {
            int goldenPiggyId = StringIds.GetItemId("piggy");
            if (itemId == goldenPiggyId && __result == null)
            {
                if (goldenPiggyItem == null)
                {
                    goldenPiggyItem = ScriptableObject.CreateInstance<ItemSO>();
                    goldenPiggyItem.itemName = "piggy";
                    goldenPiggyItem.itemId = goldenPiggyId;
                    goldenPiggyItem.priority = 1000;
                }
                __result = goldenPiggyItem;
            }
        }

        [HarmonyPatch("GetAllItems")]
        [HarmonyPostfix]
        public static void GetAllItems_Postfix(ref IEnumerable<ItemSO> __result)
        {
            if (goldenPiggyItem == null)
            {
                int goldenPiggyId = StringIds.GetItemId("piggy");
                goldenPiggyItem = ScriptableObject.CreateInstance<ItemSO>();
                goldenPiggyItem.itemName = "piggy";
                goldenPiggyItem.itemId = goldenPiggyId;
                goldenPiggyItem.priority = 1000;
            }

            var list = __result.ToList();
            if (!list.Any(item => item != null && item.itemName == "piggy"))
            {
                list.Add(goldenPiggyItem);
                __result = list;
            }
        }
    }
}
