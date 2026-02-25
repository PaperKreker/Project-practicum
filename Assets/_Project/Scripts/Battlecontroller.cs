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

    [Header("Enemy (заглушка)")]
    [SerializeField] private TextMeshProUGUI _enemyHpText;
    [SerializeField] private TextMeshProUGUI _enemyDamageText;

    [Header("Player")]
    [SerializeField] private TextMeshProUGUI _playerHpText;

    [Header("Rules")]
    [SerializeField] private int _attackCoinsPerRound = 3;
    [SerializeField] private int _maxDiscards = 3;
    [SerializeField] private int _maxDiscardCards = 5;

    [Header("Enemy Stats")]
    [SerializeField] private int _enemyMaxHp = 100;
    [SerializeField] private int _enemyDamage = 15;

    [Header("Player Stats")]
    [SerializeField] private int _playerMaxHp = 100;

    // Состояние
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
    }

    private void Update()
    {
        // Обновляем превью комбо каждый кадр
        UpdateComboPreview();
        UpdateButtonStates();

        // Пробел = атака
        if (UnityEngine.InputSystem.Keyboard.current?.spaceKey.wasPressedThisFrame == true)
            OnAttack();
    }

    // Public Init
    public void StartBattle(Deck deck)
    {
        _deck = deck;

        _enemyHp = _enemyMaxHp;
        _playerHp = _playerMaxHp;
        _attackCoins = _attackCoinsPerRound;
        _discardsLeft = _maxDiscards;

        _hand.Init(_deck);
        RefreshAllUI();
    }

    // Атака
    private void OnAttack()
    {
        List<Card> selected = _hand.GetSelectedCards();
        if (selected.Count == 0) return;
        if (_attackCoins <= 0) return;

        ComboResult result = ComboEvaluator.Evaluate(selected);
        int damage = Mathf.RoundToInt(result.TotalDamage);

        _enemyHp -= damage;
        _attackCoins -= 1;

        Debug.Log($"[Attack] {result} → Enemy HP: {_enemyHp}");

        _hand.DiscardSelected();
        _hand.DrawUpToMax();

        // Враг отвечает когда Attack Coin = 0
        if (_attackCoins <= 0)
            EnemyAttack();

        RefreshAllUI();
    }

    // Сброс
    private void OnDiscard()
    {
        if (_discardsLeft <= 0) return;

        List<Card> selected = _hand.GetSelectedCards();
        if (selected.Count == 0 || selected.Count > _maxDiscardCards) return;

        _discardsLeft--;

        _hand.DiscardSelected();
        _hand.DrawUpToMax();

        RefreshAllUI();
    }

    // Ответная атака врага
    private void EnemyAttack()
    {
        if (_enemyHp <= 0) return;

        _playerHp -= _enemyDamage;
        _attackCoins = _attackCoinsPerRound;

        Debug.Log($"[Enemy Attack] -{_enemyDamage} → Player HP: {_playerHp}");

        if (_playerHp <= 0)
            Debug.Log("[Game Over] Player died!");

        if (_enemyHp <= 0)
        {
            Debug.Log("[Victory] Enemy defeated!");
            _discardsLeft = _maxDiscards;
        }
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
        if (_attackCoinText) _attackCoinText.text = $"Атаки: {_attackCoins}";
        if (_discardsLeftText) _discardsLeftText.text = $"Сбросы: {_discardsLeft}/{_maxDiscards}";
        if (_enemyHpText) _enemyHpText.text = $"HP: {Mathf.Max(0, _enemyHp)}/{_enemyMaxHp}";
        if (_enemyDamageText) _enemyDamageText.text = $"Урон: {_enemyDamage}";
        if (_playerHpText) _playerHpText.text = $"HP: {Mathf.Max(0, _playerHp)}/{_playerMaxHp}";
    }

    // Helpers
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