using UnityEngine;

// Создаёт колоду и запускает руку.
public class GameBootstrap : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandController _handController;

    [Header("Settings")]
    [SerializeField] private float _criticalChance = 0.1f;

    private Deck _deck;

    private void Start()
    {
        _deck = new Deck();
        _handController.Init(_deck);
    }
}