using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

// Visual representation of a card in the player's hand
public class CardView : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _rankTopLeft;
    [SerializeField] private TextMeshProUGUI _rankBottomRight;
    [SerializeField] private TextMeshProUGUI _suitCenter;
    [SerializeField] private Image _background;
    [SerializeField] private Image _critGlow;
    [SerializeField] private GameObject _selectedOverlay;

    [Header("Colors")]
    [SerializeField] private Color _colorRed = new Color(0.85f, 0.1f, 0.1f);
    [SerializeField] private Color _colorBlack = new Color(0.1f, 0.1f, 0.1f);
    [SerializeField] private Color _selectedBg = new Color(0.9f, 0.85f, 0.4f);
    [SerializeField] private Color _normalBg = Color.white;
    [SerializeField] private Color _petrifiedBg = new Color(0.6f, 0.6f, 0.6f);

    [Header("Face Down")]
    [SerializeField] private Sprite _cardBackSprite;

    [Header("Hover Animation")]
    [SerializeField] private float _hoverLift = 30f;
    [SerializeField] private float _hoverSpeed = 8f;

    // Card fields
    public Card Data { get; private set; }
    public bool IsSelected { get; private set; }
    public bool IsPetrified { get; private set; }
    public bool IsFaceDown { get; private set; }

    // HandController
    public event Action<CardView> OnCardClicked;

    // Animation
    private RectTransform _rect;
    private Vector2 _basePosition;
    private Vector2 _targetPosition;
    private bool _isHovered;
    private Sprite _originalSprite;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _originalSprite = _background.sprite;
    }

    private void Update()
    {
        _rect.anchoredPosition = Vector2.Lerp(
            _rect.anchoredPosition,
            _targetPosition,
            Time.deltaTime * _hoverSpeed
        );
    }

    // Initialize the card view with card data and position
    public void Setup(Card card, Vector2 basePosition)
    {
        Data = card;
        _basePosition = basePosition;
        _targetPosition = basePosition;
        _rect.anchoredPosition = basePosition;

        IsSelected = false;
        IsPetrified = false;
        IsFaceDown = false;

        _selectedOverlay.SetActive(false);

        RefreshVisuals();
    }

    public void SetBasePosition(Vector2 pos)
    {
        _basePosition = pos;
        if (!_isHovered) _targetPosition = pos;
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        _selectedOverlay.SetActive(selected);
        _background.color = selected ? _selectedBg : _normalBg;

        // Selected card lifts slightly
        if (!_isHovered)
            _targetPosition = _basePosition + (selected ? Vector2.up * (_hoverLift * 0.5f) : Vector2.zero);
    }

    // Petrified cards are visible but cannot be clicked
    public void SetPetrified(bool petrified)
    {
        IsPetrified = petrified;
        if (petrified && IsSelected)
        {
            IsSelected = false;
            _selectedOverlay.SetActive(false);
        }
        RefreshVisuals();
    }

    // Face-down cards hide rank and suit until played
    public void SetFaceDown(bool faceDown)
    {
        IsFaceDown = faceDown;
        RefreshVisuals();
    }

    // IPointer
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsPetrified)
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

    private void RefreshVisuals()
    {
        if (IsFaceDown)
        {
            _rankTopLeft.text = "";
            _rankBottomRight.text = "";
            _suitCenter.text = "";
            _background.sprite = _cardBackSprite != null ? _cardBackSprite : _originalSprite;
            _background.color = Color.white;
            if (_critGlow) _critGlow.gameObject.SetActive(false);
            return;
        }

        string rankStr = RankToString(Data.Rank);
        string suitStr = SuitToSymbol(Data.Suit);
        Color textColor = IsRedSuit(Data.Suit) ? _colorRed : _colorBlack;

        _background.sprite = _originalSprite;

        _rankTopLeft.text = rankStr;
        _rankTopLeft.color = textColor;
        _rankBottomRight.text = rankStr;
        _rankBottomRight.color = textColor;
        _suitCenter.text = suitStr;
        _suitCenter.color = textColor;

        _background.color = IsPetrified ? _petrifiedBg : IsSelected ? _selectedBg : _normalBg;

        if (_critGlow != null)
            _critGlow.gameObject.SetActive(Data.IsCritical && !IsPetrified);
    }

    // Helpers
    private static string RankToString(Rank r)
    {
        return r switch
        {
            Rank.Ace => "A",
            Rank.King => "K",
            Rank.Queen => "Q",
            Rank.Jack => "J",
            _ => ((int)r).ToString()
        };
    }

    private static string SuitToSymbol(Suit s)
    {
        return s switch
        {
            Suit.Spades => "♠",
            Suit.Hearts => "♥",
            Suit.Diamonds => "♦",
            Suit.Clubs => "♣",
            _ => "?"
        };
    }

    private static bool IsRedSuit(Suit s) =>
        s == Suit.Hearts || s == Suit.Diamonds;
}
