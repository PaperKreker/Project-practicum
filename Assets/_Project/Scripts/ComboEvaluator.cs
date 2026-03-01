using System.Collections.Generic;
using System.Linq;

/* Combination reference:
 * High Card - 1 card
 * Pair - 2 cards of the same rank
 * Two Pair - 2 different pairs
 * Set (Three of a Kind) - 3 cards of the same rank
 * Four of a Kind - 4 cards of the same rank
 * Straight - 5 consecutive ranks, not necessarily the same suit
 * Flush - 5 cards of the same suit, not necessarily consecutive
 * Full House - 3 cards of one rank + 2 cards of another rank
 * Straight Flush - 5 consecutive ranks of the same suit
 * Royal Flush - 10, J, Q, K, A of the same suit
 */

// Combo type
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

// Result of combo evaluation
public class ComboResult
{
    public ComboType Type;
    public int BaseDamage;
    public int NominalSum;
    public int CritCount;
    public List<Card> Cards;

    public int CardCount => Cards?.Count ?? 0;

    // Total damage considering nominal values and crits.
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

    public override string ToString()
    {
        return $"{Type} | Base:{BaseDamage} + Nominal:{NominalSum} x{(CritCount > 0 ? $"CRIT×{CritCount}" : "1")} = {TotalDamage:F0} dmg";
    }
}

// Determines the combo type and calculates damage based on the given cards
public static class ComboEvaluator
{
    // Base damage per combo type
    private static readonly Dictionary<ComboType, int> BaseDamageTable = new()
    {
        { ComboType.High,          10  },
        { ComboType.Pair,          20  },
        { ComboType.TwoPair,       40  },
        { ComboType.Set,           80  },
        { ComboType.FOK,           400 },
        { ComboType.Straight,      100 },
        { ComboType.Flush,         125 },
        { ComboType.FullHouse,     175 },
        { ComboType.StraightFlush, 600 },
        { ComboType.RoyalFlush,    2000},
    };

    // Calculate the combo result based on ranks and hand type
    public static ComboResult Evaluate(List<Card> cards)
    {
        if (cards == null || cards.Count == 0)
            return new ComboResult { Type = ComboType.None };

        ComboType type = cards.Count switch
        {
            1 => ComboType.High,
            2 => EvaluateTwo(cards),
            3 => EvaluateThree(cards),
            4 => EvaluateFour(cards),
            5 => EvaluateFive(cards),
            _ => ComboType.None
        };

        int nominalSum = cards.Sum(c => c.NominalValue);
        int critCount = cards.Count(c => c.IsCritical);

        return new ComboResult
        {
            Type = type,
            BaseDamage = BaseDamageTable.GetValueOrDefault(type, 0),
            NominalSum = nominalSum,
            CritCount = critCount,
            Cards = cards
        };
    }

    // 2 cards
    private static ComboType EvaluateTwo(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.Rank).ToList();

        if (groups.Count == 1)
            return ComboType.Pair;   // Pair

        return ComboType.High;       // High Card
    }

    // 3 cards
    private static ComboType EvaluateThree(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.Rank).OrderByDescending(g => g.Count()).ToList();

        if (groups[0].Count() == 3) return ComboType.Set;  // Three of a Kind
        if (groups[0].Count() == 2) return ComboType.Pair; // Pair

        return ComboType.High;
    }

    // 4 cards
    private static ComboType EvaluateFour(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.Rank).OrderByDescending(g => g.Count()).ToList();

        if (groups[0].Count() == 4)
            return ComboType.FOK;           // Four of a Kind
        if (groups[0].Count() == 3)
            return ComboType.Set;           // Three of a Kind

        if (groups.Count == 2 && groups.All(g => g.Count() == 2))
            return ComboType.TwoPair;       // Two Pair

        if (groups[0].Count() == 2)
            return ComboType.Pair;          // Pair

        return ComboType.High;              // High Card
    }

    // 5 cards
    private static ComboType EvaluateFive(List<Card> cards)
    {
        bool isFlush = cards.All(c => c.Suit == cards[0].Suit);
        bool isStraight = IsStraight(cards);

        if (isFlush && IsRoyal(cards))
            return ComboType.RoyalFlush;            // Royal Flush
        if (isFlush && isStraight)
            return ComboType.StraightFlush;         // Straight Flush

        var groups = cards.GroupBy(c => c.Rank).OrderByDescending(g => g.Count()).ToList();

        if (groups[0].Count() == 4)
            return ComboType.FOK;                   // Four of a Kind

        if (groups[0].Count() == 3 && groups.Count > 1 && groups[1].Count() == 2)
            return ComboType.FullHouse;             // Full House

        if (isFlush)
            return ComboType.Flush;                 // Flush
        if (isStraight)
            return ComboType.Straight;              // Straight

        if (groups[0].Count() == 3)
            return ComboType.Set;                   // Three of a Kind

        if (groups.Count <= 3 && groups.Count(g => g.Count() == 2) >= 2)
            return ComboType.TwoPair;               // Two Pair

        if (groups[0].Count() == 2)
            return ComboType.Pair;                  // Pair

        return ComboType.High;                      // High Card
    }

    // Check for straight
    private static bool IsStraight(List<Card> cards)
    {
        var ranks = cards.Select(c => (int)c.Rank).OrderBy(r => r).ToList();
        for (int i = 1; i < ranks.Count; i++)
            if (ranks[i] != ranks[i - 1] + 1) return false;
        return true;
    }

    // Check for royal flush (10, J, Q, K, A)
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
