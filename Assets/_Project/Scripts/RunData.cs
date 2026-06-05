using System;
using System.Collections.Generic;

public enum RunState
{
    MainMenu,
    DifficultySelect,
    Map,
    Battle,
    Shop,
    Rest,
    Victory,
    GameOver,
}

public class RunData
{
    public DifficultyLevel Difficulty = DifficultyLevel.Normal;
    public int PlayerHp;
    public int PlayerMaxHp;
    public int Gold;
    public int CurrentNodeIndex;
    public bool CurrentNodeCompleted = false;
    public List<Sigil> ActiveSigils = new List<Sigil>();
    public List<int> VisitedNodeIndices = new List<int>();

    public const int MaxSigils = 6;

    public int Seed;
    public Random MapRng(int actIndex) => MakeRng(Seed, 0x1000 + actIndex);

    public Random ShopRng;

    public Random Rng;
    public int ShopRandomCalls;

    public void InitRngs()
    {
        Rng = MakeRng(Seed, 0x0001);
        ShopRng = MakeRng(Seed, 0x0002);

        for (int i = 0; i < ShopRandomCalls; i++)
            ShopRng.Next();
    }

    public int NextShopIndex(int maxValue)
    {
        ShopRandomCalls++;
        return ShopRng.Next(maxValue);
    }

    private static Random MakeRng(int seed, int salt)
    {
        unchecked
        {
            int h = seed ^ (salt * (int)0x9e3779b9);
            h ^= h >> 16;
            h *= unchecked((int)0x85ebca6b);
            h ^= h >> 13;
            h *= unchecked((int)0xc2b2ae35);
            h ^= h >> 16;
            return new Random(h);
        }
    }
}

