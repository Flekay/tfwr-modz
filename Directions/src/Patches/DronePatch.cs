using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Directions.Patches
{
    [HarmonyPatch(typeof(Drone))]
    public class DronePatch
    {
        private static Dictionary<int, GridDirection> droneRotations = new Dictionary<int, GridDirection>();
        private static Dictionary<int, float> droneVerticalOffsets = new Dictionary<int, float>();
        
        /// <summary>
        /// Calculate max height based on megafarm upgrade level
        /// Level 0 -> 1 drone, Level 1 -> 2, Level 2 -> 4, Level 3 -> 8, etc.
        /// </summary>
        private static float GetMaxHeight(Simulation sim)
        {
            int megafarmLevel = sim.farm.NumUnlocked("megafarm");
            int maxDrones = (int)Math.Pow(2, megafarmLevel);
            return maxDrones * 0.3f;
        }
        
        /// <summary>
        /// Intercept BuiltinFunctions.Move to handle extended directions
        /// </summary>
        [HarmonyPatch(typeof(BuiltinFunctions), "Move")]
        [HarmonyPrefix]
        public static bool Move_Prefix(ref List<IPyObject> parameters, ref Simulation sim, ref Execution exec, ref int droneId, ref double __result)
        {
            if (parameters.Count == 0)
            {
                __result = 0.0;
                return true; // Continue with original
            }

            var dirParam = parameters[0];
            
            // Check if it's our extended direction
            if (dirParam is PyExtendedDirection extDir)
            {
                Drone drone = sim.farm.drones[droneId];
                if (drone == null)
                {
                    __result = 0.0;
                    return false;
                }

                int id = drone.DroneId;
                
                // Initialize rotation if not set
                if (!droneRotations.ContainsKey(id))
                {
                    droneRotations[id] = GridDirection.North;
                }
                
                // Initialize vertical offset if not set
                if (!droneVerticalOffsets.ContainsKey(id))
                {
                    droneVerticalOffsets[id] = 0f;
                }

                switch (extDir.directionType)
                {
                    case DirectionType.Left:
                        // Rotate counter-clockwise
                        droneRotations[id] = RotateDirection(droneRotations[id], -1);
                        UpdateDroneRotation(drone, droneRotations[id]);
                        __result = 0.5; // Small operation cost
                        exec.States[droneId].ReturnValue = new PyBool(true);
                        return false; // Skip original
                        
                    case DirectionType.Right:
                        // Rotate clockwise
                        droneRotations[id] = RotateDirection(droneRotations[id], 1);
                        UpdateDroneRotation(drone, droneRotations[id]);
                        __result = 0.5;
                        exec.States[droneId].ReturnValue = new PyBool(true);
                        return false;
                        
                    case DirectionType.Forward:
                        // Move in current facing direction
                        parameters[0] = new PyGridDirection(droneRotations[id]);
                        return true; // Continue with modified direction
                        
                    case DirectionType.Backward:
                        // Move opposite to facing direction
                        GridDirection reversed = ReverseDirection(droneRotations[id]);
                        parameters[0] = new PyGridDirection(reversed);
                        return true;
                        
                    case DirectionType.Up:
                    {
                        // Move visually up with limit check
                        float maxHeight = GetMaxHeight(sim);
                        float newOffset = droneVerticalOffsets[id] + 0.3f;
                        
                        if (newOffset > maxHeight)
                        {
                            // At max height, return False
                            __result = 0.5;
                            exec.States[droneId].ReturnValue = new PyBool(false);
                            return false;
                        }
                        
                        droneVerticalOffsets[id] = newOffset;
                        __result = 0.5;
                        exec.States[droneId].ReturnValue = new PyBool(true);
                        return false;
                    }
                        
                    case DirectionType.Down:
                    {
                        // Move visually down with limit check (minimum is 0)
                        float newOffset = droneVerticalOffsets[id] - 0.3f;
                        
                        if (newOffset < 0f)
                        {
                            // At ground level, return False
                            __result = 0.5;
                            exec.States[droneId].ReturnValue = new PyBool(false);
                            return false;
                        }
                        
                        droneVerticalOffsets[id] = newOffset;
                        __result = 0.5;
                        exec.States[droneId].ReturnValue = new PyBool(true);
                        return false;
                    }
                        
                    // Absolute directions - use grid direction if available
                    case DirectionType.North:
                    case DirectionType.East:
                    case DirectionType.South:
                    case DirectionType.West:
                        if (extDir.gridDirection.HasValue)
                        {
                            parameters[0] = new PyGridDirection(extDir.gridDirection.Value);
                            droneRotations[id] = extDir.gridDirection.Value; // Update facing
                            return true;
                        }
                        break;
                }
            }
            
            // If it's a regular PyGridDirection, update facing
            if (dirParam is PyGridDirection gridDir)
            {
                int id = sim.farm.drones[droneId]?.DroneId ?? -1;
                if (id >= 0)
                {
                    droneRotations[id] = (GridDirection)gridDir;
                }
            }
            
            return true; // Continue with original
        }

        /// <summary>
        /// Update the drone's physical rotation
        /// </summary>
        private static void UpdateDroneRotation(Drone drone, GridDirection direction)
        {
            // Access the drone's rotation fields using reflection
            var endRotationField = typeof(Drone).GetField("endRotation", BindingFlags.NonPublic | BindingFlags.Instance);
            var startRotationField = typeof(Drone).GetField("startRotation", BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (endRotationField != null && startRotationField != null)
            {
                Quaternion newRotation = GetQuaternionForDirection(direction);
                Quaternion currentRotation = (Quaternion)endRotationField.GetValue(drone);
                
                startRotationField.SetValue(drone, currentRotation);
                endRotationField.SetValue(drone, newRotation);
            }
        }
        
        /// <summary>
        /// Get quaternion rotation for a grid direction
        /// </summary>
        private static Quaternion GetQuaternionForDirection(GridDirection direction)
        {
            switch (direction)
            {
                case GridDirection.North:
                    return Quaternion.identity; // 0 degrees
                case GridDirection.East:
                    return Quaternion.Euler(0, 0, 90); // 90 degrees (right)
                case GridDirection.South:
                    return Quaternion.Euler(0, 0, 180); // 180 degrees
                case GridDirection.West:
                    return Quaternion.Euler(0, 0, -90); // -90 degrees (left)
                default:
                    return Quaternion.identity;
            }
        }

        /// <summary>
        /// Patch GetTransform to apply vertical offset and rotation
        /// </summary>
        [HarmonyPatch("GetTransform")]
        [HarmonyPostfix]
        public static void GetTransform_Postfix(Drone __instance, ref Matrix4x4 __result)
        {
            int id = __instance.DroneId;
            
            // Apply vertical offset if exists
            if (droneVerticalOffsets.ContainsKey(id) && droneVerticalOffsets[id] != 0f)
            {
                // Extract position from matrix
                Vector3 position = __result.GetPosition();
                position.z += droneVerticalOffsets[id];
                
                // Keep rotation from original matrix
                Quaternion rotation = __result.rotation;
                
                // Reconstruct matrix with modified position
                __result = Matrix4x4.TRS(position, rotation, Vector3.one);
            }
        }

        /// <summary>
        /// Rotate a direction by steps (1 = clockwise, -1 = counter-clockwise)
        /// </summary>
        private static GridDirection RotateDirection(GridDirection current, int steps)
        {
            int[] directions = { 0, 1, 2, 3 }; // North, East, South, West
            int currentIndex = (int)current;
            int newIndex = (currentIndex + steps + 4) % 4;
            return (GridDirection)newIndex;
        }

        /// <summary>
        /// Reverse a direction (180 degree turn)
        /// </summary>
        private static GridDirection ReverseDirection(GridDirection dir)
        {
            return (GridDirection)(((int)dir + 2) % 4);
        }
    }
}
