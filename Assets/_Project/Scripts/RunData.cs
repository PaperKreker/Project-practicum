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
    public Random Rng;
}
