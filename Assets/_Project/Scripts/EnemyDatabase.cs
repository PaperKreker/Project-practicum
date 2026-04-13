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
    public int AttackCoinsPerRound = 3;
    public int GoldReward;
    public float CritChanceReward;
    public EnemyEffectType EffectType;
    public Sprite Sprite;

    public EnemyEffect CreateEffect() => EffectType switch
    {
        EnemyEffectType.None => new NoEffect(),
        EnemyEffectType.ReducedDiscards => new ReducedDiscards(1),
        EnemyEffectType.HeavyReducedDiscards => new ReducedDiscards(2),
        EnemyEffectType.FaceDownCards => new FaceDownCards(4),
        EnemyEffectType.SuitNoDamage => new SuitNoDamage(),
        EnemyEffectType.DamageOnDiscard => new DamageOnDiscard(2),
        EnemyEffectType.PetrifyCard => new PetrifyCard(),
        EnemyEffectType.LargeHandPenalty => new LargeHandPenalty(4, 0.5f),
        EnemyEffectType.EscalateDamage => new EscalateDamage(8),
        EnemyEffectType.NoRepeatCombo => new NoRepeatCombo(),
        EnemyEffectType.CyclingPenalty => new CyclingPenalty(),
        _ => new NoEffect(),
    };
}

public static class EnemyDatabase
{

    public static EnemyData Wolf => new EnemyData
    {
        EnemyName = "Волк",
        Tier = EnemyTier.Regular,
        MaxHp = 80,
        AttackDamage = 12,
        AttackCoinsPerRound = 3,
        GoldReward = 6,
        EffectType = EnemyEffectType.ReducedDiscards,
    };

    public static EnemyData Raven => new EnemyData
    {
        EnemyName = "Ворон",
        Tier = EnemyTier.Regular,
        MaxHp = 70,
        AttackDamage = 10,
        AttackCoinsPerRound = 3,
        GoldReward = 6,
        EffectType = EnemyEffectType.FaceDownCards,
    };

    public static EnemyData Fox => new EnemyData
    {
        EnemyName = "Лис",
        Tier = EnemyTier.Regular,
        MaxHp = 75,
        AttackDamage = 11,
        AttackCoinsPerRound = 3,
        GoldReward = 6,
        EffectType = EnemyEffectType.SuitNoDamage,
    };

    public static EnemyData AlphaWolf => new EnemyData
    {
        EnemyName = "Альфа волк",
        Tier = EnemyTier.Elite,
        MaxHp = 120,
        AttackDamage = 18,
        AttackCoinsPerRound = 3,
        GoldReward = 10,
        CritChanceReward = 0.02f,
        EffectType = EnemyEffectType.DamageOnDiscard,
    };

    public static EnemyData Basilisk => new EnemyData
    {
        EnemyName = "Василиск",
        Tier = EnemyTier.Elite,
        MaxHp = 130,
        AttackDamage = 16,
        AttackCoinsPerRound = 3,
        GoldReward = 10,
        CritChanceReward = 0.02f,
        EffectType = EnemyEffectType.PetrifyCard,
    };

    public static EnemyData Scarab => new EnemyData
    {
        EnemyName = "Скарабей",
        Tier = EnemyTier.Elite,
        MaxHp = 110,
        AttackDamage = 15,
        AttackCoinsPerRound = 3,
        GoldReward = 10,
        CritChanceReward = 0.02f,
        EffectType = EnemyEffectType.LargeHandPenalty,
    };

    public static EnemyData Minotaur => new EnemyData
    {
        EnemyName = "Минотавр",
        Tier = EnemyTier.Boss,
        MaxHp = 200,
        AttackDamage = 20,
        AttackCoinsPerRound = 3,
        GoldReward = 15,
        EffectType = EnemyEffectType.EscalateDamage,
    };

    public static EnemyData Spider => new EnemyData
    {
        EnemyName = "Паук",
        Tier = EnemyTier.Boss,
        MaxHp = 220,
        AttackDamage = 22,
        AttackCoinsPerRound = 3,
        GoldReward = 15,
        EffectType = EnemyEffectType.NoRepeatCombo,
    };

    public static EnemyData Amalgam => new EnemyData
    {
        EnemyName = "Амальгам",
        Tier = EnemyTier.Boss,
        MaxHp = 300,
        AttackDamage = 25,
        AttackCoinsPerRound = 3,
        GoldReward = 20,
        EffectType = EnemyEffectType.CyclingPenalty,
    };

    public static List<EnemyData> AllRegular => new List<EnemyData> { Wolf, Raven, Fox };
    public static List<EnemyData> AllElite => new List<EnemyData> { AlphaWolf, Basilisk, Scarab };
    public static List<EnemyData> AllBosses => new List<EnemyData> { Minotaur, Spider, Amalgam };
}
