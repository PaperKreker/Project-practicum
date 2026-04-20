using System.Collections.Generic;

public enum NodeType
{
    Start,
    Battle,
    Shop,
    Rest,
}

public class MapNode
{
    public int Index;
    public int Row;
    public float NormalizedX;
    public float NormalizedY;
    public NodeType Type;
    public EnemyData Enemy;
    public List<int> NextNodeIndices = new List<int>();
}

public class MapData
{
    public List<MapNode> Nodes = new List<MapNode>();
    public int StartNodeIndex = 0;

    public int MaxRow
    {
        get
        {
            int max = 0;
            foreach (MapNode node in Nodes)
            {
                if (node.Row > max)
                    max = node.Row;
            }

            return max;
        }
    }

    public MapNode GetNode(int index) => Nodes[index];
}
