using UnityEngine;
using System.Collections.Generic;

public enum SigilType { Damage, Defense, Utility, Economy, Other }

public abstract class Sigil
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract int Cost { get; }
    public abstract SigilType Type { get; }

    // Hook points for BattleController
    public virtual void OnBattleStart(BattleContext ctx) { }
    public virtual void OnPlayerAttack(BattleContext ctx, ComboResult result) { }
    public virtual void OnPlayerDiscard(BattleContext ctx, int cardCount) { }
    public virtual void OnEnemyAttack(BattleContext ctx) { }
    public virtual void OnBattleEnd(BattleContext ctx) { }

    public virtual int ModifyPlayerDamage(BattleContext ctx, ComboResult result, int damage) => damage;

    public virtual int BonusDamage(BattleContext ctx, ComboResult result) => 0;

    public virtual float BonusMultiplier(BattleContext ctx, ComboResult result) => 0f;
}


public class SigilTheMoon : Sigil
{
    public override string Name => "The Moon";
    public override string Description => "Each attacking Spade suit card deals +15 damage.";
    public override int Cost => 6;
    public override SigilType Type => SigilType.Damage;

    public override int BonusDamage(BattleContext ctx, ComboResult result)
    {
        int count = 0;
        foreach (var c in result.ScoringCards)
            if (c.Suit == Suit.Spades) count++;
        return count * 15;
    }
}

public class SigilTheSun : Sigil
{
    public override string Name => "The Sun";
    public override string Description => "Each attacking Heart suit card deals +15 damage.";
    public override int Cost => 6;
    public override SigilType Type => SigilType.Damage;

    public override int BonusDamage(BattleContext ctx, ComboResult result)
    {
        int count = 0;
        foreach (var c in result.ScoringCards)
            if (c.Suit == Suit.Hearts) count++;
        return count * 15;
    }
}

public class SigilIsolation : Sigil
{
    public override string Name => "Isolation";
    public override string Description => "Solo attacks deal +40 damage.";
    public override int Cost => 6;
    public override SigilType Type => SigilType.Damage;

    public override int BonusDamage(BattleContext ctx, ComboResult result)
        => result.Type == ComboType.High ? 40 : 0;
}

public class SigilDyadic : Sigil
{
    public override string Name => "Dyadic";
    public override string Description => "Attacks containing a Pair deal +30 damage per Pair.";
    public override int Cost => 7;
    public override SigilType Type => SigilType.Damage;

    public override int BonusDamage(BattleContext ctx, ComboResult result)
    {
        if (result.Type == ComboType.Pair) return 30;
        if (result.Type == ComboType.TwoPair) return 60;
        if (result.Type == ComboType.FullHouse) return 30;
        return 0;
    }
}

public class SigilTriadic : Sigil
{
    public override string Name => "Triadic";
    public override string Description => "Attacks containing a Set deal +85 damage.";
    public override int Cost => 8;
    public override SigilType Type => SigilType.Damage;

    public override int BonusDamage(BattleContext ctx, ComboResult result)
        => (result.Type == ComboType.Set || result.Type == ComboType.FullHouse) ? 85 : 0;
}

public class SigilAlignment : Sigil
{
    public override string Name => "Alignment";
    public override string Description => "Attacks containing a Straight deal +70 damage.";
    public override int Cost => 7;
    public override SigilType Type => SigilType.Damage;

    public override int BonusDamage(BattleContext ctx, ComboResult result)
        => (result.Type == ComboType.Straight || result.Type == ComboType.StraightFlush) ? 70 : 0;
}

public class SigilFlow : Sigil
{
    public override string Name => "Flow";
    public override string Description => "Attacks containing a Flush deal +100 damage.";
    public override int Cost => 8;
    public override SigilType Type => SigilType.Damage;

    public override int BonusDamage(BattleContext ctx, ComboResult result)
        => (result.Type == ComboType.Flush || result.Type == ComboType.StraightFlush || result.Type == ComboType.RoyalFlush) ? 100 : 0;
}

public class SigilMalice : Sigil
{
    private bool _used;

    public override string Name => "Malice";
    public override string Description => "Your first attack each battle deals +30% damage.";
    public override int Cost => 11;
    public override SigilType Type => SigilType.Damage;

    public override void OnBattleStart(BattleContext ctx) => _used = false;

    public override float BonusMultiplier(BattleContext ctx, ComboResult result)
    {
        if (_used) return 0f;
        _used = true;
        return 0.30f;
    }
}

public class SigilRage : Sigil
{
    public override string Name => "Rage";
    public override string Description => "While at or below 50% HP, deal +30% damage.";
    public override int Cost => 14;
    public override SigilType Type => SigilType.Damage;

    public override float BonusMultiplier(BattleContext ctx, ComboResult result)
    {
        return ctx.PlayerHp <= ctx.PlayerMaxHp / 2 ? 0.30f : 0f;
    }
}

public class SigilConformity : Sigil
{
    public override string Name => "Conformity";
    public override string Description => "If an attack only contains cards of the same rank, it deals +70% damage.";
    public override int Cost => 15;
    public override SigilType Type => SigilType.Damage;

    public override float BonusMultiplier(BattleContext ctx, ComboResult result)
    {
        if (result.ScoringCards == null || result.ScoringCards.Count == 0) return 0f;
        var rank = result.ScoringCards[0].Rank;
        foreach (var c in result.ScoringCards)
            if (c.Rank != rank) return 0f;
        return 0.70f;
    }
}

public class SigilFortification : Sigil
{
    private bool _triggered;

    public override string Name => "Fortification";
    public override string Description => "The enemy's first attack each battle deals 50% less damage.";
    public override int Cost => 9;
    public override SigilType Type => SigilType.Defense;

    public override void OnBattleStart(BattleContext ctx) => _triggered = false;

    public override void OnEnemyAttack(BattleContext ctx)
    {
        if (_triggered) return;
        _triggered = true;
        // Undo the damage just applied and re-apply at 50%
        ctx.PlayerHp += ctx.EnemyDamage;
        ctx.PlayerHp -= Mathf.CeilToInt(ctx.EnemyDamage * 0.5f);
        ctx.RequestUIRefresh?.Invoke();
    }
}

public class SigilMediation : Sigil
{
    private int _hpAtStart;

    public override string Name => "Mediation";
    public override string Description => "Restore 40% of HP lost in battle after it ends.";
    public override int Cost => 10;
    public override SigilType Type => SigilType.Defense;

    public override void OnBattleStart(BattleContext ctx) => _hpAtStart = ctx.PlayerHp;

    public override void OnBattleEnd(BattleContext ctx)
    {
        int lost = _hpAtStart - ctx.PlayerHp;
        if (lost <= 0) return;
        ctx.PlayerHp += Mathf.CeilToInt(lost * 0.4f);
    }
}

public class SigilRefusal : Sigil
{
    public override string Name => "Refusal";
    public override string Description => "Gain +1 discard each battle.";
    public override int Cost => 9;
    public override SigilType Type => SigilType.Utility;

    public override void OnBattleStart(BattleContext ctx) => ctx.Discards += 1;
}

public class SigilPower : Sigil
{
    public override string Name => "Power";
    public override string Description => "Critical cards deal an additional +25% damage.";
    public override int Cost => 14;
    public override SigilType Type => SigilType.Utility;

    public override float BonusMultiplier(BattleContext ctx, ComboResult result)
        => result.CritCount > 0 ? 0.25f : 0f;
}

public class SigilConversion : Sigil
{
    public override string Name => "Conversion";
    public override string Description => "When the battle ends, gain 2 Gold for each unused discard.";
    public override int Cost => 11;
    public override SigilType Type => SigilType.Economy;

    public override void OnBattleEnd(BattleContext ctx)
    {
        int gold = ctx.Discards * 2;
        if (gold > 0 && GameManager.Instance != null)
            GameManager.Instance.Run.Gold += gold;
    }
}
