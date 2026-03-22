using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Visual representation of a card in the player's hand
public class CardView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private AnimatedButton _button;
    [SerializeField] private TextMeshProUGUI _rankTopLeft;
    [SerializeField] private TextMeshProUGUI _rankBottomRight;
    [SerializeField] private TextMeshProUGUI _suitCenter;
    [SerializeField] private Image _background;
    [SerializeField] private Image _critGlow;

    [Header("Colors")]
    [SerializeField] private Color _colorRed = new Color(0.85f, 0.1f, 0.1f);
    [SerializeField] private Color _colorBlack = new Color(0.1f, 0.1f, 0.1f);

    [Header("Face Down")]
    [SerializeField] private Sprite _cardBackSprite;

    [SerializeField] private CardAnimationConfig _animationConfig;

    // Card fields
    public Card Data { get; private set; }
    public bool IsSelected { get; private set; }
    public bool IsPetrified { get; private set; }
    public bool IsFaceDown { get; private set; }

    // HandController
    public event System.Action<CardView> OnCardClicked;

    // Animation
    private Coroutine _currentAnimation;
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

    private void OnEnable()
    {
        _button.OnClick.AddListener(HandleClick);
    }
    private void OnDisable()
    {
        _button.OnClick.RemoveListener(HandleClick);
    }

    // Initialize the card view with card data and position
    public void Setup(Card card, Vector2 deckPosition, Vector2 basePosition)
    {
        Data = card;
        _basePosition = basePosition;
        _targetPosition = basePosition;
        _rect.position = deckPosition;

        IsSelected = false;
        IsPetrified = false;
        IsFaceDown = false;

        RefreshVisuals();
    }

    public void SetInteractable(bool interactable)
    {
        _button.interactable = interactable;
    }

    public void SetBasePosition(Vector2 pos)
    {
        _basePosition = pos;
        if (!_isHovered)
        {
            _targetPosition = pos;
            PlayMoveToTarget();
        }
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        //_background.color = selected ? _selectedBg : _normalBg;

        // Selected card lifts slightly
        if (!_isHovered)
        {
            _targetPosition = _basePosition + (selected ? Vector2.up * _animationConfig.HoverLift : Vector2.zero);
            PlayMoveToTarget();
        }
    }

    // Petrified cards are visible but cannot be clicked
    public void SetPetrified(bool petrified)
    {
        IsPetrified = petrified;
        if (petrified && IsSelected)
        {
            IsSelected = false;
        }
        RefreshVisuals();
    }

    // Face-down cards hide rank and suit until played
    public void SetFaceDown(bool faceDown)
    {
        IsFaceDown = faceDown;
        RefreshVisuals();
    }

    public Coroutine PlayAttackAnimation(Vector2 attackPosition)
    {
        _targetPosition = attackPosition;
        if (_currentAnimation != null)
            StopCoroutine(_currentAnimation);
        _currentAnimation = StartCoroutine(AnimateAttackTarget());
        return _currentAnimation;
    }

    public Coroutine PlayDiscardAnimation()
    {
        if (_currentAnimation != null)
            StopCoroutine(_currentAnimation);
        _currentAnimation = StartCoroutine(AnimateDiscard());
        return _currentAnimation;
    }

    private void PlayMoveToTarget()
    {
        if (_currentAnimation != null)
            StopCoroutine(_currentAnimation);
        _currentAnimation = StartCoroutine(MoveToTarget());
    }

    private void HandleClick()
    {
        OnCardClicked?.Invoke(this);
    }

    private void RefreshVisuals()
    {
        if (IsFaceDown)
        {
            _rankTopLeft.text = "";
            _rankBottomRight.text = "";
            _suitCenter.text = "";
            _background.sprite = _cardBackSprite != null ? _cardBackSprite : _originalSprite;
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

        if (_critGlow != null)
            _critGlow.gameObject.SetActive(Data.IsCritical && !IsPetrified);
    }

    // Coroutine animations
    private IEnumerator MoveToTarget()
    {
        float time = _animationConfig.HoverTime;
        Vector2 initialPosition = _rect.anchoredPosition;
        while (time > 0.0f)
        {
            _rect.anchoredPosition = Vector2.Lerp(
                initialPosition,
                _targetPosition,
                _animationConfig.HoverCurve.Evaluate(1 - time / _animationConfig.HoverTime));
            
            time -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator AnimateAttackTarget()
    {
        float time = _animationConfig.AttackTime;
        Vector2 initialPosition = _rect.position;
        Vector3 initialAngle = _rect.eulerAngles;
        Vector2 direction = (_targetPosition - initialPosition).normalized;
        float targetZAngle = Mathf.Atan2(direction.x, direction.y) * -Mathf.Rad2Deg;

        while (time > 0.0f)
        {
            float t = 1 - time / _animationConfig.AttackTime;
            float eval = _animationConfig.AttackCurve.Evaluate(t);
            _rect.position = Vector2.LerpUnclamped(
                initialPosition,
                _targetPosition,
                eval);
            _rect.eulerAngles = new Vector3(
                0.0f,
                0.0f,
                Mathf.LerpAngle(
                    initialAngle.z, 
                    targetZAngle,
                    t));
            time -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        gameObject.SetActive(false);
    }

    private IEnumerator AnimateDiscard()
    {
        Vector3 initialAngle = _rect.eulerAngles;
        Vector2 initialPosition = _rect.position;

        float rotation = Random.Range(-_animationConfig.DiscardRotation, _animationConfig.DiscardRotation);
        float xShift = Random.Range(-_animationConfig.DiscardXVelocity, _animationConfig.DiscardXVelocity);
        float yVelocity = _animationConfig.DiscardYVelocity;
        float time = _animationConfig.DiscardLifetime;

        while (time > 0.0f)
        {
            _rect.position += new Vector3(xShift, yVelocity, 0.0f) * Time.deltaTime;
            _rect.eulerAngles += new Vector3(0.0f, 0.0f, rotation) * Time.deltaTime;
            yVelocity += _animationConfig.DiscardGravitation * Time.deltaTime;
            time -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        Destroy(gameObject);
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
