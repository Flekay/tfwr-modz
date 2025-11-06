using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace NotEnoughLeaderboards;

public class Api
{
    const string BASE_URL = "https://tfwr.ranknado.com/api.php";

    public static IEnumerator Submit(string board, string steamId, string name, long time, Action<bool, int> callback)
    {
        var form = new WWWForm();
        form.AddField("action", "submit");
        form.AddField("board", board);
        form.AddField("steam_id", steamId);
        form.AddField("name", name);
        form.AddField("time", time.ToString());

        using (var req = UnityWebRequest.Post(BASE_URL, form))
        {
            req.timeout = 10;
            req.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<SubmitResponse>(req.downloadHandler.text);
                    callback?.Invoke(true, response.rank);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"Failed to parse response: {ex.Message}");
                    callback?.Invoke(false, 0);
                }
            }
            else
            {
                Plugin.Log.LogError($"Request failed: {req.error}");
                callback?.Invoke(false, 0);
            }
        }
    }

    public static IEnumerator GetTop(string board, Action<Entry[]> callback)
    {
        var url = $"{BASE_URL}?action=top&board={board}&limit=100";

        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 10;
            req.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var json = req.downloadHandler.text.Trim();
                    var entries = new System.Collections.Generic.List<Entry>();

                    // Parse the array manually since Unity's JsonUtility can't handle arrays properly
                    if (json.StartsWith("[") && json.EndsWith("]"))
                    {
                        json = json.Substring(1, json.Length - 2).Trim();

                        if (!string.IsNullOrEmpty(json))
                        {
                            var objects = json.Split(new[] { "},{" }, StringSplitOptions.None);

                            for (int i = 0; i < objects.Length; i++)
                            {
                                var obj = objects[i];
                                if (!obj.StartsWith("{")) obj = "{" + obj;
                                if (!obj.EndsWith("}")) obj = obj + "}";

                                try
                                {
                                    var entry = JsonUtility.FromJson<Entry>(obj);
                                    entries.Add(entry);
                                }
                                catch { }
                            }
                        }
                    }

                    callback?.Invoke(entries.ToArray());
                }
                catch
                {
                    callback?.Invoke(new Entry[0]);
                }
            }
            else
            {
                callback?.Invoke(new Entry[0]);
            }
        }
    }

    [Serializable]
    class SubmitRequest
    {
        public string steam_id;
        public string name;
        public long time;
    }

    [Serializable]
    class SubmitResponse
    {
        public string board;
        public string steam_id;
        public string name;
        public long time;
        public int rank;
    }

    [Serializable]
    class TopResponse
    {
        public Entry[] entries;
    }

    [Serializable]
    public class Entry
    {
        public int rank;
        public string name;
        public long time;
    }
}
