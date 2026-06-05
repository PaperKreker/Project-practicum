using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const int SaveVersion = 1;
    private const string SaveFileName = "run_save.json";

    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    public static bool Save(GameManager manager)
    {
        if (manager == null || manager.Run == null || manager.CurrentMap == null)
            return false;

        SaveData data = SaveData.FromGame(manager);
        if (data.CurrentState == RunState.Battle && data.Battle == null)
            return false;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        return true;
    }

    public static bool TryLoad(out SaveData data)
    {
        data = null;

        if (!File.Exists(SavePath))
            return false;

        try
        {
            string json = File.ReadAllText(SavePath);
            data = JsonUtility.FromJson<SaveData>(json);
            return data != null && data.Version == SaveVersion;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"SaveSystem: failed to load save. {exception.Message}");
            return false;
        }
    }

    [Serializable]
    public class SaveData
    {
        public int Version;
        public RunState CurrentState;
        public int CurrentActIndex;
        public RunDataSave Run;
        public MapDataSave Map;
        public BattleSaveData Battle;

        public static SaveData FromGame(GameManager manager)
        {
            RunDataSave run = RunDataSave.FromRun(manager.Run);
            BattleSaveData battle = null;
            if (manager.CurrentState == RunState.Battle)
            {
                BattleController battleController = UnityEngine.Object.FindFirstObjectByType<BattleController>();
                if (battleController != null)
                {
                    run.PlayerHp = Mathf.Max(1, battleController.GetCurrentState().playerHp);
                    battle = battleController.CreateSaveData();
                }
            }

            return new SaveData
            {
                Version = SaveVersion,
                CurrentState = manager.CurrentState,
                CurrentActIndex = manager.CurrentActIndex,
                Run = run,
                Map = MapDataSave.FromMap(manager.CurrentMap),
                Battle = battle,
            };
        }
    }

    [Serializable]
    public class BattleSaveData
    {
        public int AttackCoins;
        public int DiscardsLeft;
        public int EnemyHp;
        public int PlayerHp;
        public int EnemyDamage;
        public bool BattleOver;
        public bool HasPlayerAttackedInTutorial;
        public string EnemyName;
        public List<Suit> BlockedDamageSuits = new List<Suit>();
        public List<CardSaveData> DeckCards = new List<CardSaveData>();
        public List<CardViewSaveData> HandCards = new List<CardViewSaveData>();
        public EnemyEffectSaveData EnemyEffect = new EnemyEffectSaveData();
        public List<SigilSaveData> Sigils = new List<SigilSaveData>();
    }

    [Serializable]
    public class CardSaveData
    {
        public Suit Suit;
        public Rank Rank;
        public bool IsCritical;
        public bool IsDebuffed;

        public static CardSaveData FromCard(Card card)
        {
            return new CardSaveData
            {
                Suit = card.Suit,
                Rank = card.Rank,
                IsCritical = card.IsCritical,
                IsDebuffed = card.IsDebuffed,
            };
        }

        public Card ToCard()
        {
            return new Card(Suit, Rank, IsCritical)
            {
                IsDebuffed = IsDebuffed,
            };
        }
    }

    [Serializable]
    public class CardViewSaveData : CardSaveData
    {
        public bool IsSelected;
        public bool IsPetrified;
        public bool IsFaceDown;

        public static CardViewSaveData FromView(CardView view)
        {
            CardViewSaveData save = new CardViewSaveData
            {
                Suit = view.Data.Suit,
                Rank = view.Data.Rank,
                IsCritical = view.Data.IsCritical,
                IsDebuffed = view.Data.IsDebuffed,
                IsSelected = view.IsSelected,
                IsPetrified = view.IsPetrified,
                IsFaceDown = view.IsFaceDown,
            };

            return save;
        }
    }

    [Serializable]
    public class EnemyEffectSaveData
    {
        public int IntValue;
        public int IntValue2;
        public ComboType ComboValue;
        public bool BoolValue;
    }

    [Serializable]
    public class SigilSaveData
    {
        public string Name;
        public int IntValue;
        public bool BoolValue;
    }

    [Serializable]
    public class RunDataSave
    {
        public DifficultyLevel Difficulty;
        public int PlayerHp;
        public int PlayerMaxHp;
        public int Gold;
        public int CurrentNodeIndex;
        public bool CurrentNodeCompleted;
        public List<string> ActiveSigils = new List<string>();
        public List<int> VisitedNodeIndices = new List<int>();
        public int Seed;
        public int ShopRandomCalls;

        public static RunDataSave FromRun(RunData run)
        {
            RunDataSave save = new RunDataSave
            {
                Difficulty = run.Difficulty,
                PlayerHp = run.PlayerHp,
                PlayerMaxHp = run.PlayerMaxHp,
                Gold = run.Gold,
                CurrentNodeIndex = run.CurrentNodeIndex,
                CurrentNodeCompleted = run.CurrentNodeCompleted,
                VisitedNodeIndices = new List<int>(run.VisitedNodeIndices),
                Seed = run.Seed,
                ShopRandomCalls = run.ShopRandomCalls,
            };

            foreach (Sigil sigil in run.ActiveSigils)
                save.ActiveSigils.Add(sigil.Name);

            return save;
        }

        public RunData ToRun()
        {
            RunData run = new RunData
            {
                Difficulty = Difficulty,
                PlayerHp = PlayerHp,
                PlayerMaxHp = PlayerMaxHp,
                Gold = Gold,
                CurrentNodeIndex = CurrentNodeIndex,
                CurrentNodeCompleted = CurrentNodeCompleted,
                VisitedNodeIndices = new List<int>(VisitedNodeIndices),
                Seed = Seed,
                ShopRandomCalls = ShopRandomCalls,
            };

            List<Sigil> allSigils = SigilDatabase.All();
            foreach (string sigilName in ActiveSigils)
            {
                Sigil sigil = allSigils.Find(s => s.Name == sigilName);
                if (sigil != null)
                    run.ActiveSigils.Add(sigil);
            }

            run.InitRngs();
            return run;
        }
    }

    [Serializable]
    public class MapDataSave
    {
        public List<MapNodeSave> Nodes = new List<MapNodeSave>();
        public int StartNodeIndex;

        public static MapDataSave FromMap(MapData map)
        {
            MapDataSave save = new MapDataSave
            {
                StartNodeIndex = map.StartNodeIndex,
            };

            foreach (MapNode node in map.Nodes)
                save.Nodes.Add(MapNodeSave.FromNode(node));

            return save;
        }

        public MapData ToMap()
        {
            MapData map = new MapData
            {
                StartNodeIndex = StartNodeIndex,
            };

            foreach (MapNodeSave node in Nodes)
                map.Nodes.Add(node.ToNode());

            return map;
        }
    }

    [Serializable]
    public class MapNodeSave
    {
        public int Index;
        public int Row;
        public float NormalizedX;
        public float NormalizedY;
        public NodeType Type;
        public string EnemyName;
        public List<int> NextNodeIndices = new List<int>();

        public static MapNodeSave FromNode(MapNode node)
        {
            return new MapNodeSave
            {
                Index = node.Index,
                Row = node.Row,
                NormalizedX = node.NormalizedX,
                NormalizedY = node.NormalizedY,
                Type = node.Type,
                EnemyName = node.Enemy?.EnemyName,
                NextNodeIndices = new List<int>(node.NextNodeIndices),
            };
        }

        public MapNode ToNode()
        {
            return new MapNode
            {
                Index = Index,
                Row = Row,
                NormalizedX = NormalizedX,
                NormalizedY = NormalizedY,
                Type = Type,
                Enemy = FindEnemy(EnemyName),
                NextNodeIndices = new List<int>(NextNodeIndices),
            };
        }

        private static EnemyData FindEnemy(string enemyName)
        {
            if (string.IsNullOrEmpty(enemyName))
                return null;

            List<EnemyData> enemies = new List<EnemyData>();
            enemies.AddRange(EnemyDatabase.AllRegular);
            enemies.AddRange(EnemyDatabase.AllElite);
            enemies.AddRange(EnemyDatabase.AllBosses);
            return enemies.Find(enemy => enemy.EnemyName == enemyName);
        }
    }
}
