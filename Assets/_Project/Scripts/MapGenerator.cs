using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public static class MapGenerator
{
    private const int Floors = 8;

    private const float MinX = 0.14f;
    private const float MaxX = 0.86f;

    private const float MinY = 0.01f;
    private const float MaxY = 0.8f;

    private const float LaneCenterWobble = 0.06f;
    private const float LaneSpreadMin = 0.35f;
    private const float LaneSpreadMax = 0.45f;

    private const float MinNodeSpacingX = 0.16f;

    private const float MaxConnectionDx = 0.11f;
    private const float ExtraConnectionChance = 0.3f;

    private enum SpecialNodeType
    {
        Elite,
        Shop,
        Rest,
    }

    public static MapData BuildAct(
        int actIndex,
        Random rng,
        DifficultyLevel difficulty)
    {
        MapData map = new MapData();

        EnemyData boss =
            EnemyDatabase.AllBosses[
                actIndex % EnemyDatabase.AllBosses.Count];

        List<List<MapNode>> floors =
            BuildFloors(map, boss, rng, difficulty);

        // <<< ВОТ СЮДА
        CenterSingleNodeFloors(floors);

        ConnectFloors(floors, rng);

        FillBattles(map, rng);

        AssignSpecialNodes(map, floors, rng, difficulty);

        map.StartNodeIndex = floors[0][0].Index;

        return map;
    }

    private static void CenterSingleNodeFloors(
    List<List<MapNode>> floors)
    {
        // START
        if (floors.Count >= 2 &&
            floors[0].Count == 1)
        {
            List<MapNode> next = floors[1];

            float avg = 0f;

            foreach (MapNode node in next)
            {
                avg += node.NormalizedX;
            }

            avg /= next.Count;

            floors[0][0].NormalizedX = avg;
        }

        // BOSS
        int last = floors.Count - 1;

        if (last > 0 &&
            floors[last].Count == 1)
        {
            List<MapNode> prev = floors[last - 1];

            float avg = 0f;

            foreach (MapNode node in prev)
            {
                avg += node.NormalizedX;
            }

            avg /= prev.Count;

            floors[last][0].NormalizedX = avg;
        }
    }

    // =========================================================
    // FLOORS
    // =========================================================

    private static List<List<MapNode>> BuildFloors(
        MapData map,
        EnemyData boss,
        Random rng,
        DifficultyLevel difficulty)
    {
        List<List<MapNode>> floors =
            new List<List<MapNode>>();

        for (int row = 0; row < Floors; row++)
        {
            List<MapNode> floor =
                new List<MapNode>();

            int nodeCount =
                GetFloorNodeCount(row, difficulty);

            float y =
                Mathf.Lerp(
                    MinY,
                    MaxY,
                    EaseFloorY(row));

            List<float> xs =
                BuildFloorXs(nodeCount, row, rng);

            for (int i = 0; i < nodeCount; i++)
            {
                MapNode node = new MapNode
                {
                    Index = map.Nodes.Count,
                    Row = row,
                    NormalizedX = xs[i],
                    NormalizedY = y,
                    Type = NodeType.Battle,
                    Enemy = null,
                };

                // Start
                if (row == 0)
                {
                    node.Type = NodeType.Start;
                }

                // Boss
                if (row == Floors - 1)
                {
                    node.Type = NodeType.Battle;
                    node.Enemy = boss;
                }

                map.Nodes.Add(node);
                floor.Add(node);
            }

            floors.Add(floor);
        }

        return floors;
    }

    private static int GetFloorNodeCount(
        int row,
        DifficultyLevel difficulty)
    {
        // Start
        if (row == 0)
            return 1;

        // Boss
        if (row == Floors - 1)
            return 1;

        // Early floors simpler
        if (row <= 2)
            return 2;

        // Pre-boss floor
        if (row == Floors - 2)
            return 2;

        // Main distribution
        int min = 2;
        int max =
            difficulty == DifficultyLevel.Demon
                ? 4
                : 3;

        float t =
            Mathf.InverseLerp(0, Floors - 1, row);

        float density =
            Mathf.Sin(t * Mathf.PI);

        int count =
            Mathf.RoundToInt(
                Mathf.Lerp(min, max, density));

        return Mathf.Clamp(count, min, max);
    }

    private static float EaseFloorY(int row)
    {
        float t = (float)row / (Floors - 1);

        return Mathf.SmoothStep(0f, 1f, t);
    }

    private static List<float> BuildFloorXs(
        int count,
        int row,
        Random rng)
    {
        List<float> xs = new List<float>();

        if (count == 1)
        {
            xs.Add(0.5f);
            return xs;
        }

        float t =
            (float)row / (Floors - 1);

        float centerOffset =
            Mathf.Sin(t * Mathf.PI * 1.15f + 0.65f)
            * LaneCenterWobble;

        float center =
            Mathf.Clamp(
                0.5f + centerOffset,
                0.42f,
                0.58f);

        float spread =
            Mathf.Lerp(
                LaneSpreadMin,
                LaneSpreadMax,
                Mathf.Sin(t * Mathf.PI));

        float left =
            Mathf.Clamp(
                center - spread * 0.5f,
                MinX,
                MaxX);

        float right =
            Mathf.Clamp(
                center + spread * 0.5f,
                MinX,
                MaxX);

        for (int i = 0; i < count; i++)
        {
            float k =
                count == 1
                    ? 0.5f
                    : (float)i / (count - 1);

            float x =
                Mathf.Lerp(left, right, k);

            x += RandomRange(rng, -0.02f, 0.02f);

            xs.Add(Mathf.Clamp(x, MinX, MaxX));
        }

        EnforceMinSpacing(xs, MinNodeSpacingX);

        xs.Sort();

        return xs;
    }

    private static void EnforceMinSpacing(
        List<float> xs,
        float minSpacing)
    {
        xs.Sort();

        for (int i = 1; i < xs.Count; i++)
        {
            float diff = xs[i] - xs[i - 1];

            if (diff < minSpacing)
            {
                xs[i] =
                    xs[i - 1] + minSpacing;
            }
        }

        float overflow = xs[^1] - MaxX;

        if (overflow > 0f)
        {
            for (int i = 0; i < xs.Count; i++)
            {
                xs[i] -= overflow;
            }
        }

        for (int i = 0; i < xs.Count; i++)
        {
            xs[i] =
                Mathf.Clamp(xs[i], MinX, MaxX);
        }
    }

    // =========================================================
    // CONNECTIONS
    // =========================================================

    private static void ConnectFloors(
        List<List<MapNode>> floors,
        Random rng)
    {
        for (int row = 0; row < floors.Count - 1; row++)
        {
            List<MapNode> current =
                new List<MapNode>(floors[row]);

            List<MapNode> next =
                new List<MapNode>(floors[row + 1]);

            current.Sort((a, b) =>
                a.NormalizedX.CompareTo(b.NormalizedX));

            next.Sort((a, b) =>
                a.NormalizedX.CompareTo(b.NormalizedX));

            int[] parentCounts =
                new int[next.Count];

            int cursor = 0;

            // Main connections
            for (int i = 0; i < current.Count; i++)
            {
                float t =
                    current.Count == 1
                        ? 0.5f
                        : (float)i / (current.Count - 1);

                int ideal =
                    Mathf.RoundToInt(
                        t * (next.Count - 1));

                int min =
                    Mathf.Max(cursor, ideal - 1);

                int max =
                    Mathf.Min(next.Count - 1, ideal + 1);

                if (min > max)
                {
                    min = max =
                        Mathf.Clamp(
                            ideal,
                            0,
                            next.Count - 1);
                }

                int targetIndex =
                    PickBestTargetIndex(
                        current[i],
                        next,
                        min,
                        max);

                AddConnection(
                    current[i],
                    next[targetIndex]);

                parentCounts[targetIndex]++;

                cursor = targetIndex;

                // Extra local branch
                if (rng.NextDouble() < ExtraConnectionChance)
                {
                    int extraIndex =
                        targetIndex + 1;

                    if (extraIndex < next.Count)
                    {
                        float dx =
                            Mathf.Abs(
                                next[extraIndex].NormalizedX
                                - current[i].NormalizedX);

                        if (dx <= MaxConnectionDx)
                        {
                            AddConnection(
                                current[i],
                                next[extraIndex]);

                            parentCounts[extraIndex]++;
                        }
                    }
                }
            }

            // Ensure every node has parent
            for (int j = 0; j < next.Count; j++)
            {
                if (parentCounts[j] > 0)
                    continue;

                int parentIndex =
                    PickClosestNodeIndex(
                        current,
                        next[j].NormalizedX);

                AddConnection(
                    current[parentIndex],
                    next[j]);

                parentCounts[j]++;
            }
        }
    }

    private static int PickBestTargetIndex(
        MapNode from,
        List<MapNode> next,
        int minIndex,
        int maxIndex)
    {
        int bestIndex = minIndex;
        float bestScore = float.MinValue;

        for (int i = minIndex; i <= maxIndex; i++)
        {
            float dx =
                Mathf.Abs(
                    next[i].NormalizedX
                    - from.NormalizedX);

            float score = -dx;

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private static int PickClosestNodeIndex(
        List<MapNode> nodes,
        float x)
    {
        int bestIndex = 0;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < nodes.Count; i++)
        {
            float d =
                Mathf.Abs(
                    nodes[i].NormalizedX - x);

            if (d < bestDistance)
            {
                bestDistance = d;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    // =========================================================
    // SPECIALS
    // =========================================================

    private static void AssignSpecialNodes(
        MapData map,
        List<List<MapNode>> floors,
        Random rng,
        DifficultyLevel difficulty)
    {
        int preBossFloor = Floors - 2;

        // Floor 1 always normal battles
        foreach (MapNode node in floors[1])
        {
            node.Type = NodeType.Battle;

            node.Enemy = PickEnemy(
                EnemyDatabase.AllRegular,
                new HashSet<string>(),
                rng);
        }

        // Pre-boss rest
        foreach (MapNode node in floors[preBossFloor])
        {
            node.Type = NodeType.Rest;
            node.Enemy = null;
        }

        int eliteCount =
            difficulty == DifficultyLevel.Demon
                ? 3
                : 2;

        int shopCount = 2;

        int restCount =
            difficulty == DifficultyLevel.Demon
                ? 2
                : 1;

        PlaceSpecials(
            map,
            floors,
            SpecialNodeType.Elite,
            eliteCount,
            rng);

        PlaceSpecials(
            map,
            floors,
            SpecialNodeType.Shop,
            shopCount,
            rng);

        PlaceSpecials(
            map,
            floors,
            SpecialNodeType.Rest,
            restCount,
            rng);
    }

    private static void PlaceSpecials(
        MapData map,
        List<List<MapNode>> floors,
        SpecialNodeType type,
        int count,
        Random rng)
    {
        List<MapNode> candidates =
            new List<MapNode>();

        for (int row = 2; row < Floors - 2; row++)
        {
            if (type == SpecialNodeType.Elite &&
                row < 4)
            {
                continue;
            }

            foreach (MapNode node in floors[row])
            {
                if (node.Type != NodeType.Battle)
                    continue;

                if (node.Enemy != null &&
                    node.Enemy.Tier == EnemyTier.Boss)
                {
                    continue;
                }

                candidates.Add(node);
            }
        }

        Shuffle(candidates, rng);

        int placed = 0;

        foreach (MapNode node in candidates)
        {
            if (placed >= count)
                break;

            if (!CanPlaceSpecial(map, node, type))
                continue;

            ApplySpecialType(node, type, rng);

            placed++;
        }
    }

    private static bool CanPlaceSpecial(
        MapData map,
        MapNode node,
        SpecialNodeType type)
    {
        foreach (MapNode other in map.Nodes)
        {
            if (other.Index == node.Index)
                continue;

            if (other.Row != node.Row)
                continue;

            if (MatchesSpecial(other, type))
                return false;
        }

        foreach (MapNode parent in GetParents(map, node))
        {
            if (IsForbiddenChain(parent, type))
                return false;
        }

        foreach (int nextIndex in node.NextNodeIndices)
        {
            MapNode child =
                map.GetNode(nextIndex);

            if (IsForbiddenChain(child, type))
                return false;
        }

        return true;
    }

    private static bool IsForbiddenChain(
        MapNode node,
        SpecialNodeType targetType)
    {
        switch (targetType)
        {
            case SpecialNodeType.Shop:
                return node.Type == NodeType.Shop;

            case SpecialNodeType.Rest:
                return node.Type == NodeType.Rest;

            case SpecialNodeType.Elite:
                return node.Enemy != null &&
                       node.Enemy.Tier == EnemyTier.Elite;
        }

        return false;
    }

    private static bool MatchesSpecial(
        MapNode node,
        SpecialNodeType type)
    {
        switch (type)
        {
            case SpecialNodeType.Shop:
                return node.Type == NodeType.Shop;

            case SpecialNodeType.Rest:
                return node.Type == NodeType.Rest;

            case SpecialNodeType.Elite:
                return node.Enemy != null &&
                       node.Enemy.Tier == EnemyTier.Elite;
        }

        return false;
    }

    private static List<MapNode> GetParents(
        MapData map,
        MapNode target)
    {
        List<MapNode> parents =
            new List<MapNode>();

        foreach (MapNode node in map.Nodes)
        {
            if (node.NextNodeIndices.Contains(target.Index))
            {
                parents.Add(node);
            }
        }

        return parents;
    }

    private static void ApplySpecialType(
        MapNode node,
        SpecialNodeType type,
        Random rng)
    {
        switch (type)
        {
            case SpecialNodeType.Shop:
                {
                    node.Type = NodeType.Shop;
                    node.Enemy = null;
                    break;
                }

            case SpecialNodeType.Rest:
                {
                    node.Type = NodeType.Rest;
                    node.Enemy = null;
                    break;
                }

            case SpecialNodeType.Elite:
                {
                    node.Type = NodeType.Battle;

                    node.Enemy = PickEnemy(
                        EnemyDatabase.AllElite,
                        new HashSet<string>(),
                        rng);

                    break;
                }
        }
    }

    // =========================================================
    // ENEMIES
    // =========================================================

    private static void FillBattles(
        MapData map,
        Random rng)
    {
        HashSet<string> usedNames =
            new HashSet<string>();

        foreach (MapNode node in map.Nodes)
        {
            if (node.Type != NodeType.Battle)
                continue;

            if (node.Enemy != null)
                continue;

            node.Enemy = PickEnemy(
                EnemyDatabase.AllRegular,
                usedNames,
                rng);
        }
    }

    private static EnemyData PickEnemy(
        List<EnemyData> pool,
        HashSet<string> usedNames,
        Random rng)
    {
        List<EnemyData> available =
            pool.FindAll(e =>
                !usedNames.Contains(e.EnemyName));

        if (available.Count == 0)
            available = pool;

        EnemyData picked =
            available[rng.Next(available.Count)];

        usedNames.Add(picked.EnemyName);

        return picked;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static void AddConnection(
        MapNode from,
        MapNode to)
    {
        if (!from.NextNodeIndices.Contains(to.Index))
        {
            from.NextNodeIndices.Add(to.Index);
        }
    }

    private static float RandomRange(
        Random rng,
        float min,
        float max)
    {
        return min +
               (float)rng.NextDouble()
               * (max - min);
    }

    private static void Shuffle<T>(
        IList<T> items,
        Random rng)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int swap =
                rng.Next(i + 1);

            (items[i], items[swap]) =
                (items[swap], items[i]);
        }
    }
}