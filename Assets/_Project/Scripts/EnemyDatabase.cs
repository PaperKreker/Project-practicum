using System.Collections.Generic;
using UnityEngine;

public enum EnemyTier { Regular, Elite, Boss }

public enum EnemyEffectType
{
    None,
    ReducedDiscards,        // Wolf
    HeavyReducedDiscards,   // Starving Wolf
    FaceDownCards,          // Raven
    SuitNoDamage,           // Fox
    DamageOnDiscard,        // Alpha Wolf
    PetrifyCard,            // Basilisk
    LargeHandPenalty,       // Scarab
    EscalateDamage,         // Minotaur (Boss)
    NoRepeatCombo,          // Spider (Boss)
    CyclingPenalty,         // Amalgam (Boss)
}

public class EnemyData
{
    public string EnemyName;
    public EnemyTier Tier;
    public int MaxHp;
    public int AttackDamage;
    public int AttackCoinsPerRound = 2;
    public int GoldReward;
    public float CritChanceReward;
    public EnemyEffectType EffectType;
    public Sprite Sprite;
    public DifficultyLevel DifficultyLevel = DifficultyLevel.Normal;

    public EnemyEffect CreateEffect()
    {
        return EffectType switch
        {
            EnemyEffectType.None => new NoEffect(),
            EnemyEffectType.ReducedDiscards => DifficultyLevel switch
            {
                DifficultyLevel.Hard => new ReducedDiscards(2),
                DifficultyLevel.Demon => new ReducedDiscards(2),
                _ => new ReducedDiscards(1),
            },
            EnemyEffectType.HeavyReducedDiscards => new ReducedDiscards(2),
            EnemyEffectType.FaceDownCards => DifficultyLevel switch
            {
                DifficultyLevel.Hard => new FaceDownCards(3),
                DifficultyLevel.Demon => new FaceDownCards(2),
                _ => new FaceDownCards(4),
            },
            EnemyEffectType.SuitNoDamage => DifficultyLevel switch
            {
                DifficultyLevel.Hard => new SuitNoDamage(2),
                DifficultyLevel.Demon => new SuitNoDamage(3),
                _ => new SuitNoDamage(1),
            },
            EnemyEffectType.DamageOnDiscard => DifficultyLevel switch
            {
                DifficultyLevel.Hard => new DamageOnDiscard(4),
                DifficultyLevel.Demon => new DamageOnDiscard(5),
                _ => new DamageOnDiscard(3),
            },
            EnemyEffectType.PetrifyCard => DifficultyLevel switch
            {
                DifficultyLevel.Hard => new PetrifyCard(2),
                DifficultyLevel.Demon => new PetrifyCard(3),
                _ => new PetrifyCard(1),
            },
            EnemyEffectType.LargeHandPenalty => DifficultyLevel switch
            {
                DifficultyLevel.Hard => new LargeHandPenalty(4, 0.5f),
                DifficultyLevel.Demon => new LargeHandPenalty(3, 0.4f),
                _ => new LargeHandPenalty(4, 0.6f),
            },
            EnemyEffectType.EscalateDamage => DifficultyLevel switch
            {
                DifficultyLevel.Hard => new EscalateDamage(5),
                DifficultyLevel.Demon => new EscalateDamage(6),
                _ => new EscalateDamage(4),
            },
            EnemyEffectType.NoRepeatCombo => DifficultyLevel switch
            {
                DifficultyLevel.Hard => new NoRepeatCombo(2),
                DifficultyLevel.Demon => new NoRepeatCombo(3),
                _ => new NoRepeatCombo(1),
            },
            EnemyEffectType.CyclingPenalty => new CyclingPenalty(DifficultyLevel),
            _ => new NoEffect(),
        };
    }
}

public static class EnemyDatabase
{
    public static EnemyData Wolf => new EnemyData
    {
        EnemyName = "Волк",
        Tier = EnemyTier.Regular,
        MaxHp = 140,
        AttackDamage = 6,
        AttackCoinsPerRound = 2,
        GoldReward = 5,
        EffectType = EnemyEffectType.ReducedDiscards,
    };

    public static EnemyData Raven => new EnemyData
    {
        EnemyName = "Ворон",
        Tier = EnemyTier.Regular,
        MaxHp = 130,
        AttackDamage = 5,
        AttackCoinsPerRound = 2,
        GoldReward = 5,
        EffectType = EnemyEffectType.FaceDownCards,
    };

    public static EnemyData Fox => new EnemyData
    {
        EnemyName = "Лис",
        Tier = EnemyTier.Regular,
        MaxHp = 133,
        AttackDamage = 6,
        AttackCoinsPerRound = 2,
        GoldReward = 5,
        EffectType = EnemyEffectType.SuitNoDamage,
    };

    public static EnemyData AlphaWolf => new EnemyData
    {
        EnemyName = "Альфа волк",
        Tier = EnemyTier.Elite,
        MaxHp = 207,
        AttackDamage = 16,
        AttackCoinsPerRound = 2,
        GoldReward = 9,
        CritChanceReward = 0.02f,
        EffectType = EnemyEffectType.DamageOnDiscard,
    };

    public static EnemyData Basilisk => new EnemyData
    {
        EnemyName = "Василиск",
        Tier = EnemyTier.Elite,
        MaxHp = 223,
        AttackDamage = 15,
        AttackCoinsPerRound = 2,
        GoldReward = 9,
        CritChanceReward = 0.02f,
        EffectType = EnemyEffectType.PetrifyCard,
    };

    public static EnemyData Scarab => new EnemyData
    {
        EnemyName = "Скарабей",
        Tier = EnemyTier.Elite,
        MaxHp = 193,
        AttackDamage = 15,
        AttackCoinsPerRound = 2,
        GoldReward = 9,
        CritChanceReward = 0.02f,
        EffectType = EnemyEffectType.LargeHandPenalty,
    };

    public static EnemyData Minotaur => new EnemyData
    {
        EnemyName = "Минотавр",
        Tier = EnemyTier.Boss,
        MaxHp = 347,
        AttackDamage = 18,
        AttackCoinsPerRound = 2,
        GoldReward = 14,
        EffectType = EnemyEffectType.EscalateDamage,
    };

    public static EnemyData Spider => new EnemyData
    {
        EnemyName = "Паук",
        Tier = EnemyTier.Boss,
        MaxHp = 367,
        AttackDamage = 19,
        AttackCoinsPerRound = 2,
        GoldReward = 14,
        EffectType = EnemyEffectType.NoRepeatCombo,
    };

    public static EnemyData Amalgam => new EnemyData
    {
        EnemyName = "Амальгам",
        Tier = EnemyTier.Boss,
        MaxHp = 433,
        AttackDamage = 20,
        AttackCoinsPerRound = 2,
        GoldReward = 16,
        EffectType = EnemyEffectType.CyclingPenalty,
    };

    public static List<EnemyData> AllRegular => new List<EnemyData> { Wolf, Raven, Fox };
    public static List<EnemyData> AllElite => new List<EnemyData> { AlphaWolf, Basilisk, Scarab };
    public static List<EnemyData> AllBosses => new List<EnemyData> { Minotaur, Spider, Amalgam };
}
