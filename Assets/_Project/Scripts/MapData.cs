using System.Collections.Generic;

public enum NodeType
{
    Battle,
    Shop,
    Rest,
}

// A single stop on the map.
public class MapNode
{
    public int Index;
    public NodeType Type;

    // Only filled for NodeType.Battle
    public EnemyData Enemy;

    // Indices of nodes the player can travel to next
    public List<int> NextNodeIndices = new List<int>();
}

// Describes one map (= one of the three acts).
// Generated procedurally or built by hand via MapGenerator.cs
public class MapData
{
    public List<MapNode> Nodes = new List<MapNode>();
    public int StartNodeIndex = 0;

    public MapNode GetNode(int index) => Nodes[index];
}
