using UnityEngine;

// Only for testing BattleController without going through the whole map and GameManager flow.
public class GameBootstrap : MonoBehaviour
{
    [Header("Test mode (без GameManager)")]
    [SerializeField] private BattleController _battleController;
    [SerializeField] private bool _testMode = false;

    private void Start()
    {
        if (!_testMode) return;

        if (_battleController == null)
            _battleController = FindFirstObjectByType<BattleController>();

        if (_battleController == null)
        {
            Debug.LogError("GameBootstrap: BattleController not found!");
            return;
        }

        var deck = new Deck();
        _battleController.StartBattle(deck, EnemyDatabase.Basilisk);
    }
}
