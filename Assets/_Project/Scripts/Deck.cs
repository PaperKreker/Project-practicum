using System.Collections.Generic;
using UnityEngine;

// Standard 52-card deck with shuffle and draw mechanics
public class Deck
{
    private List<Card> _cards = new List<Card>();

    public int Remaining => _cards.Count;

    public Deck()
    {
        Reset();
    }

    // Creates a standard 52-card deck and shuffles it
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

    // Shuffles the deck using Fisher-Yates algorithm
    public void Shuffle()
    {
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }

    // Draws a single card from the top of the deck
    public Card Draw()
    {
        if (_cards.Count == 0) return null;

        Card card = _cards[_cards.Count - 1];
        _cards.RemoveAt(_cards.Count - 1);
        return card;
    }

    // Draws multiple cards
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

    // Applies critical chance to a list of cards
    public void ApplyCriticalChance(List<Card> cards, float critChance)
    {
        foreach (var card in cards)
        {
            card.IsCritical = Random.value < critChance;
        }
    }
}
