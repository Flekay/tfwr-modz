using HarmonyLib;

namespace StepBro.Patches;

[HarmonyPatch(typeof(InputBoss))]
public class InputBossPatch
{
    // Patch the Update method to add step over and step out hotkey handling
    [HarmonyPostfix]
    [HarmonyPatch("Update")]
    static void Update_Postfix()
    {
        if (OptionHolder.GetKeyCombination("Step Over").IsKeyPressed(true) && MainSim.Inst.StepByStepMode)
        {
            MainSimPatch.NextExecutionStepOver(MainSim.Inst);
        }

        if (OptionHolder.GetKeyCombination("Step Out").IsKeyPressed(true) && MainSim.Inst.StepByStepMode)
        {
            MainSimPatch.NextExecutionStepOut(MainSim.Inst);
        }

        if (OptionHolder.GetKeyCombination("Step to Function").IsKeyPressed(true) && MainSim.Inst.StepByStepMode)
        {
            MainSimPatch.NextExecutionStepToFunction(MainSim.Inst);
        }
    }
}
