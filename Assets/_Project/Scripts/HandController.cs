using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

/* 
Manages the player's hand:
- holds up to 8 cards
- lays them out as a fan
- handles selection (max 5)
- sorting toggle with Q (suit / rank)
- draws cards from the Deck
*/
public class HandController : MonoBehaviour
{
    public event Action<Deck> OnInit;

    [Header("References")]
    [SerializeField] private GameObject _cardPrefab;   // card prefab
    [SerializeField] private RectTransform _handArea;     // hand container
    [SerializeField] private RectTransform _deckTransform;

    [Header("Layout")]
    [SerializeField] private float _cardSpacing = 110f;
    [SerializeField] private float _cardWidth = 100f;
    [SerializeField] private float _fanAngle = 5f;

    [Header("Rules")]
    [SerializeField] private int _maxHandSize = 8;
    [SerializeField] private int _maxSelected = 5;

    [Header("Animation")]
    [SerializeField] private float _attackDelay = 0.1f;
    [SerializeField] private float _discardDelay = 0.05f;

    // State
    private List<CardView> _hand = new List<CardView>();
    private List<CardView> _selected = new List<CardView>();
    private Deck _deck;
    private bool _sortBySuit = true;

    // Fired when the selection changes
    public event Action OnSelectionChanged;

    // Fired when a card is drawn — used by FaceDownCards effect
    public event Action<CardView> OnCardDrawn;

    // Unity
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            ToggleSort();
    }

    // Initialize with a deck and initial draw
    public void Init(Deck deck)
    {
        _deck = deck;
        DrawUpToMax();
        OnInit?.Invoke(deck);
    }

    // Draws cards up to the maximum hand size
    public void DrawUpToMax()
    {
        int needed = _maxHandSize - _hand.Count;
        if (needed <= 0) return;

        List<Card> drawn = _deck.Draw(needed);

        foreach (var card in drawn)
            SpawnCard(card);

        ApplySort(); // ← sorts the full hand including new cards
    }

    // Returns the list of selected cards
    public List<Card> GetSelectedCards()
    {
        return _selected.Select(v => v.Data).ToList();
    }

    // Removes selected cards from the hand
    public void DiscardSelected()
    {
        foreach (var view in _selected)
        {
            _hand.Remove(view);
            Destroy(view.gameObject);
        }
        _selected.Clear();
        ApplySort();
        OnSelectionChanged?.Invoke();
    }

    // Removes the provided cards
    public void DiscardCards(List<CardView> cards)
    {
        foreach (var view in cards)
        {
            _hand.Remove(view);
            _selected.Remove(view);
            Destroy(view.gameObject);
        }
        ApplySort();
        OnSelectionChanged?.Invoke();
    }

    // Clears selection from all cards
    public void ClearSelection()
    {
        foreach (var v in _selected) v.SetSelected(false);
        _selected.Clear();
        OnSelectionChanged?.Invoke();
    }

    // Locks a random card in hand so it cannot be selected or played
    public void PetrifyRandom()
    {
        var available = _hand.Where(v => !v.IsPetrified).ToList();
        if (available.Count == 0) return;
        available[UnityEngine.Random.Range(0, available.Count)].SetPetrified(true);
    }

    public void SetCardsInteractable(bool interactable)
    {
        foreach (CardView card in _hand)
        {
            card.SetInteractable(interactable);
        }
    }

    public IEnumerator AnimateAttack(Vector2 enemyPosition, Action HitCallback)
    {
        List<Coroutine> routines = new ();

        for (int i = 0; i < _selected.Count; ++i)
        {
            routines.Add(_selected[i].PlayAttackAnimation(enemyPosition));
            yield return new WaitForSeconds(_attackDelay);
        }
        yield return CoroutineUtils.WhenAll(this, routines, HitCallback);
    }

    public IEnumerator AnimateDiscard()
    {
        while (_selected.Count > 0)
        {
            _selected[0].PlayDiscardAnimation();
            _hand.Remove(_selected[0]);
            _selected.Remove(_selected[0]);
            yield return new WaitForSeconds(_discardDelay);
        }
    }

    // Internal
    private void SpawnCard(Card card)
    {
        GameObject go = Instantiate(_cardPrefab, _handArea);
        CardView view = go.GetComponent<CardView>();

        view.Setup(card, _deckTransform.position, Vector2.zero);
        view.OnCardClicked += HandleCardClicked;

        _hand.Add(view);
        OnCardDrawn?.Invoke(view);
    }

    private void HandleCardClicked(CardView view)
    {
        if (view.IsPetrified) return;

        if (view.IsSelected)
        {
            // Deselect
            view.SetSelected(false);
            _selected.Remove(view);
        }
        else
        {
            if (_selected.Count >= _maxSelected) return;

            view.SetSelected(true);
            _selected.Add(view);
        }

        OnSelectionChanged?.Invoke();
    }

    // Layout cards as a fan/line with centering
    private void LayoutHand()
    {
        int count = _hand.Count;
        if (count == 0) return;

        float totalWidth = (count - 1) * _cardSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) : 0.5f;
            float x = startX + i * _cardSpacing;
            float rot = _fanAngle * (0.5f - t) * 2f;

            Vector2 basePos = new Vector2(x, 0f);
            _hand[i].SetBasePosition(basePos);

            RectTransform rt = _hand[i].GetComponent<RectTransform>();
            rt.localEulerAngles = new Vector3(0, 0, rot);
        }
    }

    private void ToggleSort()
    {
        _sortBySuit = !_sortBySuit;
        ApplySort();
    }

    private void ApplySort()
    {
        if (_sortBySuit)
            _hand = _hand.OrderBy(v => v.Data.Suit).ThenBy(v => v.Data.Rank).ToList();
        else
            _hand = _hand.OrderBy(v => v.Data.Rank).ThenBy(v => v.Data.Suit).ToList();

        for (int i = 0; i < _hand.Count; i++)
            _hand[i].transform.SetSiblingIndex(i);

        LayoutHand();
    }
}
