using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    private Canvas _canvas;
    private RectTransform _root;
    private RectTransform _openButtonRoot;
    private TMP_Text _statusText;
    private bool _isOpen;
    private float _previousTimeScale = 1f;
    private TMP_FontAsset _fontAsset;

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ResolveFontAsset();
        BuildUI();
        Close();
    }

    private void Update()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            Toggle();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (_isOpen)
            Time.timeScale = _previousTimeScale;
    }

    public void Toggle()
    {
        if (_isOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        if (_root == null)
            return;

        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        _root.gameObject.SetActive(true);

        if (_openButtonRoot != null)
            _openButtonRoot.gameObject.SetActive(false);

        _isOpen = true;

        if (_statusText != null)
            _statusText.text = string.Empty;
    }

    public void Close()
    {
        if (_root != null)
            _root.gameObject.SetActive(false);

        if (_openButtonRoot != null)
            _openButtonRoot.gameObject.SetActive(true);

        Time.timeScale = _previousTimeScale;
        _isOpen = false;
    }

    private void OnSaveClicked()
    {
        bool saved = GameManager.Instance != null && GameManager.Instance.SaveRun();

        if (_statusText != null)
            _statusText.text = saved ? "Игра сохранена" : "Сейчас нельзя сохранить";
    }

    private void OnSaveAndExitClicked()
    {
        if (GameManager.Instance == null || !GameManager.Instance.SaveRun())
        {
            if (_statusText != null)
                _statusText.text = "Сейчас нельзя сохранить";
            return;
        }

        Time.timeScale = 1f;
        HideForSceneTransition();
        GameManager.Instance?.ReturnToMainMenu();
    }

    private void OnExitWithoutSaveClicked()
    {
        Time.timeScale = 1f;
        HideForSceneTransition();
        GameManager.Instance?.ReturnToMainMenu();
    }

    private void HideForSceneTransition()
    {
        if (_root != null)
            _root.gameObject.SetActive(false);

        if (_openButtonRoot != null)
            _openButtonRoot.gameObject.SetActive(false);

        _isOpen = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Map" || scene.name == "Battle" || scene.name == "Shop")
            return;

        Time.timeScale = 1f;
        Destroy(gameObject);
    }

    private void BuildUI()
    {
        Canvas canvas = ResolveCanvas();
        if (canvas == null)
            return;

        _openButtonRoot = CreateIconButton("PauseOpenButton", canvas.transform, "II", new Vector2(1f, 1f), new Vector2(-54f, -54f), Open);

        _root = CreateRect("PauseMenu", canvas.transform);
        Stretch(_root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _root.SetAsLastSibling();

        Image shade = _root.gameObject.AddComponent<Image>();
        shade.color = new Color(0f, 0f, 0f, 0.66f);
        shade.raycastTarget = true;

        RectTransform panel = CreatePanel("Panel", _root, new Color(0.08f, 0.09f, 0.11f, 0.96f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(520f, 520f);
        panel.anchoredPosition = Vector2.zero;

        TMP_Text title = CreateText("Title", panel, "Пауза", 54f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.94f, 0.74f));
        Stretch(title.rectTransform, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);

        CreateButton("ResumeButton", panel, "Вернуться", new Vector2(0.5f, 0.62f), Close);
        CreateButton("ExitButton", panel, "Выйти без сохранения", new Vector2(0.5f, 0.48f), OnExitWithoutSaveClicked);
        CreateButton("SaveButton", panel, "Сохранить", new Vector2(0.5f, 0.34f), OnSaveClicked);
        CreateButton("SaveExitButton", panel, "Сохранить и выйти", new Vector2(0.5f, 0.20f), OnSaveAndExitClicked);

        _statusText = CreateText("StatusText", panel, string.Empty, 24f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.84f, 0.92f, 0.82f));
        Stretch(_statusText.rectTransform, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.13f), Vector2.zero, Vector2.zero);
    }

    private Canvas ResolveCanvas()
    {
        if (_canvas != null)
            return _canvas;

        GameObject go = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.transform.SetParent(transform, false);

        Canvas canvasComponent = go.GetComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasComponent.sortingOrder = 1000;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        _canvas = canvasComponent;
        return canvasComponent;
    }

    private RectTransform CreateIconButton(string name, Transform parent, string label, Vector2 anchor, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreatePanel(name, parent, new Color(0.08f, 0.09f, 0.11f, 0.92f));
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(72f, 72f);
        rect.anchoredPosition = position;
        rect.SetAsLastSibling();

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(action);

        TMP_Text text = CreateText("Label", rect, label, 30f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.94f, 0.74f));
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));

        return rect;
    }

    private void ResolveFontAsset()
    {
        _fontAsset = TMP_Settings.defaultFontAsset;

        TMP_Text text = FindFirstObjectByType<TMP_Text>();
        if (text != null && text.font != null)
            _fontAsset = text.font;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 anchor, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreatePanel(name, parent, new Color(0.78f, 0.72f, 0.58f, 1f));
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(360f, 68f);
        rect.anchoredPosition = Vector2.zero;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        button.onClick.AddListener(action);

        TMP_Text text = CreateText("Label", rect, label, 28f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.08f, 0.07f, 0.06f));
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 8f), new Vector2(-12f, -8f));

        return button;
    }

    private RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        Shadow shadow = rect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.26f);
        shadow.effectDistance = new Vector2(8f, -8f);

        return rect;
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        return rect;
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
}
