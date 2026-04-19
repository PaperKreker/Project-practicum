using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Visual representation of a card in the player's hand
public class CardView : MonoBehaviour
{
    public event System.Action OnDiscard;
    public event System.Action OnAttackHit;

    [Header("UI References")]
    [SerializeField] private AnimatedButton _button;
    [SerializeField] private Image _background;

    [Header("Colors")]
    [SerializeField] private Color _critColor = new Color(0.85f, 0.1f, 0.1f, 0.5f);

    [Header("Sprites")]
    [SerializeField] private Sprite _cardBackSprite;
    [SerializeField] private List<Sprite> _stoneSprites;
    [SerializeField] private List<Sprite> _moonSprites;
    [SerializeField] private List<Sprite> _fireSprites;
    [SerializeField] private List<Sprite> _sunSprites;

    [Header("Animations")]
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

    private bool _isIdle { get => _isInDeck && !IsSelected; }
    private bool _isInDeck = true;

    private Color _initialBackgroudColor;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _initialBackgroudColor = _background.color;
    }

    private void OnEnable()
    {
        _button.OnClick.AddListener(HandleClick);
    }
    private void OnDisable()
    {
        _button.OnClick.RemoveListener(HandleClick);
    }

    private void Update()
    {
        if (_isInDeck)
        {
            PlayIdleAnimation();
        }
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
        if (!_isInDeck) return;

        _basePosition = pos;
        _targetPosition = GetTargetPosition();
        PlayMoveToTarget();
    }

    public void SetSelected(bool selected)
    {
        if (!_isInDeck) return;

        IsSelected = selected;
        //_background.color = selected ? _selectedBg : _normalBg;

        // Selected card lifts slightly
        _targetPosition = GetTargetPosition();
        PlayMoveToTarget();
    }

    private void PlayIdleAnimation()
    {
        if (_currentAnimation != null)
            return;

        _rect.anchoredPosition = Vector2.Lerp(
               _rect.anchoredPosition,
               GetIdlePosition(),
               Time.deltaTime);
    }

    private Vector3 GetIdlePosition()
    {
        float siblingAddition = (float)transform.GetSiblingIndex() / transform.parent.childCount;
        float t = (Time.time * _animationConfig.IdleAnimationSpeed + siblingAddition) % 1.0f;
        Vector3 shift = Vector2.Lerp(
            -_animationConfig.IdleAmplitude, 
            _animationConfig.IdleAmplitude, 
            _animationConfig.IdleCurve.Evaluate(t));
        return GetTargetPosition() + shift;
    }

    private Vector3 GetTargetPosition()
    {
        return _basePosition + (IsSelected ? Vector2.up * _animationConfig.HoverLift : Vector2.zero);
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
        _isInDeck = false;
        _targetPosition = attackPosition;
        if (_currentAnimation != null)
            StopCoroutine(_currentAnimation);
        _currentAnimation = StartCoroutine(AnimateAttackTarget());
        return _currentAnimation;
    }

    public Coroutine PlayDiscardAnimation()
    {
        _isInDeck = false;

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
            _background.sprite = _cardBackSprite;
            _background.color = _initialBackgroudColor;

            return;
        }

        Sprite newSprite = _background.sprite;
        int index = (int)Data.Rank - 2;

        if (Data.Suit == Suit.Stone)
        {
            newSprite = _stoneSprites[index];
        }
        else if (Data.Suit == Suit.Moon)
        {
            newSprite = _moonSprites[index];
        }
        else if (Data.Suit == Suit.Fire)
        {
            newSprite = _fireSprites[index];
        }
        else if (Data.Suit == Suit.Sun)
        {
            newSprite = _sunSprites[index];
        }

        _background.sprite = newSprite;
        _background.color = Data.IsCritical && !IsPetrified ? _critColor : _initialBackgroudColor;
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
            yield return new WaitForFixedUpdate();
        }
        _rect.anchoredPosition = _targetPosition;
        _currentAnimation = null;
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
        OnAttackHit?.Invoke();
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

        OnDiscard?.Invoke();

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
}
