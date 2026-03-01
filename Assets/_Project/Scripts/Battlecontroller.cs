using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandController _hand;
    [SerializeField] private Button _attackButton;
    [SerializeField] private Button _discardButton;

    [Header("UI — Preview")]
    [SerializeField] private TextMeshProUGUI _comboNameText;
    [SerializeField] private TextMeshProUGUI _comboDamageText;

    [Header("UI — Counters")]
    [SerializeField] private TextMeshProUGUI _attackCoinText;
    [SerializeField] private TextMeshProUGUI _discardsLeftText;

    [Header("UI — Enemy")]
    [SerializeField] private TextMeshProUGUI _enemyNameText;
    [SerializeField] private TextMeshProUGUI _enemyHpText;
    [SerializeField] private TextMeshProUGUI _enemyDamageText;
    [SerializeField] private TextMeshProUGUI _enemyEffectText;

    [Header("UI — Player")]
    [SerializeField] private TextMeshProUGUI _playerHpText;

    [Header("Rules")]
    [SerializeField] private int _attackCoinsPerRound = 3;
    [SerializeField] private int _maxDiscards = 3;
    [SerializeField] private int _maxDiscardCards = 5;

    [Header("Player Stats")]
    [SerializeField] private int _playerMaxHp = 100;

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
    private void Start()
    {
        _attackButton.onClick.AddListener(OnAttack);
        _discardButton.onClick.AddListener(OnDiscard);

        _hand.OnSelectionChanged += UpdateComboPreview;
        _hand.OnSelectionChanged += UpdateButtonStates;
    }

    private void OnDestroy()
    {
        _hand.OnSelectionChanged -= UpdateComboPreview;
        _hand.OnSelectionChanged -= UpdateButtonStates;
    }

    private void Update()
    {
        // Space for attack
        if (UnityEngine.InputSystem.Keyboard.current?.spaceKey.wasPressedThisFrame == true)
            OnAttack();
    }

    // Public Init
    public void StartBattle(Deck deck, EnemyData enemy)
    {
        _deck = deck;
        _enemyData = enemy;

        _playerHp = _playerMaxHp;
        _enemyHp = enemy.MaxHp;
        _attackCoins = enemy.AttackCoinsPerRound;
        _discardsLeft = _maxDiscards;

        _ctx = new BattleContext
        {
            Hand = _hand,
            PlayerHp = _playerHp,
            EnemyDamage = enemy.AttackDamage,
            Discards = _discardsLeft,
            RequestUIRefresh = RefreshAllUI,
        };

        _enemyEffect = enemy.CreateEffect();
        _enemyEffect.OnBattleStart(_ctx);
        SyncFromContext();

        _hand.Init(_deck);
        RefreshAllUI();
        RefreshEnemyUI();
        UpdateComboPreview();
        UpdateButtonStates();
    }

    // Attack enemy
    private void OnAttack()
    {
        List<Card> selected = _hand.GetSelectedCards();
        if (selected.Count == 0 || _attackCoins <= 0) return;

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

        _hand.DiscardSelected();
        _hand.DrawUpToMax();

        // Attack player when attack coin turns to 0
        if (_attackCoins <= 0)
            EnemyAttack();

        SyncFromContext();
        RefreshAllUI();

        if (_enemyHp <= 0)
            OnVictory();
    }

    // Discard selected cards
    private void OnDiscard()
    {
        if (_discardsLeft <= 0) return;

        List<Card> selected = _hand.GetSelectedCards();
        if (selected.Count == 0 || selected.Count > _maxDiscardCards) return;

        int cardCount = selected.Count;
        _discardsLeft--;
        _ctx.Discards = _discardsLeft;

        _hand.DiscardSelected();
        _hand.DrawUpToMax();

        _enemyEffect.OnPlayerDiscard(_ctx, cardCount);

        SyncFromContext();
        RefreshAllUI();
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
            OnGameOver();
    }

    private void OnVictory()
    {
        _enemyEffect.OnBattleEnd(_ctx);
        Debug.Log($"[Victory] {_enemyData.EnemyName} defeated! +{_enemyData.GoldReward} Gold");
        // TODO: award gold, advance map
    }

    private void OnGameOver()
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

    // UI
    private void UpdateComboPreview()
    {
        List<Card> selected = _hand.GetSelectedCards();

        if (selected.Count == 0)
        {
            if (_comboNameText) _comboNameText.text = "—";
            if (_comboDamageText) _comboDamageText.text = "";
            return;
        }

        ComboResult result = ComboEvaluator.Evaluate(selected);

        if (_comboNameText)
            _comboNameText.text = ComboDisplayName(result.Type);

        if (_comboDamageText)
            _comboDamageText.text = result.Type == ComboType.None
                ? "NaN"
                : $"{Mathf.RoundToInt(result.TotalDamage)}";
    }

    private void UpdateButtonStates()
    {
        bool hasSelection = _hand.GetSelectedCards().Count > 0;

        if (_attackButton)
            _attackButton.interactable = hasSelection && _attackCoins > 0;

        if (_discardButton)
            _discardButton.interactable = hasSelection && _discardsLeft > 0;
    }

    private void RefreshAllUI()
    {
        if (_attackCoinText) _attackCoinText.text = $"{_attackCoins}";
        if (_discardsLeftText) _discardsLeftText.text = $"{_discardsLeft}/{_maxDiscards}";
        if (_enemyHpText) _enemyHpText.text = $"{Mathf.Max(0, _enemyHp)}/{_enemyData?.MaxHp}";
        if (_enemyDamageText) _enemyDamageText.text = $"{_ctx?.EnemyDamage}";
        if (_playerHpText) _playerHpText.text = $"{Mathf.Max(0, _playerHp)}/{_playerMaxHp}";
    }

    private void RefreshEnemyUI()
    {
        if (_enemyData == null) return;
        if (_enemyNameText) _enemyNameText.text = _enemyData.EnemyName;
        if (_enemyEffectText) _enemyEffectText.text = _enemyEffect?.Description;
    }

    // Display name for combo types
    private static string ComboDisplayName(ComboType t) => t switch
    {
        ComboType.High => "High",
        ComboType.Pair => "Pair",
        ComboType.TwoPair => "Pair Set",
        ComboType.Set => "Set",
        ComboType.FOK => "FOK",
        ComboType.Straight => "Straight",
        ComboType.Flush => "Flush",
        ComboType.FullHouse => "Full House",
        ComboType.StraightFlush => "Straight Flush",
        ComboType.RoyalFlush => "ROYAL FLUSH",
        _ => "—"
    };
}
