using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public static class MapGenerator
{
    private const int ActLength = 5;
    private const int ActWidth = 3;
    private const int MinMiddleRowWidth = 2;
    private const float PositionJitter = 0.035f;

    public static MapData BuildAct(int actIndex, Random rng)
    {
        int totalRows = Mathf.Max(3, ActLength);
        int maxWidth = Mathf.Max(1, ActWidth);
        List<int> rowWidths = BuildRowWidths(totalRows, maxWidth);

        var map = new MapData();
        var rows = new List<List<MapNode>>();
        int nextIndex = 0;

        for (int rowIndex = 0; rowIndex < rowWidths.Count; rowIndex++)
        {
            int rowWidth = rowWidths[rowIndex];
            var row = new List<MapNode>(rowWidth);

            foreach (float x in GenerateRowPositions(rowWidth, rng))
            {
                var node = new MapNode
                {
                    Index = nextIndex++,
                    Row = rowIndex,
                    NormalizedX = x,
                    Type = NodeType.Battle,
                };

                row.Add(node);
                map.Nodes.Add(node);
            }

            rows.Add(row);
        }

        rows[0][0].Type = NodeType.Start;

        EnemyData boss = EnemyDatabase.AllBosses[actIndex % EnemyDatabase.AllBosses.Count];
        rows[^1][0].Type = NodeType.Battle;
        rows[^1][0].Enemy = boss;

        ConnectRowsWithoutCrossing(rows);
        PopulateRows(rows, rng);

        map.StartNodeIndex = rows[0][0].Index;
        return map;
    }

    private static List<int> BuildRowWidths(int totalRows, int maxWidth)
    {
        var widths = new List<int>(totalRows);
        widths.Add(1);

        int middleRowCount = totalRows - 2;
        int minWidth = Mathf.Min(MinMiddleRowWidth, maxWidth);

        for (int i = 0; i < middleRowCount; i++)
        {
            if (maxWidth <= 1)
            {
                widths.Add(1);
                continue;
            }

            if (middleRowCount == 1)
            {
                widths.Add(maxWidth);
                continue;
            }

            float centerBias = 1f - Mathf.Abs(((float)i / (middleRowCount - 1)) * 2f - 1f);
            int width = minWidth + Mathf.RoundToInt(centerBias * (maxWidth - minWidth));
            widths.Add(Mathf.Clamp(width, minWidth, maxWidth));
        }

        widths.Add(1);
        return widths;
    }

    private static List<float> GenerateRowPositions(int count, Random rng)
    {
        var positions = new List<float>(count);
        if (count <= 0)
            return positions;

        if (count == 1)
        {
            positions.Add(0.5f);
            return positions;
        }

        float span = count == 2 ? 0.34f : 0.62f;
        float start = 0.5f - span * 0.5f;
        float step = span / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float jitter = ((float)rng.NextDouble() - 0.5f) * PositionJitter;
            positions.Add(Mathf.Clamp(start + i * step + jitter, 0.18f, 0.82f));
        }

        positions.Sort();
        return positions;
    }

    private static void ConnectRowsWithoutCrossing(List<List<MapNode>> rows)
    {
        for (int rowIndex = 0; rowIndex < rows.Count - 1; rowIndex++)
        {
            List<MapNode> currentRow = rows[rowIndex];
            List<MapNode> nextRow = rows[rowIndex + 1];

            for (int currentIndex = 0; currentIndex < currentRow.Count; currentIndex++)
            {
                int startTarget = Mathf.FloorToInt(currentIndex * nextRow.Count / (float)currentRow.Count);
                int endTarget = Mathf.CeilToInt((currentIndex + 1) * nextRow.Count / (float)currentRow.Count) - 1;

                startTarget = Mathf.Clamp(startTarget, 0, nextRow.Count - 1);
                endTarget = Mathf.Clamp(endTarget, startTarget, nextRow.Count - 1);

                for (int targetIndex = startTarget; targetIndex <= endTarget; targetIndex++)
                    AddConnection(currentRow[currentIndex], nextRow[targetIndex]);
            }
        }
    }

    private static void AddConnection(MapNode from, MapNode to)
    {
        if (!from.NextNodeIndices.Contains(to.Index))
            from.NextNodeIndices.Add(to.Index);
    }

    private static void PopulateRows(List<List<MapNode>> rows, Random rng)
    {
        for (int rowIndex = 1; rowIndex < rows.Count - 1; rowIndex++)
            FillRowWithRegularBattles(rows[rowIndex], rng);

        int lastPlayableRowIndex = rows.Count - 2;
        int supportRowIndex = Mathf.Clamp(2, 1, lastPlayableRowIndex);

        if (supportRowIndex == lastPlayableRowIndex)
        {
            ConfigureCombinedRow(rows[supportRowIndex], rng);
            return;
        }

        ConfigureSupportRow(rows[supportRowIndex], rng);
        ConfigureChallengeRow(rows[lastPlayableRowIndex], rng);
    }

    private static void ConfigureSupportRow(List<MapNode> row, Random rng)
    {
        var used = new HashSet<int>();

        ConfigureNodeAsShop(row[TakeRandomNodeIndex(row, used, rng)]);

        if (row.Count > 2)
            ConfigureNodeAsRest(row[TakeRandomNodeIndex(row, used, rng)]);
    }

    private static void ConfigureChallengeRow(List<MapNode> row, Random rng)
    {
        var used = new HashSet<int>();

        ConfigureNodeAsEliteBattle(row[TakeRandomNodeIndex(row, used, rng)], rng);

        if (row.Count > 1)
        {
            int utilityIndex = TakeRandomNodeIndex(row, used, rng);
            if (rng.NextDouble() < 0.5d)
                ConfigureNodeAsRest(row[utilityIndex]);
            else
                ConfigureNodeAsShop(row[utilityIndex]);
        }
    }

    private static void ConfigureCombinedRow(List<MapNode> row, Random rng)
    {
        var used = new HashSet<int>();

        if (row.Count == 1)
        {
            ConfigureNodeAsEliteBattle(row[0], rng);
            return;
        }

        ConfigureNodeAsEliteBattle(row[TakeRandomNodeIndex(row, used, rng)], rng);

        int utilityIndex = TakeRandomNodeIndex(row, used, rng);
        if (rng.NextDouble() < 0.5d)
            ConfigureNodeAsRest(row[utilityIndex]);
        else
            ConfigureNodeAsShop(row[utilityIndex]);

        if (row.Count > 2)
            ConfigureNodeAsShop(row[TakeRandomNodeIndex(row, used, rng)]);
    }

    private static void FillRowWithRegularBattles(List<MapNode> row, Random rng)
    {
        var usedNames = new HashSet<string>();
        foreach (MapNode node in row)
        {
            node.Type = NodeType.Battle;
            node.Enemy = PickEnemy(EnemyDatabase.AllRegular, usedNames, rng);
        }
    }

    private static void ConfigureNodeAsEliteBattle(MapNode node, Random rng)
    {
        node.Type = NodeType.Battle;
        node.Enemy = PickEnemy(EnemyDatabase.AllElite, new HashSet<string>(), rng);
    }

    private static void ConfigureNodeAsShop(MapNode node)
    {
        node.Type = NodeType.Shop;
        node.Enemy = null;
    }

    private static void ConfigureNodeAsRest(MapNode node)
    {
        node.Type = NodeType.Rest;
        node.Enemy = null;
    }

    private static EnemyData PickEnemy(List<EnemyData> pool, HashSet<string> usedNames, Random rng)
    {
        var available = pool.FindAll(enemy => !usedNames.Contains(enemy.EnemyName));
        if (available.Count == 0)
            available = pool;

        EnemyData picked = available[rng.Next(available.Count)];
        usedNames.Add(picked.EnemyName);
        return picked;
    }

    private static int TakeRandomNodeIndex(List<MapNode> row, HashSet<int> usedIndices, Random rng)
    {
        var available = new List<int>();
        for (int i = 0; i < row.Count; i++)
        {
            if (!usedIndices.Contains(i))
                available.Add(i);
        }

        int picked = available[rng.Next(available.Count)];
        usedIndices.Add(picked);
        return picked;
    }
}
