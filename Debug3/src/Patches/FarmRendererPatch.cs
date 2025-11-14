using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace Debug3.Patches
{
    [HarmonyPatch(typeof(FarmRenderer))]
    public static class FarmRendererPatch
    {
        // Material cache per FarmRenderer instance
        private static readonly Dictionary<FarmRenderer, Dictionary<Color, Material>> materialCache =
            new Dictionary<FarmRenderer, Dictionary<Color, Material>>();

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void Update_Postfix(FarmRenderer __instance,
            Mesh ___droneHighlightArrowMesh,
            bool ___renderDrones,
            Material ___material)
        {
            if (!___renderDrones) return;

            // Initialize material cache for this FarmRenderer if needed
            if (!materialCache.ContainsKey(__instance))
            {
                materialCache[__instance] = new Dictionary<Color, Material>();
            }

            var cache = materialCache[__instance];
            Transform sceneScaler = MainSim.Inst.sceneScaler;
            var debugArrows = MainSimPatch.GetDebugArrows();

            if (debugArrows == null || debugArrows.Count == 0)
                return;

            // Count arrows at each position to determine scaling
            Dictionary<string, int> arrowCountAtPosition = new Dictionary<string, int>();
            foreach (DebugArrow arrow in debugArrows)
            {
                string key = arrow.x + "," + arrow.y;
                if (!arrowCountAtPosition.ContainsKey(key))
                    arrowCountAtPosition[key] = 0;
                arrowCountAtPosition[key]++;
            }

            foreach (DebugArrow debugArrow in debugArrows)
            {
                Vector3 position = new Vector3(-(float)debugArrow.x, (float)debugArrow.y, 0f);
                Quaternion rotation = Quaternion.identity;

                // Scale down if multiple arrows exist at this position
                string posKey = debugArrow.x + "," + debugArrow.y;
                int arrowCount = arrowCountAtPosition[posKey];
                Vector3 scaleVector = arrowCount > 1 ? new Vector3(0.6f, 0.6f, 0.6f) : Vector3.one;

                if (debugArrow.hasDirection)
                {
                    GridDirection dir = (GridDirection)debugArrow.direction;
                    if (dir == GridDirection.North)
                    {
                        rotation = Quaternion.Euler(90, 0, 0);
                        position.y += 1f;
                    }
                    else if (dir == GridDirection.East)
                    {
                        rotation = Quaternion.Euler(0, 90, 90);
                        position.x -= 1f;
                    }
                    else if (dir == GridDirection.South)
                    {
                        rotation = Quaternion.Euler(-90, 0, 0);
                        position.y -= 1f;
                    }
                    else if (dir == GridDirection.West)
                    {
                        rotation = Quaternion.Euler(0, -90, 90);
                        position.x += 1f;
                    }

                    position.z += 1f;
                }
                else
                {
                    // For arrows without direction, position them in the center
                    position.z += 0.5f;
                }

                // Get or create material from cache using color hash
                Color arrowColor = debugArrow.color;

                if (!cache.TryGetValue(arrowColor, out Material arrowMaterial))
                {
                    arrowMaterial = new Material(___material);
                    arrowMaterial.color = arrowColor;
                    cache[arrowColor] = arrowMaterial;
                }

                Matrix4x4 arrowMatrix = sceneScaler.localToWorldMatrix * Matrix4x4.TRS(position, rotation, scaleVector);
                Graphics.DrawMesh(___droneHighlightArrowMesh, arrowMatrix, arrowMaterial, 0);
            }
        }
    }
}
