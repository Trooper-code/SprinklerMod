using System;
using System.Collections.Generic;
using MelonLoader;
using S1API.UI;
using ScheduleOne.Property;
using UnityEngine;
using UnityEngine.UI;
using S1PhoneApp = S1API.PhoneApp.PhoneApp;

namespace OverheadSprinkler;

/// <summary>
/// In-game "Sprinklers" phone app built on S1API.PhoneApp.
/// Lists every owned property with Overhead Sprinkler Heads and lets the player
/// water any property or all properties at once from anywhere.
/// </summary>
public sealed class SprinklerPhoneApp : S1PhoneApp
{
    private static Sprite? _cachedIconSprite;

    private RectTransform? _listContent;
    private Text? _statusText;
    private Button? _waterAllButton;

    public SprinklerPhoneApp()
    {
    }

    protected override string AppName => "overhead_sprinklers";

    protected override string AppTitle => "Sprinklers";

    protected override string IconLabel => "Sprinklers";

    protected override string IconFileName => "sprinklers_icon.png";

    protected override Sprite? IconSprite => _cachedIconSprite ??= CreateDropletIconSprite();

    protected override EOrientation Orientation => EOrientation.Horizontal;

    protected override void OnCreatedUI(GameObject container)
    {
        GameObject background = UIFactory.Panel(
            "SprinklerAppBackground",
            container.transform,
            new Color(0.09f, 0.11f, 0.15f, 1f),
            fullAnchor: true);

        GameObject topBar = UIFactory.TopBar(
            "SprinklerTopBar",
            background.transform,
            "Overhead Sprinklers",
            0.85f,
            20,
            20,
            10,
            10);

        (_, Button refreshBtn, _) = UIFactory.RoundedButtonWithLabel(
            "RefreshButton",
            "Refresh",
            topBar.transform,
            new Color(0.24f, 0.28f, 0.36f, 1f),
            110f,
            40f,
            15,
            Color.white);
        refreshBtn.onClick.AddListener(() =>
        {
            RefreshList();
            SetStatus("Refreshed property list.");
        });

        (_, Button waterAllBtn, _) = UIFactory.RoundedButtonWithLabel(
            "WaterAllButton",
            "Water All",
            topBar.transform,
            new Color(0.14f, 0.52f, 0.85f, 1f),
            130f,
            40f,
            15,
            Color.white);
        waterAllBtn.onClick.AddListener(OnWaterAllClicked);
        _waterAllButton = waterAllBtn;

        GameObject statusPanel = UIFactory.Panel(
            "StatusBanner",
            background.transform,
            new Color(0.13f, 0.16f, 0.22f, 1f),
            new Vector2(0f, 0.76f),
            new Vector2(1f, 0.85f));
        RectTransform statusRect = statusPanel.GetComponent<RectTransform>();
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;

        _statusText = UIFactory.Text(
            "StatusText",
            "Tap Water on a property or Water All.",
            statusPanel.transform,
            14,
            TextAnchor.MiddleLeft,
            FontStyle.Normal);
        _statusText.color = new Color(0.78f, 0.86f, 0.96f, 1f);
        RectTransform statusTextRect = _statusText.GetComponent<RectTransform>();
        statusTextRect.anchorMin = Vector2.zero;
        statusTextRect.anchorMax = Vector2.one;
        statusTextRect.offsetMin = new Vector2(20f, 4f);
        statusTextRect.offsetMax = new Vector2(-20f, -4f);

        GameObject listHost = UIFactory.Panel(
            "PropertyListHost",
            background.transform,
            new Color(0f, 0f, 0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0.76f));
        RectTransform listHostRect = listHost.GetComponent<RectTransform>();
        listHostRect.offsetMin = new Vector2(12f, 12f);
        listHostRect.offsetMax = new Vector2(-12f, -8f);

        _listContent = UIFactory.ScrollableVerticalList("PropertyList", listHost.transform, out _);

        SprinklerAppRefreshHook refreshHook = container.AddComponent<SprinklerAppRefreshHook>();
        refreshHook.OnShown = RefreshList;

        RefreshList();
    }

    public void RefreshList()
    {
        if (_listContent == null)
        {
            return;
        }

        UIFactory.ClearChildren(_listContent);

        List<PropertySprinklerInfo> properties = SprinklerController.GetOwnedPropertiesWithSprinklers();
        if (_waterAllButton != null)
        {
            _waterAllButton.interactable = properties.Count > 0;
        }

        if (properties.Count == 0)
        {
            GameObject emptyCard = UIFactory.Panel("EmptyStateCard", _listContent, new Color(0.14f, 0.17f, 0.22f, 1f));
            LayoutElement emptyLayout = emptyCard.AddComponent<LayoutElement>();
            emptyLayout.minHeight = 90f;
            emptyLayout.flexibleWidth = 1f;

            Text emptyText = UIFactory.Text(
                "EmptyStateText",
                "No Overhead Sprinkler Heads installed yet.\nBuy them from Oscar at the Warehouse ($1,500) and hang them on a Suspension Rack.",
                emptyCard.transform,
                16,
                TextAnchor.MiddleCenter,
                FontStyle.Normal);
            emptyText.color = new Color(0.75f, 0.80f, 0.88f, 1f);
            RectTransform emptyTextRect = emptyText.GetComponent<RectTransform>();
            emptyTextRect.anchorMin = Vector2.zero;
            emptyTextRect.anchorMax = Vector2.one;
            emptyTextRect.offsetMin = new Vector2(16f, 12f);
            emptyTextRect.offsetMax = new Vector2(-16f, -12f);

            UIFactory.FitContentHeight(_listContent);
            return;
        }

        for (int i = 0; i < properties.Count; i++)
        {
            AddPropertyRow(properties[i]);
        }

        UIFactory.FitContentHeight(_listContent);
    }

    private void AddPropertyRow(PropertySprinklerInfo info)
    {
        if (_listContent == null)
        {
            return;
        }

        GameObject row = UIFactory.Panel(
            "PropertyRow_" + info.PropertyName,
            _listContent,
            new Color(0.15f, 0.18f, 0.24f, 1f));
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.minHeight = 76f;
        rowLayout.preferredHeight = 76f;
        rowLayout.flexibleWidth = 1f;

        UIFactory.HorizontalLayoutOnGO(row, spacing: 16, padLeft: 18, padRight: 18, padTop: 10, padBottom: 10);

        GameObject textColumn = new GameObject("TextColumn");
        textColumn.transform.SetParent(row.transform, false);
        LayoutElement textColLayout = textColumn.AddComponent<LayoutElement>();
        textColLayout.flexibleWidth = 1f;
        UIFactory.VerticalLayoutOnGO(textColumn, spacing: 4);

        Text titleText = UIFactory.Text(
            "PropertyTitle",
            info.PropertyName,
            textColumn.transform,
            18,
            TextAnchor.MiddleLeft,
            FontStyle.Bold);
        titleText.color = Color.white;

        string headWord = info.HeadCount == 1 ? "head" : "heads";
        string rackWord = info.RackCount == 1 ? "rack" : "racks";
        string potWord = info.ContainerCount == 1 ? "pot" : "pots";
        string moistureText = info.ContainerCount > 0
            ? $"Avg water: {info.AverageMoistureNormalized * 100f:F0}%"
            : "No pots under racks";

        Text subtitleText = UIFactory.Text(
            "PropertyDetails",
            $"{info.HeadCount} {headWord} · {info.RackCount} {rackWord} · {info.ContainerCount} {potWord} · {moistureText}",
            textColumn.transform,
            14,
            TextAnchor.MiddleLeft,
            FontStyle.Normal);
        subtitleText.color = new Color(0.75f, 0.82f, 0.90f, 1f);

        (_, Button waterBtn, _) = UIFactory.RoundedButtonWithLabel(
            "WaterButton_" + info.PropertyName,
            "Water",
            row.transform,
            new Color(0.16f, 0.56f, 0.88f, 1f),
            120f,
            44f,
            16,
            Color.white);

        Property targetProperty = info.Property;
        waterBtn.onClick.AddListener(() => OnWaterPropertyClicked(targetProperty));
    }

    private void OnWaterPropertyClicked(Property property)
    {
        try
        {
            WaterSummary summary = SprinklerController.WaterProperty(property);
            RefreshList();

            if (summary.SkippedNotServer)
            {
                SetStatus($"Cannot water {summary.PropertyName} as a non-host client yet (co-op sync comes in M4).");
                return;
            }

            string potWord = summary.ContainersWatered == 1 ? "pot" : "pots";
            string headWord = summary.HeadCount == 1 ? "head" : "heads";
            string msg = $"Watered {summary.PropertyName}: {summary.HeadCount} {headWord}, {summary.ContainersWatered} {potWord} filled to 100%.";
            SetStatus(msg);
            MelonLogger.Msg("[OverheadSprinkler] " + msg);
        }
        catch (Exception ex)
        {
            SetStatus($"Failed to water {property.PropertyName}: {ex.Message}");
            MelonLogger.Error($"[OverheadSprinkler] PhoneApp WaterProperty failed: {ex}");
        }
    }

    private void OnWaterAllClicked()
    {
        try
        {
            List<WaterSummary> summaries = SprinklerController.WaterAllOwnedProperties();
            RefreshList();

            if (summaries.Count == 0)
            {
                SetStatus("No properties with Overhead Sprinkler Heads found.");
                return;
            }

            int totalPots = 0;
            int totalHeads = 0;
            bool anySkipped = false;
            for (int i = 0; i < summaries.Count; i++)
            {
                if (summaries[i].SkippedNotServer)
                {
                    anySkipped = true;
                }
                else
                {
                    totalPots += summaries[i].ContainersWatered;
                    totalHeads += summaries[i].HeadCount;
                }
            }

            if (anySkipped && totalPots == 0)
            {
                SetStatus("Cannot water as a non-host client yet (co-op sync comes in M4).");
                return;
            }

            string propWord = summaries.Count == 1 ? "property" : "properties";
            string potWord = totalPots == 1 ? "pot" : "pots";
            string msg = $"Watered {summaries.Count} {propWord} ({totalHeads} heads, {totalPots} {potWord} filled to 100%).";
            SetStatus(msg);
            MelonLogger.Msg("[OverheadSprinkler] " + msg);
        }
        catch (Exception ex)
        {
            SetStatus($"Water All failed: {ex.Message}");
            MelonLogger.Error($"[OverheadSprinkler] PhoneApp WaterAll failed: {ex}");
        }
    }

    private void SetStatus(string message)
    {
        if (_statusText != null)
        {
            _statusText.text = message;
        }
    }

    private static Sprite CreateDropletIconSprite()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;

        var bgOuter = new Color(0.08f, 0.24f, 0.44f, 1f);
        var bgInner = new Color(0.12f, 0.36f, 0.64f, 1f);
        var dropFill = new Color(0.58f, 0.88f, 1f, 1f);
        var dropHighlight = new Color(0.88f, 0.97f, 1f, 1f);
        var clear = new Color(0f, 0f, 0f, 0f);

        const float cornerRadius = 14f;
        const float half = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Rounded-square distance check.
                float dx = Mathf.Max(Mathf.Abs(x - half) - (half - cornerRadius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y - half) - (half - cornerRadius), 0f);
                float cornerDist = Mathf.Sqrt(dx * dx + dy * dy);
                if (cornerDist > cornerRadius)
                {
                    texture.SetPixel(x, y, clear);
                    continue;
                }

                float gradT = y / (float)(size - 1);
                Color pixel = Color.Lerp(bgOuter, bgInner, gradT);

                // Water droplet centered horizontally, bulbous bottom + tapered tip at top.
                float nx = (x - half) / 18f;
                float ny = (y - 25f) / 18f;
                bool inDrop = false;
                if (ny <= 0f)
                {
                    inDrop = (nx * nx + ny * ny) <= 0.72f;
                }
                else if (ny <= 1.25f)
                {
                    float widthAtY = 0.85f * (1f - ny / 1.25f);
                    inDrop = Mathf.Abs(nx) <= widthAtY;
                }

                if (inDrop)
                {
                    bool highlight = nx < -0.15f && nx > -0.45f && ny < 0.25f && ny > -0.35f;
                    pixel = highlight ? dropHighlight : dropFill;
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }

    private sealed class SprinklerAppRefreshHook : MonoBehaviour
    {
        public Action? OnShown;

        private void OnEnable()
        {
            OnShown?.Invoke();
        }
    }
}
