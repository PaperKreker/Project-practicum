using UnityEngine;

public enum DifficultyLevel
{
    Normal,
    Hard,
    Demon,
}

public readonly struct DifficultyModifiers
{
    public readonly string DisplayName;
    public readonly string Description;
    public readonly float PlayerMaxHpMultiplier;
    public readonly float PlayerDamageMultiplier;
    public readonly float GoldRewardMultiplier;
    public readonly float ShopCostMultiplier;
    public readonly float RestHealMultiplier;
    public readonly int HandSizeAdjustment;
    public readonly int ShopOfferCount;
    public readonly float ShopNodeWeight;
    public readonly float RestNodeWeight;
    public readonly float EliteNodeWeight;

    public DifficultyModifiers(
        string displayName,
        string description,
        float playerMaxHpMultiplier,
        float playerDamageMultiplier,
        float goldRewardMultiplier,
        float shopCostMultiplier,
        float restHealMultiplier,
        int handSizeAdjustment,
        int shopOfferCount,
        float shopNodeWeight,
        float restNodeWeight,
        float eliteNodeWeight)
    {
        DisplayName = displayName;
        Description = description;
        PlayerMaxHpMultiplier = playerMaxHpMultiplier;
        PlayerDamageMultiplier = playerDamageMultiplier;
        GoldRewardMultiplier = goldRewardMultiplier;
        ShopCostMultiplier = shopCostMultiplier;
        RestHealMultiplier = restHealMultiplier;
        HandSizeAdjustment = handSizeAdjustment;
        ShopOfferCount = shopOfferCount;
        ShopNodeWeight = shopNodeWeight;
        RestNodeWeight = restNodeWeight;
        EliteNodeWeight = eliteNodeWeight;
    }
}

public static class GameBalance
{
    public const int BasePlayerMaxHp = 150;
    public const float BaseRestHealRatio = 0.40f;
    public const int BaseShopRerollCost = 2;
    private static readonly float[] ActEnemyHpMultipliers = { 0.9f, 1.0f, 1.15f };
    private static readonly float[] ActEnemyDamageMultipliers = { 0.9f, 1.0f, 1.15f };

    public static DifficultyModifiers GetDifficulty(DifficultyLevel level)
    {
        return level switch
        {
            DifficultyLevel.Hard => new DifficultyModifiers(
                "Сложная",
                "Игрок слабее, доход ниже, а способности врагов заметно сильнее.",
                130f / 150f,
                0.90f,
                0.85f,
                1.10f,
                0.85f,
                0,
                3,
                0.42f,
                0.36f,
                0.22f),
            DifficultyLevel.Demon => new DifficultyModifiers(
                "Демоническая",
                "Максимальное давление: меньше здоровья, слабее атаки, меньше карт в руке и более жесткая карта.",
                100f / 150f,
                0.80f,
                0.70f,
                1.25f,
                0.70f,
                -1,
                2,
                0.16f,
                0.16f,
                0.68f),
            _ => new DifficultyModifiers(
                "Обычная",
                "Рекомендуется для первого забега.",
                1.00f,
                1.00f,
                1.00f,
                1.00f,
                1.00f,
                0,
                3,
                0.42f,
                0.36f,
                0.22f),
        };
    }

    public static int GetPlayerMaxHp(DifficultyLevel level)
    {
        DifficultyModifiers modifiers = GetDifficulty(level);
        return Mathf.Max(1, Mathf.RoundToInt(BasePlayerMaxHp * modifiers.PlayerMaxHpMultiplier));
    }

    public static int GetRestHealAmount(RunData run)
    {
        DifficultyModifiers modifiers = GetDifficulty(run.Difficulty);
        int missing = run.PlayerMaxHp - run.PlayerHp;
        float ratio = BaseRestHealRatio * modifiers.RestHealMultiplier;
        return Mathf.CeilToInt(missing * ratio);
    }

    public static int GetSigilCost(Sigil sigil, DifficultyLevel level)
    {
        DifficultyModifiers modifiers = GetDifficulty(level);
        return Mathf.Max(1, Mathf.CeilToInt(sigil.Cost * modifiers.ShopCostMultiplier));
    }

    public static int GetShopRerollCost(int rerollsSpent, DifficultyLevel level)
    {
        DifficultyModifiers modifiers = GetDifficulty(level);
        int baseCost = BaseShopRerollCost + rerollsSpent;
        return Mathf.Max(1, Mathf.CeilToInt(baseCost * modifiers.ShopCostMultiplier));
    }

    public static int GetPlayerHandSize(int baseHandSize, DifficultyLevel level)
    {
        DifficultyModifiers modifiers = GetDifficulty(level);
        return Mathf.Max(1, baseHandSize + modifiers.HandSizeAdjustment);
    }

    public static int GetShopOfferCount(DifficultyLevel level)
    {
        return Mathf.Max(1, GetDifficulty(level).ShopOfferCount);
    }

    public static EnemyData ApplyDifficulty(EnemyData enemy, DifficultyLevel level, int actIndex)
    {
        DifficultyModifiers modifiers = GetDifficulty(level);
        int clampedActIndex = Mathf.Clamp(actIndex, 0, ActEnemyHpMultipliers.Length - 1);
        float hpMultiplier = ActEnemyHpMultipliers[clampedActIndex];
        float damageMultiplier = ActEnemyDamageMultipliers[clampedActIndex];

        return new EnemyData
        {
            EnemyName = enemy.EnemyName,
            Tier = enemy.Tier,
            MaxHp = Mathf.Max(1, Mathf.RoundToInt(enemy.MaxHp * hpMultiplier)),
            AttackDamage = Mathf.Max(1, Mathf.RoundToInt(enemy.AttackDamage * damageMultiplier)),
            AttackCoinsPerRound = enemy.AttackCoinsPerRound,
            GoldReward = Mathf.Max(1, Mathf.RoundToInt(enemy.GoldReward * modifiers.GoldRewardMultiplier)),
            CritChanceReward = enemy.CritChanceReward,
            EffectType = enemy.EffectType,
            Sprite = enemy.Sprite,
            DifficultyLevel = level,
        };
    }
}
