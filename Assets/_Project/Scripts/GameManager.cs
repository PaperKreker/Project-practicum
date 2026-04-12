using System;
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

    private const string SCENE_MAIN_MENU = "MainMenu";
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

    // seed = -1 means random
    public void StartNewRun(int playerMaxHp = 100, int seed = -1)
    {
        CurrentActIndex = 0;

        int resolvedSeed = seed >= 0 ? seed : UnityEngine.Random.Range(0, int.MaxValue);

        Run = new RunData
        {
            PlayerMaxHp = playerMaxHp,
            PlayerHp = playerMaxHp,
            Gold = 0,
            CurrentNodeIndex = 0,
            CurrentNodeCompleted = false,
            Seed = resolvedSeed,
            Rng = new System.Random(resolvedSeed),
        };

        Debug.Log($"[GameManager] New run. Seed={resolvedSeed}");
        LoadAct(0);
    }

    public void EnterNode(int nodeIndex)
    {
        Run.CurrentNodeIndex = nodeIndex;
        Run.CurrentNodeCompleted = false;
        MapNode node = CurrentMap.GetNode(nodeIndex);

        switch (node.Type)
        {
            case NodeType.Battle: TransitionTo(RunState.Battle, SCENE_BATTLE); break;
            case NodeType.Shop: TransitionTo(RunState.Shop, SCENE_SHOP); break;
            case NodeType.Rest: ApplyRest(); break;
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

    public EnemyData GetCurrentEnemy() =>
        CurrentMap.GetNode(Run.CurrentNodeIndex).Enemy;

    private void LoadAct(int actIndex)
    {
        CurrentMap = MapGenerator.BuildAct(actIndex, Run.Rng);
        Run.CurrentNodeIndex = CurrentMap.StartNodeIndex;
        Run.CurrentNodeCompleted = false;
        TransitionTo(RunState.Map, SCENE_MAP);
    }

    private void ApplyRest()
    {
        int missing = Run.PlayerMaxHp - Run.PlayerHp;
        int healed = Mathf.CeilToInt(missing * 0.40f);
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
