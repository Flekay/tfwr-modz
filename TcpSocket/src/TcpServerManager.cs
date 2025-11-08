using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using BepInEx.Logging;

namespace TcpSocket
{
    public class TcpServerManager
    {
        private TcpListener listener;
        private Thread listenerThread;
        private volatile bool isRunning;
        private ManualLogSource logger;
        private readonly int port;
        private readonly Queue<string> commandQueue = new Queue<string>();
        private readonly object queueLock = new object();

        // For synchronous query responses
        private readonly Dictionary<string, ManualResetEvent> pendingQueries = new Dictionary<string, ManualResetEvent>();
        private readonly Dictionary<string, string> queryResults = new Dictionary<string, string>();
        private readonly object queryLock = new object();
        private int queryIdCounter = 0;

        public TcpServerManager(int port, ManualLogSource logger)
        {
            this.port = port;
            this.logger = logger;
        }

        public void Start()
        {
            if (isRunning)
            {
                logger.LogWarning("TCP server is already running");
                return;
            }

            isRunning = true;
            listenerThread = new Thread(ListenerThread)
            {
                IsBackground = true
            };
            listenerThread.Start();

            // Get local IP address
            string localIp = GetLocalIPAddress();
            logger.LogInfo($"TCP server started on {localIp}:{port}");
        }

        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        public void Stop()
        {
            if (!isRunning) return;

            isRunning = false;
            listener?.Stop();
            listenerThread?.Join(1000);
            logger.LogInfo("TCP server stopped");
        }

        private void ListenerThread()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();

                while (isRunning)
                {
                    if (listener.Pending())
                    {
                        TcpClient client = listener.AcceptTcpClient();
                        Thread clientThread = new Thread(() => HandleClient(client))
                        {
                            IsBackground = true
                        };
                        clientThread.Start();
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            catch (SocketException ex)
            {
                logger.LogError($"Socket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Listener thread error: {ex.Message}");
            }
        }

        private void HandleClient(TcpClient client)
        {
            try
            {
                using (client)
                {
                    NetworkStream stream = client.GetStream();

                    // Read request
                    byte[] buffer = new byte[4096];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimEnd('\r', '\n');

                    if (string.IsNullOrEmpty(request))
                    {
                        byte[] errorBytes = Encoding.UTF8.GetBytes("ERROR: Empty request\n");
                        stream.Write(errorBytes, 0, errorBytes.Length);
                        return;
                    }

                    logger.LogInfo($"Received request: {request}");

                    string response = ProcessCommand(request);
                    logger.LogInfo($"Sending response: {response}");

                    // Write response
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response + "\n");
                    stream.Write(responseBytes, 0, responseBytes.Length);
                    stream.Flush();

                    // Give the client time to receive the data before closing
                    System.Threading.Thread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Client handler error: {ex.Message}");
            }
        }

        private string ProcessCommand(string command)
        {
            try
            {
                string[] parts = command.Split(new[] { ' ' }, 3);
                string cmd = parts[0].ToLower();

                switch (cmd)
                {
                    case "runstart":
                        if (parts.Length < 2)
                        {
                            return "ERROR: runstart requires a window name";
                        }
                        return QueueCommand("runstart", parts[1]);

                    case "stop":
                        return QueueCommand("stop", "");

                    case "camera":
                        // camera x y [zoom] or camera reset
                        if (parts.Length < 2)
                        {
                            return "ERROR: camera requires x y [zoom] coordinates or 'reset'";
                        }
                        // Pass everything after "camera " (parts[1] contains the rest due to split limit 3)
                        string cameraData = parts.Length == 2 ? parts[1] : parts[1] + " " + parts[2];
                        return QueueCommand("camera", cameraData);

                    case "hideui":
                        // hideui true/false
                        if (parts.Length < 2)
                        {
                            return "ERROR: hideui requires true or false";
                        }
                        return QueueCommand("hideui", parts[1]);

                    case "setcode":
                        // setcode window_name code_content
                        if (parts.Length < 3)
                        {
                            return "ERROR: setcode requires window_name and code";
                        }
                        return QueueCommand("setcode", parts[1] + "|" + parts[2]);

                    case "createfile":
                        // createfile window_name
                        if (parts.Length < 2)
                        {
                            return "ERROR: createfile requires window_name";
                        }
                        return QueueCommand("createfile", parts[1]);

                    case "deletefile":
                        // deletefile window_name
                        if (parts.Length < 2)
                        {
                            return "ERROR: deletefile requires window_name";
                        }
                        return QueueCommand("deletefile", parts[1]);

                    case "savegame":
                        return QueueCommand("savegame", "");

                    case "loadlevel":
                        // loadlevel level_name
                        if (parts.Length < 2)
                        {
                            return "ERROR: loadlevel requires level_name";
                        }
                        return QueueCommand("loadlevel", parts[1]);

                    case "getlevels":
                        // Execute immediately, not queued (filesystem access is safe)
                        return ExecuteGetLevelsImmediate();

                    case "getwindows":
                        // Execute on main thread with result
                        return ExecuteQueryCommand("getwindows", "");

                    case "getcode":
                        // getcode window_name
                        if (parts.Length < 2)
                        {
                            return "ERROR: getcode requires window_name";
                        }
                        // Execute on main thread with result
                        return ExecuteQueryCommand("getcode", parts[1]);

                    case "getoutput":
                        // Get console output
                        return ExecuteQueryCommand("getoutput", "");

                    case "clearoutput":
                        // Clear console output
                        return QueueCommand("clearoutput", "");

                    case "getfarm":
                        // Get farm state as JSON
                        return ExecuteQueryCommand("getfarm", "");

                    case "getunlocks":
                        // Get unlocks as JSON
                        return ExecuteQueryCommand("getunlocks", "");

                    case "getwindowposition":
                        // getwindowposition window_name
                        if (parts.Length < 2)
                        {
                            return "ERROR: getwindowposition requires window_name";
                        }
                        return ExecuteQueryCommand("getwindowposition", parts[1]);

                    case "stepbystep":
                        // stepbystep window_name
                        if (parts.Length < 2)
                        {
                            return "ERROR: stepbystep requires window_name";
                        }
                        return QueueCommand("stepbystep", parts[1]);

                    case "movewindow":
                        // movewindow window_name x y
                        if (parts.Length < 3)
                        {
                            return "ERROR: movewindow requires window_name x y";
                        }
                        // Combine parts[1] (window_name) and parts[2] (x y)
                        return QueueCommand("movewindow", parts[1] + " " + parts[2]);

                    case "exitgame":
                        return QueueCommand("exitgame", "");

                    case "ping":
                        return "PONG";

                    default:
                        return $"ERROR: Unknown command '{cmd}'";
                }
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }

        private string QueueCommand(string commandType, string data)
        {
            lock (queueLock)
            {
                commandQueue.Enqueue(commandType + "|" + data);
            }

            logger.LogInfo($"Queued command: {commandType} with data: {data}");
            return $"OK: Queued {commandType}";
        }

        // Execute a query command and wait for result from main thread
        private string ExecuteQueryCommand(string commandType, string data)
        {
            string queryId = (++queryIdCounter).ToString();
            ManualResetEvent waitHandle = new ManualResetEvent(false);

            lock (queryLock)
            {
                pendingQueries[queryId] = waitHandle;
            }

            // Queue the query command with ID
            lock (queueLock)
            {
                commandQueue.Enqueue($"QUERY|{queryId}|{commandType}|{data}");
            }

            logger.LogInfo($"Queued query: {commandType} with ID: {queryId}");

            // Wait for result (timeout 5 seconds)
            if (waitHandle.WaitOne(5000))
            {
                lock (queryLock)
                {
                    if (queryResults.TryGetValue(queryId, out string result))
                    {
                        queryResults.Remove(queryId);
                        pendingQueries.Remove(queryId);
                        return result;
                    }
                }
            }

            // Timeout
            lock (queryLock)
            {
                pendingQueries.Remove(queryId);
            }

            return "ERROR: Query timeout";
        }

        // Called from main thread to set query result
        public void SetQueryResult(string queryId, string result)
        {
            lock (queryLock)
            {
                queryResults[queryId] = result;
                if (pendingQueries.TryGetValue(queryId, out ManualResetEvent waitHandle))
                {
                    waitHandle.Set();
                }
            }
        }

        private string QueueRunStartCommand(string windowName)
        {
            // Validate window name (simple validation)
            if (string.IsNullOrWhiteSpace(windowName))
            {
                return "ERROR: Window name cannot be empty";
            }

            lock (queueLock)
            {
                commandQueue.Enqueue(windowName);
            }

            logger.LogInfo($"Queued runstart command for window: {windowName}");
            return $"OK: Queued runstart for {windowName}";
        }

        public string DequeueCommand()
        {
            lock (queueLock)
            {
                if (commandQueue.Count > 0)
                {
                    return commandQueue.Dequeue();
                }
                return null;
            }
        }

        public bool HasPendingCommands()
        {
            lock (queueLock)
            {
                return commandQueue.Count > 0;
            }
        }

        // Immediate execution methods (called from TCP thread, not main thread)
        private string ExecuteGetLevelsImmediate()
        {
            try
            {
                string savesPath = UnityEngine.Application.persistentDataPath + "/Saves";

                if (!System.IO.Directory.Exists(savesPath))
                {
                    return "[]";
                }

                string[] directories = System.IO.Directory.GetDirectories(savesPath);
                var levelNames = new System.Collections.Generic.List<string>();

                foreach (string dir in directories)
                {
                    string levelName = System.IO.Path.GetFileName(dir);
                    levelNames.Add(levelName);
                }

                // Return as JSON array
                return "[\"" + string.Join("\",\"", levelNames.ToArray()) + "\"]";
            }
            catch (System.Exception ex)
            {
                logger.LogError($"GetLevels error: {ex.Message}");
                return $"ERROR: {ex.Message}";
            }
        }

        private string ExecuteGetWindowsImmediate()
        {
            try
            {
                // Access Workspace.windows via reflection
                var workspaceType = System.Type.GetType("Workspace, Core");
                if (workspaceType == null)
                {
                    return "ERROR: Workspace type not found";
                }

                var instanceProperty = workspaceType.GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (instanceProperty == null)
                {
                    return "ERROR: Workspace.instance not found";
                }

                var workspace = instanceProperty.GetValue(null);
                if (workspace == null)
                {
                    return "ERROR: Workspace.instance is null";
                }

                var windowsField = workspaceType.GetField("windows", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (windowsField == null)
                {
                    return "ERROR: windows field not found";
                }

                var windows = windowsField.GetValue(workspace) as System.Collections.IList;
                if (windows == null)
                {
                    return "[]";
                }

                var windowNames = new System.Collections.Generic.List<string>();
                var codeWindowType = System.Type.GetType("CodeWindow, Core");

                foreach (var window in windows)
                {
                    if (window != null && window.GetType() == codeWindowType)
                    {
                        var fileNameField = codeWindowType.GetField("fileName", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (fileNameField != null)
                        {
                            string fileName = fileNameField.GetValue(window) as string;
                            if (!string.IsNullOrEmpty(fileName))
                            {
                                windowNames.Add(fileName);
                            }
                        }
                    }
                }

                // Return as JSON array
                return "[\"" + string.Join("\",\"", windowNames.ToArray()) + "\"]";
            }
            catch (System.Exception ex)
            {
                logger.LogError($"GetWindows error: {ex.Message}");
                return $"ERROR: {ex.Message}";
            }
        }

        private string ExecuteGetCodeImmediate(string windowName)
        {
            try
            {
                // Access Workspace.windows via reflection
                var workspaceType = System.Type.GetType("Workspace, Core");
                if (workspaceType == null)
                {
                    return "ERROR: Workspace type not found";
                }

                var instanceProperty = workspaceType.GetProperty("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (instanceProperty == null)
                {
                    return "ERROR: Workspace.instance not found";
                }

                var workspace = instanceProperty.GetValue(null);
                if (workspace == null)
                {
                    return "ERROR: Workspace.instance is null";
                }

                var windowsField = workspaceType.GetField("windows", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (windowsField == null)
                {
                    return "ERROR: windows field not found";
                }

                var windows = windowsField.GetValue(workspace) as System.Collections.IList;
                if (windows == null)
                {
                    return "ERROR: No windows found";
                }

                var codeWindowType = System.Type.GetType("CodeWindow, Core");

                foreach (var window in windows)
                {
                    if (window != null && window.GetType() == codeWindowType)
                    {
                        var fileNameField = codeWindowType.GetField("fileName", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        if (fileNameField != null)
                        {
                            string fileName = fileNameField.GetValue(window) as string;
                            if (fileName == windowName)
                            {
                                // Found the window, get its code
                                var getCodeMethod = codeWindowType.GetMethod("GetCode", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                if (getCodeMethod != null)
                                {
                                    string code = getCodeMethod.Invoke(window, null) as string;
                                    return code ?? "";
                                }
                                else
                                {
                                    return "ERROR: GetCode method not found";
                                }
                            }
                        }
                    }
                }

                return $"ERROR: Window '{windowName}' not found";
            }
            catch (System.Exception ex)
            {
                logger.LogError($"GetCode error: {ex.Message}");
                return $"ERROR: {ex.Message}";
            }
        }
    }
}
