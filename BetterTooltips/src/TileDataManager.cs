using System.Collections.Generic;
using UnityEngine;

namespace BetterTooltips;

/// <summary>
/// Manages custom data associated with farm tiles
/// </summary>
public class TileDataManager : MonoBehaviour
{
    private static TileDataManager instance;
    public static TileDataManager Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("TileDataManager");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<TileDataManager>();
            }
            return instance;
        }
    }

    // Dictionary to store custom data per tile (key: "x,y")
    private Dictionary<string, string> tileData = new Dictionary<string, string>();

    /// <summary>
    /// Set custom info for a specific tile
    /// </summary>
    public void SetTileInfo(int x, int y, string info)
    {
        string key = $"{x},{y}";
        if (string.IsNullOrEmpty(info))
        {
            tileData.Remove(key);
        }
        else
        {
            tileData[key] = info;
        }
    }

    /// <summary>
    /// Get custom info for a specific tile
    /// </summary>
    public string GetTileInfo(int x, int y)
    {
        string key = $"{x},{y}";
        if (tileData.TryGetValue(key, out string info))
        {
            return info;
        }
        return null;
    }

    /// <summary>
    /// Clear all custom tile data
    /// </summary>
    public void ClearAllTileInfo()
    {
        tileData.Clear();
        Plugin.Log.LogInfo("Cleared all tile custom data");
    }

    /// <summary>
    /// Get the tile data dictionary for saving
    /// </summary>
    public Dictionary<string, string> GetAllTileData()
    {
        return new Dictionary<string, string>(tileData);
    }

    /// <summary>
    /// Load tile data from a dictionary (for loading saves)
    /// </summary>
    public void LoadTileData(Dictionary<string, string> data)
    {
        tileData = new Dictionary<string, string>(data);
        Plugin.Log.LogInfo($"Loaded {tileData.Count} tile custom data entries");
    }
}
