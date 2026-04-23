using System.Collections.Generic;
using UnityEngine;

// Base class for all enemy effects. Override only the hooks you need.
// BattleController calls these at the appropriate moments.
public abstract class EnemyEffect
{
    public virtual void OnBattleStart(BattleContext ctx) { }
    public virtual void OnPlayerAttack(BattleContext ctx, ComboResult result) { }
    public virtual void OnPlayerDiscard(BattleContext ctx, int cardCount) { }
    public virtual void OnEnemyAttack(BattleContext ctx) { }
    public virtual int ModifyPlayerDamage(BattleContext ctx, ComboResult result, int damage) => damage;
    public virtual void OnBattleEnd(BattleContext ctx) { }

    // Shown in enemy UI
    public abstract string Description { get; }
}

public class NoEffect : EnemyEffect
{
    public override string Description => "Без особого эффекта.";
}

public class ReducedDiscards : EnemyEffect
{
    private readonly int _reduction;

    public ReducedDiscards(int reduction) => _reduction = reduction;

    public override void OnBattleStart(BattleContext ctx)
        => ctx.Discards = Mathf.Max(0, ctx.Discards - _reduction);

    public override string Description => $"- {_reduction} {DiscardToRussian()} в этом бою";

    private string DiscardToRussian()
    {
        if (_reduction % 10 == 1 && _reduction % 100 != 11)
            return "сброс";

        if (_reduction % 10 == 2 && _reduction % 100 != 12 ||
            _reduction % 10 == 3 && _reduction % 100 != 13 ||
            _reduction % 10 == 4 && _reduction % 100 != 14)
        {
            return "сброса";
        }

        return "сбросов";
    }
}

public class FaceDownCards : EnemyEffect
{
    private readonly int _interval;
    private readonly int _initialFaceDownCount;
    private int _drawCounter;

    public FaceDownCards(int interval, int initialFaceDownCount = 0)
    {
        _interval = interval;
        _initialFaceDownCount = initialFaceDownCount;
    }

    public override void OnBattleStart(BattleContext ctx)
    {
        _drawCounter = 0;
        ctx.Hand.OnCardDrawn += HandleCardDrawn;
    }

    public override void OnBattleEnd(BattleContext ctx)
        => ctx.Hand.OnCardDrawn -= HandleCardDrawn;

    private void HandleCardDrawn(CardView card)
    {
        _drawCounter++;
        if (_drawCounter <= _initialFaceDownCount || _drawCounter % _interval == 0)
            card.SetFaceDown(true);
    }

    public override string Description
    {
        get
        {
            if (_initialFaceDownCount <= 0)
                return $"Каждая {_interval}-я карта вытягивается рубашкой вверх";

            return $"{_initialFaceDownCount} старт. {CardToRussian(_initialFaceDownCount)} скрыты, затем каждая {_interval}-я карта тянется рубашкой вверх";
        }
    }

    private static string CardToRussian(int count)
    {
        if (count % 10 == 1 && count % 100 != 11)
            return "карта";

        if (count % 10 is >= 2 and <= 4 && (count % 100 < 12 || count % 100 > 14))
            return "карты";

        return "карт";
    }
}

public class SuitNoDamage : EnemyEffect
{
    private readonly int _blockedSuitCount;

    public SuitNoDamage(int blockedSuitCount = 1)
    {
        _blockedSuitCount = Mathf.Clamp(blockedSuitCount, 1, 4);
    }

    public override void OnBattleStart(BattleContext ctx)
    {
        List<Suit> suits = new List<Suit> { Suit.Stone, Suit.Fire, Suit.Sun, Suit.Moon };
        Shuffle(suits);

        ctx.BlockedDamageSuits = suits.GetRange(0, _blockedSuitCount);
        Debug.Log($"[Fox] Blocked suits: {string.Join(", ", ctx.BlockedDamageSuits)}");
    }

    public override string Description
        => _blockedSuitCount == 1
            ? "Карты одной случайной масти не наносят урона"
            : $"Карты {_blockedSuitCount} случайных мастей не наносят урона";

    private static void Shuffle(List<Suit> suits)
    {
        for (int i = suits.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (suits[i], suits[swapIndex]) = (suits[swapIndex], suits[i]);
        }
    }
}

public class DamageOnDiscard : EnemyEffect
{
    private readonly int _damagePerCard;

    public DamageOnDiscard(int damagePerCard) => _damagePerCard = damagePerCard;

    public override void OnPlayerDiscard(BattleContext ctx, int cardCount)
    {
        ctx.PlayerHp -= _damagePerCard * cardCount;
        ctx.RequestUIRefresh?.Invoke();
    }

    public override string Description => $"Получайте {_damagePerCard} {DamageToRussian()} за каждую сброшенную карту";

    private string DamageToRussian()
    {
        if (_damagePerCard % 10 == 1 && _damagePerCard % 100 != 11)
            return "урон";

        return "урона";
    }
}

public class PetrifyCard : EnemyEffect
{
    private readonly int _petrifyCount;

    public PetrifyCard(int petrifyCount = 1)
    {
        _petrifyCount = Mathf.Max(1, petrifyCount);
    }

    public override void OnBattleStart(BattleContext ctx) => Petrify(ctx);

    public override void OnPlayerAttack(BattleContext ctx, ComboResult result) => Petrify(ctx);

    public override string Description
        => _petrifyCount == 1
            ? "В начале битвы и после атаки одна карта блокируется"
            : $"В начале битвы и после атаки блокируются {_petrifyCount} случайные карты";

    private void Petrify(BattleContext ctx)
    {
        for (int i = 0; i < _petrifyCount; i++)
            ctx.Hand.PetrifyRandom();
    }
}

public class LargeHandPenalty : EnemyEffect
{
    private readonly int _minCards;
    private readonly float _multiplier;

    public LargeHandPenalty(int minCards, float multiplier)
    {
        _minCards = minCards;
        _multiplier = multiplier;
    }

    public override int ModifyPlayerDamage(BattleContext ctx, ComboResult result, int damage)
        => result.CardCount >= _minCards ? Mathf.RoundToInt(damage * _multiplier) : damage;

    public override string Description
        => $"Атаки с {_minCards}+ картами наносят {(int)(_multiplier * 100)}% урона";
}

public class EscalateDamage : EnemyEffect
{
    private readonly int _increasePerAttack;

    public EscalateDamage(int increasePerAttack) => _increasePerAttack = increasePerAttack;

    public override void OnEnemyAttack(BattleContext ctx)
        => ctx.EnemyDamage += _increasePerAttack;

    public override string Description
        => $"После каждой атаки врага, его урон увеличивается на {_increasePerAttack}";
}

public class NoRepeatCombo : EnemyEffect
{
    private readonly int _memoryLength;
    private readonly Queue<ComboType> _recentCombos = new Queue<ComboType>();

    public NoRepeatCombo(int memoryLength = 1)
    {
        _memoryLength = Mathf.Max(1, memoryLength);
    }

    public override void OnBattleStart(BattleContext ctx) => _recentCombos.Clear();

    public override int ModifyPlayerDamage(BattleContext ctx, ComboResult result, int damage)
    {
        if (result.Type != ComboType.None && _recentCombos.Contains(result.Type))
        {
            Debug.Log("[Spider] Blocked repeated combo.");
            return 0;
        }

        if (result.Type != ComboType.None)
        {
            _recentCombos.Enqueue(result.Type);
            while (_recentCombos.Count > _memoryLength)
                _recentCombos.Dequeue();
        }

        return damage;
    }

    public override string Description
        => _memoryLength == 1
            ? "Одна и та же комбинация не может нанести урон 2 раза подряд"
            : $"Комбинации из последних {_memoryLength} атак не наносят урона при повторе";
}

public class CyclingPenalty : EnemyEffect
{
    private readonly EnemyEffect[] _phases;
    private int _phaseIndex;

    public CyclingPenalty(DifficultyLevel difficulty)
    {
        _phases = difficulty switch
        {
            DifficultyLevel.Hard => new EnemyEffect[]
            {
                new ReducedDiscards(2),
                new LargeHandPenalty(4, 0.5f),
                new DamageOnDiscard(5),
                new PetrifyCard(2),
            },
            DifficultyLevel.Demon => new EnemyEffect[]
            {
                new ReducedDiscards(2),
                new LargeHandPenalty(3, 0.4f),
                new DamageOnDiscard(6),
                new PetrifyCard(3),
            },
            _ => new EnemyEffect[]
            {
                new ReducedDiscards(1),
                new LargeHandPenalty(4, 0.6f),
                new DamageOnDiscard(4),
                new PetrifyCard(1),
            },
        };
    }

    public override void OnBattleStart(BattleContext ctx)
    {
        _phaseIndex = 0;
        _phases[_phaseIndex].OnBattleStart(ctx);
    }

    public override void OnEnemyAttack(BattleContext ctx)
    {
        _phaseIndex = (_phaseIndex + 1) % _phases.Length;
        Debug.Log($"[Amalgam] Phase -> {_phases[_phaseIndex].Description}");
        _phases[_phaseIndex].OnBattleStart(ctx);
    }

    public override int ModifyPlayerDamage(BattleContext ctx, ComboResult result, int damage)
        => _phases[_phaseIndex].ModifyPlayerDamage(ctx, result, damage);

    public override string Description => "После каждой атаки врага активный дебафф меняется";
}
