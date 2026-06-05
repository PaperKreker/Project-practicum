using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TMP_InputField _seedInput;
    [SerializeField] private SettingsMenuController _settingsMenu;

    private Button _continueButton;
    private TMP_FontAsset _fontAsset;

    private void Start()
    {
        ResolveFontAsset();

        if (GameManager.Instance != null && GameManager.Instance.HasSave())
            BuildContinueButton();
    }

    public void OnNewGameClicked()
    {
        OpenDifficultySelection();
    }

    public void OnNewGameHard()
    {
        OpenDifficultySelection();
    }

    public void OnNewGameDemon()
    {
        OpenDifficultySelection();
    }

    public void OnTutorialClicked()
    {
        if (TutorialManager.Instance == null)
        {
            GameObject go = new GameObject("TutorialManager");
            go.AddComponent<TutorialManager>();
        }

        TutorialManager.Instance.StartTutorial();
    }

    public void OnContinueClicked()
    {
        GameManager.Instance?.LoadSavedRun();
    }

    public void OnSettingsClicked()
    {
        if (_settingsMenu != null)
            _settingsMenu.OpenSettings();
        else
            Debug.LogWarning("MainMenuController: SettingsMenuController не назначен в Inspector.");
    }

    private int ParseSeed()
    {
        if (_seedInput == null) return -1;
        string text = _seedInput.text.Trim();
        if (string.IsNullOrEmpty(text)) return -1;
        return int.TryParse(text, out int seed) ? Mathf.Abs(seed) : Mathf.Abs(text.GetHashCode());
    }

    private void OpenDifficultySelection()
    {
        GameManager.Instance.OpenDifficultySelection(ParseSeed());
    }

    private void RefreshContinueButton()
    {
        if (_continueButton == null)
            return;

        bool hasSave = GameManager.Instance != null && GameManager.Instance.HasSave();
        _continueButton.gameObject.SetActive(hasSave);
        _continueButton.interactable = hasSave;
    }

    private void BuildContinueButton()
    {
        Button newGameButton = FindButtonWithClickMethod("OnNewGameClicked");
        Button tutorialButton = FindButtonWithClickMethod("OnTutorialClicked");

        if (newGameButton == null)
        {
            BuildFallbackContinueButton();
            return;
        }

        RectTransform newGameRect = newGameButton.GetComponent<RectTransform>();
        RectTransform tutorialRect = tutorialButton != null ? tutorialButton.GetComponent<RectTransform>() : null;
        Transform parent = newGameRect.parent;
        Bounds newGameBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, newGameRect);
        float visualWidth = Mathf.Max(newGameRect.sizeDelta.x, newGameBounds.size.x);
        float spacing = GetButtonGap(parent, newGameRect, tutorialRect);
        Vector2 basePosition = newGameRect.anchoredPosition;
        float xOffset = (visualWidth + spacing) * 0.5f;

        newGameRect.anchoredPosition = basePosition + new Vector2(-xOffset, 0f);

        GameObject go = Instantiate(newGameButton.gameObject, newGameRect.parent);
        go.name = "ContinueButton";
        go.transform.SetSiblingIndex(newGameRect.GetSiblingIndex() + 1);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = newGameRect.sizeDelta;
        rect.anchoredPosition = basePosition + new Vector2(xOffset, 0f);

        _continueButton = go.GetComponent<Button>();
        _continueButton.onClick = new Button.ButtonClickedEvent();
        _continueButton.onClick.AddListener(OnContinueClicked);

        TMP_Text label = go.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = "Продолжить";

        RefreshContinueButton();
    }

    private float GetButtonGap(Transform parent, RectTransform first, RectTransform second)
    {
        if (first != null && second != null)
        {
            Bounds firstBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, first);
            Bounds secondBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, second);
            float centerDistance = Mathf.Abs(firstBounds.center.y - secondBounds.center.y);
            float averageHeight = (firstBounds.size.y + secondBounds.size.y) * 0.5f;
            float gap = centerDistance - averageHeight;

            if (gap > 1f)
                return gap;
        }

        return 48f;
    }

    private Button FindButtonWithClickMethod(string methodName)
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in buttons)
        {
            int count = button.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                if (button.onClick.GetPersistentMethodName(i) == methodName)
                    return button;
            }
        }

        return null;
    }

    private void BuildFallbackContinueButton()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        RectTransform rect = CreatePanel("ContinueButton", canvas.transform, new Color(0.32f, 0.08f, 0.31f, 1f));
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(340f, 64f);
        rect.anchoredPosition = new Vector2(0f, -90f);
        rect.SetAsLastSibling();

        _continueButton = rect.gameObject.AddComponent<Button>();
        _continueButton.targetGraphic = rect.GetComponent<Image>();
        _continueButton.onClick.AddListener(OnContinueClicked);

        TMP_Text label = CreateText("Label", rect, "Продолжить", 28f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 8f), new Vector2(-12f, -8f));
    }

    private void ResolveFontAsset()
    {
        _fontAsset = TMP_Settings.defaultFontAsset;

        TMP_Text text = FindFirstObjectByType<TMP_Text>();
        if (text != null && text.font != null)
            _fontAsset = text.font;
    }

    private RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.26f);
        shadow.effectDistance = new Vector2(8f, -8f);

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
