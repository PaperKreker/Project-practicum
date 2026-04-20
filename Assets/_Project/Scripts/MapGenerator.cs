using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public static class MapGenerator
{
    private const int ApproxPathLength = 5;
    private const int ApproxPathWidth = 4;
    private const int MinMainPathLength = 4;
    private const int MinBranchSpan = 2;
    private const int MaxBranchSpan = 5;
    private const int BranchAttemptMultiplier = 12;
    private const float MinX = 0.1f;
    private const float MaxX = 0.9f;
    private const float MinY = 0.06f;
    private const float MaxY = 0.94f;
    private const float SpineMinX = 0.22f;
    private const float SpineMaxX = 0.78f;
    private const float SpineDrift = 0.12f;
    private const float MinNodeSpacing = 0.105f;
    private const float MinPathClearance = 0.048f;
    private const float MinBranchGapY = 0.2f;

    private enum SpecialNodeType
    {
        Elite,
        Shop,
        Rest,
    }

    public static MapData BuildAct(int actIndex, Random rng)
    {
        int spineLength = Mathf.Max(MinMainPathLength, ApproxPathLength);
        int branchCount = Mathf.Max(0, ApproxPathWidth - 1);

        MapData map = new MapData();
        EnemyData boss = EnemyDatabase.AllBosses[actIndex % EnemyDatabase.AllBosses.Count];
        List<MapNode> spine = BuildMainSpine(map, spineLength, boss, rng);

        AddBranches(map, branchCount, rng);
        PopulateNodes(map, rng);

        map.StartNodeIndex = spine[0].Index;
        return map;
    }

    private static List<MapNode> BuildMainSpine(MapData map, int spineLength, EnemyData boss, Random rng)
    {
        List<MapNode> spine = new List<MapNode>(spineLength);
        List<float> yPositions = BuildSpineYPositions(spineLength, rng);
        int nextIndex = 0;
        float previousX = 0.5f;

        for (int i = 0; i < spineLength; i++)
        {
            float x = FindSpineX(map, previousX, yPositions[i], i == 0 || i == spineLength - 1, rng);
            previousX = x;

            MapNode node = new MapNode
            {
                Index = nextIndex++,
                Row = i,
                NormalizedX = Mathf.Clamp(x, MinX, MaxX),
                NormalizedY = yPositions[i],
                Type = i == 0 ? NodeType.Start : NodeType.Battle,
                Enemy = i == spineLength - 1 ? boss : null,
            };

            spine.Add(node);
            map.Nodes.Add(node);

            if (i > 0)
                AddConnection(spine[i - 1], node);
        }

        return spine;
    }

    private static float FindSpineX(MapData map, float previousX, float y, bool keepCentered, Random rng)
    {
        float bestX = 0.5f;
        float bestScore = float.MinValue;

        for (int attempt = 0; attempt < 18; attempt++)
        {
            float candidate = keepCentered
                ? 0.5f + RandomRange(rng, -0.04f, 0.04f)
                : Mathf.Clamp(previousX + RandomRange(rng, -SpineDrift, SpineDrift), SpineMinX, SpineMaxX);

            float score = GetPlacementScore(map.Nodes, candidate, y);
            if (!keepCentered)
                score -= Mathf.Abs(candidate - previousX) * 0.35f;

            if (score > bestScore)
            {
                bestScore = score;
                bestX = candidate;
            }
        }

        return Mathf.Clamp(bestX, MinX, MaxX);
    }

    private static List<float> BuildSpineYPositions(int count, Random rng)
    {
        List<float> positions = new List<float>(count) { MinY };
        if (count == 1)
            return positions;

        List<float> weights = new List<float>(count - 1);
        float totalWeight = 0f;

        for (int i = 0; i < count - 1; i++)
        {
            float weight = 0.8f + (float)rng.NextDouble() * 0.7f;
            weights.Add(weight);
            totalWeight += weight;
        }

        float accumulatedWeight = 0f;
        for (int i = 1; i < count; i++)
        {
            accumulatedWeight += weights[i - 1];
            float y = Mathf.Lerp(MinY, MaxY, accumulatedWeight / totalWeight);
            positions.Add(y);
        }

        positions[^1] = MaxY;
        return positions;
    }

    private static void AddBranches(MapData map, int branchCount, Random rng)
    {
        int attemptsLeft = branchCount * BranchAttemptMultiplier;
        int createdBranches = 0;

        while (createdBranches < branchCount && attemptsLeft-- > 0)
        {
            if (TryAddBranch(map, rng))
                createdBranches++;
        }
    }

    private static bool TryAddBranch(MapData map, Random rng)
    {
        List<(MapNode from, MapNode to, int baseEdges)> pairs = CollectBranchPairs(map);
        if (pairs.Count == 0)
            return false;

        Shuffle(pairs, rng);

        foreach ((MapNode from, MapNode to, int baseEdges) pair in pairs)
        {
            int branchEdges = ChooseBranchEdgeCount(pair.baseEdges, rng);
            List<MapNode> branchNodes = CreateBranchNodes(map, pair.from, pair.to, branchEdges, rng);
            if (!CanAddBranch(map, pair.from, pair.to, branchNodes))
                continue;

            CommitBranch(map, pair.from, pair.to, branchNodes);
            return true;
        }

        return false;
    }

    private static List<(MapNode from, MapNode to, int baseEdges)> CollectBranchPairs(MapData map)
    {
        List<(MapNode from, MapNode to, int baseEdges)> pairs = new List<(MapNode from, MapNode to, int baseEdges)>();

        foreach (MapNode from in map.Nodes)
        {
            if (from.NextNodeIndices.Count == 0)
                continue;

            Dictionary<int, int> distances = BuildShortestPathLengths(map, from.Index);
            foreach (KeyValuePair<int, int> entry in distances)
            {
                int targetIndex = entry.Key;
                if (targetIndex == from.Index || from.NextNodeIndices.Contains(targetIndex))
                    continue;
                if (entry.Value < MinBranchSpan || entry.Value > MaxBranchSpan)
                    continue;

                MapNode to = map.GetNode(targetIndex);
                if (to.NormalizedY - from.NormalizedY < MinBranchGapY)
                    continue;

                pairs.Add((from, to, entry.Value));
            }
        }

        return pairs;
    }

    private static Dictionary<int, int> BuildShortestPathLengths(MapData map, int startIndex)
    {
        Dictionary<int, int> distances = new Dictionary<int, int>();
        Queue<int> frontier = new Queue<int>();

        distances[startIndex] = 0;
        frontier.Enqueue(startIndex);

        while (frontier.Count > 0)
        {
            int currentIndex = frontier.Dequeue();
            MapNode currentNode = map.GetNode(currentIndex);
            int nextDistance = distances[currentIndex] + 1;

            foreach (int nextIndex in currentNode.NextNodeIndices)
            {
                if (distances.ContainsKey(nextIndex))
                    continue;

                distances[nextIndex] = nextDistance;
                frontier.Enqueue(nextIndex);
            }
        }

        return distances;
    }

    private static int ChooseBranchEdgeCount(int baseEdges, Random rng)
    {
        List<int> candidates = new List<int>();
        int minEdges = Mathf.Max(1, baseEdges - 1);
        int maxEdges = baseEdges + 1;

        for (int edgeCount = minEdges; edgeCount <= maxEdges; edgeCount++)
        {
            if (edgeCount != baseEdges)
                candidates.Add(edgeCount);
        }

        if (candidates.Count == 0)
            candidates.Add(baseEdges + 1);

        return candidates[rng.Next(candidates.Count)];
    }

    private static List<MapNode> CreateBranchNodes(MapData map, MapNode from, MapNode to, int branchEdges, Random rng)
    {
        List<MapNode> branchNodes = new List<MapNode>(Mathf.Max(0, branchEdges - 1));
        int interiorCount = Mathf.Max(0, branchEdges - 1);
        if (interiorCount == 0)
            return branchNodes;

        float side = ChooseBranchSide(map, from, to, rng);
        float amplitude = Mathf.Lerp(0.18f, 0.32f, 1f - Mathf.Abs(to.NormalizedX - from.NormalizedX));
        List<float> yPositions = BuildIntermediateYPositions(from.NormalizedY, to.NormalizedY, interiorCount, rng);

        for (int i = 0; i < interiorCount; i++)
        {
            float y = yPositions[i];
            float t = Mathf.InverseLerp(from.NormalizedY, to.NormalizedY, y);
            float baseX = Mathf.Lerp(from.NormalizedX, to.NormalizedX, t);
            float x = FindBranchX(map, branchNodes, from, to, baseX, y, side, amplitude, t, rng);

            MapNode node = new MapNode
            {
                Index = -1 - i,
                Row = GetNodeRow(from, to, t),
                NormalizedX = x,
                NormalizedY = y,
                Type = NodeType.Battle,
            };

            branchNodes.Add(node);
        }

        return branchNodes;
    }

    private static float FindBranchX(MapData map, List<MapNode> branchNodes, MapNode from, MapNode to, float baseX, float y, float side, float amplitude, float t, Random rng)
    {
        float bestX = Mathf.Clamp(baseX, MinX, MaxX);
        float bestScore = float.MinValue;
        float curve = Mathf.Sin(t * Mathf.PI);

        for (int attempt = 0; attempt < 18; attempt++)
        {
            float jitter = RandomRange(rng, -0.028f, 0.028f);
            float candidate = Mathf.Clamp(baseX + side * amplitude * curve + jitter, MinX, MaxX);
            float score = Mathf.Min(
                GetPlacementScore(map.Nodes, candidate, y),
                GetPlacementScore(branchNodes, candidate, y));

            score -= DistancePointToSegment(new Vector2(candidate, y), GetPosition(from), GetPosition(to)) * 0.08f;

            if (score > bestScore)
            {
                bestScore = score;
                bestX = candidate;
            }
        }

        return bestX;
    }

    private static bool CanAddBranch(MapData map, MapNode from, MapNode to, List<MapNode> branchNodes)
    {
        List<MapNode> chain = new List<MapNode>(branchNodes.Count + 2) { from };
        chain.AddRange(branchNodes);
        chain.Add(to);

        List<Segment> existingSegments = BuildSegments(map);
        List<Segment> candidateSegments = BuildSegments(chain);

        for (int i = 0; i < branchNodes.Count; i++)
        {
            Vector2 point = GetPosition(branchNodes[i]);
            if (GetMinimumDistance(map.Nodes, point) < MinNodeSpacing)
                return false;
            if (GetMinimumDistance(branchNodes, point, i) < MinNodeSpacing)
                return false;

            foreach (Segment segment in existingSegments)
            {
                if (segment.FromIndex == from.Index || segment.ToIndex == from.Index ||
                    segment.FromIndex == to.Index || segment.ToIndex == to.Index)
                    continue;
                if (DistancePointToSegment(point, segment.From, segment.To) < MinPathClearance)
                    return false;
            }
        }

        foreach (Segment candidate in candidateSegments)
        {
            foreach (Segment existing in existingSegments)
            {
                if (SharesEndpoint(candidate, existing))
                    continue;
                if (SegmentsIntersect(candidate.From, candidate.To, existing.From, existing.To))
                    return false;
            }

            foreach (MapNode node in map.Nodes)
            {
                if (node.Index == candidate.FromIndex || node.Index == candidate.ToIndex)
                    continue;
                if (DistancePointToSegment(GetPosition(node), candidate.From, candidate.To) < MinPathClearance)
                    return false;
            }
        }

        for (int i = 0; i < candidateSegments.Count; i++)
        {
            for (int j = i + 1; j < candidateSegments.Count; j++)
            {
                if (Mathf.Abs(i - j) <= 1)
                    continue;
                if (SegmentsIntersect(candidateSegments[i].From, candidateSegments[i].To, candidateSegments[j].From, candidateSegments[j].To))
                    return false;
            }
        }

        return true;
    }

    private static void CommitBranch(MapData map, MapNode from, MapNode to, List<MapNode> branchNodes)
    {
        MapNode previous = from;

        foreach (MapNode node in branchNodes)
        {
            node.Index = map.Nodes.Count;
            map.Nodes.Add(node);
            AddConnection(previous, node);
            previous = node;
        }

        AddConnection(previous, to);
    }

    private static float ChooseBranchSide(MapData map, MapNode from, MapNode to, Random rng)
    {
        float midX = (from.NormalizedX + to.NormalizedX) * 0.5f;
        float midY = (from.NormalizedY + to.NormalizedY) * 0.5f;
        int leftCount = 0;
        int rightCount = 0;

        foreach (MapNode node in map.Nodes)
        {
            if (Mathf.Abs(node.NormalizedY - midY) > 0.16f)
                continue;

            if (node.NormalizedX < midX)
                leftCount++;
            else
                rightCount++;
        }

        if (midX < 0.28f)
            return 1f;
        if (midX > 0.72f)
            return -1f;
        if (leftCount < rightCount)
            return -1f;
        if (rightCount < leftCount)
            return 1f;

        return rng.NextDouble() < 0.5d ? -1f : 1f;
    }

    private static List<float> BuildIntermediateYPositions(float startY, float endY, int count, Random rng)
    {
        List<float> positions = new List<float>(count);
        if (count <= 0)
            return positions;

        List<float> weights = new List<float>(count + 1);
        float totalWeight = 0f;

        for (int i = 0; i < count + 1; i++)
        {
            float weight = 0.8f + (float)rng.NextDouble() * 0.8f;
            weights.Add(weight);
            totalWeight += weight;
        }

        float accumulatedWeight = 0f;
        for (int i = 0; i < count; i++)
        {
            accumulatedWeight += weights[i];
            float y = Mathf.Lerp(startY, endY, accumulatedWeight / totalWeight);
            positions.Add(y);
        }

        return positions;
    }

    private static int GetNodeRow(MapNode from, MapNode to, float t)
    {
        int minRow = from.Row + 1;
        int maxRow = Mathf.Max(minRow, to.Row - 1);
        return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(minRow, maxRow, t)), minRow, maxRow);
    }

    private static void PopulateNodes(MapData map, Random rng)
    {
        FillBattles(map, rng);

        List<MapNode> candidates = new List<MapNode>();
        foreach (MapNode node in map.Nodes)
        {
            bool isBoss = node.Enemy != null && node.Enemy.Tier == EnemyTier.Boss;
            if (node.Type == NodeType.Start || isBoss || node.Row < 2)
                continue;

            candidates.Add(node);
        }

        if (candidates.Count == 0)
            return;

        List<SpecialNodeType> plan = BuildSpecialPlan(candidates.Count, rng);
        Dictionary<int, SpecialNodeType> assignments = BuildSpecialAssignments(map, candidates, plan, rng);

        foreach (KeyValuePair<int, SpecialNodeType> assignment in assignments)
            ApplySpecialType(map.GetNode(assignment.Key), assignment.Value, rng);
    }

    private static void FillBattles(MapData map, Random rng)
    {
        HashSet<string> usedNames = new HashSet<string>();

        foreach (MapNode node in map.Nodes)
        {
            bool isBoss = node.Enemy != null && node.Enemy.Tier == EnemyTier.Boss;
            if (node.Type != NodeType.Battle || isBoss)
                continue;

            node.Enemy = PickEnemy(EnemyDatabase.AllRegular, usedNames, rng);
        }
    }

    private static List<SpecialNodeType> BuildSpecialPlan(int candidateCount, Random rng)
    {
        List<SpecialNodeType> plan = new List<SpecialNodeType>();

        if (candidateCount >= 1)
            plan.Add(SpecialNodeType.Shop);
        if (candidateCount >= 2)
            plan.Add(SpecialNodeType.Rest);
        if (candidateCount >= 3)
            plan.Add(SpecialNodeType.Elite);

        int extraCount = Mathf.Min(Mathf.Max(0, (candidateCount - 3) / 3), candidateCount - plan.Count);
        for (int i = 0; i < extraCount; i++)
            plan.Add(PickExtraSpecialType(rng));

        Shuffle(plan, rng);
        return plan;
    }

    private static SpecialNodeType PickExtraSpecialType(Random rng)
    {
        double roll = rng.NextDouble();
        if (roll < 0.42d)
            return SpecialNodeType.Shop;
        if (roll < 0.78d)
            return SpecialNodeType.Rest;
        return SpecialNodeType.Elite;
    }

    private static Dictionary<int, SpecialNodeType> BuildSpecialAssignments(MapData map, List<MapNode> candidates, List<SpecialNodeType> plan, Random rng)
    {
        List<MapNode> pool = new List<MapNode>(candidates);
        Dictionary<int, List<int>> parentLookup = BuildParentLookup(map);
        List<SpecialNodeType> activePlan = new List<SpecialNodeType>(plan);

        while (activePlan.Count > 0)
        {
            Dictionary<int, SpecialNodeType> assignments = new Dictionary<int, SpecialNodeType>();
            HashSet<int> usedRows = new HashSet<int>();

            Shuffle(pool, rng);
            if (TryAssignSpecialNodes(pool, activePlan, 0, parentLookup, assignments, usedRows, rng))
                return assignments;

            activePlan.RemoveAt(activePlan.Count - 1);
        }

        return new Dictionary<int, SpecialNodeType>();
    }

    private static bool TryAssignSpecialNodes(
        List<MapNode> candidates,
        List<SpecialNodeType> plan,
        int planIndex,
        Dictionary<int, List<int>> parentLookup,
        Dictionary<int, SpecialNodeType> assignments,
        HashSet<int> usedRows,
        Random rng)
    {
        if (planIndex >= plan.Count)
            return true;

        SpecialNodeType type = plan[planIndex];
        List<MapNode> orderedCandidates = BuildCandidateOrder(candidates, assignments, usedRows, rng);

        foreach (MapNode node in orderedCandidates)
        {
            if (!CanAssignSpecialType(node, type, parentLookup, assignments))
                continue;

            assignments[node.Index] = type;
            bool addedRow = usedRows.Add(node.Row);

            if (TryAssignSpecialNodes(candidates, plan, planIndex + 1, parentLookup, assignments, usedRows, rng))
                return true;

            assignments.Remove(node.Index);
            if (addedRow)
                usedRows.Remove(node.Row);
        }

        return false;
    }

    private static List<MapNode> BuildCandidateOrder(List<MapNode> candidates, Dictionary<int, SpecialNodeType> assignments, HashSet<int> usedRows, Random rng)
    {
        List<MapNode> ordered = new List<MapNode>(candidates.Count);

        foreach (MapNode node in candidates)
        {
            if (!assignments.ContainsKey(node.Index))
                ordered.Add(node);
        }

        Shuffle(ordered, rng);
        ordered.Sort((a, b) =>
        {
            int rowUsageCompare = usedRows.Contains(a.Row).CompareTo(usedRows.Contains(b.Row));
            if (rowUsageCompare != 0)
                return rowUsageCompare;

            int rowCompare = a.Row.CompareTo(b.Row);
            if (rowCompare != 0)
                return rowCompare;

            return a.NormalizedY.CompareTo(b.NormalizedY);
        });

        return ordered;
    }

    private static bool CanAssignSpecialType(
        MapNode node,
        SpecialNodeType type,
        Dictionary<int, List<int>> parentLookup,
        Dictionary<int, SpecialNodeType> assignments)
    {
        if (HasNeighborWithSameSpecialType(node.NextNodeIndices, type, assignments))
            return false;

        if (parentLookup.TryGetValue(node.Index, out List<int> parents) &&
            HasNeighborWithSameSpecialType(parents, type, assignments))
        {
            return false;
        }

        return true;
    }

    private static bool HasNeighborWithSameSpecialType(List<int> neighborIndices, SpecialNodeType type, Dictionary<int, SpecialNodeType> assignments)
    {
        foreach (int neighborIndex in neighborIndices)
        {
            if (assignments.TryGetValue(neighborIndex, out SpecialNodeType assignedType) && assignedType == type)
                return true;
        }

        return false;
    }

    private static Dictionary<int, List<int>> BuildParentLookup(MapData map)
    {
        Dictionary<int, List<int>> parents = new Dictionary<int, List<int>>();

        foreach (MapNode node in map.Nodes)
        {
            foreach (int nextIndex in node.NextNodeIndices)
            {
                if (!parents.TryGetValue(nextIndex, out List<int> list))
                {
                    list = new List<int>();
                    parents[nextIndex] = list;
                }

                list.Add(node.Index);
            }
        }

        return parents;
    }

    private static void ApplySpecialType(MapNode node, SpecialNodeType type, Random rng)
    {
        switch (type)
        {
            case SpecialNodeType.Elite:
                node.Type = NodeType.Battle;
                node.Enemy = PickEnemy(EnemyDatabase.AllElite, new HashSet<string>(), rng);
                break;
            case SpecialNodeType.Shop:
                node.Type = NodeType.Shop;
                node.Enemy = null;
                break;
            case SpecialNodeType.Rest:
                node.Type = NodeType.Rest;
                node.Enemy = null;
                break;
        }
    }

    private static List<Segment> BuildSegments(MapData map)
    {
        List<Segment> segments = new List<Segment>();

        foreach (MapNode node in map.Nodes)
        {
            foreach (int nextIndex in node.NextNodeIndices)
                segments.Add(new Segment(node.Index, nextIndex, GetPosition(node), GetPosition(map.GetNode(nextIndex))));
        }

        return segments;
    }

    private static List<Segment> BuildSegments(List<MapNode> nodes)
    {
        List<Segment> segments = new List<Segment>(Mathf.Max(0, nodes.Count - 1));

        for (int i = 0; i < nodes.Count - 1; i++)
            segments.Add(new Segment(nodes[i].Index, nodes[i + 1].Index, GetPosition(nodes[i]), GetPosition(nodes[i + 1])));

        return segments;
    }

    private static float GetPlacementScore(IReadOnlyList<MapNode> nodes, float x, float y)
    {
        float minDistance = GetMinimumDistance(nodes, new Vector2(x, y));
        float edgeDistance = Mathf.Min(x - MinX, MaxX - x);
        return minDistance + edgeDistance * 0.2f;
    }

    private static float GetMinimumDistance(IReadOnlyList<MapNode> nodes, Vector2 point, int ignoreIndex = -1)
    {
        float minDistance = 1f;
        bool foundNode = false;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (i == ignoreIndex)
                continue;

            MapNode node = nodes[i];
            float distance = Vector2.Distance(point, GetPosition(node));
            if (!foundNode || distance < minDistance)
                minDistance = distance;

            foundNode = true;
        }

        return foundNode ? minDistance : 1f;
    }

    private static Vector2 GetPosition(MapNode node)
    {
        return new Vector2(node.NormalizedX, node.NormalizedY);
    }

    private static bool SharesEndpoint(Segment a, Segment b)
    {
        return a.FromIndex == b.FromIndex ||
               a.FromIndex == b.ToIndex ||
               a.ToIndex == b.FromIndex ||
               a.ToIndex == b.ToIndex;
    }

    private static bool SegmentsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        if (PointsEqual(a1, b1) || PointsEqual(a1, b2) || PointsEqual(a2, b1) || PointsEqual(a2, b2))
            return false;

        float o1 = Cross(a2 - a1, b1 - a1);
        float o2 = Cross(a2 - a1, b2 - a1);
        float o3 = Cross(b2 - b1, a1 - b1);
        float o4 = Cross(b2 - b1, a2 - b1);

        if (o1 * o2 < 0f && o3 * o4 < 0f)
            return true;

        return IsPointOnSegment(a1, a2, b1) ||
               IsPointOnSegment(a1, a2, b2) ||
               IsPointOnSegment(b1, b2, a1) ||
               IsPointOnSegment(b1, b2, a2);
    }

    private static float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float length = ab.sqrMagnitude;
        if (length <= 0.0001f)
            return Vector2.Distance(point, a);

        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / length);
        Vector2 closest = a + ab * t;
        return Vector2.Distance(point, closest);
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    private static bool IsPointOnSegment(Vector2 a, Vector2 b, Vector2 point)
    {
        if (!PointsEqual(point, a) && !PointsEqual(point, b) && Mathf.Abs(Cross(b - a, point - a)) > 0.0001f)
            return false;

        return point.x >= Mathf.Min(a.x, b.x) - 0.0001f &&
               point.x <= Mathf.Max(a.x, b.x) + 0.0001f &&
               point.y >= Mathf.Min(a.y, b.y) - 0.0001f &&
               point.y <= Mathf.Max(a.y, b.y) + 0.0001f;
    }

    private static bool PointsEqual(Vector2 a, Vector2 b)
    {
        return Vector2.SqrMagnitude(a - b) <= 0.000001f;
    }

    private static void AddConnection(MapNode from, MapNode to)
    {
        if (!from.NextNodeIndices.Contains(to.Index))
            from.NextNodeIndices.Add(to.Index);
    }

    private static EnemyData PickEnemy(List<EnemyData> pool, HashSet<string> usedNames, Random rng)
    {
        List<EnemyData> available = pool.FindAll(enemy => !usedNames.Contains(enemy.EnemyName));
        if (available.Count == 0)
            available = pool;

        EnemyData picked = available[rng.Next(available.Count)];
        usedNames.Add(picked.EnemyName);
        return picked;
    }

    private static float RandomRange(Random rng, float minValue, float maxValue)
    {
        return minValue + (float)rng.NextDouble() * (maxValue - minValue);
    }

    private static void Shuffle<T>(IList<T> items, Random rng)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(i + 1);
            (items[i], items[swapIndex]) = (items[swapIndex], items[i]);
        }
    }

    private readonly struct Segment
    {
        public readonly int FromIndex;
        public readonly int ToIndex;
        public readonly Vector2 From;
        public readonly Vector2 To;

        public Segment(int fromIndex, int toIndex, Vector2 from, Vector2 to)
        {
            FromIndex = fromIndex;
            ToIndex = toIndex;
            From = from;
            To = to;
        }
    }
}
