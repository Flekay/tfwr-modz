using HarmonyLib;
using System.IO;
using System.Reflection;
using System.Linq;

namespace TcpSocket.Patches
{
    [HarmonyPatch(typeof(MainSim))]
    public static class MainSimPatch
    {
        private static string EscapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;
            return str.Replace("\\", "\\\\")
                     .Replace("\"", "\\\"")
                     .Replace("\n", "\\n")
                     .Replace("\r", "\\r")
                     .Replace("\t", "\\t");
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void Update_Postfix(MainSim __instance)
        {
            // Check for pending commands from the TCP server
            if (Plugin.Instance?.ServerManager != null && Plugin.Instance.ServerManager.HasPendingCommands())
            {
                string commandData = Plugin.Instance.ServerManager.DequeueCommand();
                if (!string.IsNullOrEmpty(commandData))
                {
                    ProcessCommand(__instance, commandData);
                }
            }
        }

        private static void ProcessCommand(MainSim mainSim, string commandData)
        {
            // First, check if it's a QUERY command (format: QUERY|id|type|data)
            if (commandData.StartsWith("QUERY|"))
            {
                string[] parts = commandData.Split(new[] { '|' }, 4);
                if (parts.Length >= 3)
                {
                    string queryId = parts[1];
                    string queryType = parts[2];
                    string data = parts.Length > 3 ? parts[3] : "";

                    try
                    {
                        string result = ExecuteQuery(mainSim, queryType, data);
                        Plugin.Instance?.ServerManager?.SetQueryResult(queryId, result);
                    }
                    catch (System.Exception ex)
                    {
                        Plugin.Instance?.ServerManager?.SetQueryResult(queryId, $"ERROR: {ex.Message}");
                    }
                }
                return;
            }

            // Regular commands (format: commandType|data)
            string[] regularParts = commandData.Split(new[] { '|' }, 2);
            string commandType = regularParts[0];
            string commandData2 = regularParts.Length > 1 ? regularParts[1] : "";

            try
            {
                switch (commandType)
                {
                    case "runstart":
                        ExecuteRunStart(mainSim, commandData2);
                        break;

                    case "stop":
                        ExecuteStop(mainSim);
                        break;

                    case "camera":
                        ExecuteCamera(mainSim, commandData2);
                        break;

                    case "hideui":
                        ExecuteHideUI(mainSim, commandData2);
                        break;

                    case "setcode":
                        ExecuteSetCode(mainSim, commandData2);
                        break;

                    case "createfile":
                        ExecuteCreateFile(mainSim, commandData2);
                        break;

                    case "deletefile":
                        ExecuteDeleteFile(mainSim, commandData2);
                        break;

                    case "savegame":
                        ExecuteSaveGame(mainSim);
                        break;

                    case "loadlevel":
                        ExecuteLoadLevel(mainSim, commandData2);
                        break;

                    case "exitgame":
                        ExecuteExitGame(mainSim);
                        break;

                    case "clearoutput":
                        ExecuteClearOutput(mainSim);
                        break;

                    case "stepbystep":
                        ExecuteStepByStep(mainSim, commandData2);
                        break;

                    case "movewindow":
                        ExecuteMoveWindow(mainSim, commandData2);
                        break;

                    default:
                        Plugin.Log.LogWarning($"Unknown command type: {commandType}");
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error processing command {commandType}: {ex.Message}");
            }
        }

        // Execute query commands and return result
        private static string ExecuteQuery(MainSim mainSim, string queryType, string data)
        {
            switch (queryType)
            {
                case "getwindows":
                    return ExecuteGetWindows(mainSim);

                case "getcode":
                    return ExecuteGetCode(mainSim, data);

                case "getoutput":
                    return ExecuteGetOutput(mainSim);

                case "getfarm":
                    return ExecuteGetFarm(mainSim);

                case "getunlocks":
                    return ExecuteGetUnlocks(mainSim);

                case "getwindowposition":
                    return ExecuteGetWindowPosition(mainSim, data);

                default:
                    return $"ERROR: Unknown query type: {queryType}";
            }
        }

        private static void ExecuteRunStart(MainSim mainSim, string windowName)
        {
            try
            {
                Plugin.Log.LogInfo($"Executing runstart for window: {windowName}");

                var workspace = mainSim.workspace;
                if (workspace == null)
                {
                    Plugin.Log.LogError("Workspace is null");
                    return;
                }

                // Get code window by name
                CodeWindow codeWindow = null;
                if (workspace.codeWindows.TryGetValue(windowName, out codeWindow))
                {
                    Plugin.Log.LogInfo($"Found code window: {windowName}");
                }
                else
                {
                    Plugin.Log.LogError($"Code window '{windowName}' not found. Available windows: {string.Join(", ", workspace.codeWindows.Keys)}");
                    return;
                }

                // Use the internal method to run the script
                codeWindow.PressExecuteOrStop();
                Plugin.Log.LogInfo("Started execution via PressExecuteOrStop");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error executing runstart: {ex.Message}");
                Plugin.Log.LogError($"Stack trace: {ex.StackTrace}");
            }
        }

        private static void ExecuteStop(MainSim mainSim)
        {
            try
            {
                Plugin.Log.LogInfo("Stopping execution");
                mainSim.StopMainExecution();
                Plugin.Log.LogInfo("Execution stopped");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error stopping execution: {ex.Message}");
            }
        }

        private static void ExecuteCamera(MainSim mainSim, string data)
        {
            try
            {
                var workspace = mainSim.workspace;
                if (workspace == null)
                {
                    Plugin.Log.LogError("Workspace is null");
                    return;
                }

                if (data.ToLower() == "reset")
                {
                    // Reset camera to default position and zoom
                    workspace.container.localPosition = UnityEngine.Vector3.zero;
                    workspace.cameraController.zoom = 1f;
                    workspace.zoomContainer.localScale = UnityEngine.Vector3.one;
                    Plugin.Log.LogInfo("Camera position and zoom reset");
                    return;
                }

                string[] coords = data.Split(' ');

                // Set position if x y provided
                if (coords.Length >= 2)
                {
                    if (float.TryParse(coords[0], out float x) && float.TryParse(coords[1], out float y))
                    {
                        workspace.container.localPosition = new UnityEngine.Vector3(-x, y, 0);
                        Plugin.Log.LogInfo($"Camera moved to ({x}, {y})");
                    }
                }

                // Set zoom if provided (use built-in Zoom for proper scaling)
                if (coords.Length >= 3 && float.TryParse(coords[2], out float zoom))
                {
                    workspace.cameraController.zoom = UnityEngine.Mathf.Clamp(zoom, 0.1f, 10f);
                    workspace.zoomContainer.localScale = UnityEngine.Vector3.one * workspace.cameraController.zoom;
                    workspace.container.GetComponent<ContainerScaler>().UpdateMarginSize();
                    Plugin.Log.LogInfo($"Camera zoom set to {workspace.cameraController.zoom}");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error moving camera: {ex.Message}");
            }
        }

        private static void ExecuteHideUI(MainSim mainSim, string data)
        {
            try
            {
                bool hide = data.ToLower() == "true";

                // Access workspace to toggle UI canvases
                var workspace = mainSim.workspace;
                if (workspace != null)
                {
                    // Get uiCanvases field via reflection
                    var uiCanvasesField = workspace.GetType().GetField("uiCanvases", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (uiCanvasesField != null)
                    {
                        var uiCanvases = uiCanvasesField.GetValue(workspace) as UnityEngine.GameObject[];
                        if (uiCanvases != null)
                        {
                            foreach (var canvas in uiCanvases)
                            {
                                if (canvas != null)
                                {
                                    canvas.SetActive(!hide);
                                }
                            }
                        }
                    }

                    // Also toggle the workspace container itself
                    if (workspace.container != null)
                    {
                        workspace.container.gameObject.SetActive(!hide);
                    }

                    Plugin.Log.LogInfo($"UI {(hide ? "hidden" : "shown")}");
                }
                else
                {
                    Plugin.Log.LogError("Workspace is null");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error toggling UI: {ex.Message}");
            }
        }

        private static void ExecuteSetCode(MainSim mainSim, string data)
        {
            try
            {
                string[] parts = data.Split(new[] { '|' }, 2);
                if (parts.Length < 2)
                {
                    Plugin.Log.LogError("SetCode requires window name and code");
                    return;
                }

                string windowName = parts[0];
                string code = parts[1];

                var workspace = mainSim.workspace;
                if (workspace == null)
                {
                    Plugin.Log.LogError("Workspace is null");
                    return;
                }

                CodeWindow codeWindow = null;
                if (workspace.codeWindows.TryGetValue(windowName, out codeWindow))
                {
                    codeWindow.Load(code);
                    codeWindow.Parse();
                    Plugin.Log.LogInfo($"Code updated in window: {windowName}");
                }
                else
                {
                    Plugin.Log.LogError($"Code window '{windowName}' not found");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error setting code: {ex.Message}");
            }
        }

        private static void ExecuteCreateFile(MainSim mainSim, string windowName)
        {
            try
            {
                var workspace = mainSim.workspace;
                if (workspace == null)
                {
                    Plugin.Log.LogError("Workspace is null");
                    return;
                }

                // Check if window already exists
                if (workspace.codeWindows.ContainsKey(windowName))
                {
                    Plugin.Log.LogWarning($"Window '{windowName}' already exists");
                    return;
                }

                // Create new code window
                workspace.OpenCodeWindow(windowName, "", UnityEngine.Vector2.zero);
                Plugin.Log.LogInfo($"Created new window: {windowName}");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error creating file: {ex.Message}");
            }
        }

        private static void ExecuteDeleteFile(MainSim mainSim, string windowName)
        {
            try
            {
                var workspace = mainSim.workspace;
                if (workspace == null)
                {
                    Plugin.Log.LogError("Workspace is null");
                    return;
                }

                // Check if window exists
                CodeWindow codeWindow = null;
                if (!workspace.codeWindows.TryGetValue(windowName, out codeWindow))
                {
                    Plugin.Log.LogError($"Window '{windowName}' not found");
                    return;
                }

                // Get the Window component
                var window = codeWindow.GetComponent<Window>();
                if (window != null)
                {
                    // Call the close method
                    window.Close();
                    Plugin.Log.LogInfo($"Deleted window: {windowName}");
                }
                else
                {
                    Plugin.Log.LogError($"Could not get Window component for '{windowName}'");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error deleting file: {ex.Message}");
            }
        }

        private static void ExecuteSaveGame(MainSim mainSim)
        {
            try
            {
                Saver.Save(mainSim);
                Plugin.Log.LogInfo("Game saved");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error saving game: {ex.Message}");
            }
        }

        private static void ExecuteLoadLevel(MainSim mainSim, string levelName)
        {
            try
            {
                Plugin.Log.LogInfo($"[DEBUG] ExecuteLoadLevel called with levelName: {levelName}");

                // LoadSave just sets the option, then we need to reload the scene
                // This mimics what Menu.Start() does
                OptionHolder.SetOption("activeSave", levelName);
                Plugin.Log.LogInfo($"[DEBUG] Set activeSave option to: {levelName}");

                // Stop file watcher before scene reload
                Saver.StopFileWatcher();
                Plugin.Log.LogInfo("[DEBUG] Stopped file watcher");

                // Reload scene 0 which will load the new save
                UnityEngine.SceneManagement.SceneManager.LoadScene(0);
                Plugin.Log.LogInfo($"[DEBUG] Scene reload triggered, will load level: {levelName}");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[DEBUG] Error loading level: {ex.Message}");
                Plugin.Log.LogError($"[DEBUG] Stack trace: {ex.StackTrace}");
            }
        }

        private static void ExecuteGetLevels(MainSim mainSim)
        {
            try
            {
                // Get saves from directory
                string savesPath = UnityEngine.Application.persistentDataPath + "/Saves";
                if (System.IO.Directory.Exists(savesPath))
                {
                    var directories = System.IO.Directory.GetDirectories(savesPath);
                    var levelNames = new System.Collections.Generic.List<string>();
                    foreach (var dir in directories)
                    {
                        levelNames.Add(new System.IO.DirectoryInfo(dir).Name);
                    }
                    var levels = System.String.Join(", ", levelNames);
                    Plugin.Log.LogInfo($"Available levels: {levels}");
                }
                else
                {
                    Plugin.Log.LogError("Saves directory not found");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error getting levels: {ex.Message}");
            }
        }

        private static string ExecuteGetWindows(MainSim mainSim)
        {
            try
            {
                var workspace = mainSim.workspace;
                if (workspace == null)
                {
                    return "ERROR: Workspace is null";
                }

                var windowNames = new System.Collections.Generic.List<string>();
                foreach (var key in workspace.codeWindows.Keys)
                {
                    windowNames.Add(key);
                }

                // Return as JSON array
                return "[\"" + string.Join("\",\"", windowNames.ToArray()) + "\"]";
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error getting windows: {ex.Message}");
                return $"ERROR: {ex.Message}";
            }
        }

        private static string ExecuteGetCode(MainSim mainSim, string windowName)
        {
            try
            {
                var workspace = mainSim.workspace;
                if (workspace == null)
                {
                    return "ERROR: Workspace is null";
                }

                CodeWindow codeWindow = null;
                if (workspace.codeWindows.TryGetValue(windowName, out codeWindow))
                {
                    // Access the codeInput field (TMPro InputField)
                    var codeInputField = typeof(CodeWindow).GetField("codeInput", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (codeInputField != null)
                    {
                        var inputComponent = codeInputField.GetValue(codeWindow);
                        if (inputComponent != null)
                        {
                            // Get text property from TMP_InputField
                            var textProperty = inputComponent.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
                            if (textProperty != null)
                            {
                                string code = textProperty.GetValue(inputComponent) as string;
                                return code ?? "";
                            }
                        }
                    }

                    return "ERROR: Could not access codeInput field";
                }
                else
                {
                    return $"ERROR: Window '{windowName}' not found";
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error getting code: {ex.Message}");
                return $"ERROR: {ex.Message}";
            }
        }        private static void ExecuteExitGame(MainSim mainSim)
        {
            try
            {
                UnityEngine.Application.Quit();
                Plugin.Log.LogInfo("Game exit requested");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error exiting game: {ex.Message}");
            }
        }

        private static string ExecuteGetOutput(MainSim mainSim)
        {
            try
            {
                string output = Logger.GetOutputString();
                return output ?? "";
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error getting output: {ex.Message}");
                return $"ERROR: {ex.Message}";
            }
        }

        private static void ExecuteClearOutput(MainSim mainSim)
        {
            try
            {
                Logger.Clear();
                Plugin.Log.LogInfo("Output cleared");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error clearing output: {ex.Message}");
            }
        }

        private static string ExecuteGetFarm(MainSim mainSim)
        {
            try
            {
                // Access private sim field using reflection
                var simField = typeof(MainSim).GetField("sim", BindingFlags.NonPublic | BindingFlags.Instance);
                if (simField == null)
                {
                    return "ERROR: Could not access sim field";
                }

                var sim = simField.GetValue(mainSim) as Simulation;
                if (sim == null || sim.farm == null)
                {
                    return "ERROR: No active farm";
                }

                var farm = sim.farm;
                var json = new System.Text.StringBuilder();
                json.Append("{");

                // Drones
                json.Append("\"drones\":[");
                for (int i = 0; i < farm.drones.Count; i++)
                {
                    var drone = farm.drones[i];
                    if (i > 0) json.Append(",");
                    json.Append("{");
                    json.Append($"\"id\":{drone.DroneId},");
                    json.Append($"\"x\":{drone.pos.x},");
                    json.Append($"\"y\":{drone.pos.y}");
                    json.Append("}");
                }
                json.Append("],");

                // Grid tiles (grounds and entities)
                json.Append("\"tiles\":[");
                bool firstTile = true;
                var grounds = farm.grid.grounds;
                var entities = farm.grid.entities;

                // Get grid bounds
                int minX = int.MaxValue, maxX = int.MinValue;
                int minY = int.MaxValue, maxY = int.MinValue;

                foreach (var kvp in grounds)
                {
                    minX = System.Math.Min(minX, kvp.Key.x);
                    maxX = System.Math.Max(maxX, kvp.Key.x);
                    minY = System.Math.Min(minY, kvp.Key.y);
                    maxY = System.Math.Max(maxY, kvp.Key.y);
                }
                foreach (var kvp in entities)
                {
                    minX = System.Math.Min(minX, kvp.Key.x);
                    maxX = System.Math.Max(maxX, kvp.Key.x);
                    minY = System.Math.Min(minY, kvp.Key.y);
                    maxY = System.Math.Max(maxY, kvp.Key.y);
                }

                // Iterate through grid
                if (minX != int.MaxValue)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        for (int x = minX; x <= maxX; x++)
                        {
                            var pos = new UnityEngine.Vector2Int(x, y);
                            FarmObject ground = null;
                            FarmObject entity = null;

                            grounds.TryGetValue(pos, out ground);
                            entities.TryGetValue(pos, out entity);

                            if (ground != null || entity != null)
                            {
                                if (!firstTile) json.Append(",");
                                firstTile = false;

                                json.Append("{");
                                json.Append($"\"x\":{x},");
                                json.Append($"\"y\":{y}");

                                // Ground info - use objectSO.objectName to get proper type (Soil/Grassland/etc)
                                if (ground != null && ground.objectSO != null)
                                {
                                    json.Append($",\"ground\":\"{EscapeJsonString(ground.objectSO.objectName)}\"");

                                    // Get water level from grid.waterVolume array
                                    try
                                    {
                                        var waterVolumeField = typeof(GridManager).GetField("waterVolume", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                        if (waterVolumeField != null)
                                        {
                                            var waterVolume = waterVolumeField.GetValue(farm.grid) as double[,];
                                            if (waterVolume != null && x < waterVolume.GetLength(0) && y < waterVolume.GetLength(1))
                                            {
                                                double waterLevel = waterVolume[x, y];
                                                json.Append($",\"water\":{waterLevel}");
                                            }
                                        }
                                    }
                                    catch { }
                                }

                                // Entity info - use objectSO.objectName for actual plant/object type
                                if (entity != null)
                                {
                                    if (entity.objectSO != null)
                                    {
                                        json.Append($",\"entity\":\"{EscapeJsonString(entity.objectSO.objectName)}\"");
                                    }
                                    else
                                    {
                                        json.Append($",\"entity\":\"{entity.GetType().Name}\"");
                                    }

                                    // Add growth stage for growables
                                    if (entity is Growable growable)
                                    {
                                        // Access stage field via reflection
                                        var stageField = typeof(Growable).GetField("stage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                        if (stageField != null)
                                        {
                                            int stage = (int)stageField.GetValue(growable);
                                            json.Append($",\"stage\":{stage}");
                                        }
                                    }

                                    // Add measure() result
                                    try
                                    {
                                        IPyObject measureResult = entity.Measure();
                                        if (measureResult != null && !(measureResult is PyNone))
                                        {
                                            string measureStr = CodeUtilities.ToNiceString(measureResult, 0, null, false);
                                            if (!string.IsNullOrEmpty(measureStr))
                                            {
                                                json.Append($",\"measure\":\"{EscapeJsonString(measureStr)}\"");
                                            }
                                        }
                                    }
                                    catch { }

                                    // Add companion info for plants that can have companions
                                    if (entity is Growable growableWithCompanion && growableWithCompanion.objectSO.canHaveCompanion)
                                    {
                                        try
                                        {
                                            IPyObject companionInfo = growableWithCompanion.GetCompanion();
                                            if (companionInfo is PyTuple tuple && tuple.Count == 2)
                                            {
                                                json.Append(",\"companion\":{");

                                                // Companion type
                                                if (tuple[0] is FarmObjectSO companionType)
                                                {
                                                    json.Append($"\"type\":\"{EscapeJsonString(companionType.objectName)}\"");
                                                }

                                                // Companion position
                                                if (tuple[1] is PyTuple posTuple && posTuple.Count == 2)
                                                {
                                                    if (posTuple[0] is PyNumber xNum && posTuple[1] is PyNumber yNum)
                                                    {
                                                        int compX = (int)xNum.num;
                                                        int compY = (int)yNum.num;
                                                        json.Append($",\"x\":{compX},\"y\":{compY}");

                                                        // Check if companion is actually present
                                                        UnityEngine.Vector2Int companionPos = new UnityEngine.Vector2Int(compX, compY);
                                                        if (entities.TryGetValue(companionPos, out FarmObject actualCompanion))
                                                        {
                                                            bool matches = actualCompanion.objectSO != null &&
                                                                         tuple[0] is FarmObjectSO expectedType &&
                                                                         actualCompanion.objectSO.objectName == expectedType.objectName;
                                                            json.Append($",\"present\":{(matches ? "true" : "false")}");
                                                        }
                                                        else
                                                        {
                                                            json.Append(",\"present\":false");
                                                        }
                                                    }
                                                }

                                                json.Append("}");
                                            }
                                        }
                                        catch { }
                                    }
                                }

                                json.Append("}");
                            }
                        }
                    }
                }

                json.Append("],");

                // Items
                json.Append("\"items\":{");
                bool firstItem = true;
                var items = farm.Items;
                for (int i = 0; i < items.items.Length; i++)
                {
                    if (items.items[i] > 0)
                    {
                        var itemSO = ResourceManager.GetItem(i);
                        if (itemSO != null)
                        {
                            if (!firstItem) json.Append(",");
                            firstItem = false;
                            json.Append($"\"{itemSO.itemId}\":{items.items[i]}");
                        }
                    }
                }
                json.Append("}");

                json.Append("}");
                return json.ToString();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error getting farm: {ex.Message}\n{ex.StackTrace}");
                return $"ERROR: {ex.Message}";
            }
        }

        private static void ExecuteStepByStep(MainSim mainSim, string windowName)
        {
            try
            {
                var workspace = mainSim.workspace;
                if (workspace == null)
                {
                    Plugin.Log.LogError("Workspace is null");
                    return;
                }

                CodeWindow codeWindow = null;
                if (!workspace.codeWindows.TryGetValue(windowName, out codeWindow))
                {
                    Plugin.Log.LogError($"Window '{windowName}' not found");
                    return;
                }

                // Check if window is currently executing
                bool isExecuting = codeWindow.isExecuting;

                if (!isExecuting)
                {
                    // Window is not running - start it in step mode
                    // We need to set sim.stepByStepMode directly using reflection because
                    // mainSim.StepByStepMode property setter only works if already executing
                    var simField = typeof(MainSim).GetField("sim", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (simField != null)
                    {
                        var sim = simField.GetValue(mainSim) as Simulation;
                        if (sim != null)
                        {
                            sim.stepByStepMode = true;
                        }
                    }

                    // Start execution - will automatically pause after first step
                    codeWindow.PressExecuteOrStop();
                }
                else
                {
                    // Window is already running - ensure step mode is enabled and advance one step
                    if (!mainSim.StepByStepMode)
                    {
                        mainSim.StepByStepMode = true;
                    }

                    // Advance one step - game will automatically pause after this step completes
                    mainSim.NextExecutionStep();
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error executing step: {ex.Message}");
            }
        }

        private static void ExecuteMoveWindow(MainSim mainSim, string data)
        {
            try
            {
                // Format: windowName x y
                string[] parts = data.Split(new[] { ' ' }, 3);
                if (parts.Length < 3)
                {
                    Plugin.Log.LogError("MoveWindow requires window name, x, and y coordinates");
                    return;
                }

                string windowName = parts[0];
                if (!float.TryParse(parts[1], out float x) || !float.TryParse(parts[2], out float y))
                {
                    Plugin.Log.LogError("Invalid coordinates for MoveWindow");
                    return;
                }

                var workspace = mainSim.workspace;
                if (workspace == null)
                {
                    Plugin.Log.LogError("Workspace is null");
                    return;
                }

                Window window = null;
                if (workspace.openWindows.TryGetValue(windowName, out window))
                {
                    var rectTransform = window.GetComponent<UnityEngine.RectTransform>();
                    if (rectTransform != null)
                    {
                        // Set position directly on RectTransform
                        rectTransform.anchoredPosition = new UnityEngine.Vector2(x, y);

                        // Update workspace container size to reflect new position
                        workspace.UpdateContainerSize();

                        Plugin.Log.LogInfo($"Moved window '{windowName}' to ({x}, {y})");
                    }
                    else
                    {
                        Plugin.Log.LogError("Could not get RectTransform component");
                    }
                }
                else
                {
                    Plugin.Log.LogError($"Window '{windowName}' not found");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error moving window: {ex.Message}");
            }
        }

        private static string ExecuteGetUnlocks(MainSim mainSim)
        {
            try
            {
                // Access private sim field using reflection
                var simField = typeof(MainSim).GetField("sim", BindingFlags.NonPublic | BindingFlags.Instance);
                if (simField == null)
                {
                    return "ERROR: Could not access sim field";
                }

                var sim = simField.GetValue(mainSim) as Simulation;
                if (sim == null || sim.farm == null)
                {
                    return "ERROR: No active farm";
                }

                var farm = sim.farm;

                // Access the private unlocks dictionary using reflection
                var unlocksField = typeof(Farm).GetField("unlocks", BindingFlags.NonPublic | BindingFlags.Instance);
                if (unlocksField == null)
                {
                    return "ERROR: Could not access unlocks field";
                }

                var unlocks = unlocksField.GetValue(farm) as System.Collections.Generic.Dictionary<string, int>;
                if (unlocks == null)
                {
                    return "ERROR: Unlocks dictionary is null";
                }

                // Build JSON object
                var json = new System.Text.StringBuilder();
                json.Append("{");

                bool first = true;
                foreach (var unlock in unlocks)
                {
                    if (!first) json.Append(",");
                    first = false;

                    // Escape the key for JSON
                    string escapedKey = unlock.Key.Replace("\\", "\\\\").Replace("\"", "\\\"");
                    json.Append($"\"{escapedKey}\":{unlock.Value}");
                }

                json.Append("}");
                return json.ToString();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error getting unlocks: {ex.Message}\n{ex.StackTrace}");
                return $"ERROR: {ex.Message}";
            }
        }

        private static string ExecuteGetWindowPosition(MainSim mainSim, string windowName)
        {
            try
            {
                var workspace = mainSim.workspace;
                if (workspace == null)
                {
                    return "ERROR: Workspace is null";
                }

                Window window = null;
                if (workspace.openWindows.TryGetValue(windowName, out window))
                {
                    var rectTransform = window.GetComponent<UnityEngine.RectTransform>();
                    if (rectTransform != null)
                    {
                        var pos = rectTransform.anchoredPosition;
                        return $"{{\"x\":{pos.x},\"y\":{pos.y}}}";
                    }
                    else
                    {
                        return "ERROR: Could not get RectTransform component";
                    }
                }
                else
                {
                    return $"ERROR: Window '{windowName}' not found";
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error getting window position: {ex.Message}");
                return $"ERROR: {ex.Message}";
            }
        }
    }
}
