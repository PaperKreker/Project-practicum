using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleController _battleController;
    [SerializeField] private BattleConfig _battleConfig;
    [SerializeField] private HandController _hand;
    [SerializeField] private Button _attackButton;
    [SerializeField] private Button _discardButton;

    [Header("UI Preview")]
    [SerializeField] private TextMeshProUGUI _comboNameText;
    [SerializeField] private TextMeshProUGUI _comboDamageText;
    [SerializeField] private TextMeshProUGUI _comboNominalText;

    [Header("UI Counters")]
    [SerializeField] private TextMeshProUGUI _attackCoinText;
    [SerializeField] private TextMeshProUGUI _discardsLeftText;

    [Header("UI Enemy")]
    [SerializeField] private Slider _enemyHpSlider;
    [SerializeField] private TextMeshProUGUI _enemyNameText;
    [SerializeField] private TextMeshProUGUI _enemyHpText;
    [SerializeField] private TextMeshProUGUI _enemyDamageText;
    [SerializeField] private TextMeshProUGUI _enemyEffectText;

    [Header("UI Player")]
    [SerializeField] private TextMeshProUGUI _playerHpText;

    private void OnEnable()
    {
        _attackButton.onClick.AddListener(_battleController.Attack);
        _discardButton.onClick.AddListener(_battleController.Discard);

        _hand.OnSelectionChanged += UpdateComboPreview;
        _hand.OnSelectionChanged += UpdateButtonStates;

        _battleController.OnRefresh += RefreshText;
        _battleController.OnRefreshAll += RefreshAll;
        _battleController.OnAnimationStarted += DisableGameplayButtons;
        _battleController.OnAnimationStopped += EnableGameplayButtons;
    }

    private void OnDisable()
    {
        _attackButton.onClick.RemoveListener(_battleController.Attack);
        _discardButton.onClick.RemoveListener(_battleController.Discard);

        _hand.OnSelectionChanged -= UpdateComboPreview;
        _hand.OnSelectionChanged -= UpdateButtonStates;

        _battleController.OnRefresh -= RefreshText;
        _battleController.OnRefreshAll -= RefreshAll;
        _battleController.OnAnimationStarted -= DisableGameplayButtons;
        _battleController.OnAnimationStopped -= EnableGameplayButtons;
    }

    private void UpdateComboPreview()
    {
        List<CardView> selectedViews = _hand.GetSelectedCardViews();

        if (selectedViews.Count == 0)
        {
            _comboNameText.text = "";
            _comboDamageText.text = "0";
            _comboNominalText.text = "0";
            return;
        }

        bool hasHidden = selectedViews.Any(v => v.IsFaceDown);

        // Apply fox debuff temporarily for preview
        BattleController.State battleState = _battleController.GetCurrentState();
        List<Card> visibleCards = selectedViews.Where(v => !v.IsFaceDown).Select(v => v.Data).ToList();

        if (battleState.ctx?.BlockedDamageSuit.HasValue == true)
        {
            foreach (var c in visibleCards)
                c.IsDebuffed = c.Suit == battleState.ctx.BlockedDamageSuit.Value;
        }

        // Evaluate with debuff applied
        ComboResult result = visibleCards.Count > 0
            ? ComboEvaluator.Evaluate(visibleCards)
            : new ComboResult { Type = ComboType.None };

        // Clear preview debuff flags
        foreach (var c in visibleCards)
            c.IsDebuffed = false;

        string suffix = hasHidden ? "???" : "";

        if (_comboNameText)
            _comboNameText.text = result.Type == ComboType.None
                ? (hasHidden ? "???" : "")
                : ComboDisplayName(result.Type) + suffix;

        _comboDamageText.text = result.Type == ComboType.None
            ? "0"
            : $"{Mathf.RoundToInt(result.BaseDamage)}{suffix}";

        _comboNominalText.text = result.Type == ComboType.None
            ? "0"
            : $"{Mathf.RoundToInt(result.NominalSum)}{suffix}";
    }

    private void UpdateButtonStates()
    {
        bool hasSelection = _hand.GetSelectedCards().Count > 0;
        BattleController.State battleState = _battleController.GetCurrentState();

        _attackButton.interactable = hasSelection && battleState.attackCoins > 0;

        _discardButton.interactable = hasSelection && battleState.discardsLeft > 0;
    }

    private void EnableGameplayButtons()
    {
        UpdateButtonStates();
    }
    private void DisableGameplayButtons()
    {
        _attackButton.interactable = false;
        _discardButton.interactable = false;
    }

    private void RefreshAll()
    {
        RefreshEnemyUI();
        UpdateComboPreview();
        UpdateButtonStates();
        RefreshText();
    }

    private void RefreshText()
    {
        BattleController.State battleState = _battleController.GetCurrentState();

        _attackCoinText.text = $"{battleState.attackCoins}";
        _discardsLeftText.text = $"{battleState.discardsLeft}/{_battleConfig.MaxDiscards}";
        _enemyHpText.text = $"{Mathf.Max(0, battleState.enemyHp)}/{battleState.enemyData?.MaxHp}";
        _enemyDamageText.text = $"{battleState.ctx?.EnemyDamage}";
        _playerHpText.text = $"{Mathf.Max(0, battleState.playerHp)}/{_battleConfig.PlayerMaxHp}";

        _enemyHpSlider.value = Mathf.Max(0.0f, battleState.enemyHp) / battleState.enemyData.MaxHp;
    }

    private void RefreshEnemyUI()
    {
        BattleController.State battleState = _battleController.GetCurrentState();

        if (battleState.enemyData == null)
            return;

        _enemyNameText.text = battleState.enemyData.EnemyName;
        _enemyEffectText.text = battleState.enemyEffect?.Description;
    }

    // Display name for combo types
    private static string ComboDisplayName(ComboType t) => t switch
    {
        ComboType.High => "Старшая карта",
        ComboType.Pair => "Пара",
        ComboType.TwoPair => "Две пары",
        ComboType.Set => "Сет",
        ComboType.FOK => "Каре",
        ComboType.Straight => "Стрит",
        ComboType.Flush => "Флеш",
        ComboType.FullHouse => "Фулл хаус",
        ComboType.StraightFlush => "Стрит флеш",
        ComboType.RoyalFlush => "Флеш рояль",
        _ => "?"
    };
}
