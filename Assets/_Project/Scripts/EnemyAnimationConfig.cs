using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAnimationConfig", menuName = "Scriptable Objects/EnemyAnimationConfig")]
public class EnemyAnimationConfig : ScriptableObject
{
    [Header("Hit animation")]
    [field: SerializeField] public float AnimationDuration { get; private set; } = 0.5f;
    [field: SerializeField] public float ShakeAmplitude { get; private set; } = 2.0f;

    [Header("Attack animation")]
    [field: SerializeField] public float AttackDuration { get; private set; } = 1.5f;
    [field: SerializeField] public float AttackDelay { get; private set; } = 1.0f;
    [field: SerializeField] public float AttackSlashDelay { get; private set; } = 1.5f;
    [field: SerializeField] public AnimationCurve AttackCurve { get; private set; }

    [Header("Death animation")]
    [field: SerializeField] public Material DeathMaterial { get; private set; }
    [field: SerializeField] public float DeathScale { get; private set; } = 0.6f;
    [field: SerializeField] public float DeathDuration { get; private set; } = 1.0f;

    [Header("Idle animation")]
    [field: SerializeField] public Vector2 IdleAmplitude { get; private set; } = Vector2.one;
    [field: SerializeField] public float IdleAnimationSpeed { get; private set; } = 1.0f;
    [field: SerializeField] public float IdleAnimationSmooth { get; private set; } = 1.0f;
    [field: SerializeField] public AnimationCurve IdleCurve { get; private set; }
}
