using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/* 
Управляет рукой игрока:
- хранит до 8 карт
- раскладывает их веером/линией
- обрабатывает выбор (макс 5)
- сортировка по Q (масть / ранг)
- добор карт из Deck 
*/
public class HandController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _cardPrefab;   // prefab с картой
    [SerializeField] private RectTransform _handArea;    // контейнер руки

    [Header("Layout")]
    [SerializeField] private float _cardSpacing = 110f;
    [SerializeField] private float _cardWidth = 100f;
    [SerializeField] private float _fanAngle = 5f;

    [Header("Rules")]
    [SerializeField] private int _maxHandSize = 8;
    [SerializeField] private int _maxSelected = 5;

    // Состояние
    private List<CardView> _hand = new List<CardView>();
    private List<CardView> _selected = new List<CardView>();
    private Deck _deck;

    private bool _sortBySuit = true;

    // Unity
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            ToggleSort();
    }

    // Инициализация с колодой и начальной раздачей
    public void Init(Deck deck)
    {
        _deck = deck;
        DrawUpToMax();
    }

    // Добирает карты до максимального размера руки
    public void DrawUpToMax()
    {
        int needed = _maxHandSize - _hand.Count;
        if (needed <= 0) return;

        List<Card> drawn = _deck.Draw(needed);
        foreach (var card in drawn)
            SpawnCard(card);

        LayoutHand();
    }

    // Возвращает список выбранных карт
    public List<Card> GetSelectedCards()
    {
        return _selected.Select(v => v.Data).ToList();
    }

    // Убирает выбранные карты из руки
    public void DiscardSelected()
    {
        foreach (var view in _selected)
        {
            _hand.Remove(view);
            Destroy(view.gameObject);
        }
        _selected.Clear();
        LayoutHand();
    }

    // Убирает переданные карты
    public void DiscardCards(List<CardView> cards)
    {
        foreach (var view in cards)
        {
            _hand.Remove(view);
            _selected.Remove(view);
            Destroy(view.gameObject);
        }
        LayoutHand();
    }

    // Снимает выделение со всех карт
    public void ClearSelection()
    {
        foreach (var v in _selected) v.SetSelected(false);
        _selected.Clear();
    }

    // Internal
    private void SpawnCard(Card card)
    {
        GameObject go = Instantiate(_cardPrefab, _handArea);
        CardView view = go.GetComponent<CardView>();

        view.Setup(card, Vector2.zero);
        view.OnCardClicked += HandleCardClicked;

        _hand.Add(view);
    }

    private void HandleCardClicked(CardView view)
    {
        if (view.IsSelected)
        {
            // Снять выделение
            view.SetSelected(false);
            _selected.Remove(view);
        }
        else
        {
            if (_selected.Count >= _maxSelected) return;

            view.SetSelected(true);
            _selected.Add(view);
        }
    }

    // Раскладывает карты веером/линией с центрированием
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

        if (_sortBySuit)
            _hand = _hand.OrderBy(v => v.Data.Suit).ThenBy(v => v.Data.Rank).ToList();
        else
            _hand = _hand.OrderBy(v => v.Data.Rank).ThenBy(v => v.Data.Suit).ToList();

        for (int i = 0; i < _hand.Count; i++)
            _hand[i].transform.SetSiblingIndex(i);

        LayoutHand();
    }
}