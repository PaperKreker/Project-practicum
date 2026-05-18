using UnityEngine;
using System.Collections.Generic;

public enum SigilType { Damage, Defense, Utility, Economy }

public static class SigilTypeExtensions
{
    public static string ToFriendlyString(this SigilType status)
    {
        return status switch
        {
            SigilType.Damage => "Урон",
            SigilType.Defense => "Защита",
            SigilType.Utility => "Прочее",
            SigilType.Economy => "Экономика",
            _ => status.ToString()
        };
    }
}

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
    public override string Name => "Луна";
    public override string Description => "Каждая карта луны наносит +10 урона.";
    public override int Cost => 7;
    public override SigilType Type => SigilType.Damage;

    public override int BonusDamage(BattleContext ctx, ComboResult result)
    {
        int count = 0;
        foreach (var c in result.ScoringCards)
            if (c.Suit == Suit.Moon) count++;
        return count * 10;
    }
}

public class SigilTheSun : Sigil
{
    public override string Name => "Солнце";
    public override string Description => "Каждая карта солнца наносит +10 урона.";
    public override int Cost => 7;
    public override SigilType Type => SigilType.Damage;

    public override int BonusDamage(BattleContext ctx, ComboResult result)
    {
        int count = 0;
        foreach (var c in result.ScoringCards)
            if (c.Suit == Suit.Sun) count++;
        return count * 10;
    }
}

public class SigilIsolation : Sigil
{
    public override string Name => "Изоляция";
    public override string Description => "Старшая карта даёт +20 урона";
    public override int Cost => 8;
    public override SigilType Type => SigilType.Damage;

    public override int BonusDamage(BattleContext ctx, ComboResult result)
        => result.Type == ComboType.High ? 20 : 0;
}

public class SigilDyadic : Sigil
{
    public override string Name => "Двойная связь";
    public override string Description => "Пара даёт +12 урона, две пары +24, фулл-хаус +12.";
    public override int Cost => 9;
    public override SigilType Type => SigilType.Damage;

    public override int BonusDamage(BattleContext ctx, ComboResult result)
    {
        if (result.Type == ComboType.Pair) return 12;
        if (result.Type == ComboType.TwoPair) return 24;
        if (result.Type == ComboType.FullHouse) return 12;
        return 0;
    }
}

public class SigilTriadic : Sigil
{
    public override string Name => "Триада";
    public override string Description => "Атаки с сетом получают +40 урона.";
    public override int Cost => 10;
    public override SigilType Type => SigilType.Damage;

    public override int BonusDamage(BattleContext ctx, ComboResult result)
        => (result.Type == ComboType.Set || result.Type == ComboType.FullHouse) ? 40 : 0;
}

public class SigilAlignment : Sigil
{
    public override string Name => "Линия";
    public override string Description => "Атаки со стритом получают +32.";
    public override int Cost => 9;
    public override SigilType Type => SigilType.Damage;

    public override int BonusDamage(BattleContext ctx, ComboResult result)
        => (result.Type == ComboType.Straight || result.Type == ComboType.StraightFlush) ? 32 : 0;
}

public class SigilFlow : Sigil
{
    public override string Name => "Флоу";
    public override string Description => "Атаки с флешем получают +45 урона.";
    public override int Cost => 10;
    public override SigilType Type => SigilType.Damage;

    public override int BonusDamage(BattleContext ctx, ComboResult result)
        => (result.Type == ComboType.Flush || result.Type == ComboType.StraightFlush || result.Type == ComboType.RoyalFlush) ? 45 : 0;
}

public class SigilMalice : Sigil
{
    private bool _used;

    public override string Name => "Злой умысел";
    public override string Description => "Первая атака в бою получает +20% к урону.";
    public override int Cost => 11;
    public override SigilType Type => SigilType.Damage;

    public override void OnBattleStart(BattleContext ctx) => _used = false;

    public override float BonusMultiplier(BattleContext ctx, ComboResult result)
    {
        if (_used) return 0f;
        _used = true;
        return 0.20f;
    }
}

public class SigilRage : Sigil
{
    public override string Name => "Ярость";
    public override string Description => "Когда Ваше здоровье опускается до 40%, наносите +20% урона.";
    public override int Cost => 12;
    public override SigilType Type => SigilType.Damage;

    public override float BonusMultiplier(BattleContext ctx, ComboResult result)
    {
        return ctx.PlayerHp <= Mathf.CeilToInt(ctx.PlayerMaxHp * 0.4f) ? 0.20f : 0f;
    }
}

public class SigilConformity : Sigil
{
    public override string Name => "Подчинение";
    public override string Description => "Атаки с картами только одного ранга получают +40% к урону.";
    public override int Cost => 13;
    public override SigilType Type => SigilType.Damage;

    public override float BonusMultiplier(BattleContext ctx, ComboResult result)
    {
        if (result.ScoringCards == null || result.ScoringCards.Count == 0) return 0f;
        var rank = result.ScoringCards[0].Rank;
        foreach (var c in result.ScoringCards)
            if (c.Rank != rank) return 0f;
        return 0.40f;
    }
}

public class SigilFortification : Sigil
{
    private bool _triggered;

    public override string Name => "Форт";
    public override string Description => "Первая атака врага наносит на 35% меньше урона.";
    public override int Cost => 9;
    public override SigilType Type => SigilType.Defense;

    public override void OnBattleStart(BattleContext ctx) => _triggered = false;

    public override void OnEnemyAttack(BattleContext ctx)
    {
        if (_triggered) return;
        _triggered = true;
        // Undo the damage just applied and re-apply at 65%
        ctx.PlayerHp += ctx.EnemyDamage;
        ctx.PlayerHp -= Mathf.CeilToInt(ctx.EnemyDamage * 0.65f);
        ctx.RequestUIRefresh?.Invoke();
    }
}

public class SigilMediation : Sigil
{
    private int _hpAtStart;

    public override string Name => "Посредник";
    public override string Description => "Восстанавливает 25% потерянного в битве здоровья после победы.";
    public override int Cost => 10;
    public override SigilType Type => SigilType.Defense;

    public override void OnBattleStart(BattleContext ctx) => _hpAtStart = ctx.PlayerHp;

    public override void OnBattleEnd(BattleContext ctx)
    {
        int lost = _hpAtStart - ctx.PlayerHp;
        if (lost <= 0) return;
        ctx.PlayerHp += Mathf.CeilToInt(lost * 0.25f);
    }
}

public class SigilRefusal : Sigil
{
    public override string Name => "Отречение";
    public override string Description => "+1 сброс каждую битву.";
    public override int Cost => 8;
    public override SigilType Type => SigilType.Utility;

    public override void OnBattleStart(BattleContext ctx) => ctx.Discards += 1;
}

public class SigilPower : Sigil
{
    public override string Name => "Мощь";
    public override string Description => "Критические карты наносят на +50% больше урона.";
    public override int Cost => 11;
    public override SigilType Type => SigilType.Utility;

    public override float BonusMultiplier(BattleContext ctx, ComboResult result)
        => result.CritCount > 0 ? 0.50f : 0f;
}

public class SigilConversion : Sigil
{
    public override string Name => "Конвертация";
    public override string Description => "+2 золота за каждый неиспользованный сброс после окончания битвы.";
    public override int Cost => 9;
    public override SigilType Type => SigilType.Economy;

    public override void OnBattleEnd(BattleContext ctx)
    {
        int gold = ctx.Discards * 2;
        if (gold > 0 && GameManager.Instance != null)
            GameManager.Instance.Run.Gold += gold;
    }
}
