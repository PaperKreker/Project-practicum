using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapController : MonoBehaviour
{
    private const float MapWidth = 1180f;
    private const float RowSpacing = 220f;
    private const float BottomPadding = 150f;
    private const float TopPadding = 150f;
    private const float SidePadding = 190f;

    [Header("References")]
    [SerializeField] private Transform _nodeContainer;
    [SerializeField] private GameObject _nodeButtonPrefab;

    [Header("HUD")]
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _actText;

    private RectTransform _screenRoot;
    private RectTransform _paperInner;
    private RectTransform _viewport;
    private RectTransform _content;
    private RectTransform _decorationLayer;
    private RectTransform _connectionLayer;
    private RectTransform _runtimeNodeLayer;
    private ScrollRect _scrollRect;
    private TMP_Text _hintText;
    private TMP_Text _titleText;
    private TMP_FontAsset _fontAsset;

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[MapController] GameManager not found!");
            return;
        }

        EnsureLayout();
        RefreshHUD();
        BuildMapUI();
    }

    private void EnsureLayout()
    {
        if (_screenRoot != null)
            return;

        ResolveFontAsset();

        Canvas canvas = ResolveMainCanvas();
        if (canvas == null)
        {
            Debug.LogError("[MapController] Main canvas not found.");
            return;
        }

        HideOldNodeList();

        _screenRoot = CreateRect("MapScreenRoot", canvas.transform);
        Stretch(_screenRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        BuildScreenFrame();
        BuildTopBar();
        BuildPaperArea();
        BuildLegendPanel();
    }

    private void RefreshHUD()
    {
        RunData run = GameManager.Instance.Run;

        if (_hpText != null)
            _hpText.text = $"HP {run.PlayerHp}/{run.PlayerMaxHp}";

        if (_goldText != null)
            _goldText.text = $"Золото {run.Gold}";

        if (_actText != null)
            _actText.text = $"Акт {GameManager.Instance.CurrentActIndex + 1}";

        if (_titleText != null)
            _titleText.text = $"Карта акта {GameManager.Instance.CurrentActIndex + 1}";
    }

    private void BuildMapUI()
    {
        if (_content == null || _runtimeNodeLayer == null)
            return;

        ClearChildren(_decorationLayer);
        ClearChildren(_connectionLayer);
        ClearChildren(_runtimeNodeLayer);

        MapData map = GameManager.Instance.CurrentMap;
        RunData run = GameManager.Instance.Run;
        MapNode currentNode = map.GetNode(run.CurrentNodeIndex);

        HashSet<int> reachable = run.CurrentNodeCompleted
            ? new HashSet<int>(currentNode.NextNodeIndices)
            : new HashSet<int> { currentNode.Index };

        HashSet<int> visitedNodes = new HashSet<int>(run.VisitedNodeIndices);
        HashSet<long> traversedEdges = BuildTraversedEdges(run.VisitedNodeIndices);

        ResizeMapLayers(BottomPadding + TopPadding + map.MaxRow * RowSpacing);
        Dictionary<int, Vector2> positions = BuildNodePositions(map);

        BuildRowGuides(map);

        foreach (MapNode node in map.Nodes)
        {
            foreach (int nextIndex in node.NextNodeIndices)
            {
                bool isTraversed = traversedEdges.Contains(MakeEdgeKey(node.Index, nextIndex));
                bool isReachable = run.CurrentNodeCompleted && node.Index == currentNode.Index && reachable.Contains(nextIndex);
                DrawConnection(positions[node.Index], positions[nextIndex], isTraversed, isReachable);
            }
        }

        foreach (MapNode node in map.Nodes)
        {
            bool isCurrent = node.Index == currentNode.Index;
            bool isReachable = reachable.Contains(node.Index);
            bool isVisited = visitedNodes.Contains(node.Index);
            MapNodeView.Style style = BuildPresentation(node, isCurrent, isReachable, isVisited);

            CreateNode(node, positions[node.Index], style, isReachable);
        }

        UpdateHint(currentNode, reachable.Count);

        Canvas.ForceUpdateCanvases();
        FocusOnRow(currentNode.Row);
    }

    private void CreateNode(MapNode node, Vector2 position, MapNodeView.Style style, bool isReachable)
    {
        GameObject instance = _nodeButtonPrefab != null
            ? Instantiate(_nodeButtonPrefab, _runtimeNodeLayer)
            : CreateFallbackNode(_runtimeNodeLayer);

        instance.name = $"Node_{node.Index}_{GetNodeLabel(node)}";

        RectTransform rect = instance.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;

        MapNodeView nodeView = instance.GetComponent<MapNodeView>();
        if (nodeView == null)
            nodeView = instance.AddComponent<MapNodeView>();

        nodeView.Setup(style, isReachable ? () => OnNodeClicked(node.Index) : null);
    }

    private MapNodeView.Style BuildPresentation(MapNode node, bool isCurrent, bool isReachable, bool isVisited)
    {
        (Color fill, Color frame) = GetNodeColors(node);
        string kind = GetNodeKind(node);
        string subtitle = kind;

        if (isCurrent)
            subtitle = isReachable ? $"{kind} / выбор" : $"{kind} / вы здесь";
        else if (isReachable)
            subtitle = $"{kind} / доступно";
        else if (isVisited)
            subtitle = $"{kind} / пройдено";

        Color titleColor = Color.white;
        Color subtitleColor = new Color(0.98f, 0.97f, 0.9f, 1f);
        Color iconColor = Color.white;
        Color glowColor = new Color(0.98f, 0.84f, 0.45f, 0.42f);
        bool showGlow = isReachable || isCurrent;

        if (!isReachable && !isCurrent && !isVisited)
        {
            fill = Color.Lerp(fill, Color.black, 0.42f);
            frame = Color.Lerp(frame, Color.black, 0.3f);
            titleColor = new Color(0.9f, 0.89f, 0.85f, 0.94f);
            subtitleColor = new Color(0.82f, 0.8f, 0.76f, 0.92f);
            iconColor = new Color(0.98f, 0.98f, 0.98f, 0.9f);
            showGlow = false;
        }
        else if (isVisited && !isCurrent)
        {
            fill = Color.Lerp(fill, Color.black, 0.18f);
            frame = Color.Lerp(frame, Color.white, 0.12f);
            glowColor = new Color(0.9f, 0.78f, 0.46f, 0.24f);
        }

        if (isCurrent)
        {
            glowColor = new Color(1f, 0.9f, 0.58f, 0.52f);
            frame = Color.Lerp(frame, Color.white, 0.18f);
        }

        Vector2 size = new Vector2(180f, 160f);
        if (node.Type == NodeType.Start)
            size = new Vector2(172f, 152f);
        else if (node.Enemy != null && node.Enemy.Tier == EnemyTier.Boss)
            size = new Vector2(194f, 172f);

        return new MapNodeView.Style
        {
            Icon = GetNodeIcon(node),
            Title = GetNodeLabel(node),
            Subtitle = subtitle,
            Size = size,
            GlowColor = glowColor,
            FrameColor = frame,
            FillColor = fill,
            IconColor = iconColor,
            TitleColor = titleColor,
            SubtitleColor = subtitleColor,
            ShowGlow = showGlow,
            Interactable = isReachable,
        };
    }

    private void DrawConnection(Vector2 from, Vector2 to, bool isTraversed, bool isReachable)
    {
        Color mainColor;
        float thickness;

        if (isTraversed)
        {
            mainColor = new Color(1f, 0.84f, 0.35f, 0.98f);
            thickness = 8f;
        }
        else if (isReachable)
        {
            mainColor = new Color(0.98f, 0.94f, 0.78f, 0.95f);
            thickness = 7f;
        }
        else
        {
            mainColor = new Color(0.23f, 0.18f, 0.13f, 0.45f);
            thickness = 4.5f;
        }

        Color shadowColor = new Color(0f, 0f, 0f, isTraversed || isReachable ? 0.18f : 0.1f);
        CreateConnectionSegment(from, to, thickness + 6f, shadowColor);
        CreateConnectionSegment(from, to, thickness, mainColor);
    }

    private void CreateConnectionSegment(Vector2 from, Vector2 to, float thickness, Color color)
    {
        GameObject segment = new GameObject("PathSegment", typeof(RectTransform), typeof(Image));
        segment.transform.SetParent(_connectionLayer, false);
        segment.layer = _connectionLayer.gameObject.layer;

        RectTransform rect = segment.GetComponent<RectTransform>();
        Vector2 direction = to - from;
        float length = direction.magnitude;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(length, thickness);
        rect.anchoredPosition = from;
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        Image image = segment.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private void BuildRowGuides(MapData map)
    {
        for (int row = 0; row <= map.MaxRow; row++)
        {
            float y = BottomPadding + row * RowSpacing;

            GameObject line = new GameObject($"RowGuide_{row}", typeof(RectTransform), typeof(Image));
            line.transform.SetParent(_decorationLayer, false);
            line.layer = _decorationLayer.gameObject.layer;

            RectTransform lineRect = line.GetComponent<RectTransform>();
            lineRect.anchorMin = Vector2.zero;
            lineRect.anchorMax = Vector2.zero;
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.anchoredPosition = new Vector2(MapWidth * 0.5f, y);
            lineRect.sizeDelta = new Vector2(MapWidth - 260f, row == 0 ? 4f : 2.5f);

            Image lineImage = line.GetComponent<Image>();
            lineImage.color = row == 0
                ? new Color(0.24f, 0.22f, 0.18f, 0.28f)
                : new Color(0.18f, 0.16f, 0.14f, 0.12f);
            lineImage.raycastTarget = false;
        }
    }

    private void UpdateHint(MapNode currentNode, int reachableCount)
    {
        if (_hintText == null)
            return;

        if (currentNode.Type == NodeType.Start)
        {
            _hintText.text = "Выберите первый узел. Подсвеченные ветви показывают доступные пути.";
            return;
        }

        if (reachableCount > 0)
        {
            _hintText.text = reachableCount == 1
                ? "Доступен один следующий узел. Продолжайте по подсвеченной ветви."
                : "Доступно несколько путей. Сравните типы узлов и выберите следующий маршрут.";
            return;
        }

        _hintText.text = "Путь завершен.";
    }

    private void FocusOnRow(int row)
    {
        if (_scrollRect == null || _viewport == null || _content == null)
            return;

        float viewHeight = _viewport.rect.height;
        float contentHeight = _content.rect.height;
        float maxScroll = Mathf.Max(0f, contentHeight - viewHeight);
        if (maxScroll <= 0.01f)
        {
            _scrollRect.verticalNormalizedPosition = 0f;
            return;
        }

        float focusY = BottomPadding + row * RowSpacing;
        float targetOffset = Mathf.Clamp(focusY - viewHeight * 0.35f, 0f, maxScroll);
        _scrollRect.verticalNormalizedPosition = targetOffset / maxScroll;
    }

    private Vector2 GetNodePosition(MapNode node)
    {
        float x = Mathf.Lerp(SidePadding, MapWidth - SidePadding, node.NormalizedX);
        float y = BottomPadding + node.Row * RowSpacing;
        return new Vector2(x, y);
    }

    private static HashSet<long> BuildTraversedEdges(List<int> history)
    {
        HashSet<long> edges = new HashSet<long>();

        for (int i = 0; i < history.Count - 1; i++)
            edges.Add(MakeEdgeKey(history[i], history[i + 1]));

        return edges;
    }

    private static long MakeEdgeKey(int from, int to)
    {
        return ((long)from << 32) | (uint)to;
    }

    private static string GetNodeLabel(MapNode node)
    {
        return node.Type switch
        {
            NodeType.Start => "Старт",
            NodeType.Shop => "Магазин",
            NodeType.Rest => "Отдых",
            NodeType.Battle when node.Enemy != null => node.Enemy.EnemyName,
            _ => "Путь",
        };
    }

    private static string GetNodeKind(MapNode node)
    {
        if (node.Type == NodeType.Start)
            return "Начало";

        if (node.Type == NodeType.Shop)
            return "Магазин";

        if (node.Type == NodeType.Rest)
            return "Отдых";

        if (node.Enemy == null)
            return "Бой";

        return node.Enemy.Tier switch
        {
            EnemyTier.Elite => "Элита",
            EnemyTier.Boss => "Босс",
            _ => "Бой",
        };
    }

    private static string GetNodeIcon(MapNode node)
    {
        if (node.Type == NodeType.Start)
            return "^";

        if (node.Type == NodeType.Shop)
            return "$";

        if (node.Type == NodeType.Rest)
            return "+";

        if (node.Enemy == null)
            return "X";

        return node.Enemy.Tier switch
        {
            EnemyTier.Elite => "!!",
            EnemyTier.Boss => "B",
            _ => "X",
        };
    }

    private static (Color fill, Color frame) GetNodeColors(MapNode node)
    {
        if (node.Type == NodeType.Start)
            return (new Color(0.18f, 0.22f, 0.26f), new Color(0.9f, 0.8f, 0.56f));

        if (node.Type == NodeType.Shop)
            return (new Color(0.15f, 0.39f, 0.39f), new Color(0.65f, 0.88f, 0.82f));

        if (node.Type == NodeType.Rest)
            return (new Color(0.28f, 0.39f, 0.23f), new Color(0.82f, 0.9f, 0.68f));

        if (node.Enemy == null)
            return (new Color(0.28f, 0.31f, 0.35f), new Color(0.78f, 0.78f, 0.73f));

        return node.Enemy.Tier switch
        {
            EnemyTier.Elite => (new Color(0.47f, 0.17f, 0.18f), new Color(0.94f, 0.72f, 0.48f)),
            EnemyTier.Boss => (new Color(0.47f, 0.34f, 0.14f), new Color(0.98f, 0.86f, 0.56f)),
            _ => (new Color(0.28f, 0.31f, 0.35f), new Color(0.78f, 0.78f, 0.73f)),
        };
    }

    private void BuildLegend(RectTransform legend)
    {
        TMP_Text title = CreateText("LegendTitle", legend, "Легенда", 30, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.08f, 0.1f, 0.14f));
        Stretch(title.rectTransform, new Vector2(0.1f, 0.86f), new Vector2(0.9f, 0.95f), Vector2.zero, Vector2.zero);

        RectTransform entries = CreateRect("Entries", legend);
        Stretch(entries, new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.82f), Vector2.zero, Vector2.zero);

        (NodeType type, EnemyTier tier, string label)[] legendEntries =
        {
            (NodeType.Battle, EnemyTier.Regular, "Бой"),
            (NodeType.Battle, EnemyTier.Elite, "Элита"),
            (NodeType.Battle, EnemyTier.Boss, "Босс"),
            (NodeType.Shop, EnemyTier.Regular, "Магазин"),
            (NodeType.Rest, EnemyTier.Regular, "Отдых"),
            (NodeType.Start, EnemyTier.Regular, "Старт"),
        };

        for (int i = 0; i < legendEntries.Length; i++)
            BuildLegendEntry(entries, i, legendEntries[i].type, legendEntries[i].tier, legendEntries[i].label);
    }

    private void BuildLegendEntry(RectTransform parent, int index, NodeType type, EnemyTier tier, string label)
    {
        RectTransform row = CreateRect($"LegendRow_{index}", parent);
        Stretch(row, new Vector2(0f, 0.8f - index * 0.16f), new Vector2(1f, 0.94f - index * 0.16f), Vector2.zero, Vector2.zero);

        MapNode node = new MapNode { Type = type };
        if (type == NodeType.Battle)
            node.Enemy = new EnemyData { Tier = tier };

        (Color fill, Color frame) = GetNodeColors(node);

        RectTransform sample = CreateRect("Sample", row);
        sample.anchorMin = new Vector2(0f, 0.5f);
        sample.anchorMax = new Vector2(0f, 0.5f);
        sample.pivot = new Vector2(0.5f, 0.5f);
        sample.sizeDelta = new Vector2(34f, 34f);
        sample.anchoredPosition = new Vector2(22f, 0f);
        sample.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image frameImage = sample.gameObject.AddComponent<Image>();
        frameImage.color = frame;
        frameImage.raycastTarget = false;

        RectTransform inner = CreateRect("Inner", sample);
        inner.anchorMin = new Vector2(0.5f, 0.5f);
        inner.anchorMax = new Vector2(0.5f, 0.5f);
        inner.pivot = new Vector2(0.5f, 0.5f);
        inner.sizeDelta = new Vector2(24f, 24f);
        inner.anchoredPosition = Vector2.zero;
        inner.localRotation = Quaternion.Euler(0f, 0f, 45f);

        Image innerImage = inner.gameObject.AddComponent<Image>();
        innerImage.color = fill;
        innerImage.raycastTarget = false;

        TMP_Text icon = CreateText("Icon", row, GetNodeIcon(node), 22, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        icon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        icon.rectTransform.sizeDelta = new Vector2(40f, 30f);
        icon.rectTransform.anchoredPosition = new Vector2(22f, 0f);

        TMP_Text text = CreateText("Label", row, label, 22, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.08f, 0.1f, 0.14f));
        Stretch(text.rectTransform, new Vector2(0f, 0.1f), new Vector2(1f, 0.9f), new Vector2(52f, 0f), Vector2.zero);
    }

    private void OnNodeClicked(int nodeIndex)
    {
        GameManager.Instance.EnterNode(nodeIndex);
    }

    private void BuildScreenFrame()
    {
        CreateShade("ShadeLeft", _screenRoot, new Vector2(0f, 0f), new Vector2(0.12f, 1f));
        CreateShade("ShadeRight", _screenRoot, new Vector2(0.88f, 0f), new Vector2(1f, 1f));
    }

    private void BuildTopBar()
    {
        RectTransform topBar = CreatePanel("TopBar", _screenRoot, new Color(0.08f, 0.09f, 0.11f, 0.92f), true);
        Stretch(topBar, new Vector2(0.045f, 0.905f), new Vector2(0.955f, 0.985f), Vector2.zero, Vector2.zero);

        _titleText = CreateText("Title", topBar, "Карта акта", 40, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.99f, 0.97f, 0.9f));
        Stretch(_titleText.rectTransform, new Vector2(0.02f, 0.2f), new Vector2(0.34f, 0.8f), Vector2.zero, Vector2.zero);

        _actText = CreateText("ActText", topBar, string.Empty, 30, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.93f, 0.66f));
        Stretch(_actText.rectTransform, new Vector2(0.52f, 0.2f), new Vector2(0.66f, 0.8f), Vector2.zero, Vector2.zero);

        _hpText = CreateText("HpText", topBar, string.Empty, 28, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        Stretch(_hpText.rectTransform, new Vector2(0.67f, 0.2f), new Vector2(0.81f, 0.8f), Vector2.zero, Vector2.zero);

        _goldText = CreateText("GoldText", topBar, string.Empty, 28, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        Stretch(_goldText.rectTransform, new Vector2(0.82f, 0.2f), new Vector2(0.97f, 0.8f), Vector2.zero, Vector2.zero);
    }

    private void BuildPaperArea()
    {
        RectTransform paper = CreatePanel("PaperPanel", _screenRoot, new Color(0.84f, 0.79f, 0.69f, 0.98f), true);
        Stretch(paper, new Vector2(0.06f, 0.08f), new Vector2(0.78f, 0.88f), Vector2.zero, Vector2.zero);

        _paperInner = CreatePanel("PaperInner", paper, new Color(0.93f, 0.89f, 0.79f, 0.98f), false);
        Stretch(_paperInner, Vector2.zero, Vector2.one, new Vector2(18f, 18f), new Vector2(-18f, -18f));

        TMP_Text paperTitle = CreateText("PaperTitle", _paperInner, "Тропа восхождения", 36, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.08f, 0.07f, 0.06f));
        Stretch(paperTitle.rectTransform, new Vector2(0.04f, 0.91f), new Vector2(0.62f, 0.985f), Vector2.zero, Vector2.zero);

        TMP_Text paperSubtitle = CreateText("PaperSubtitle", _paperInner, "Выбирайте подсвеченные узлы и стройте свой маршрут до босса.", 20, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.14f, 0.12f, 0.1f));
        Stretch(paperSubtitle.rectTransform, new Vector2(0.04f, 0.865f), new Vector2(0.72f, 0.93f), Vector2.zero, Vector2.zero);

        _hintText = CreateText("HintText", _paperInner, string.Empty, 22, FontStyles.Bold, TextAlignmentOptions.Left, new Color(0.09f, 0.08f, 0.07f));
        Stretch(_hintText.rectTransform, new Vector2(0.04f, 0.01f), new Vector2(0.96f, 0.08f), Vector2.zero, Vector2.zero);

        _viewport = CreateRect("Viewport", _paperInner);
        Stretch(_viewport, new Vector2(0.035f, 0.09f), new Vector2(0.965f, 0.84f), Vector2.zero, Vector2.zero);

        Image viewportImage = _viewport.gameObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
        viewportImage.raycastTarget = true;
        _viewport.gameObject.AddComponent<RectMask2D>();

        _scrollRect = _viewport.gameObject.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.scrollSensitivity = 48f;

        _content = CreateRect("Content", _viewport);
        _content.anchorMin = Vector2.zero;
        _content.anchorMax = Vector2.zero;
        _content.pivot = Vector2.zero;
        _content.anchoredPosition = Vector2.zero;

        _scrollRect.viewport = _viewport;
        _scrollRect.content = _content;

        _decorationLayer = CreateMapLayer("DecorationLayer");
        _connectionLayer = CreateMapLayer("ConnectionLayer");
        _runtimeNodeLayer = CreateMapLayer("NodeLayer");
    }

    private void BuildLegendPanel()
    {
        RectTransform legend = CreatePanel("LegendPanel", _screenRoot, new Color(0.78f, 0.84f, 0.88f, 0.95f), true);
        Stretch(legend, new Vector2(0.805f, 0.24f), new Vector2(0.955f, 0.76f), Vector2.zero, Vector2.zero);
        BuildLegend(legend);
    }

    private void HideOldNodeList()
    {
        if (_nodeContainer != null)
            _nodeContainer.gameObject.SetActive(false);
    }

    private Canvas ResolveMainCanvas()
    {
        if (_nodeContainer != null)
        {
            Canvas parentCanvas = _nodeContainer.GetComponentInParent<Canvas>(true);
            if (parentCanvas != null)
                return parentCanvas;
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (!canvas.name.Contains("Background"))
                return canvas;
        }

        return canvases.Length > 0 ? canvases[0] : null;
    }

    private void ResolveFontAsset()
    {
        if (_fontAsset != null)
            return;

        if (_nodeButtonPrefab != null)
        {
            TMP_Text prefabLabel = _nodeButtonPrefab.GetComponentInChildren<TMP_Text>(true);
            if (prefabLabel != null)
                _fontAsset = prefabLabel.font;
        }

        if (_fontAsset == null)
            _fontAsset = TMP_Settings.defaultFontAsset;
    }

    private RectTransform CreateMapLayer(string name)
    {
        RectTransform layer = CreateRect(name, _content);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.zero;
        layer.pivot = Vector2.zero;
        return layer;
    }

    private void ResizeMapLayers(float contentHeight)
    {
        Vector2 contentSize = new Vector2(MapWidth, contentHeight);
        _content.sizeDelta = contentSize;
        _decorationLayer.sizeDelta = contentSize;
        _connectionLayer.sizeDelta = contentSize;
        _runtimeNodeLayer.sizeDelta = contentSize;
    }

    private Dictionary<int, Vector2> BuildNodePositions(MapData map)
    {
        Dictionary<int, Vector2> positions = new Dictionary<int, Vector2>(map.Nodes.Count);

        foreach (MapNode node in map.Nodes)
            positions[node.Index] = GetNodePosition(node);

        return positions;
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private void CreateShade(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        RectTransform shade = CreateRect(name, parent);
        Stretch(shade, anchorMin, anchorMax, Vector2.zero, Vector2.zero);

        Image image = shade.gameObject.AddComponent<Image>();
        image.color = new Color(0.02f, 0.02f, 0.03f, 0.74f);
        image.raycastTarget = false;
    }

    private RectTransform CreatePanel(string name, Transform parent, Color color, bool addShadow)
    {
        RectTransform rect = CreateRect(name, parent);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        if (addShadow)
        {
            Shadow shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
            shadow.effectDistance = new Vector2(10f, -10f);
        }

        return rect;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        return rect;
    }

    private TMP_Text CreateText(string name, Transform parent, string value, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;

        TMP_Text text = go.GetComponent<TextMeshProUGUI>();
        text.font = _fontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;

        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private GameObject CreateFallbackNode(Transform parent)
    {
        GameObject go = new GameObject("MapNode", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;

        GameObject label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(go.transform, false);
        label.layer = go.layer;

        TMP_Text tmp = label.GetComponent<TextMeshProUGUI>();
        tmp.font = _fontAsset;
        tmp.text = string.Empty;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return go;
    }
}
