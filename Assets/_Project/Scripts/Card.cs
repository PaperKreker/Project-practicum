using UnityEngine;

// Card suit
public enum Suit
{
    Stone,
    Fire,
    Sun,
    Moon
}

// Card rank
public enum Rank
{
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14
}

// Card class representing a single playing card
[System.Serializable]
public class Card
{
    public Suit Suit;
    public Rank Rank;
    public bool IsCritical;
    public bool IsDebuffed;

    public int NominalValue
    {
        get
        {
            if (IsDebuffed) return 0;
            if (Rank == Rank.Ace) return 11;
            if (Rank >= Rank.Jack) return 10;
            return (int)Rank;
        }
    }

    public Card(Suit suit, Rank rank, bool isCritical = false)
    {
        Suit = suit;
        Rank = rank;
        IsCritical = isCritical;
    }

    public override string ToString()
    {
        return $"{Rank} of {Suit}{(IsCritical ? " [CRIT]" : "")}{(IsDebuffed ? " [DEBUFF]" : "")}";
    }
}
