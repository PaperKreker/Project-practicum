using System;
using System.Collections.Generic;
using System.Collections;
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
    public SaveSystem.BattleSaveData PendingBattleSave { get; private set; }

    private const string SCENE_MAIN_MENU = "MainMenu";
    private const string SCENE_DIFFICULTY_SELECT = "DifficultySelect";
    private const string SCENE_MAP = "Map";
    private const string SCENE_BATTLE = "Battle";
    private const string SCENE_SHOP = "Shop";
    private const string SCENE_GAME_OVER = "GameOver";
    private const string SCENE_VICTORY = "Victory";

    private static readonly HashSet<string> PauseScenes = new HashSet<string>
    {
        SCENE_MAP,
        SCENE_BATTLE,
        SCENE_SHOP,
    };

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        EnsurePauseMenuForActiveScene();
    }

    public void OpenDifficultySelection(int seed = -1)
    {
        PendingSeed = seed;
        TransitionTo(RunState.DifficultySelect, SCENE_DIFFICULTY_SELECT);
    }

    public void ReturnToMainMenu()
    {
        ReturnToMainMenu(true);
    }

    public void ReturnToMainMenu(bool useTransition)
    {
        PendingSeed = -1;
        TransitionTo(RunState.MainMenu, SCENE_MAIN_MENU, useTransition);
    }

    public bool HasSave()
    {
        return SaveSystem.HasSave();
    }

    public bool SaveRun()
    {
        return SaveSystem.Save(this);
    }

    public bool LoadSavedRun()
    {
        if (!SaveSystem.TryLoad(out SaveSystem.SaveData save))
            return false;

        Run = save.Run.ToRun();
        CurrentMap = save.Map.ToMap();
        CurrentActIndex = save.CurrentActIndex;
        PendingBattleSave = save.Battle;
        PendingSeed = -1;

        RunState state = save.CurrentState;
        if (state == RunState.MainMenu || state == RunState.DifficultySelect || state == RunState.GameOver || state == RunState.Victory)
            state = RunState.Map;

        string sceneName = GetSceneName(state);
        TransitionTo(state, sceneName);
        return true;
    }

    public void DeleteSave()
    {
        SaveSystem.DeleteSave();
    }

    public void StartNewRun(DifficultyLevel difficulty = DifficultyLevel.Normal, int seed = -1)
    {
        StartNewRun(difficulty, seed, true);
    }

    public void StartNewRun(DifficultyLevel difficulty, int seed, bool useTransition)
    {
        SaveSystem.DeleteSave();
        PendingBattleSave = null;
        CurrentActIndex = 0;

        int resolvedSeed = seed >= 0 ? seed : UnityEngine.Random.Range(0, int.MaxValue);
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
            ShopRandomCalls = 0,
        };

        Run.InitRngs();

        Debug.Log($"[GameManager] New run. Seed={resolvedSeed}");
        LoadAct(0, useTransition);
    }

    public void StartRunFromTutorial(DifficultyLevel difficulty = DifficultyLevel.Normal, int playerMaxHp = 100, int seed = -1)
    {
        PendingBattleSave = null;
        CurrentActIndex = 0;
        int resolvedSeed = seed >= 0 ? seed : UnityEngine.Random.Range(0, int.MaxValue);

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
            ShopRandomCalls = 0,
        };
        Run.InitRngs();

        Debug.Log($"[GameManager] Tutorial -> Run. Seed={resolvedSeed}");
        LoadAct(0);
    }


    public void EnterNode(int nodeIndex)
    {
        PendingBattleSave = null;
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
        if (Run == null) return;
        PendingBattleSave = null;
        if (!playerWon)
        {
            SaveSystem.DeleteSave();
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
                SaveSystem.DeleteSave();
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
        PendingBattleSave = null;
        Run.CurrentNodeCompleted = true;
        TransitionTo(RunState.Map, SCENE_MAP);
    }

    public SaveSystem.BattleSaveData ConsumePendingBattleSave()
    {
        SaveSystem.BattleSaveData save = PendingBattleSave;
        PendingBattleSave = null;
        return save;
    }

    public EnemyData GetCurrentEnemy()
    {
        EnemyData enemy = CurrentMap.GetNode(Run.CurrentNodeIndex).Enemy;
        return enemy == null ? null : GameBalance.ApplyDifficulty(enemy, Run.Difficulty, CurrentActIndex);
    }

    private void LoadAct(int actIndex)
    {
        LoadAct(actIndex, true);
    }

    private void LoadAct(int actIndex, bool useTransition)
    {
        CurrentMap = MapGenerator.BuildAct(actIndex, Run.MapRng(actIndex), Run.Difficulty);
        Run.CurrentNodeIndex = CurrentMap.StartNodeIndex;
        Run.CurrentNodeCompleted = true;
        Run.VisitedNodeIndices = new List<int> { CurrentMap.StartNodeIndex };
        TransitionTo(RunState.Map, SCENE_MAP, useTransition);
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
        TransitionTo(newState, sceneName, true);
    }

    private void TransitionTo(RunState newState, string sceneName, bool useTransition)
    {
        Time.timeScale = 1f;
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
        if (useTransition && TransitionManager.Instance != null)
        {
            TransitionManager.Instance.LoadScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private static string GetSceneName(RunState state)
    {
        return state switch
        {
            RunState.DifficultySelect => SCENE_DIFFICULTY_SELECT,
            RunState.Map => SCENE_MAP,
            RunState.Battle => SCENE_BATTLE,
            RunState.Shop => SCENE_SHOP,
            RunState.GameOver => SCENE_GAME_OVER,
            RunState.Victory => SCENE_VICTORY,
            _ => SCENE_MAIN_MENU,
        };
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(EnsurePauseMenuAfterSceneLoaded());
    }

    private IEnumerator EnsurePauseMenuAfterSceneLoaded()
    {
        yield return null;
        EnsurePauseMenuForActiveScene();
    }

    private void EnsurePauseMenuForActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!PauseScenes.Contains(scene.name))
            return;

        if (FindFirstObjectByType<PauseMenuController>() != null)
            return;

        GameObject go = new GameObject("PauseMenuController");
        go.AddComponent<PauseMenuController>();
    }
}
