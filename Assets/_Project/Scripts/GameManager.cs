using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static event Action<RunState> OnStateChanged;

    public RunState CurrentState { get; private set; } = RunState.MainMenu;
    public RunData Run { get; private set; }
    public MapData CurrentMap { get; private set; }
    public int CurrentActIndex { get; private set; }
    public int PendingSeed { get; private set; } = -1;

    private const string SCENE_MAIN_MENU = "MainMenu";
    private const string SCENE_DIFFICULTY_SELECT = "DifficultySelect";
    private const string SCENE_MAP = "Map";
    private const string SCENE_BATTLE = "Battle";
    private const string SCENE_SHOP = "Shop";
    private const string SCENE_GAME_OVER = "GameOver";
    private const string SCENE_VICTORY = "Victory";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void OpenDifficultySelection(int seed = -1)
    {
        PendingSeed = seed;
        TransitionTo(RunState.DifficultySelect, SCENE_DIFFICULTY_SELECT);
    }

    public void ReturnToMainMenu()
    {
        PendingSeed = -1;
        TransitionTo(RunState.MainMenu, SCENE_MAIN_MENU);
    }

    public void StartNewRun(DifficultyLevel difficulty = DifficultyLevel.Normal, int seed = -1)
    {
        CurrentActIndex = 0;

        int selectedSeed = seed >= 0 ? seed : PendingSeed;
        int resolvedSeed = selectedSeed >= 0 ? selectedSeed : UnityEngine.Random.Range(0, int.MaxValue);
        int playerMaxHp = GameBalance.GetPlayerMaxHp(difficulty);

        Run = new RunData
        {
            Difficulty = difficulty,
            PlayerMaxHp = playerMaxHp,
            PlayerHp = playerMaxHp,
            Gold = 0,
            CurrentNodeIndex = 0,
            CurrentNodeCompleted = false,
            VisitedNodeIndices = new List<int>(),
            Seed = resolvedSeed,
            Rng = new System.Random(resolvedSeed),
        };

        PendingSeed = -1;

        Debug.Log($"[GameManager] New run. Seed={resolvedSeed}");
        LoadAct(0);
    }

    public void EnterNode(int nodeIndex)
    {
        Run.CurrentNodeIndex = nodeIndex;
        Run.CurrentNodeCompleted = false;

        if (Run.VisitedNodeIndices.Count == 0 || Run.VisitedNodeIndices[^1] != nodeIndex)
            Run.VisitedNodeIndices.Add(nodeIndex);

        MapNode node = CurrentMap.GetNode(nodeIndex);

        switch (node.Type)
        {
            case NodeType.Start:
                Run.CurrentNodeCompleted = true;
                TransitionTo(RunState.Map, SCENE_MAP);
                break;
            case NodeType.Battle:
                TransitionTo(RunState.Battle, SCENE_BATTLE);
                break;
            case NodeType.Shop:
                TransitionTo(RunState.Shop, SCENE_SHOP);
                break;
            case NodeType.Rest:
                ApplyRest();
                break;
        }
    }

    public void OnBattleEnded(bool playerWon, int finalPlayerHp, int goldEarned)
    {
        if (!playerWon)
        {
            TransitionTo(RunState.GameOver, SCENE_GAME_OVER);
            return;
        }

        Run.PlayerHp = finalPlayerHp;
        Run.Gold += goldEarned;
        Run.CurrentNodeCompleted = true;

        MapNode node = CurrentMap.GetNode(Run.CurrentNodeIndex);

        if (node.NextNodeIndices.Count == 0)
        {
            CurrentActIndex++;
            if (VictoryChecker.IsRunComplete(CurrentActIndex))
            {
                TransitionTo(RunState.Victory, SCENE_VICTORY);
                return;
            }
            LoadAct(CurrentActIndex);
        }
        else
        {
            TransitionTo(RunState.Map, SCENE_MAP);
        }
    }

    public void OnShopExited()
    {
        Run.CurrentNodeCompleted = true;
        TransitionTo(RunState.Map, SCENE_MAP);
    }

    public EnemyData GetCurrentEnemy()
    {
        EnemyData enemy = CurrentMap.GetNode(Run.CurrentNodeIndex).Enemy;
        return enemy == null ? null : GameBalance.ApplyDifficulty(enemy, Run.Difficulty, CurrentActIndex);
    }

    private void LoadAct(int actIndex)
    {
        CurrentMap = MapGenerator.BuildAct(actIndex, Run.Rng, Run.Difficulty);
        Run.CurrentNodeIndex = CurrentMap.StartNodeIndex;
        Run.CurrentNodeCompleted = true;
        Run.VisitedNodeIndices = new List<int> { CurrentMap.StartNodeIndex };
        TransitionTo(RunState.Map, SCENE_MAP);
    }

    private void ApplyRest()
    {
        int healed = GameBalance.GetRestHealAmount(Run);
        Run.PlayerHp = Mathf.Min(Run.PlayerHp + healed, Run.PlayerMaxHp);
        Run.CurrentNodeCompleted = true;
        TransitionTo(RunState.Map, SCENE_MAP);
    }

    private void TransitionTo(RunState newState, string sceneName)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.LoadScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
