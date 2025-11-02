using HarmonyLib;
using System.IO;
using System.Reflection;
using System.Linq;

namespace TcpSocket.Patches
{
    [HarmonyPatch(typeof(MainSim))]
    public static class MainSimPatch
    {
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
                Plugin.Log.LogInfo("Exiting game");
                UnityEngine.Application.Quit();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error exiting game: {ex.Message}");
            }
        }
    }
}
