using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class BattleController : MonoBehaviour
{
    public event Action OnAnimationStarted;
    public event Action OnAnimationStopped;
    public event Action OnRefreshAll;
    public event Action OnRefresh;

    [Header("References")]
    [SerializeField] private HandController _hand;
    [SerializeField] private Transform _enemy;

    [SerializeField] private BattleConfig _battleConfig;

    // Current enemy
    private EnemyData _enemyData;
    private EnemyEffect _enemyEffect;
    private BattleContext _ctx;

    // State
    private int _attackCoins;
    private int _discardsLeft;
    private int _enemyHp;
    private int _playerHp;
    private Deck _deck;

    // Unity
    private void Update()
    {
        // Space for attack
        if (UnityEngine.InputSystem.Keyboard.current?.spaceKey.wasPressedThisFrame == true)
            Attack();
    }

    // Public Init
    public void StartBattle(Deck deck, EnemyData enemy)
    {
        _deck = deck;
        _enemyData = enemy;

        _playerHp = _battleConfig.PlayerMaxHp;
        _enemyHp = enemy.MaxHp;
        _attackCoins = enemy.AttackCoinsPerRound;
        _discardsLeft = _battleConfig.MaxDiscards;

        _ctx = new BattleContext
        {
            Hand = _hand,
            PlayerHp = _playerHp,
            EnemyDamage = enemy.AttackDamage,
            Discards = _discardsLeft,
            RequestUIRefresh = OnRefresh,
        };

        _enemyEffect = enemy.CreateEffect();
        _enemyEffect.OnBattleStart(_ctx);
        SyncFromContext();

        _hand.Init(_deck);

        OnRefreshAll?.Invoke();
    }

    // Attack enemy
    public void Attack()
    {
        StartCoroutine(AttackSequence());
    }

    private IEnumerator AttackSequence()
    {
        OnAnimationStarted?.Invoke();
        _hand.SetCardsInteractable(false);
        List<Card> selected = _hand.GetSelectedCards();
        if (selected.Count == 0 || _attackCoins <= 0) yield return null;

        ComboResult result = ComboEvaluator.Evaluate(selected);
        int damage = Mathf.RoundToInt(result.TotalDamage);

        // Fox: remove chip damage from blocked suit cards
        if (_ctx.BlockedDamageSuit.HasValue)
            damage = ApplyBlockedSuit(selected, damage);

        // Let the enemy effect modify damage (Scarab, Spider, etc.)
        damage = Mathf.Max(0, _enemyEffect.ModifyPlayerDamage(_ctx, result, damage));

        _enemyHp -= damage;
        _attackCoins -= 1;

        Debug.Log($"[Attack] {result} → Enemy HP: {_enemyHp}");

        _enemyEffect.OnPlayerAttack(_ctx, result);

        yield return _hand.AnimateAttack(_enemy.position);

        _hand.DiscardSelected();
        _hand.DrawUpToMax();
        _hand.SetCardsInteractable(true);

        // Attack player when attack coin turns to 0
        if (_attackCoins <= 0)
            EnemyAttack();

        SyncFromContext();
        OnRefresh?.Invoke();

        if (_enemyHp <= 0)
            Victory();
        OnAnimationStopped?.Invoke();
    }

    // Discard selected cards
    public void Discard()
    {
        StartCoroutine(DiscardSequence());
    }

    private IEnumerator DiscardSequence()
    {
        OnAnimationStarted?.Invoke();
        if (_discardsLeft <= 0) yield return null;

        List<Card> selected = _hand.GetSelectedCards();
        if (selected.Count == 0 || selected.Count > _battleConfig.MaxDiscardCards) yield return null;

        int cardCount = selected.Count;
        _discardsLeft--;
        _ctx.Discards = _discardsLeft;

        yield return _hand.AnimateDiscard();

        _hand.DiscardSelected();
        _hand.DrawUpToMax();

        _enemyEffect.OnPlayerDiscard(_ctx, cardCount);

        SyncFromContext();
        OnRefresh?.Invoke();
        OnAnimationStopped?.Invoke();
    }

    // Enemy attacks player
    private void EnemyAttack()
    {
        if (_enemyHp <= 0) return;

        _ctx.PlayerHp -= _ctx.EnemyDamage;
        _attackCoins = _enemyData.AttackCoinsPerRound;

        Debug.Log($"[Enemy Attack] -{_ctx.EnemyDamage} → Player HP: {_ctx.PlayerHp}");

        _enemyEffect.OnEnemyAttack(_ctx);
        SyncFromContext();

        if (_playerHp <= 0)
            GameOver();
    }

    private void Victory()
    {
        _enemyEffect.OnBattleEnd(_ctx);
        Debug.Log($"[Victory] {_enemyData.EnemyName} defeated! +{_enemyData.GoldReward} Gold");
        // TODO: award gold, advance map
    }

    private void GameOver()
    {
        Debug.Log("[Game Over] Player died!");
        // TODO: show game over screen
    }

    // Remove chip damage from cards in the blocked suit
    private int ApplyBlockedSuit(List<Card> cards, int baseDamage)
    {
        int blockedChips = 0;
        foreach (var card in cards)
            if (card.Suit == _ctx.BlockedDamageSuit.Value)
                blockedChips += card.NominalValue;
        return Mathf.Max(0, baseDamage - blockedChips);
    }

    // Write context values back to local state after effects may have changed them
    private void SyncFromContext()
    {
        _playerHp = _ctx.PlayerHp;
        _discardsLeft = _ctx.Discards;
    }

    public State GetCurrentState()
    {
        return new State
        {
            enemyData = _enemyData,
            enemyEffect = _enemyEffect,
            ctx = _ctx,

            attackCoins = _attackCoins,
            discardsLeft = _discardsLeft,
            enemyHp = _enemyHp,
            playerHp = _playerHp,
            deck = _deck,
        };
    }

    public struct State
    {
        public EnemyData enemyData;
        public EnemyEffect enemyEffect;
        public BattleContext ctx;

        public int attackCoins;
        public int discardsLeft;
        public int enemyHp;
        public int playerHp;
        public Deck deck;
    }
}
