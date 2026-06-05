using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleController : MonoBehaviour
{
    public event Action OnAnimationStarted;
    public event Action OnAnimationStopped;
    public event Action<bool> OnBattleEnd;
    public event Action OnEnemyHit;
    public event Action OnEnemyLastHit;
    public event Action OnEnemyAttack;
    public event Action<int> OnEnemyAttackFinish;
    public event Action OnRefreshAll;
    public event Action OnRefresh;

    [Header("References")]
    [SerializeField] private HandController _hand;
    [SerializeField] private BattleConfig _battleConfig;
    [SerializeField] private Transform _enemy;

    private EnemyData _enemyData;
    private EnemyEffect _enemyEffect;
    private BattleContext _ctx;
    private List<Sigil> _sigils;
    private List<Coroutine> _animationsWait = new();
    private DifficultyModifiers _difficultyModifiers;

    private int _attackCoins;
    private int _discardsLeft;
    private int _enemyHp;
    private Deck _deck;
    private bool _battleOver;
    private bool _hasPlayerAttackedInTutorial = false;
    private bool _isResolvingAction;

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

    public SaveSystem.BattleSaveData CreateSaveData()
    {
        if (_isResolvingAction)
            return null;

        SaveSystem.BattleSaveData save = new SaveSystem.BattleSaveData
        {
            AttackCoins = _attackCoins,
            DiscardsLeft = _discardsLeft,
            EnemyHp = _enemyHp,
            PlayerHp = _ctx.PlayerHp,
            EnemyDamage = _ctx.EnemyDamage,
            BattleOver = _battleOver,
            HasPlayerAttackedInTutorial = _hasPlayerAttackedInTutorial,
            EnemyName = _enemyData.EnemyName,
            BlockedDamageSuits = _ctx.BlockedDamageSuits != null ? new List<Suit>(_ctx.BlockedDamageSuits) : new List<Suit>(),
            EnemyEffect = _enemyEffect.CreateSaveData(),
        };

        foreach (Card card in _deck.GetCards())
            save.DeckCards.Add(SaveSystem.CardSaveData.FromCard(card));

        foreach (CardView view in _hand.GetCardViews())
            save.HandCards.Add(SaveSystem.CardViewSaveData.FromView(view));

        foreach (Sigil sigil in _sigils)
            save.Sigils.Add(sigil.CreateSaveData());

        return save;
    }

    private void Start()
    {
        // В режиме обучения запуск боя берёт на себя TutorialBattleHook
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[BattleController] No GameManager — using test defaults.");
            StartBattle(new Deck(), EnemyDatabase.Basilisk);
            return;
        }

        var run = GameManager.Instance.Run;
        var enemy = GameManager.Instance.GetCurrentEnemy();
        Debug.Log($"[BattleController] Starting battle. PlayerHp={run.PlayerHp}, Enemy={enemy.EnemyName}");
        SaveSystem.BattleSaveData save = GameManager.Instance.ConsumePendingBattleSave();
        if (save != null)
            RestoreBattle(save, enemy, run.PlayerMaxHp);
        else
            StartBattle(new Deck(), enemy, run.PlayerHp, run.PlayerMaxHp);
    }

    private void Update()
    {
        // Space for attack
        if (UnityEngine.InputSystem.Keyboard.current?.spaceKey.wasPressedThisFrame == true)
            Attack();
    }

    // Public Init
    public void StartBattle(Deck deck, EnemyData enemy, int currentHp = -1, int maxHp = -1)
    {
        _battleOver = false;
        _deck = deck;
        _enemyData = enemy;
        _enemyHp = enemy.MaxHp;
        _attackCoins = enemy.AttackCoinsPerRound;
        _discardsLeft = _battleConfig != null ? _battleConfig.MaxDiscards : 3;
        _sigils = GameManager.Instance?.Run?.ActiveSigils ?? new List<Sigil>();
        var difficulty = GameManager.Instance?.Run != null ? GameManager.Instance.Run.Difficulty : enemy.DifficultyLevel;
        _difficultyModifiers = GameBalance.GetDifficulty(difficulty);

        int resolvedMaxHp = maxHp > 0 ? maxHp
            : _battleConfig != null ? _battleConfig.PlayerMaxHp : 100;
        int resolvedHp = currentHp > 0 ? currentHp : resolvedMaxHp;

        _ctx = new BattleContext
        {
            Hand = _hand,
            PlayerHp = resolvedHp,
            PlayerMaxHp = resolvedMaxHp,
            EnemyDamage = enemy.AttackDamage,
            Discards = _discardsLeft,
            BlockedDamageSuits = new List<Suit>(),
            RequestUIRefresh = () => OnRefresh?.Invoke(),
        };

        _enemyEffect = enemy.CreateEffect();
        _enemyEffect.OnBattleStart(_ctx);

        foreach (var s in _sigils)
            s.OnBattleStart(_ctx);

        // Sync back in case effects/sigils modified discards
        _discardsLeft = _ctx.Discards;

        _hand.Init(_deck);
        OnRefreshAll?.Invoke();
    }

    public void RestoreBattle(SaveSystem.BattleSaveData save, EnemyData enemy, int maxHp)
    {
        _battleOver = save.BattleOver;
        _isResolvingAction = false;
        _deck = new Deck(false);
        List<Card> deckCards = new List<Card>();
        foreach (SaveSystem.CardSaveData cardSave in save.DeckCards)
            deckCards.Add(cardSave.ToCard());
        _deck.SetCards(deckCards);

        _enemyData = enemy;
        _enemyHp = save.EnemyHp;
        _attackCoins = save.AttackCoins;
        _discardsLeft = save.DiscardsLeft;
        _hasPlayerAttackedInTutorial = save.HasPlayerAttackedInTutorial;
        _sigils = GameManager.Instance?.Run?.ActiveSigils ?? new List<Sigil>();
        var difficulty = GameManager.Instance?.Run != null ? GameManager.Instance.Run.Difficulty : enemy.DifficultyLevel;
        _difficultyModifiers = GameBalance.GetDifficulty(difficulty);

        _ctx = new BattleContext
        {
            Hand = _hand,
            PlayerHp = save.PlayerHp,
            PlayerMaxHp = maxHp,
            EnemyDamage = save.EnemyDamage,
            Discards = save.DiscardsLeft,
            BlockedDamageSuits = new List<Suit>(save.BlockedDamageSuits),
            RequestUIRefresh = () => OnRefresh?.Invoke(),
        };

        _enemyEffect = enemy.CreateEffect();
        if (_enemyEffect is FaceDownCards)
            _enemyEffect.OnBattleStart(_ctx);

        foreach (Sigil sigil in _sigils)
        {
            sigil.OnBattleStart(_ctx);
        }

        _ctx.PlayerHp = save.PlayerHp;
        _ctx.EnemyDamage = save.EnemyDamage;
        _ctx.Discards = save.DiscardsLeft;
        _ctx.BlockedDamageSuits = new List<Suit>(save.BlockedDamageSuits);
        _discardsLeft = save.DiscardsLeft;

        _hand.Restore(_deck, save.HandCards);
        _enemyEffect.RestoreSaveData(save.EnemyEffect, _ctx);

        foreach (Sigil sigil in _sigils)
        {
            SaveSystem.SigilSaveData sigilSave = save.Sigils.Find(s => s.Name == sigil.Name);
            sigil.RestoreSaveData(sigilSave, _ctx);
        }

        OnRefreshAll?.Invoke();
    }

    // Attack enemy
    public void Attack()
    {
        StartCoroutine(AttackSequence());
    }

    private IEnumerator AttackSequence()
    {
        _isResolvingAction = true;
        OnAnimationStarted?.Invoke();
        _hand.SetCardsInteractable(false);

        if (_battleOver)
        {
            _isResolvingAction = false;
            yield break;
        }
        List<Card> selected = _hand.GetSelectedCards();
        if (selected.Count == 0 || _attackCoins <= 0)
        {
            _hand.SetCardsInteractable(true);
            OnAnimationStopped?.Invoke();
            _isResolvingAction = false;
            yield break;
        }

        // Apply debuffs before evaluating combo
        if (_ctx.BlockedDamageSuits != null && _ctx.BlockedDamageSuits.Count > 0)
        {
            foreach (var c in selected)
                c.IsDebuffed = _ctx.BlockedDamageSuits.Contains(c.Suit);
        }

        ComboResult result = ComboEvaluator.Evaluate(selected);
        int damage = Mathf.RoundToInt(result.TotalDamage);

        // Enemy modifies damage first
        damage = Mathf.Max(0, _enemyEffect.ModifyPlayerDamage(_ctx, result, damage));

        // If spider blocks the combo, skip all damage including sigils
        bool attackBlocked = damage == 0 && result.Type != ComboType.None
            && _enemyEffect is NoRepeatCombo spider && spider.IsRepeatBlocked(result);

        if (!attackBlocked)
        {
            // Sigils: flat bonus then multiplier
            int bonus = 0;
            float mult = 1f;
            foreach (var s in _sigils)
            {
                bonus += s.BonusDamage(_ctx, result);
                mult += s.BonusMultiplier(_ctx, result);
            }
            damage = Mathf.RoundToInt((damage + bonus) * mult * _difficultyModifiers.PlayerDamageMultiplier);
        }

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
        {
            if (!_hasPlayerAttackedInTutorial)
            {
                _hasPlayerAttackedInTutorial = true;

                if (damage >= _enemyHp)
                {
                    damage = Mathf.Max(0, _enemyHp - 1);
                }
            }
        }

        _enemyHp -= damage;

        _enemyEffect.OnPlayerAttack(_ctx, result);
        foreach (var s in _sigils)
            s.OnPlayerAttack(_ctx, result);

        yield return _hand.AnimateAttack(_enemy.position, OnEnemyHit);

        FloatingTextController.Instance.ShowText($"-{damage}", _enemy.position);

        _attackCoins--;
        _hand.DiscardSelected();
        _hand.DrawUpToMax();
        _hand.SetCardsInteractable(true);

        OnEnemyLastHit?.Invoke();

        // Check victory before enemy gets a turn — dead enemies don't attack
        if (VictoryChecker.IsBattleWon(_enemyHp))
        {
            OnRefreshAll?.Invoke();
            OnAnimationStopped?.Invoke();
            _isResolvingAction = false;
            EndBattle(playerWon: true);
            yield break;
        }

        if (!_hand.CanAttack())
        {
            yield return AttackPlayerWithoutCards();
        }
        else if (_attackCoins <= 0)
        {
            yield return EnemyTakeTurn();
        }    

        OnRefreshAll?.Invoke();
        OnAnimationStopped?.Invoke();
        _isResolvingAction = false;
    }

    // Discard selected cards
    public void Discard()
    {
        StartCoroutine(DiscardSequence());
    }

    private IEnumerator DiscardSequence()
    {
        _isResolvingAction = true;
        OnAnimationStarted?.Invoke();

        if (_battleOver)
        {
            _isResolvingAction = false;
            yield break;
        }
        if (_discardsLeft <= 0)
        {
            _isResolvingAction = false;
            yield break;
        }

        int count = _hand.GetSelectedCards().Count;
        if (count == 0)
        {
            OnAnimationStopped?.Invoke();
            _isResolvingAction = false;
            yield break;
        }

        _enemyEffect.OnPlayerDiscard(_ctx, count);
        foreach (var s in _sigils)
            s.OnPlayerDiscard(_ctx, count);

        _discardsLeft = _ctx.Discards;

        yield return _hand.AnimateDiscard();

        _hand.DiscardSelected();
        _hand.DrawUpToMax();

        _discardsLeft--;
        _ctx.Discards = _discardsLeft;

        if (!_hand.CanAttack())
        {
            yield return AttackPlayerWithoutCards();
        }

        OnRefreshAll?.Invoke();
        OnAnimationStopped?.Invoke();
        _isResolvingAction = false;
    }

    // No cards attack
    private IEnumerator AttackPlayerWithoutCards()
    {
        while (!VictoryChecker.IsGameOver(_ctx.PlayerHp))
        {
            yield return EnemyTakeTurn();
            OnRefreshAll?.Invoke();
        }
    }

    public void AddAnimationToWait(Coroutine animation)
    {
        _animationsWait.Add(animation);
    }

    private IEnumerator WaitForAnimations()
    {
        yield return CoroutineUtils.WhenAll(this, _animationsWait);
        _animationsWait.Clear();
    }

    private IEnumerator EnemyTakeTurn()
    {
        _ctx.PlayerHp -= _ctx.EnemyDamage;

        _enemyEffect.OnEnemyAttack(_ctx);
        foreach (var s in _sigils)
            s.OnEnemyAttack(_ctx);

        _attackCoins = _enemyData.AttackCoinsPerRound;

        OnEnemyAttack?.Invoke();
        yield return WaitForAnimations();
        OnEnemyAttackFinish?.Invoke(_ctx.EnemyDamage);

        if (VictoryChecker.IsGameOver(_ctx.PlayerHp))
            EndBattle(playerWon: false);
    }

    private void EndBattle(bool playerWon)
    {
        StartCoroutine(EndBattleSequence(playerWon));
    }

    private IEnumerator EndBattleSequence(bool playerWon)
    {
        if (_battleOver) yield break;
        _battleOver = true;

        _enemyEffect.OnBattleEnd(_ctx);
        foreach (var s in _sigils)
            s.OnBattleEnd(_ctx);

        OnBattleEnd?.Invoke(playerWon);
        yield return WaitForAnimations();

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
        {
            // В туториале GameManager.OnBattleEnded не вызываем — TutorialManager сам обрабатывает
            Debug.Log($"[BattleController] Tutorial battle ended. PlayerWon={playerWon}");
        }
        else if (playerWon)
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
}
