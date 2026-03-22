using UnityEngine;

[CreateAssetMenu(fileName = "BattleConfig", menuName = "Scriptable Objects/BattleConfig")]
public class BattleConfig : ScriptableObject
{
    [Header("Rules")]
    [field: SerializeField] public int AttackCoinsPerRound { get; private set; } = 3;
    [field: SerializeField] public int MaxDiscards { get; private set; } = 3;
    [field: SerializeField] public int MaxDiscardCards { get; private set; } = 5;

    [Header("Player Stats")]
    [field: SerializeField] public int PlayerMaxHp { get; private set; } = 100;
}
