using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;

namespace SubtleDroneMovement.Patches
{
    [HarmonyPatch(typeof(Drone))]
    public static class DronePatch
    {
        // Store per-drone random offsets for organic variation
        private static Dictionary<int, Vector3> droneOffsets = new Dictionary<int, Vector3>();
        private static Dictionary<int, float> droneTimeOffsets = new Dictionary<int, float>();
        private static bool hasLogged = false;

        [HarmonyPatch("GetTransform")]
        [HarmonyPostfix]
        public static void GetTransform_Postfix(Drone __instance, ref Matrix4x4 __result)
        {
            try
            {
                if (!hasLogged)
                {
                    Plugin.Log.LogInfo("GetTransform patch is running!");
                    hasLogged = true;
                }

                // Get drone state using reflection
                var droneStateField = typeof(Drone).GetField("droneState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (droneStateField == null)
                {
                    Plugin.Log.LogWarning("Could not find droneState field");
                    return;
                }

                var droneState = droneStateField.GetValue(__instance);
                if (droneState == null) return;

                // Convert enum to int to check if idle (idle = 0)
                int stateValue = (int)droneState;
                if (stateValue != 0) return; // Only apply to idle state

                int droneId = __instance.DroneId;

                // Initialize random offset for this drone if not exists
                if (!droneOffsets.ContainsKey(droneId))
                {
                    droneOffsets[droneId] = new Vector3(
                        UnityEngine.Random.Range(0f, 100f),
                        UnityEngine.Random.Range(0f, 100f),
                        UnityEngine.Random.Range(0f, 100f)
                    );
                    droneTimeOffsets[droneId] = UnityEngine.Random.Range(0f, 1000f);
                    Plugin.Log.LogInfo($"Initialized movement for drone {droneId}");
                }

                Vector3 offset = droneOffsets[droneId];
                float timeOffset = droneTimeOffsets[droneId];
                float time = Time.time * Plugin.MovementSpeed.Value + timeOffset;

                // Calculate organic drone-like movement with multiple frequency layers
                // Quick micro-adjustments (like propeller corrections)
                float microX = Mathf.Sin(time * 3.5f + offset.x) * Plugin.MovementAmplitude.Value * 0.3f;
                float microY = Mathf.Sin(time * 4.2f + offset.y) * Plugin.MovementAmplitude.Value * 0.3f;

                // Medium frequency drift
                float mediumX = Mathf.Sin(time * 0.8f + offset.x * 0.5f) * Plugin.MovementAmplitude.Value * 0.5f;
                float mediumY = Mathf.Sin(time * 0.6f + offset.y * 0.5f) * Plugin.MovementAmplitude.Value * 0.5f;

                // Slow overall sway
                float slowX = Mathf.Sin(time * 0.3f + offset.x * 0.2f) * Plugin.MovementAmplitude.Value * 0.2f;
                float slowY = Mathf.Sin(time * 0.25f + offset.y * 0.2f) * Plugin.MovementAmplitude.Value * 0.2f;

                // Combine for complex organic movement
                float xMove = microX + mediumX + slowX;
                float yMove = microY + mediumY + slowY;

                // Vertical bobbing - subtle altitude corrections
                float zMove = 0f;
                if (Plugin.EnableVerticalBob.Value)
                {
                    // Quick altitude micro-adjustments
                    float microZ = Mathf.Sin(time * 5.0f + offset.z) * Plugin.MovementAmplitude.Value * 0.15f;
                    // Slower breathing motion
                    float slowZ = Mathf.Sin(time * 0.4f + offset.z * 0.3f) * Plugin.MovementAmplitude.Value * 0.35f;
                    zMove = microZ + slowZ;
                }

                // Extract current position from matrix
                Vector3 currentPos = __result.GetPosition();

                // Apply organic movement offset
                currentPos.x += xMove;
                currentPos.y += yMove;
                currentPos.z += zMove;

                // Calculate subtle rotation - drone tilts as it corrects position
                Quaternion currentRot = __result.rotation;

                // Quick micro-tilts (propeller balance)
                float microTiltX = Mathf.Sin(time * 4.5f + offset.x) * Plugin.RotationAmplitude.Value * 0.4f;
                float microTiltY = Mathf.Sin(time * 3.8f + offset.y) * Plugin.RotationAmplitude.Value * 0.4f;

                // Slower drift rotation
                float driftTiltX = Mathf.Sin(time * 0.7f + offset.x * 0.6f) * Plugin.RotationAmplitude.Value * 0.6f;
                float driftTiltY = Mathf.Sin(time * 0.5f + offset.y * 0.6f) * Plugin.RotationAmplitude.Value * 0.6f;

                float rotationX = microTiltX + driftTiltX;
                float rotationY = microTiltY + driftTiltY;

                Quaternion organicRotation = Quaternion.Euler(rotationX, rotationY, 0f);
                Quaternion finalRotation = currentRot * organicRotation;

                // Rebuild the matrix with new position and rotation
                __result = Matrix4x4.TRS(currentPos, finalRotation, Vector3.one);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error in GetTransform_Postfix: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
