using System.Collections.Generic;

// Builds all three acts.
// Battle -> Battle -> Shop -> Battle -> Boss
// TODO: Add branching paths and randomize enemies
public static class MapGenerator
{
    public static MapData BuildAct(int actIndex)
    {
        var bosses = EnemyDatabase.AllBosses;
        var regulars = EnemyDatabase.AllRegular;
        var elites = EnemyDatabase.AllElite;

        EnemyData boss = bosses[actIndex % bosses.Count];
        EnemyData elite = elites[actIndex % elites.Count];

        var nodes = new List<MapNode>
        {
            new MapNode { Index = 0, Type = NodeType.Battle, Enemy = regulars[0], NextNodeIndices = new List<int> { 1 } },
            new MapNode { Index = 1, Type = NodeType.Battle, Enemy = regulars[1 % regulars.Count], NextNodeIndices = new List<int> { 2 } },
            new MapNode { Index = 2, Type = NodeType.Shop,   NextNodeIndices = new List<int> { 3 } },
            new MapNode { Index = 3, Type = NodeType.Battle, Enemy = elite, NextNodeIndices = new List<int> { 4 } },
            new MapNode { Index = 4, Type = NodeType.Battle, Enemy = boss,  NextNodeIndices = new List<int>() },
        };

        return new MapData { Nodes = nodes, StartNodeIndex = 0 };
    }
}
