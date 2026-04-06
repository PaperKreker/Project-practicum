using System;
using System.Collections.Generic;

// Battle -> Battle -> Shop -> Battle(Elite) -> Boss
// Enemies (except bosses) are picked from the pool using the run's seeded Rng.
public static class MapGenerator
{
    public static MapData BuildAct(int actIndex, Random rng)
    {
        var bosses = EnemyDatabase.AllBosses;
        var regulars = EnemyDatabase.AllRegular;
        var elites = EnemyDatabase.AllElite;

        EnemyData boss = bosses[actIndex % bosses.Count];
        EnemyData elite = elites[rng.Next(elites.Count)];
        EnemyData reg0 = regulars[rng.Next(regulars.Count)];

        // Pick a different regular for node 1 if possible
        EnemyData reg1;
        do { reg1 = regulars[rng.Next(regulars.Count)]; }
        while (reg1.EnemyName == reg0.EnemyName && regulars.Count > 1);

        var nodes = new List<MapNode>
        {
            new MapNode { Index = 0, Type = NodeType.Battle, Enemy = reg0,  NextNodeIndices = new List<int> { 1 } },
            new MapNode { Index = 1, Type = NodeType.Battle, Enemy = reg1,  NextNodeIndices = new List<int> { 2 } },
            new MapNode { Index = 2, Type = NodeType.Shop,                  NextNodeIndices = new List<int> { 3 } },
            new MapNode { Index = 3, Type = NodeType.Battle, Enemy = elite, NextNodeIndices = new List<int> { 4 } },
            new MapNode { Index = 4, Type = NodeType.Battle, Enemy = boss,  NextNodeIndices = new List<int>() },
        };

        return new MapData { Nodes = nodes, StartNodeIndex = 0 };
    }
}
