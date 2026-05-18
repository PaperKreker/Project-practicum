using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DifficultySelectController : MonoBehaviour
{
    private const string RootName = "DifficultyRoot";

    private Canvas _canvas;
    private TMP_FontAsset _fontAsset;
    private RectTransform _root;

    private void Start()
    {
        CleanupExistingRoots();

        _canvas = ResolveSceneCanvas();
        if (_canvas == null)
        {
            Debug.LogError("[DifficultySelectController] Canvas not found.");
            return;
        }

        _fontAsset = TMP_Settings.defaultFontAsset;
        BuildScreen();
    }

    private void BuildScreen()
    {
        RectTransform root = CreateRect(RootName, _canvas.transform);
        _root = root;
        Stretch(root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image backdrop = root.gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.04f, 0.03f, 0.03f, 0.88f);
        backdrop.raycastTarget = true;

        RectTransform panel = CreateRect("Panel", root);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(820f, 640f);
        panel.anchoredPosition = Vector2.zero;

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.13f, 0.1f, 0.08f, 0.97f);
        panelImage.raycastTarget = false;

        Shadow shadow = panel.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = new Vector2(12f, -12f);

        TMP_Text title = CreateText("Title", panel, "Выбор сложности", 42, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.98f, 0.92f, 0.8f));
        Stretch(title.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.95f), Vector2.zero, Vector2.zero);

        CreateDifficultyButton(panel, new Vector2(0.5f, 0.7f), DifficultyLevel.Normal);
        CreateDifficultyButton(panel, new Vector2(0.5f, 0.49f), DifficultyLevel.Hard);
        CreateDifficultyButton(panel, new Vector2(0.5f, 0.28f), DifficultyLevel.Demon);

        Button backButton = CreateButton(panel, "Назад", string.Empty, new Vector2(220f, 56f), new Color(0.24f, 0.21f, 0.19f), new Color(0.92f, 0.88f, 0.79f));
        RectTransform backRect = backButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0.09f);
        backRect.anchorMax = new Vector2(0.5f, 0.09f);
        backRect.pivot = new Vector2(0.5f, 0.5f);
        backRect.anchoredPosition = Vector2.zero;
        backButton.onClick.AddListener(OnBackClicked);
    }

    private void CreateDifficultyButton(RectTransform parent, Vector2 anchor, DifficultyLevel level)
    {
        DifficultyModifiers modifiers = GameBalance.GetDifficulty(level);
        Button button = CreateButton(parent, modifiers.DisplayName, modifiers.Description, new Vector2(620f, 120f), new Color(0.25f, 0.17f, 0.13f), Color.white);

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        button.onClick.AddListener(() => OnDifficultySelected(level));
    }

    private Button CreateButton(RectTransform parent, string title, string description, Vector2 size, Color fillColor, Color titleColor)
    {
        RectTransform rect = CreateRect(title.Replace(" ", string.Empty) + "Button", parent);
        rect.sizeDelta = size;

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = fillColor;
        image.raycastTarget = true;

        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.97f, 0.97f, 0.97f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        TMP_Text titleText = CreateText("Title", rect, title, 30, FontStyles.Bold, TextAlignmentOptions.Center, titleColor);

        if (!string.IsNullOrEmpty(description))
        {
            Stretch(titleText.rectTransform, new Vector2(0.06f, 0.52f), new Vector2(0.94f, 0.9f), Vector2.zero, Vector2.zero);
            TMP_Text descText = CreateText("Description", rect, description, 19, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.92f, 0.88f, 0.79f));
            Stretch(descText.rectTransform, new Vector2(0.07f, 0.14f), new Vector2(0.93f, 0.5f), Vector2.zero, Vector2.zero);
        }
        else
        {
            Stretch(titleText.rectTransform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.82f), Vector2.zero, Vector2.zero);
        }

        return button;
    }

    private void OnDifficultySelected(DifficultyLevel level)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StartNewRun(level);
    }

    private void OnBackClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
            return;
        }

        SceneManager.LoadScene("MainMenu");
    }

    public void IgnoreLegacyButton()
    {
    }

    private void OnDestroy()
    {
        if (_root != null)
            Destroy(_root.gameObject);
    }

    private Canvas ResolveSceneCanvas()
    {
        if (TransitionProxy.Instance != null && TransitionProxy.Instance.TargetCanvas != null)
        {
            Canvas transitionCanvas = TransitionProxy.Instance.TargetCanvas.GetComponent<Canvas>();
            if (transitionCanvas != null && transitionCanvas.gameObject.scene == gameObject.scene)
                return transitionCanvas;
        }

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || canvas.gameObject.scene != gameObject.scene)
                continue;

            return canvas;
        }

        return null;
    }

    private void CleanupExistingRoots()
    {
        RectTransform[] leftovers = FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
        foreach (RectTransform candidate in leftovers)
        {
            if (candidate != null && candidate.name == RootName)
                Destroy(candidate.gameObject);
        }
    }

    private TMP_Text CreateText(string name, Transform parent, string value, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;

        TMP_Text text = go.GetComponent<TextMeshProUGUI>();
        text.font = _fontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
