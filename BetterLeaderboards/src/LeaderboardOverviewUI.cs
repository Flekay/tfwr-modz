using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLeaderboards;

public class LeaderboardOverviewUI : MonoBehaviour
{
    private GameObject panel;
    private GameObject scrollView;
    private GameObject content;
    private GameObject statusText;
    private GameObject loadingBar;
    private GameObject loadingBarFill;
    private List<GameObject> entryObjects = new List<GameObject>();

    private bool isVisible = false;
    private bool isOffline = false;

    // Hardcoded leaderboard icon mapping (based on actual log output)
    private static readonly Dictionary<string, string> LeaderboardIcons = new Dictionary<string, string>
    {
        { "carrots", "carrot" },
        { "carrots_single", "carrot" },
        { "pumpkins", "pumpkin" },
        { "pumpkins_single", "pumpkin" },
        { "wood", "wood" },
        { "wood_single", "wood" },
        { "hay", "hay" },
        { "hay_single", "hay" },
        { "sunflowers", "gold" },
        { "sunflowers_single", "gold" },
        { "maze", "carrot" },
        { "maze_single", "carrot" },
        { "cactus", "cactus" },
        { "cactus_single", "cactus" },
        { "dinosaur", "bone" },
        { "fastest_reset", "leaderboard_unlock" } // Use unlock icon as main icon
    };

    public event Action OnReloadRequested;

    private void OnReloadClicked()
    {
        Plugin.Log.LogInfo("Reload button clicked!");

        // Clear existing entries
        foreach (var entry in entryObjects)
        {
            Destroy(entry);
        }
        entryObjects.Clear();

        // Show loading bar
        ShowLoadingBar(true);

        OnReloadRequested?.Invoke();
    }

    public void ShowLoadingBar(bool show)
    {
        if (loadingBar != null)
        {
            loadingBar.SetActive(show);
            if (show && loadingBarFill != null)
            {
                var fillRect = loadingBarFill.GetComponent<RectTransform>();
                fillRect.anchorMax = new Vector2(0, 1); // Reset to 0
            }
        }
    }

    public void UpdateLoadingProgress(float progress)
    {
        if (loadingBarFill != null)
        {
            var fillRect = loadingBarFill.GetComponent<RectTransform>();
            fillRect.anchorMax = new Vector2(progress, 1);
        }
    }

    public void CreateUI(Canvas parentCanvas)
    {
        Plugin.Log.LogInfo("Creating leaderboard overview UI...");

        // Create main panel
        panel = new GameObject("LeaderboardOverviewPanel");
        panel.transform.SetParent(parentCanvas.transform, false);

        var panelRect = panel.AddComponent<RectTransform>();
        // Wider panel on the right side with padding from edges
        panelRect.anchorMin = new Vector2(0.55f, 0.05f); // Tiefer starten (mehr Platz unten)
        panelRect.anchorMax = new Vector2(0.95f, 0.95f); // Höher enden (mehr Platz oben)
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.08f, 0.12f, 0.97f); // Darker, more opaque

        Plugin.Log.LogInfo("Panel created and positioned");

        // Create title
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);

        var titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.93f); // Niedriger für mehr Platz oben
        titleRect.anchorMax = new Vector2(1, 0.98f); // Nicht ganz oben
        titleRect.offsetMin = new Vector2(10, 0);
        titleRect.offsetMax = new Vector2(-10, 0);

        var titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "MY LEADERBOARDS";
        titleText.fontSize = 26;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.95f, 0.85f, 0.4f, 1f); // Golden color
        titleText.fontStyle = FontStyles.Bold;

        // Create reload button (larger)
        var reloadButtonObj = new GameObject("ReloadButton");
        reloadButtonObj.transform.SetParent(panel.transform, false);

        var reloadButtonRect = reloadButtonObj.AddComponent<RectTransform>();
        reloadButtonRect.anchorMin = new Vector2(0.83f, 0.935f); // Angepasst an neue Titel-Position
        reloadButtonRect.anchorMax = new Vector2(0.98f, 0.975f);
        reloadButtonRect.offsetMin = Vector2.zero;
        reloadButtonRect.offsetMax = Vector2.zero;

        var reloadButtonImage = reloadButtonObj.AddComponent<Image>();
        reloadButtonImage.color = new Color(0.25f, 0.55f, 0.75f, 1f); // Nicer blue

        var reloadButton = reloadButtonObj.AddComponent<Button>();
        reloadButton.onClick.AddListener(OnReloadClicked);

        var reloadButtonTextObj = new GameObject("Text");
        reloadButtonTextObj.transform.SetParent(reloadButtonObj.transform, false);
        var reloadButtonTextRect = reloadButtonTextObj.AddComponent<RectTransform>();
        reloadButtonTextRect.anchorMin = Vector2.zero;
        reloadButtonTextRect.anchorMax = Vector2.one;
        reloadButtonTextRect.offsetMin = Vector2.zero;
        reloadButtonTextRect.offsetMax = Vector2.zero;

        var reloadButtonText = reloadButtonTextObj.AddComponent<TextMeshProUGUI>();
        reloadButtonText.text = "RELOAD";
        reloadButtonText.fontSize = 12;
        reloadButtonText.alignment = TextAlignmentOptions.Center;
        reloadButtonText.color = Color.white;
        reloadButtonText.fontStyle = FontStyles.Bold;
        reloadButtonText.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        reloadButtonText.overflowMode = TextOverflowModes.Overflow;

        // Create status text for offline message
        statusText = new GameObject("StatusText");
        statusText.transform.SetParent(panel.transform, false);

        var statusRect = statusText.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.89f); // Angepasst
        statusRect.anchorMax = new Vector2(1, 0.92f);
        statusRect.offsetMin = new Vector2(10, 0);
        statusRect.offsetMax = new Vector2(-10, 0);

        var statusTMP = statusText.AddComponent<TextMeshProUGUI>();
        statusTMP.text = "";
        statusTMP.fontSize = 11;
        statusTMP.alignment = TextAlignmentOptions.Center;
        statusTMP.color = new Color(1f, 0.6f, 0.3f, 1f); // Orange warning color
        statusTMP.fontStyle = FontStyles.Italic;

        // Create loading bar
        loadingBar = new GameObject("LoadingBar");
        loadingBar.transform.SetParent(panel.transform, false);

        var loadingBarRect = loadingBar.AddComponent<RectTransform>();
        loadingBarRect.anchorMin = new Vector2(0.05f, 0.88f); // Angepasst
        loadingBarRect.anchorMax = new Vector2(0.95f, 0.885f);
        loadingBarRect.offsetMin = Vector2.zero;
        loadingBarRect.offsetMax = Vector2.zero;

        var loadingBarBg = loadingBar.AddComponent<Image>();
        loadingBarBg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

        // Loading bar fill
        loadingBarFill = new GameObject("Fill");
        loadingBarFill.transform.SetParent(loadingBar.transform, false);

        var fillRect = loadingBarFill.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(0, 1); // Start at 0 width
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        var fillImage = loadingBarFill.AddComponent<Image>();
        fillImage.color = new Color(0.25f, 0.55f, 0.75f, 1f); // Same blue as reload button

        loadingBar.SetActive(false); // Hidden by default

        // Create ScrollView (adjusted position for status text and loading bar)
        scrollView = new GameObject("ScrollView");
        scrollView.transform.SetParent(panel.transform, false);

        var scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 0.88f); // Angepasst für mehr Platz
        scrollRect.offsetMin = new Vector2(10, 10);
        scrollRect.offsetMax = new Vector2(-10, -10);

        var scrollComponent = scrollView.AddComponent<ScrollRect>();
        scrollComponent.horizontal = false;
        scrollComponent.vertical = true;
        scrollComponent.scrollSensitivity = 20f;

        var scrollViewImage = scrollView.AddComponent<Image>();
        scrollViewImage.color = new Color(0.03f, 0.03f, 0.05f, 1f); // Very dark background

        // Create viewport
        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);

        var viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        viewport.AddComponent<Mask>().showMaskGraphic = false;
        viewport.AddComponent<Image>();

        // Create content container
        content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);

        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        var layoutGroup = content.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 8; // M3: More generous spacing
        layoutGroup.padding = new RectOffset(12, 12, 12, 12); // M3: Padding
        layoutGroup.childControlHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;

        var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollComponent.viewport = viewportRect;
        scrollComponent.content = contentRect;

        Plugin.Log.LogInfo("ScrollView created successfully");

        // Show the panel immediately (even if empty)
        panel.SetActive(true);
        isVisible = true;
        Plugin.Log.LogInfo("UI panel is now visible");
    }

    public void PopulateLeaderboards(List<LeaderboardDataManager.LeaderboardData> data)
    {
        // Hide loading bar
        ShowLoadingBar(false);

        // Clear existing entries
        foreach (var entry in entryObjects)
        {
            Destroy(entry);
        }
        entryObjects.Clear();

        // Plugin.Log.LogInfo($"Populating UI with {data.Count} leaderboards");

        // Check if offline
        isOffline = data.Count > 0 && !data[0].HasPlayerEntry && !SteamManager.Initialized;

        // Update status text
        if (statusText != null)
        {
            var statusTMP = statusText.GetComponent<TextMeshProUGUI>();
            if (isOffline)
            {
                statusTMP.text = "⚠ OFFLINE MODE - No leaderboard data available";
            }
            else if (data.Count == 0)
            {
                statusTMP.text = "No leaderboards found";
            }
            else
            {
                int participated = data.Count(d => d.HasPlayerEntry);
                if (participated > 0)
                {
                    statusTMP.text = $"You participated in {participated} of {data.Count} leaderboards";
                    statusTMP.color = new Color(0.5f, 0.8f, 0.5f, 1f); // Green for online
                }
                else
                {
                    statusTMP.text = $"You haven't participated in any leaderboard yet";
                    statusTMP.color = new Color(0.7f, 0.7f, 0.7f, 1f); // Grey
                }
            }
        }

        foreach (var leaderboard in data)
        {
            CreateLeaderboardEntry(leaderboard);
        }
    }

    private void CreateLeaderboardEntry(LeaderboardDataManager.LeaderboardData data)
    {
        var entryObj = new GameObject($"Entry_{data.LeaderboardName}");
        entryObj.transform.SetParent(content.transform, false);

        var entryRect = entryObj.AddComponent<RectTransform>();
        entryRect.sizeDelta = new Vector2(0, 65); // Taller for M3 design

        var entryImage = entryObj.AddComponent<Image>();
        // M3: Subtle elevation with slight border
        int index = entryObjects.Count;
        if (index % 2 == 0)
            entryImage.color = new Color(0.14f, 0.14f, 0.20f, 1f);
        else
            entryImage.color = new Color(0.12f, 0.12f, 0.18f, 1f);

        // M3: Add rounded corners effect with padding
        var padding = 4f;

        // Left: Icon placeholder
        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(entryObj.transform, false);

        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.sizeDelta = new Vector2(50, 50);
        iconRect.anchoredPosition = new Vector2(35, 0);

        var iconImage = iconObj.AddComponent<Image>();
        iconImage.color = new Color(0.2f, 0.22f, 0.28f, 1f); // M3 surface variant
        iconImage.preserveAspect = true;

        // Try to load icon from hardcoded mapping
        bool iconLoaded = false;
        if (LeaderboardIcons.TryGetValue(data.LeaderboardName, out string iconName))
        {
            try
            {
                Sprite sprite = null;

                // Special handling for unlock icons
                if (iconName == "leaderboard_unlock")
                {
                    sprite = Resources.Load<Sprite>("UnlockTextures/leaderboard");
                }
                else
                {
                    sprite = Resources.Load<Sprite>("ItemTextures/" + iconName);
                }

                if (sprite != null)
                {
                    iconImage.sprite = sprite;
                    iconImage.color = Color.white;
                    iconLoaded = true;
                    // Plugin.Log.LogInfo($"Loaded icon for {data.LeaderboardName}: {iconName}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to load icon {iconName} for {data.LeaderboardName}: {ex.Message}");
            }
        }

        // Show first letters as fallback
        if (!iconLoaded)
        {
            var letterObj = new GameObject("Letter");
            letterObj.transform.SetParent(iconObj.transform, false);
            var letterRect = letterObj.AddComponent<RectTransform>();
            letterRect.anchorMin = Vector2.zero;
            letterRect.anchorMax = Vector2.one;
            letterRect.offsetMin = Vector2.zero;
            letterRect.offsetMax = Vector2.zero;

            var letterText = letterObj.AddComponent<TextMeshProUGUI>();
            letterText.text = data.LeaderboardName.Substring(0, Math.Min(2, data.LeaderboardName.Length)).ToUpper();
            letterText.fontSize = 20;
            letterText.alignment = TextAlignmentOptions.Center;
            letterText.color = new Color(0.85f, 0.75f, 0.35f, 1f);
            letterText.fontStyle = FontStyles.Bold;
        }

        // Add megafarm icon overlay for non-single, non-reset, non-dinosaur leaderboards
        bool isSingle = data.LeaderboardName.Contains("_single");
        bool isReset = data.LeaderboardName.Contains("reset");
        bool isDinosaur = data.LeaderboardName.Contains("dinosaur");

        if (!isSingle && !isReset && !isDinosaur)
        {
            var megafarmIconObj = new GameObject("MegafarmIcon");
            megafarmIconObj.transform.SetParent(iconObj.transform, false);

            var megafarmIconRect = megafarmIconObj.AddComponent<RectTransform>();
            // Größer: 65% der Icon-Größe (50px * 0.65 = 32.5px)
            float iconSize = 32.5f;
            // Anchor top-right corner
            megafarmIconRect.anchorMin = new Vector2(1f, 1f);
            megafarmIconRect.anchorMax = new Vector2(1f, 1f);
            megafarmIconRect.sizeDelta = new Vector2(iconSize, iconSize);
            // Offset: nur 25% überlappend, 75% außerhalb
            megafarmIconRect.anchoredPosition = new Vector2(-iconSize * 0.25f, -iconSize * 0.25f);

            var megafarmIconImage = megafarmIconObj.AddComponent<Image>();

            try
            {
                var megafarmSprite = Resources.Load<Sprite>("UnlockTextures/megafarm");
                if (megafarmSprite != null)
                {
                    megafarmIconImage.sprite = megafarmSprite;
                    megafarmIconImage.color = new Color(1f, 0.85f, 0.4f, 0.95f); // Slightly transparent gold
                    megafarmIconImage.preserveAspect = true;
                    // Plugin.Log.LogInfo($"Loaded megafarm icon for {data.LeaderboardName}");
                }
                else
                {
                    Plugin.Log.LogWarning($"Megafarm sprite not found at UnlockTextures/megafarm");
                    Destroy(megafarmIconObj);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to load megafarm icon: {ex.Message}");
                Destroy(megafarmIconObj);
            }
        }
        // No secondary icon for reset - it already has leaderboard icon as main icon

        // Center-Left: Leaderboard name and subtitle
        var textContainerObj = new GameObject("TextContainer");
        textContainerObj.transform.SetParent(entryObj.transform, false);

        var textContainerRect = textContainerObj.AddComponent<RectTransform>();
        textContainerRect.anchorMin = new Vector2(0, 0);
        textContainerRect.anchorMax = new Vector2(0.6f, 1);
        textContainerRect.offsetMin = new Vector2(75, padding);
        textContainerRect.offsetMax = new Vector2(-10, -padding);

        // Title
        var nameObj = new GameObject("Name");
        nameObj.transform.SetParent(textContainerObj.transform, false);

        var nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.55f);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;

        var nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = CodeUtilities.ToUpperSnake(data.LeaderboardName);
        nameText.fontSize = 14;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = new Color(0.90f, 0.88f, 0.92f, 1f); // M3 on-surface
        nameText.alignment = TextAlignmentOptions.BottomLeft;

        // Subtitle
        var subtitleObj = new GameObject("Subtitle");
        subtitleObj.transform.SetParent(textContainerObj.transform, false);

        var subtitleRect = subtitleObj.AddComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0, 0);
        subtitleRect.anchorMax = new Vector2(1, 0.45f);
        subtitleRect.offsetMin = Vector2.zero;
        subtitleRect.offsetMax = Vector2.zero;

        var subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
        if (data.HasPlayerEntry)
        {
            subtitleText.text = $"Best attempt";
            subtitleText.color = new Color(0.70f, 0.72f, 0.75f, 1f); // M3 on-surface-variant
        }
        else
        {
            subtitleText.text = "Not attempted";
            subtitleText.color = new Color(0.55f, 0.55f, 0.58f, 1f);
        }
        subtitleText.fontSize = 11;
        subtitleText.alignment = TextAlignmentOptions.TopLeft;

        // Right: Stats (Rank and Time)
        var statsContainerObj = new GameObject("StatsContainer");
        statsContainerObj.transform.SetParent(entryObj.transform, false);

        var statsContainerRect = statsContainerObj.AddComponent<RectTransform>();
        statsContainerRect.anchorMin = new Vector2(0.6f, 0);
        statsContainerRect.anchorMax = new Vector2(1, 1);
        statsContainerRect.offsetMin = new Vector2(10, padding);
        statsContainerRect.offsetMax = new Vector2(-15, -padding);

        if (data.HasPlayerEntry)
        {
            // Rank (top right)
            var rankObj = new GameObject("Rank");
            rankObj.transform.SetParent(statsContainerObj.transform, false);

            var rankRect = rankObj.AddComponent<RectTransform>();
            rankRect.anchorMin = new Vector2(0, 0.55f);
            rankRect.anchorMax = new Vector2(1, 1);
            rankRect.offsetMin = Vector2.zero;
            rankRect.offsetMax = Vector2.zero;

            var rankText = rankObj.AddComponent<TextMeshProUGUI>();
            rankText.text = $"#{data.PlayerRank}";
            rankText.fontSize = 16;
            rankText.fontStyle = FontStyles.Bold;
            rankText.color = new Color(0.95f, 0.82f, 0.40f, 1f); // M3 primary (gold)
            rankText.alignment = TextAlignmentOptions.BottomRight;

            // Time (bottom right)
            var timeObj = new GameObject("Time");
            timeObj.transform.SetParent(statsContainerObj.transform, false);

            var timeRect = timeObj.AddComponent<RectTransform>();
            timeRect.anchorMin = new Vector2(0, 0);
            timeRect.anchorMax = new Vector2(1, 0.45f);
            timeRect.offsetMin = Vector2.zero;
            timeRect.offsetMax = Vector2.zero;

            var timeText = timeObj.AddComponent<TextMeshProUGUI>();
            var timeString = LeaderboardManager.StringFromTimeSpan(TimeSpan.FromMilliseconds(data.PlayerScore));
            timeText.text = timeString;
            timeText.fontSize = 12;
            timeText.color = new Color(0.65f, 0.78f, 0.88f, 1f); // M3 secondary (light blue)
            timeText.alignment = TextAlignmentOptions.TopRight;
        }
        else
        {
            // "No data" centered
            var noDataObj = new GameObject("NoData");
            noDataObj.transform.SetParent(statsContainerObj.transform, false);

            var noDataRect = noDataObj.AddComponent<RectTransform>();
            noDataRect.anchorMin = Vector2.zero;
            noDataRect.anchorMax = Vector2.one;
            noDataRect.offsetMin = Vector2.zero;
            noDataRect.offsetMax = Vector2.zero;

            var noDataText = noDataObj.AddComponent<TextMeshProUGUI>();
            noDataText.text = "—";
            noDataText.fontSize = 20;
            noDataText.color = new Color(0.45f, 0.45f, 0.48f, 1f);
            noDataText.alignment = TextAlignmentOptions.Center;
        }

        entryObjects.Add(entryObj);
    }

    public void Show()
    {
        if (panel != null)
        {
            panel.SetActive(true);
            isVisible = true;
            Plugin.Log.LogInfo("Leaderboard overview UI shown");
        }
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
            isVisible = false;
            Plugin.Log.LogInfo("Leaderboard overview UI hidden");
        }
    }

    public void Toggle()
    {
        if (isVisible)
            Hide();
        else
            Show();
    }
}
