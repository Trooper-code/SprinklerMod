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
/// Presents a responsive smart-irrigation dashboard for every owned property
/// with Overhead Sprinkler Heads installed.
/// </summary>
public sealed class SprinklerPhoneApp : S1PhoneApp
{
    private static Sprite? _cachedIconSprite;
    private static Sprite? _cachedRoundedSprite;

    // Palette
    private static readonly Color BgRoot = new(0.055f, 0.078f, 0.106f, 1f);
    private static readonly Color CardHeader = new(0.082f, 0.133f, 0.188f, 1f);
    private static readonly Color CardProperty = new(0.090f, 0.129f, 0.173f, 1f);
    private static readonly Color FooterBg = new(0.071f, 0.102f, 0.141f, 1f);
    private static readonly Color TrackBg = new(0.043f, 0.063f, 0.090f, 1f);

    private static readonly Color AccentCyan = new(0.133f, 0.655f, 0.941f, 1f);
    private static readonly Color BtnPrimary = new(0.106f, 0.541f, 0.902f, 1f);
    private static readonly Color BtnSecondary = new(0.141f, 0.204f, 0.278f, 1f);
    private static readonly Color BtnTopUp = new(0.129f, 0.478f, 0.408f, 1f);

    private static readonly Color StatusDry = new(0.878f, 0.322f, 0.322f, 1f);
    private static readonly Color StatusLow = new(0.910f, 0.659f, 0.220f, 1f);
    private static readonly Color StatusHealthy = new(0.157f, 0.761f, 0.627f, 1f);

    private static readonly Color TextMuted = new(0.561f, 0.659f, 0.761f, 1f);
    private static readonly Color TextStats = new(0.604f, 0.690f, 0.780f, 1f);
    private static readonly Color TextStatus = new(0.486f, 0.769f, 0.980f, 1f);

    private RectTransform? _listContent;
    private Text? _headerSummaryText;
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
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Full-screen background
        GameObject background = UIFactory.Panel("SprinklerAppBackground", container.transform, BgRoot, fullAnchor: true);

        // Safe-area root so nothing clips against the phone bezel
        GameObject safeArea = new("SafeArea");
        safeArea.transform.SetParent(background.transform, false);
        RectTransform safeRect = safeArea.AddComponent<RectTransform>();
        safeRect.anchorMin = Vector2.zero;
        safeRect.anchorMax = Vector2.one;
        safeRect.offsetMin = new Vector2(24f, 18f);
        safeRect.offsetMax = new Vector2(-24f, -18f);

        VerticalLayoutGroup rootLayout = safeArea.AddComponent<VerticalLayoutGroup>();
        rootLayout.spacing = 12f;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        BuildHeaderCard(safeArea.transform, font);
        BuildPropertyListHost(safeArea.transform);
        BuildFooterBar(safeArea.transform, font);

        SprinklerAppRefreshHook refreshHook = container.AddComponent<SprinklerAppRefreshHook>();
        refreshHook.OnShown = RefreshList;

        RefreshList();
    }

    private void BuildHeaderCard(Transform parent, Font font)
    {
        GameObject header = CreateRoundedPanel("HeaderCard", parent, CardHeader);
        LayoutElement headerLayout = header.AddComponent<LayoutElement>();
        headerLayout.minHeight = 68f;
        headerLayout.preferredHeight = 68f;
        headerLayout.flexibleWidth = 1f;

        HorizontalLayoutGroup row = header.AddComponent<HorizontalLayoutGroup>();
        row.padding = new RectOffset(18, 18, 10, 10);
        row.spacing = 14f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        // Subtle 2px cyan accent line along the bottom of the header card
        GameObject accentLine = new("HeaderAccentLine");
        accentLine.transform.SetParent(header.transform, false);
        RectTransform accentRect = accentLine.AddComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(1f, 0f);
        accentRect.pivot = new Vector2(0.5f, 0f);
        accentRect.offsetMin = new Vector2(6f, 0f);
        accentRect.offsetMax = new Vector2(-6f, 2f);
        Image accentImg = accentLine.AddComponent<Image>();
        accentImg.color = AccentCyan;
        LayoutElement accentIgnore = accentLine.AddComponent<LayoutElement>();
        accentIgnore.ignoreLayout = true;

        // App icon
        GameObject iconGo = new("HeaderIcon");
        iconGo.transform.SetParent(header.transform, false);
        Image iconImg = iconGo.AddComponent<Image>();
        iconImg.sprite = IconSprite;
        iconImg.preserveAspect = true;
        LayoutElement iconLe = iconGo.AddComponent<LayoutElement>();
        iconLe.minWidth = 38f;
        iconLe.minHeight = 38f;
        iconLe.preferredWidth = 38f;
        iconLe.preferredHeight = 38f;

        // Title + live summary stack
        GameObject titleCol = new("TitleStack");
        titleCol.transform.SetParent(header.transform, false);
        LayoutElement titleColLe = titleCol.AddComponent<LayoutElement>();
        titleColLe.flexibleWidth = 1f;

        VerticalLayoutGroup titleV = titleCol.AddComponent<VerticalLayoutGroup>();
        titleV.spacing = 2f;
        titleV.childAlignment = TextAnchor.MiddleLeft;
        titleV.childControlWidth = true;
        titleV.childControlHeight = true;
        titleV.childForceExpandWidth = true;
        titleV.childForceExpandHeight = false;

        Text titleText = CreateText("AppTitle", "SPRINKLERS", titleCol.transform, font, 20, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        _headerSummaryText = CreateText(
            "HeaderSummary",
            "0 Properties · 0 Heads · 0 Pots",
            titleCol.transform,
            font,
            13,
            FontStyle.Normal,
            TextMuted,
            TextAnchor.MiddleLeft);
        _headerSummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        // Action buttons
        Button refreshBtn = CreateRoundedButton("RefreshButton", "Refresh", header.transform, font, BtnSecondary, 96f, 38f, 14, FontStyle.Normal);
        refreshBtn.onClick.AddListener(() =>
        {
            RefreshList();
            SetStatus("Refreshed sprinkler status.");
        });

        _waterAllButton = CreateRoundedButton("WaterAllButton", "Water All", header.transform, font, BtnPrimary, 124f, 38f, 14, FontStyle.Bold);
        _waterAllButton.onClick.AddListener(OnWaterAllClicked);
    }

    private void BuildPropertyListHost(Transform parent)
    {
        GameObject listHost = new("PropertyListHost");
        listHost.transform.SetParent(parent, false);
        listHost.AddComponent<RectTransform>();

        LayoutElement hostLe = listHost.AddComponent<LayoutElement>();
        hostLe.flexibleWidth = 1f;
        hostLe.flexibleHeight = 1f;
        hostLe.minHeight = 120f;

        _listContent = UIFactory.ScrollableVerticalList("PropertyList", listHost.transform, out ScrollRect scrollRect);
        scrollRect.scrollSensitivity = 25f;

        VerticalLayoutGroup listLayout = _listContent.GetComponent<VerticalLayoutGroup>();
        if (listLayout != null)
        {
            listLayout.spacing = 12f;
            listLayout.padding = new RectOffset(2, 2, 2, 2);
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
        }
    }

    private void BuildFooterBar(Transform parent, Font font)
    {
        GameObject footer = CreateRoundedPanel("FooterBar", parent, FooterBg);
        LayoutElement footerLe = footer.AddComponent<LayoutElement>();
        footerLe.minHeight = 34f;
        footerLe.preferredHeight = 34f;
        footerLe.flexibleWidth = 1f;

        HorizontalLayoutGroup footerLayout = footer.AddComponent<HorizontalLayoutGroup>();
        footerLayout.padding = new RectOffset(14, 14, 6, 6);
        footerLayout.childAlignment = TextAnchor.MiddleLeft;
        footerLayout.childControlWidth = true;
        footerLayout.childControlHeight = true;
        footerLayout.childForceExpandWidth = true;
        footerLayout.childForceExpandHeight = true;

        _statusText = CreateText(
            "StatusText",
            "Ready — select a property or tap Water All.",
            footer.transform,
            font,
            13,
            FontStyle.Normal,
            TextStatus,
            TextAnchor.MiddleLeft);
    }

    public void RefreshList()
    {
        if (_listContent == null)
        {
            return;
        }

        UIFactory.ClearChildren(_listContent);
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        List<PropertySprinklerInfo> properties = SprinklerController.GetOwnedPropertiesWithSprinklers();

        int totalHeads = 0;
        int totalPots = 0;
        for (int i = 0; i < properties.Count; i++)
        {
            totalHeads += properties[i].HeadCount;
            totalPots += properties[i].ContainerCount;
        }

        if (_headerSummaryText != null)
        {
            string propWord = properties.Count == 1 ? "Property" : "Properties";
            string headWord = totalHeads == 1 ? "Head" : "Heads";
            string potWord = totalPots == 1 ? "Pot" : "Pots";
            _headerSummaryText.text = $"{properties.Count} {propWord} · {totalHeads} {headWord} · {totalPots} {potWord}";
        }

        if (_waterAllButton != null)
        {
            _waterAllButton.interactable = properties.Count > 0;
        }

        if (properties.Count == 0)
        {
            GameObject emptyCard = CreateRoundedPanel("EmptyStateCard", _listContent, CardProperty);
            LayoutElement emptyLayout = emptyCard.AddComponent<LayoutElement>();
            emptyLayout.minHeight = 110f;
            emptyLayout.preferredHeight = 110f;
            emptyLayout.flexibleWidth = 1f;

            VerticalLayoutGroup emptyV = emptyCard.AddComponent<VerticalLayoutGroup>();
            emptyV.padding = new RectOffset(24, 24, 20, 20);
            emptyV.childAlignment = TextAnchor.MiddleCenter;
            emptyV.childControlWidth = true;
            emptyV.childControlHeight = true;
            emptyV.childForceExpandWidth = true;
            emptyV.childForceExpandHeight = true;

            CreateText(
                "EmptyStateText",
                "No Overhead Sprinkler Heads installed yet.\nBuy them from Oscar at the Warehouse ($1,500) and hang them on a Suspension Rack.",
                emptyCard.transform,
                font,
                15,
                FontStyle.Normal,
                TextStats,
                TextAnchor.MiddleCenter);

            UIFactory.FitContentHeight(_listContent);
            return;
        }

        for (int i = 0; i < properties.Count; i++)
        {
            AddPropertyCard(properties[i], font);
        }

        UIFactory.FitContentHeight(_listContent);
    }

    private void AddPropertyCard(PropertySprinklerInfo info, Font font)
    {
        if (_listContent == null)
        {
            return;
        }

        float moisture = Mathf.Clamp01(info.AverageMoistureNormalized);
        int moisturePct = Mathf.RoundToInt(moisture * 100f);
        Color statusColor = GetStatusColor(moisture, info.ContainerCount);
        string statusBadge = GetStatusLabel(moisture, moisturePct, info.ContainerCount);

        GameObject card = CreateRoundedPanel("PropertyCard_" + info.PropertyName, _listContent, CardProperty);
        LayoutElement cardLe = card.AddComponent<LayoutElement>();
        cardLe.minHeight = 104f;
        cardLe.preferredHeight = 104f;
        cardLe.flexibleWidth = 1f;

        HorizontalLayoutGroup cardLayout = card.AddComponent<HorizontalLayoutGroup>();
        cardLayout.padding = new RectOffset(20, 18, 14, 14);
        cardLayout.spacing = 18f;
        cardLayout.childAlignment = TextAnchor.MiddleLeft;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = false;
        cardLayout.childForceExpandHeight = false;

        // 6px left status accent bar (ignored by layout so it hugs the card edge)
        GameObject accentBar = new("StatusAccentBar");
        accentBar.transform.SetParent(card.transform, false);
        RectTransform accentRect = accentBar.AddComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.offsetMin = new Vector2(0f, 6f);
        accentRect.offsetMax = new Vector2(6f, -6f);
        Image accentImg = accentBar.AddComponent<Image>();
        accentImg.color = statusColor;
        LayoutElement accentIgnore = accentBar.AddComponent<LayoutElement>();
        accentIgnore.ignoreLayout = true;

        // Left info column
        GameObject infoCol = new("InfoColumn");
        infoCol.transform.SetParent(card.transform, false);
        LayoutElement infoColLe = infoCol.AddComponent<LayoutElement>();
        infoColLe.flexibleWidth = 1f;

        VerticalLayoutGroup infoV = infoCol.AddComponent<VerticalLayoutGroup>();
        infoV.spacing = 8f;
        infoV.childAlignment = TextAnchor.MiddleLeft;
        infoV.childControlWidth = true;
        infoV.childControlHeight = true;
        infoV.childForceExpandWidth = true;
        infoV.childForceExpandHeight = false;

        // Top row: Property Name + Status Badge
        GameObject topRow = new("CardTopRow");
        topRow.transform.SetParent(infoCol.transform, false);
        LayoutElement topRowLe = topRow.AddComponent<LayoutElement>();
        topRowLe.preferredHeight = 24f;

        HorizontalLayoutGroup topRowH = topRow.AddComponent<HorizontalLayoutGroup>();
        topRowH.spacing = 10f;
        topRowH.childAlignment = TextAnchor.MiddleLeft;
        topRowH.childControlWidth = true;
        topRowH.childControlHeight = true;
        topRowH.childForceExpandWidth = false;
        topRowH.childForceExpandHeight = true;

        Text nameText = CreateText("PropertyName", info.PropertyName, topRow.transform, font, 19, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        nameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        Text badgeText = CreateText("StatusBadge", statusBadge, topRow.transform, font, 13, FontStyle.Bold, statusColor, TextAnchor.MiddleRight);
        LayoutElement badgeLe = badgeText.gameObject.AddComponent<LayoutElement>();
        badgeLe.minWidth = 100f;
        badgeLe.preferredWidth = 120f;

        // Visual Moisture Progress Bar
        GameObject track = CreateRoundedPanel("MoistureTrack", infoCol.transform, TrackBg);
        LayoutElement trackLe = track.AddComponent<LayoutElement>();
        trackLe.minHeight = 12f;
        trackLe.preferredHeight = 12f;

        GameObject fill = CreateRoundedPanel("MoistureFill", track.transform, statusColor);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(info.ContainerCount > 0 ? moisture : 0f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // Bottom stats row
        string headWord = info.HeadCount == 1 ? "Head" : "Heads";
        string rackWord = info.RackCount == 1 ? "Rack" : "Racks";
        string potLabel = info.ContainerCount > 0
            ? $"{info.ContainerCount} {(info.ContainerCount == 1 ? "Pot" : "Pots")}"
            : "No pots under racks";

        Text statsText = CreateText(
            "StatsText",
            $"{info.HeadCount} {headWord}   ·   {info.RackCount} {rackWord}   ·   {potLabel}",
            infoCol.transform,
            font,
            13,
            FontStyle.Normal,
            TextStats,
            TextAnchor.MiddleLeft);
        statsText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        // Right action button
        bool alreadyFull = info.ContainerCount > 0 && moisture >= 0.99f;
        string buttonLabel = alreadyFull ? "Top Up" : "Water Now";
        Color buttonColor = alreadyFull ? BtnTopUp : BtnPrimary;

        Button waterBtn = CreateRoundedButton(
            "WaterButton_" + info.PropertyName,
            buttonLabel,
            card.transform,
            font,
            buttonColor,
            132f,
            46f,
            15,
            FontStyle.Bold);

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

    private static Color GetStatusColor(float moisture, int containerCount)
    {
        if (containerCount == 0)
        {
            return TextMuted;
        }
        if (moisture < 0.30f)
        {
            return StatusDry;
        }
        if (moisture < 0.70f)
        {
            return StatusLow;
        }
        return StatusHealthy;
    }

    private static string GetStatusLabel(float moisture, int moisturePct, int containerCount)
    {
        if (containerCount == 0)
        {
            return "NO POTS";
        }
        if (moisture < 0.30f)
        {
            return $"DRY · {moisturePct}%";
        }
        if (moisture < 0.70f)
        {
            return $"LOW · {moisturePct}%";
        }
        if (moisture >= 0.99f)
        {
            return $"FULL · {moisturePct}%";
        }
        return $"GOOD · {moisturePct}%";
    }

    private static GameObject CreateRoundedPanel(string name, Transform parent, Color color)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        Image img = go.AddComponent<Image>();
        img.sprite = GetRoundedRectSprite();
        img.type = Image.Type.Sliced;
        img.color = color;
        return go;
    }

    private static Button CreateRoundedButton(
        string name,
        string label,
        Transform parent,
        Font font,
        Color bgColor,
        float width,
        float height,
        int fontSize,
        FontStyle fontStyle)
    {
        GameObject go = CreateRoundedPanel(name, parent, bgColor);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minWidth = width;
        le.preferredWidth = width;
        le.minHeight = height;
        le.preferredHeight = height;

        Button btn = go.AddComponent<Button>();
        Image targetImg = go.GetComponent<Image>();
        btn.targetGraphic = targetImg;

        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        btn.colors = colors;

        Text labelText = CreateText("Label", label, go.transform, font, fontSize, fontStyle, Color.white, TextAnchor.MiddleCenter);
        RectTransform labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 2f);
        labelRect.offsetMax = new Vector2(-8f, -2f);

        return btn;
    }

    private static Text CreateText(
        string name,
        string content,
        Transform parent,
        Font font,
        int fontSize,
        FontStyle style,
        Color color,
        TextAnchor alignment)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        Text txt = go.AddComponent<Text>();
        txt.text = content;
        txt.font = font;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = alignment;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Truncate;
        return txt;
    }

    private static Sprite GetRoundedRectSprite()
    {
        if (_cachedRoundedSprite != null)
        {
            return _cachedRoundedSprite;
        }

        const int size = 32;
        const float radius = 8f;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float half = (size - 1) * 0.5f;
        var white = Color.white;
        var clear = new Color(1f, 1f, 1f, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - half) - (half - radius), 0f);
                float dy = Mathf.Max(Mathf.Abs(y - half) - (half - radius), 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius + 0.5f - dist);
                tex.SetPixel(x, y, alpha > 0f ? new Color(white.r, white.g, white.b, alpha) : clear);
            }
        }

        tex.Apply();
        _cachedRoundedSprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0u,
            SpriteMeshType.FullRect,
            new Vector4(8f, 8f, 8f, 8f));
        return _cachedRoundedSprite;
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
