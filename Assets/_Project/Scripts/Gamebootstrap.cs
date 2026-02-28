using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BattleController _battleController;

    private Deck _deck;

    private void Start()
    {
        if (_battleController == null)
            _battleController = FindObjectOfType<BattleController>();

        if (_battleController == null)
        {
            Debug.LogError("GameBootstrap: BattleController not found!");
            return;
        }

        _deck = new Deck();
        _battleController.StartBattle(_deck);
    }
}
