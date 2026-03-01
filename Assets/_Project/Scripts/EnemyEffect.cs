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

// No special effect
public class NoEffect : EnemyEffect
{
    public override string Description => "No special effect.";
}

// Wolf: player has fewer discards this battle
public class ReducedDiscards : EnemyEffect
{
    private readonly int _reduction;
    public ReducedDiscards(int reduction) => _reduction = reduction;

    public override void OnBattleStart(BattleContext ctx)
        => ctx.Discards = Mathf.Max(0, ctx.Discards - _reduction);

    public override string Description
        => $"You have {_reduction} fewer discard{(_reduction > 1 ? "s" : "")} this battle.";
}

// Raven: every Nth card drawn is face-down
public class FaceDownCards : EnemyEffect
{
    private readonly int _interval;
    private int _drawCounter;

    public FaceDownCards(int interval) => _interval = interval;

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
        if (_drawCounter % _interval == 0)
            card.SetFaceDown(true);
    }

    public override string Description => $"Every {_interval}th card drawn is face-down.";
}

// Fox: one random suit contributes no chip damage
public class SuitNoDamage : EnemyEffect
{
    public override void OnBattleStart(BattleContext ctx)
    {
        ctx.BlockedDamageSuit = (Suit)Random.Range(0, 4);
        Debug.Log($"[Fox] Blocked suit: {ctx.BlockedDamageSuit}");
    }

    public override string Description
        => "Cards in one random suit deal no chip damage this battle (revealed at start).";
}

// Alpha Wolf: lose HP per card discarded
public class DamageOnDiscard : EnemyEffect
{
    private readonly int _damagePerCard;
    public DamageOnDiscard(int damagePerCard) => _damagePerCard = damagePerCard;

    public override void OnPlayerDiscard(BattleContext ctx, int cardCount)
    {
        ctx.PlayerHp -= _damagePerCard * cardCount;
        ctx.RequestUIRefresh?.Invoke();
    }

    public override string Description => $"Lose {_damagePerCard} HP per card discarded.";
}

// Basilisk: one random card locked at battle start and after each attack
public class PetrifyCard : EnemyEffect
{
    public override void OnBattleStart(BattleContext ctx) => ctx.Hand.PetrifyRandom();
    public override void OnPlayerAttack(BattleContext ctx, ComboResult result) => ctx.Hand.PetrifyRandom();

    public override string Description
        => "At battle start and after each attack, one random card in hand becomes unusable.";
}

// Scarab: attacks using N+ cards deal reduced damage
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
        => $"Attacks using {_minCards}+ cards deal {(int)(_multiplier * 100)}% damage.";
}

// Minotaur (Boss): enemy damage increases after each enemy attack
public class EscalateDamage : EnemyEffect
{
    private readonly int _increasePerAttack;
    public EscalateDamage(int increasePerAttack) => _increasePerAttack = increasePerAttack;

    public override void OnEnemyAttack(BattleContext ctx)
        => ctx.EnemyDamage += _increasePerAttack;

    public override string Description
        => $"After each enemy attack, its damage increases by {_increasePerAttack}.";
}

// Spider (Boss): consecutive attacks of the same combo type deal 0 damage
public class NoRepeatCombo : EnemyEffect
{
    private ComboType _lastCombo = ComboType.None;

    public override int ModifyPlayerDamage(BattleContext ctx, ComboResult result, int damage)
    {
        if (result.Type != ComboType.None && result.Type == _lastCombo)
        {
            Debug.Log("[Spider] Repeated combo — 0 damage!");
            return 0;
        }
        _lastCombo = result.Type;
        return damage;
    }

    public override string Description => "Consecutive attacks of the same hand type deal 0 damage.";
}

// Leviathan (Boss): penalty cycles each time the enemy attacks
public class CyclingPenalty : EnemyEffect
{
    private readonly EnemyEffect[] _phases = new EnemyEffect[]
    {
        new ReducedDiscards(1),
        new LargeHandPenalty(4, 0.5f),
        new DamageOnDiscard(3),
        new PetrifyCard(),
    };

    private int _phaseIndex;

    public override void OnBattleStart(BattleContext ctx)
    {
        _phaseIndex = 0;
        _phases[_phaseIndex].OnBattleStart(ctx);
    }

    public override void OnEnemyAttack(BattleContext ctx)
    {
        _phaseIndex = (_phaseIndex + 1) % _phases.Length;
        Debug.Log($"[Leviathan] Phase → {_phases[_phaseIndex].Description}");
        _phases[_phaseIndex].OnBattleStart(ctx);
    }

    public override int ModifyPlayerDamage(BattleContext ctx, ComboResult result, int damage)
        => _phases[_phaseIndex].ModifyPlayerDamage(ctx, result, damage);

    public override string Description => "Each time the enemy attacks, its penalty changes.";
}
