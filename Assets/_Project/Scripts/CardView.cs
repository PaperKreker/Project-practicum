using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

// Визуальное представление карты
public class CardView : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _rankTopLeft;
    [SerializeField] private TextMeshProUGUI _rankBottomRight;
    [SerializeField] private TextMeshProUGUI _suitCenter;
    [SerializeField] private Image           _background;
    [SerializeField] private Image           _critGlow;
    [SerializeField] private GameObject      _selectedOverlay;

    [Header("Colors")]
    [SerializeField] private Color _colorRed    = new Color(0.85f, 0.1f, 0.1f);
    [SerializeField] private Color _colorBlack  = new Color(0.1f, 0.1f, 0.1f);
    [SerializeField] private Color _selectedBg  = new Color(0.9f, 0.85f, 0.4f);
    [SerializeField] private Color _normalBg    = Color.white;

    [Header("Hover Animation")]
    [SerializeField] private float _hoverLift   = 30f;
    [SerializeField] private float _hoverSpeed  = 8f;

    // Данные
    public Card Data { get; private set; }
    public bool IsSelected { get; private set; }

    // Событие — HandController
    public event Action<CardView> OnCardClicked;

    // Анимация
    private RectTransform _rect;
    private Vector2       _basePosition;
    private Vector2       _targetPosition;
    private bool          _isHovered;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        // Плавное движение к целевой позиции
        _rect.anchoredPosition = Vector2.Lerp(
            _rect.anchoredPosition,
            _targetPosition,
            Time.deltaTime * _hoverSpeed
        );
    }

    // Инициализирует карту данными и позицией в руке
    public void Setup(Card card, Vector2 basePosition)
    {
        Data          = card;
        _basePosition = basePosition;
        _targetPosition = basePosition;
        _rect.anchoredPosition = basePosition;

        IsSelected = false;
        _selectedOverlay.SetActive(false);

        RefreshVisuals();
    }

    // Устанавливает базовую позицию
    public void SetBasePosition(Vector2 pos)
    {
        _basePosition = pos;
        if (!_isHovered) _targetPosition = pos;
    }

    private void RefreshVisuals()
    {
        string rankStr = RankToString(Data.Rank);
        string suitStr = SuitToSymbol(Data.Suit);
        Color  textColor = IsRedSuit(Data.Suit) ? _colorRed : _colorBlack;

        _rankTopLeft.text     = rankStr;
        _rankTopLeft.color    = textColor;
        _rankBottomRight.text = rankStr;
        _rankBottomRight.color = textColor;
        _suitCenter.text      = suitStr;
        _suitCenter.color     = textColor;

        _background.color = IsSelected ? _selectedBg : _normalBg;

        if (_critGlow != null)
            _critGlow.gameObject.SetActive(Data.IsCritical);
    }

    // IPointer
    public void OnPointerClick(PointerEventData eventData)
    {
        OnCardClicked?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        _targetPosition = _basePosition + Vector2.up * _hoverLift;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        _targetPosition = _basePosition + (IsSelected ? Vector2.up * (_hoverLift * 0.5f) : Vector2.zero);
    }

    // Public API
    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        _selectedOverlay.SetActive(selected);
        _background.color = selected ? _selectedBg : _normalBg;

        // Выбранная карта остаётся чуть приподнятой
        if (!_isHovered)
            _targetPosition = _basePosition + (selected ? Vector2.up * (_hoverLift * 0.5f) : Vector2.zero);
    }

    // Helpers
    private static string RankToString(Rank r)
    {
        return r switch
        {
            Rank.Ace   => "A",
            Rank.King  => "K",
            Rank.Queen => "Q",
            Rank.Jack  => "J",
            _          => ((int)r).ToString()
        };
    }

    private static string SuitToSymbol(Suit s)
    {
        return s switch
        {
            Suit.Spades   => "♠",
            Suit.Hearts   => "♥",
            Suit.Diamonds => "♦",
            Suit.Clubs    => "♣",
            _             => "?"
        };
    }

    private static bool IsRedSuit(Suit s) =>
        s == Suit.Hearts || s == Suit.Diamonds;
}