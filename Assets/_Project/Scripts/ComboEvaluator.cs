using System.Collections.Generic;
using System.Linq;

/* Справка по комбинациям:
 * Старшая карта (High Card) - 1 карта
 * Пара (Pair) - 2 карты одного ранга
 * Две пары (Two Pair) - 2 разные пары
 * Сет (Set) - 3 карты одного ранга
 * Каре (Four of a Kind) - 4 карты одного ранга
 * Стрит (Straight) - 5 карт по порядку, не обязательно одной масти
 * Флеш (Flush) - 5 карт одной масти, не обязательно по порядку
 * Фулл-хаус (Full House) - 3 карты одного ранга + 2 карты другого ранга
 * Стрит-флеш (Straight Flush) - 5 карт по порядку и одной масти
 * Роял-флеш (Royal Flush) - 10, J, Q, K, A одной масти
 */

// Тип комбинации
public enum ComboType
{
    None,
    High,           // Старшая карта
    Pair,           // Пара
    TwoPair,        // Две пары
    Set,            // Тройка
    FOK,            // Каре
    Straight,       // Стрит
    Flush,          // Флеш
    FullHouse,      // Фулл-хаус
    StraightFlush,  // Стрит-флеш
    RoyalFlush      // Роял-флеш
}

// Результат оценки комбинации
public class ComboResult
{
    public ComboType Type;
    public int BaseDamage;
    public int NominalSum;
    public int CritCount; 
    public List<Card> Cards;

    // Итоговый урон с учётом номиналов и критов.
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

// Определяет покерную комбинацию
public static class ComboEvaluator
{
    // Базовый урон по типу комбинации
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

    // 2 карты
    private static ComboType EvaluateTwo(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.Rank).ToList();

        if (groups.Count == 1) 
            return ComboType.Pair;   // Пара

        return ComboType.High;       // Старшая карта
    }

    // 3 карты
    private static ComboType EvaluateThree(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.Rank).OrderByDescending(g => g.Count()).ToList();

        if (groups[0].Count() == 3) return ComboType.Set;  // Сет
        if (groups[0].Count() == 2) return ComboType.Pair; // Пара

        return ComboType.High;
    }

    // 4 карты
    private static ComboType EvaluateFour(List<Card> cards)
    {
        var groups = cards.GroupBy(c => c.Rank).OrderByDescending(g => g.Count()).ToList();

        if (groups[0].Count() == 4) 
            return ComboType.FOK;           // Каре
        if (groups[0].Count() == 3) 
            return ComboType.Set;           // Сет

        if (groups.Count == 2 && groups.All(g => g.Count() == 2))
            return ComboType.TwoPair;       // Две пары

        if (groups[0].Count() == 2) 
            return ComboType.Pair;          // Пара

        return ComboType.High;              // Старшая карта
    }

    // 5 карт
    private static ComboType EvaluateFive(List<Card> cards)
    {
        bool isFlush = cards.All(c => c.Suit == cards[0].Suit);
        bool isStraight = IsStraight(cards);

        if (isFlush && IsRoyal(cards)) 
            return ComboType.RoyalFlush;            // Роял-флеш
        if (isFlush && isStraight) 
            return ComboType.StraightFlush;         // Стрит-флеш

        var groups = cards.GroupBy(c => c.Rank).OrderByDescending(g => g.Count()).ToList();

        if (groups[0].Count() == 4) 
            return ComboType.FOK;                   // Каре

        if (groups[0].Count() == 3 && groups.Count > 1 && groups[1].Count() == 2)
            return ComboType.FullHouse;             // Фулл-хаус

        if (isFlush) 
            return ComboType.Flush;                 // Флеш
        if (isStraight) 
            return ComboType.Straight;              // Стрит

        if (groups[0].Count() == 3) 
            return ComboType.Set;                   // Сет

        if (groups.Count <= 3 && groups.Count(g => g.Count() == 2) >= 2)
            return ComboType.TwoPair;               // Две пары

        if (groups[0].Count() == 2) 
            return ComboType.Pair;                  // Пара

        return ComboType.High;                      // Старшая карта
    }

    // Проверка на стрит
    private static bool IsStraight(List<Card> cards)
    {
        var ranks = cards.Select(c => (int)c.Rank).OrderBy(r => r).ToList();
        for (int i = 1; i < ranks.Count; i++)
            if (ranks[i] != ranks[i - 1] + 1) return false;
        return true;
    }

    // Проверка на роял-флеш (10, J, Q, K, A)
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