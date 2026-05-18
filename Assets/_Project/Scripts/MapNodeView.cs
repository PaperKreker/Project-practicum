using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapNodeView : MonoBehaviour
{
    // Было 22
    // Слишком раздувало высоту ноды
    public const float DiamondOffsetY = 8f;

    public struct Style
    {
        public string Icon;
        public string Title;
        public string Subtitle;

        public Vector2 Size;
        public Vector2 LabelOffset;

        public Color GlowColor;
        public Color FrameColor;
        public Color FillColor;

        public Color IconColor;
        public Color TitleColor;
        public Color SubtitleColor;

        public bool ShowGlow;
        public bool Interactable;
    }

    private Button _button;

    private Image _hitbox;
    private Image _glow;
    private Image _frame;
    private Image _fill;
    private Image _labelPlate;

    private TMP_Text _iconText;
    private TMP_Text _titleText;
    private TMP_Text _subtitleText;

    private Action _onClick;

    public void Setup(
        Style style,
        Action onClick)
    {
        EnsureStructure();

        _onClick = onClick;

        _button.onClick.RemoveAllListeners();

        if (onClick != null)
        {
            _button.onClick.AddListener(HandleClick);
        }

        _button.interactable = style.Interactable;

        RectTransform root =
            (RectTransform)transform;

        // Важно:
        // root теперь меньше влияет на layout
        root.sizeDelta = new Vector2(
            style.Size.x * 0.62f,
            style.Size.y * 0.62f);

        // Было слишком жирно
        float diamondSize =
            Mathf.Min(style.Size.x, style.Size.y) * 0.42f;

        ConfigureDiamond(
            _glow.rectTransform,
            diamondSize + 18f,
            DiamondOffsetY,
            style.GlowColor);

        ConfigureDiamond(
            _frame.rectTransform,
            diamondSize + 7f,
            DiamondOffsetY,
            style.FrameColor);

        ConfigureDiamond(
            _fill.rectTransform,
            diamondSize,
            DiamondOffsetY,
            style.FillColor);

        ConfigureLabelPlate(style);
        ConfigureLabelText(style.LabelOffset);

        _glow.enabled = style.ShowGlow;

        // ----------------------------------------------------
        // ICON
        // ----------------------------------------------------

        _iconText.text = style.Icon;
        _iconText.color = style.IconColor;

        // Было 0.44
        _iconText.fontSize = diamondSize * 0.34f;

        _iconText.rectTransform.anchoredPosition =
            new Vector2(0f, DiamondOffsetY);

        // ----------------------------------------------------
        // LABELS
        // ----------------------------------------------------

        bool hasTitle =
            !string.IsNullOrWhiteSpace(style.Title);

        bool hasSubtitle =
            !string.IsNullOrWhiteSpace(style.Subtitle);

        bool hasLabel =
            hasTitle || hasSubtitle;

        _titleText.text = style.Title;
        _titleText.color = style.TitleColor;
        _titleText.gameObject.SetActive(hasTitle);

        _subtitleText.text = style.Subtitle;
        _subtitleText.color = style.SubtitleColor;
        _subtitleText.gameObject.SetActive(hasSubtitle);

        _labelPlate.gameObject.SetActive(hasLabel);

        // layering
        _glow.transform.SetSiblingIndex(0);
        _frame.transform.SetSiblingIndex(1);
        _fill.transform.SetSiblingIndex(2);
        _labelPlate.transform.SetSiblingIndex(3);
        _iconText.transform.SetSiblingIndex(4);
        _titleText.transform.SetSiblingIndex(5);
        _subtitleText.transform.SetSiblingIndex(6);
    }

    private void HandleClick()
    {
        _onClick?.Invoke();
    }

    private void EnsureStructure()
    {
        if (_button != null)
            return;

        _button =
            GetComponent<Button>()
            ?? gameObject.AddComponent<Button>();

        _button.transition =
            Selectable.Transition.None;

        _hitbox =
            GetComponent<Image>()
            ?? gameObject.AddComponent<Image>();

        _hitbox.color =
            new Color(1f, 1f, 1f, 0.001f);

        _hitbox.raycastTarget = true;

        _glow = CreateImage("Glow");
        _frame = CreateImage("Frame");
        _fill = CreateImage("Fill");
        _labelPlate = CreateImage("LabelPlate");

        _iconText =
            GetComponentInChildren<TMP_Text>(true);

        if (_iconText == null)
        {
            _iconText =
                CreateText("Icon", null, 28f);
        }

        ConfigureIcon(_iconText);

        // ----------------------------------------------------
        // TITLE
        // ----------------------------------------------------

        _titleText =
            CreateText("Title", _iconText, 18f);

        _titleText.enableAutoSizing = true;

        _titleText.fontSizeMin = 13;
        _titleText.fontSizeMax = 19;

        _titleText.alignment =
            TextAlignmentOptions.Center;

        _titleText.rectTransform.anchorMin =
            new Vector2(0.5f, 0.5f);

        _titleText.rectTransform.anchorMax =
            new Vector2(0.5f, 0.5f);

        _titleText.rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        // Было 150x32
        _titleText.rectTransform.sizeDelta =
            new Vector2(112f, 24f);

        // ----------------------------------------------------
        // SUBTITLE
        // ----------------------------------------------------

        _subtitleText =
            CreateText("Subtitle", _iconText, 14f);

        _subtitleText.enableAutoSizing = true;

        _subtitleText.fontSizeMin = 11;
        _subtitleText.fontSizeMax = 15;

        _subtitleText.alignment =
            TextAlignmentOptions.Center;

        _subtitleText.rectTransform.anchorMin =
            new Vector2(0.5f, 0.5f);

        _subtitleText.rectTransform.anchorMax =
            new Vector2(0.5f, 0.5f);

        _subtitleText.rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        // Было 150x30
        _subtitleText.rectTransform.sizeDelta =
            new Vector2(112f, 20f);
    }

    private Image CreateImage(string name)
    {
        GameObject go =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image));

        go.transform.SetParent(transform, false);

        go.layer = gameObject.layer;

        Image image = go.GetComponent<Image>();

        image.raycastTarget = false;

        return image;
    }

    private void ConfigureLabelPlate(Style style)
    {
        RectTransform rect =
            _labelPlate.rectTransform;

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        // БЫЛО:
        // style.Size.x, 84f
        //
        // Это и создавало гигантские коллизии.
        rect.sizeDelta =
            new Vector2(
                style.Size.x * 0.68f,
                34f);

        rect.anchoredPosition =
            style.LabelOffset;

        rect.localRotation =
            Quaternion.identity;

        Color plateColor =
            style.Interactable || style.ShowGlow
                ? new Color(0.05f, 0.06f, 0.08f, 0.88f)
                : new Color(0.07f, 0.07f, 0.09f, 0.76f);

        _labelPlate.color = plateColor;
    }

    private void ConfigureLabelText(
        Vector2 labelOffset)
    {
        // Было 14 / -12
        // Слишком раздувало высоту
        _titleText.rectTransform.anchoredPosition =
            labelOffset + new Vector2(0f, 4f);

        _subtitleText.rectTransform.anchoredPosition =
            labelOffset + new Vector2(0f, -4f);
    }

    private TMP_Text CreateText(
        string name,
        TMP_Text template,
        float fontSize)
    {
        GameObject go =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));

        go.transform.SetParent(transform, false);

        go.layer = gameObject.layer;

        TMP_Text text =
            go.GetComponent<TextMeshProUGUI>();

        if (template != null)
        {
            text.font = template.font;
            text.fontSharedMaterial =
                template.fontSharedMaterial;
        }
        else
        {
            text.font =
                TMP_Settings.defaultFontAsset;
        }

        text.fontSize = fontSize;

        text.fontStyle = FontStyles.Bold;

        text.textWrappingMode =
            TextWrappingModes.Normal;

        text.raycastTarget = false;

        Shadow shadow =
            go.AddComponent<Shadow>();

        shadow.effectColor =
            new Color(0f, 0f, 0f, 0.62f);

        shadow.effectDistance =
            new Vector2(2f, -2f);

        return text;
    }

    private void ConfigureIcon(TMP_Text iconText)
    {
        iconText.transform.SetParent(
            transform,
            false);

        iconText.rectTransform.anchorMin =
            new Vector2(0.5f, 0.5f);

        iconText.rectTransform.anchorMax =
            new Vector2(0.5f, 0.5f);

        iconText.rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        // Было 84x46
        iconText.rectTransform.sizeDelta =
            new Vector2(64f, 36f);

        iconText.alignment =
            TextAlignmentOptions.Center;

        iconText.fontStyle =
            FontStyles.Bold;

        iconText.enableAutoSizing = false;

        iconText.raycastTarget = false;

        Shadow shadow =
            iconText.GetComponent<Shadow>();

        if (shadow == null)
        {
            shadow =
                iconText.gameObject.AddComponent<Shadow>();

            shadow.effectColor =
                new Color(0f, 0f, 0f, 0.62f);

            shadow.effectDistance =
                new Vector2(2f, -2f);
        }
    }

    private static void ConfigureDiamond(
        RectTransform rect,
        float size,
        float offsetY,
        Color color)
    {
        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.sizeDelta =
            new Vector2(size, size);

        rect.anchoredPosition =
            new Vector2(0f, offsetY);

        rect.localRotation =
            Quaternion.Euler(0f, 0f, 45f);

        Image image =
            rect.GetComponent<Image>();

        image.color = color;
    }
}