using HarmonyLib;
using UnityEngine;

namespace BetterTooltips.Patches;

[HarmonyPatch(typeof(FarmRenderer))]
public class FarmRendererPatch
{
    private static Material companionCorrectMaterial;
    private static Material companionIncorrectMaterial;
    private static Material appleTargetMaterial;
    private static Material ghostAppleMaterial;

    [HarmonyPostfix]
    [HarmonyPatch("Update")]
    static void Update_Postfix(FarmRenderer __instance)
    {
        if (MainSim.Inst.hoveredCell.x < 0 || MainSim.Inst.hoveredCell.y < 0)
            return;

        var simField = AccessTools.Field(typeof(MainSim), "sim");
        Simulation sim = (Simulation)simField.GetValue(MainSim.Inst);

        if (sim?.farm?.grid == null)
            return;

        Vector2Int hoveredCell = MainSim.Inst.hoveredCell;

        if (!sim.farm.grid.entities.TryGetValue(hoveredCell, out FarmObject farmObject))
            return;

        var materialField = AccessTools.Field(typeof(FarmRenderer), "material");
        Material baseMaterial = (Material)materialField.GetValue(__instance);

        var hoverMeshField = AccessTools.Field(typeof(FarmRenderer), "hoverMesh");
        Mesh hoverMesh = (Mesh)hoverMeshField.GetValue(__instance);

        Transform sceneScaler = MainSim.Inst.sceneScaler;

        // Apple target highlighting
        if (farmObject is Apple apple)
        {
            IPyObject measureResult = apple.Measure();

            if (measureResult is PyTuple appleTuple && appleTuple.Count == 2)
            {
                if (appleTuple[0] is PyNumber xNum && appleTuple[1] is PyNumber yNum)
                {
                    int targetX = (int)xNum.num;
                    int targetY = (int)yNum.num;

                    if (appleTargetMaterial == null)
                    {
                        appleTargetMaterial = new Material(baseMaterial);
                        appleTargetMaterial.color = new Color(1f, 0.8f, 0f, 0.6f);
                    }

                    Graphics.DrawMesh(
                        hoverMesh,
                        sceneScaler.localToWorldMatrix * Matrix4x4.Translate(new Vector3(-targetX, targetY, 0f)),
                        appleTargetMaterial,
                        0
                    );

                    DrawGhostApple(__instance, new Vector2Int(targetX, targetY), sceneScaler, apple.objectSO);
                }
            }
        }

        // Companion highlighting
        if (farmObject is Growable growable && growable.objectSO.canHaveCompanion)
        {
            IPyObject companionInfo = growable.GetCompanion();
            if (companionInfo is PyTuple tuple && tuple.Count == 2 &&
                tuple[1] is PyTuple posTuple && posTuple.Count == 2 &&
                posTuple[0] is PyNumber xNum && posTuple[1] is PyNumber yNum)
            {
                int compX = (int)xNum.num;
                int compY = (int)yNum.num;

                bool isCorrect = sim.farm.grid.entities.TryGetValue(new Vector2Int(compX, compY), out FarmObject actualCompanion) &&
                                 tuple[0] is FarmObjectSO expectedType &&
                                 actualCompanion.objectSO.objectName == expectedType.objectName;

                if (companionCorrectMaterial == null)
                {
                    companionCorrectMaterial = new Material(baseMaterial);
                    companionCorrectMaterial.color = new Color(0f, 1f, 0f, 0.5f);
                }

                if (companionIncorrectMaterial == null)
                {
                    companionIncorrectMaterial = new Material(baseMaterial);
                    companionIncorrectMaterial.color = new Color(1f, 0f, 0f, 0.5f);
                }

                Graphics.DrawMesh(
                    hoverMesh,
                    sceneScaler.localToWorldMatrix * Matrix4x4.Translate(new Vector3(-compX, compY, 0f)),
                    isCorrect ? companionCorrectMaterial : companionIncorrectMaterial,
                    0
                );
            }
        }
    }

    private static void DrawGhostApple(FarmRenderer farmRenderer, Vector2Int position, Transform sceneScaler, FarmObjectSO appleSO)
    {
        if (appleSO?.meshes == null || appleSO.meshes.Count == 0)
            return;

        if (ghostAppleMaterial == null)
        {
            var materialField = AccessTools.Field(typeof(FarmRenderer), "material");
            Material baseMaterial = (Material)materialField.GetValue(farmRenderer);
            ghostAppleMaterial = new Material(baseMaterial);
        }

        ghostAppleMaterial.color = appleSO.color;

        Vector3 localPos = GridManager.CellToLocal(position);
        Matrix4x4 transform = Matrix4x4.TRS(localPos, Quaternion.identity, Vector3.one);

        Graphics.DrawMesh(
            appleSO.meshes[0],
            sceneScaler.localToWorldMatrix * transform,
            ghostAppleMaterial,
            0
        );
    }
}
