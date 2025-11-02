using HarmonyLib;
using UnityEngine;

namespace NomorePiggy.Patches;

[HarmonyPatch(typeof(PiggyBank))]
public class PiggyBankPatch
{
    // Patch the Update method to disable all piggy bank functionality
    [HarmonyPatch("Update")]
    [HarmonyPrefix]
    public static bool UpdatePrefix(PiggyBank __instance)
    {
        // Disable the piggy bank by hiding it
        if (__instance.gameObject.activeSelf)
        {
            __instance.gameObject.SetActive(false);
            Plugin.Log.LogInfo("Piggy bank disabled!");
        }
        
        // Return false to prevent the original Update method from running
        return false;
    }
    
    // Optionally prevent items from being collected
    [HarmonyPatch("EnqueueCollect")]
    [HarmonyPrefix]
    public static bool EnqueueCollectPrefix()
    {
        // Prevent any items from being enqueued to the piggy bank
        return false;
    }
}
