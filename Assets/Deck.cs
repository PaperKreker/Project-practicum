using System.Collections.Generic;
using UnityEngine;

// Стандартная колода 52 карты.
// Управляет перемешиванием и раздачей.
public class Deck
{
    private List<Card> _cards = new List<Card>();

    public int Remaining => _cards.Count;

    public Deck()
    {
        Reset();
    }

    // Создаёт и перемешивает полную колоду
    public void Reset()
    {
        _cards.Clear();

        foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
            {
                _cards.Add(new Card(suit, rank));
            }
        }

        Shuffle();
    }

    // Перемешивает колоду алгоритмом Fisher-Yates
    public void Shuffle()
    {
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }

    // Берёт одну карту с верха колоды.
    public Card Draw()
    {
        if (_cards.Count == 0) return null;

        Card card = _cards[_cards.Count - 1];
        _cards.RemoveAt(_cards.Count - 1);
        return card;
    }

    // Берёт несколько карт за раз
    public List<Card> Draw(int count)
    {
        List<Card> drawn = new List<Card>();
        for (int i = 0; i < count; i++)
        {
            Card c = Draw();
            if (c == null) break;
            drawn.Add(c);
        }
        return drawn;
    }

    // Применяет шанс крита ко всем взятым картам
    public void ApplyCriticalChance(List<Card> cards, float critChance)
    {
        foreach (var card in cards)
        {
            card.IsCritical = Random.value < critChance;
        }
    }
}