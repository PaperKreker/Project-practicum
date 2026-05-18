using UnityEngine;

public class TutorialBattleHook : MonoBehaviour
{
    [Header("Tutorial")]
    [SerializeField] private TutorialPopup _tutorialPopup;
    [SerializeField] private BattleController _battleController;
    [SerializeField] private BattleView _battleView;

    private void Awake()
    {
        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsTutorialActive)
        {
            enabled = false;
            return;
        }

        if (_battleController == null)
            _battleController = GetComponent<BattleController>();
        if (_battleView == null)
            _battleView = FindFirstObjectByType<BattleView>();
    }

    private void Start()
    {
        if (!enabled) return;

        _battleController.OnRefreshAll += NotifyAttackIfNeeded;
        _battleController.StartBattle(new Deck(), TutorialManager.TutorialEnemy,
            currentHp: 100, maxHp: 100);

        TutorialManager.Instance.RegisterBattleScene(_battleController, _battleView, _tutorialPopup);
    }

    private int _lastAttackCoins = -1;

    private void NotifyAttackIfNeeded()
    {
        var state = _battleController.GetCurrentState();
        if (_lastAttackCoins < 0) { _lastAttackCoins = state.attackCoins; return; }
        if (state.attackCoins < _lastAttackCoins)
            TutorialManager.Instance?.NotifyAttackExecuted();
        _lastAttackCoins = state.attackCoins;
    }

    private void OnDestroy()
    {
        if (_battleController != null)
            _battleController.OnRefreshAll -= NotifyAttackIfNeeded;
    }
}
