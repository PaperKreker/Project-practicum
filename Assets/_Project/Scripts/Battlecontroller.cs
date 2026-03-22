using System.Collections.Generic;
using UnityEngine;
using System;

public class BattleController : MonoBehaviour
{
    public struct State
    {
        public int attackCoins;
        public int discardsLeft;
        public int enemyHp;
        public int playerHp;
        public EnemyData enemyData;
        public EnemyEffect enemyEffect;
        public BattleContext ctx;
    }

    public State GetCurrentState() => new State
    {
        attackCoins = _attackCoins,
        discardsLeft = _discardsLeft,
        enemyHp = _enemyHp,
        playerHp = _ctx.PlayerHp,
        enemyData = _enemyData,
        enemyEffect = _enemyEffect,
        ctx = _ctx,
    };

    public event Action OnRefreshAll;
    public event Action OnRefresh;

    [Header("References")]
    [SerializeField] private HandController _hand;
    [SerializeField] private BattleConfig _battleConfig;

    private EnemyData _enemyData;
    private EnemyEffect _enemyEffect;
    private BattleContext _ctx;

    private int _attackCoins;
    private int _discardsLeft;
    private int _enemyHp;
    private Deck _deck;

    private bool _battleOver;

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current?.spaceKey.wasPressedThisFrame == true)
            Attack();
    }

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[BattleController] No GameManager — using test defaults.");
            StartBattle(new Deck(), EnemyDatabase.Raven);
            return;
        }

        var run = GameManager.Instance.Run;
        var enemy = GameManager.Instance.GetCurrentEnemy();
        Debug.Log($"[BattleController] Starting battle. PlayerHp={run.PlayerHp}, Enemy={enemy.EnemyName}");
        StartBattle(new Deck(), enemy, run.PlayerHp, run.PlayerMaxHp);
    }

    public void StartBattle(Deck deck, EnemyData enemy, int currentHp = -1, int maxHp = -1)
    {
        _battleOver = false;
        _deck = deck;
        _enemyData = enemy;
        _enemyHp = enemy.MaxHp;
        _attackCoins = enemy.AttackCoinsPerRound;
        _discardsLeft = _battleConfig != null ? _battleConfig.MaxDiscards : 3;

        int resolvedHp = currentHp > 0 ? currentHp
            : _battleConfig != null ? _battleConfig.PlayerMaxHp : 100;

        _ctx = new BattleContext
        {
            Hand = _hand,
            PlayerHp = resolvedHp,
            EnemyDamage = enemy.AttackDamage,
            Discards = _discardsLeft,
            RequestUIRefresh = () => OnRefresh?.Invoke(),
        };

        _enemyEffect = enemy.CreateEffect();
        _enemyEffect.OnBattleStart(_ctx);

        // Sync back in case effect modified discards (e.g. Wolf)
        _discardsLeft = _ctx.Discards;

        _hand.Init(_deck);
        OnRefreshAll?.Invoke();
    }

    public void Attack()
    {
        if (_battleOver) return;
        List<Card> selected = _hand.GetSelectedCards();
        if (selected.Count == 0 || _attackCoins <= 0) return;

        ComboResult result = ComboEvaluator.Evaluate(selected);
        int damage = Mathf.RoundToInt(result.TotalDamage);

        if (_ctx.BlockedDamageSuit.HasValue)
            damage = ApplyBlockedSuit(selected, damage);

        damage = Mathf.Max(0, _enemyEffect.ModifyPlayerDamage(_ctx, result, damage));

        _enemyHp -= damage;
        _enemyEffect.OnPlayerAttack(_ctx, result);

        _attackCoins--;
        _hand.DiscardSelected();
        _hand.DrawUpToMax();

        // Check victory before enemy gets a turn — dead enemies don't attack
        if (VictoryChecker.IsBattleWon(_enemyHp))
        {
            OnRefreshAll?.Invoke();
            EndBattle(playerWon: true);
            return;
        }

        if (_attackCoins <= 0)
            EnemyTakeTurn();

        OnRefreshAll?.Invoke();
    }

    public void Discard()
    {
        if (_battleOver) return;
        if (_discardsLeft <= 0) return;

        int count = _hand.GetSelectedCards().Count;
        if (count == 0) return;

        _enemyEffect.OnPlayerDiscard(_ctx, count);
        _discardsLeft = _ctx.Discards;

        _hand.DiscardSelected();
        _hand.DrawUpToMax();

        _discardsLeft--;
        _ctx.Discards = _discardsLeft;
        OnRefreshAll?.Invoke();
    }

    private void EnemyTakeTurn()
    {
        _ctx.PlayerHp -= _ctx.EnemyDamage;
        _enemyEffect.OnEnemyAttack(_ctx);
        _attackCoins = _enemyData.AttackCoinsPerRound;

        if (VictoryChecker.IsGameOver(_ctx.PlayerHp))
            EndBattle(playerWon: false);
    }

    // Single exit point for battle resolution — never called twice thanks to _battleOver guard
    private void EndBattle(bool playerWon)
    {
        if (_battleOver) return;
        _battleOver = true;

        _enemyEffect.OnBattleEnd(_ctx);

        if (playerWon)
        {
            Debug.Log($"[BattleController] Victory. Gold: {_enemyData.GoldReward}");
            GameManager.Instance?.OnBattleEnded(true, _ctx.PlayerHp, _enemyData.GoldReward);
        }
        else
        {
            Debug.Log("[BattleController] Defeat.");
            GameManager.Instance?.OnBattleEnded(false, 0, 0);
        }
    }

    private int ApplyBlockedSuit(List<Card> cards, int damage)
    {
        int removed = 0;
        foreach (var c in cards)
            if (c.Suit == _ctx.BlockedDamageSuit.Value)
                removed += (int)c.Rank;
        return Mathf.Max(0, damage - removed);
    }
}
