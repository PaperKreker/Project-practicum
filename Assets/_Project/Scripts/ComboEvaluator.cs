using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/* Combination reference:
 * High Card       - only the highest-rank card scores
 * Pair            - 2 cards of the same rank
 * Two Pair        - 2 different pairs (4 cards)
 * Set             - 3 cards of the same rank
 * Four of a Kind  - 4 cards of the same rank
 * Straight        - 5 consecutive ranks, any suit
 * Flush           - 5 cards of the same suit
 * Full House      - 3 of one rank + 2 of another
 * Straight Flush  - 5 consecutive ranks, same suit
 * Royal Flush     - 10 J Q K A of the same suit
 */

public enum ComboType
{
    None,
    High,
    Pair,
    TwoPair,
    Set,
    FOK,
    Straight,
    Flush,
    FullHouse,
    StraightFlush,
    RoyalFlush
}

public class ComboResult
{
    public ComboType Type;
    public int BaseDamage;
    public int NominalSum;
    public int CritCount;
    public List<Card> Cards;        // all selected cards
    public List<Card> ScoringCards; // only cards that form the combo

    public int CardCount => Cards?.Count ?? 0;

    public float TotalDamage
    {
        get
        {
            float dmg = BaseDamage + NominalSum;
            float mult = 1f;
            for (int i = 0; i < CritCount; i++)
                mult *= 1.25f;
            return dmg * mult;
        }
    }

    public override string ToString() =>
        $"{Type} | Base:{BaseDamage} + Nominal:{NominalSum} x{(CritCount > 0 ? $"CRIT×{CritCount}" : "1")} = {TotalDamage:F0} dmg";
}

public static class ComboEvaluator
{
    private static readonly Dictionary<ComboType, int> BaseDamageTable = new()
    {
        { ComboType.High,          10   },
        { ComboType.Pair,          20   },
        { ComboType.TwoPair,       40   },
        { ComboType.Set,           80   },
        { ComboType.FOK,           400  },
        { ComboType.Straight,      100  },
        { ComboType.Flush,         125  },
        { ComboType.FullHouse,     175  },
        { ComboType.StraightFlush, 600  },
        { ComboType.RoyalFlush,    2000 },
    };

    public static ComboResult Evaluate(List<Card> cards)
    {
        if (cards == null || cards.Count == 0)
            return new ComboResult { Type = ComboType.None };

        List<Card> scoring;
        ComboType type = cards.Count switch
        {
            1 => EvaluateOne(cards, out scoring),
            2 => EvaluateTwo(cards, out scoring),
            3 => EvaluateThree(cards, out scoring),
            4 => EvaluateFour(cards, out scoring),
            5 => EvaluateFive(cards, out scoring),
            _ => Fallback(cards, out scoring),
        };

        int nominalSum = scoring.Sum(c => c.NominalValue);
        int critCount = scoring.Count(c => c.IsCritical);

        return new ComboResult
        {
            Type = type,
            BaseDamage = BaseDamageTable.GetValueOrDefault(type, 0),
            NominalSum = nominalSum,
            CritCount = critCount,
            Cards = cards,
            ScoringCards = scoring,
        };
    }

    // 1 card — the card itself scores
    private static ComboType EvaluateOne(List<Card> cards, out List<Card> scoring)
    {
        scoring = new List<Card>(cards);
        return ComboType.High;
    }

    // 2 cards
    private static ComboType EvaluateTwo(List<Card> cards, out List<Card> scoring)
    {
        if (cards[0].Rank == cards[1].Rank)
        {
            scoring = new List<Card>(cards);
            return ComboType.Pair;
        }

        scoring = new List<Card> { cards.OrderByDescending(c => c.Rank).First() };
        return ComboType.High;
    }

    // 3 cards
    private static ComboType EvaluateThree(List<Card> cards, out List<Card> scoring)
    {
        var groups = cards.GroupBy(c => c.Rank).OrderByDescending(g => g.Count()).ToList();

        if (groups[0].Count() == 3)
        {
            scoring = new List<Card>(cards);
            return ComboType.Set;
        }
        if (groups[0].Count() == 2)
        {
            scoring = groups[0].ToList();
            return ComboType.Pair;
        }

        scoring = new List<Card> { cards.OrderByDescending(c => c.Rank).First() };
        return ComboType.High;
    }

    // 4 cards
    private static ComboType EvaluateFour(List<Card> cards, out List<Card> scoring)
    {
        var groups = cards.GroupBy(c => c.Rank).OrderByDescending(g => g.Count()).ToList();

        if (groups[0].Count() == 4)
        {
            scoring = new List<Card>(cards);
            return ComboType.FOK;
        }
        if (groups[0].Count() == 3)
        {
            scoring = groups[0].ToList();
            return ComboType.Set;
        }
        if (groups.Count == 2 && groups.All(g => g.Count() == 2))
        {
            scoring = new List<Card>(cards);
            return ComboType.TwoPair;
        }
        if (groups[0].Count() == 2)
        {
            scoring = groups[0].ToList();
            return ComboType.Pair;
        }

        scoring = new List<Card> { cards.OrderByDescending(c => c.Rank).First() };
        return ComboType.High;
    }

    // 5 cards
    private static ComboType EvaluateFive(List<Card> cards, out List<Card> scoring)
    {
        bool isFlush = cards.All(c => c.Suit == cards[0].Suit);
        bool isStraight = IsStraight(cards);

        if (isFlush && IsRoyal(cards))
        {
            scoring = new List<Card>(cards);
            return ComboType.RoyalFlush;
        }
        if (isFlush && isStraight)
        {
            scoring = new List<Card>(cards);
            return ComboType.StraightFlush;
        }

        var groups = cards.GroupBy(c => c.Rank).OrderByDescending(g => g.Count()).ToList();

        if (groups[0].Count() == 4)
        {
            scoring = new List<Card>(cards);
            return ComboType.FOK;
        }
        if (groups[0].Count() == 3 && groups.Count > 1 && groups[1].Count() == 2)
        {
            scoring = new List<Card>(cards);
            return ComboType.FullHouse;
        }
        if (isFlush)
        {
            scoring = new List<Card>(cards);
            return ComboType.Flush;
        }
        if (isStraight)
        {
            scoring = new List<Card>(cards);
            return ComboType.Straight;
        }
        if (groups[0].Count() == 3)
        {
            scoring = groups[0].ToList();
            return ComboType.Set;
        }
        if (groups.Count <= 3 && groups.Count(g => g.Count() == 2) >= 2)
        {
            scoring = groups.Where(g => g.Count() == 2).SelectMany(g => g).ToList();
            return ComboType.TwoPair;
        }
        if (groups[0].Count() == 2)
        {
            scoring = groups[0].ToList();
            return ComboType.Pair;
        }

        scoring = new List<Card> { cards.OrderByDescending(c => c.Rank).First() };
        return ComboType.High;
    }

    private static ComboType Fallback(List<Card> cards, out List<Card> scoring)
    {
        scoring = new List<Card> { cards.OrderByDescending(c => c.Rank).First() };
        return ComboType.High;
    }

    private static bool IsStraight(List<Card> cards)
    {
        var ranks = cards.Select(c => (int)c.Rank).OrderBy(r => r).ToList();
        for (int i = 1; i < ranks.Count; i++)
            if (ranks[i] != ranks[i - 1] + 1) return false;
        return true;
    }

    private static bool IsRoyal(List<Card> cards)
    {
        var ranks = cards.Select(c => c.Rank).ToHashSet();
        return ranks.Contains(Rank.Ten) &&
               ranks.Contains(Rank.Jack) &&
               ranks.Contains(Rank.Queen) &&
               ranks.Contains(Rank.King) &&
               ranks.Contains(Rank.Ace);
    }
}
