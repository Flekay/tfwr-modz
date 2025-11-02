using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Debug3.Patches
{
    [HarmonyPatch(typeof(FarmRenderer))]
    public static class FarmRendererPatch
    {
        private static readonly Dictionary<FarmRenderer, MaterialSet> materialsCache = new Dictionary<FarmRenderer, MaterialSet>();

        private class MaterialSet
        {
            public Material North;
            public Material East;
            public Material South;
            public Material West;
            public Material Default;
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void Update_Prefix(FarmRenderer __instance, Material ___material)
        {
            // Initialize materials on first Update call
            if (!materialsCache.ContainsKey(__instance))
            {
                var materials = new MaterialSet
                {
                    North = new Material(___material) { color = Color.blue },
                    East = new Material(___material) { color = Color.red },
                    South = new Material(___material) { color = Color.green },
                    West = new Material(___material) { color = Color.yellow },
                    Default = new Material(___material) { color = Color.white }
                };

                materialsCache[__instance] = materials;
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void Update_Postfix(FarmRenderer __instance,
            Mesh ___droneHighlightArrowMesh,
            bool ___renderDrones)
        {
            if (!___renderDrones || !materialsCache.ContainsKey(__instance)) return;

            Transform sceneScaler = MainSim.Inst.sceneScaler;
            var debugArrows = MainSim.Inst.GetDebugArrows();
            var materials = materialsCache[__instance];

            foreach (DebugArrow debugArrow in debugArrows)
            {
                Vector3 position = new Vector3(-(float)debugArrow.x, (float)debugArrow.y, 0f);
                Quaternion rotation = Quaternion.identity;
                Material arrowMaterial = materials.Default;

                if (debugArrow.hasDirection)
                {
                    GridDirection dir = (GridDirection)debugArrow.direction;
                    if (dir == GridDirection.North)
                    {
                        rotation = Quaternion.Euler(90, 0, 0);
                        position.y += 1f;
                        arrowMaterial = materials.North;
                    }
                    else if (dir == GridDirection.East)
                    {
                        rotation = Quaternion.Euler(0, 90, 90);
                        position.x -= 1f;
                        arrowMaterial = materials.East;
                    }
                    else if (dir == GridDirection.South)
                    {
                        rotation = Quaternion.Euler(-90, 0, 0);
                        position.y -= 1f;
                        arrowMaterial = materials.South;
                    }
                    else if (dir == GridDirection.West)
                    {
                        rotation = Quaternion.Euler(0, -90, 90);
                        position.x += 1f;
                        arrowMaterial = materials.West;
                    }

                    position.z += 1f;
                }

                Matrix4x4 arrowMatrix = sceneScaler.localToWorldMatrix * Matrix4x4.TRS(position, rotation, Vector3.one);
                Graphics.DrawMesh(___droneHighlightArrowMesh, arrowMatrix, arrowMaterial, 0);
            }
        }
    }
}
