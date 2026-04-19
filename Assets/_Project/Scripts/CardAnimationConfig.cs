using UnityEngine;

[CreateAssetMenu(fileName = "CardAnimationConfig", menuName = "Scriptable Objects/CardAnimationConfig")]
public class CardAnimationConfig : ScriptableObject
{
    [Header("Hover")]
    [field: SerializeField] public AnimationCurve HoverCurve { get; private set; }
    [field: SerializeField] public float HoverLift { get; private set; } = 30f;
    [field: SerializeField] public float HoverTime { get; private set; } = 0.1f;
    [Header("Attack")]
    [field: SerializeField] public AnimationCurve AttackCurve { get; private set; }
    [field: SerializeField] public float AttackTime { get; private set; } = 0.1f;
    [Header("Discard")]
    [field: SerializeField] public float DiscardLifetime { get; private set; } = 1.0f;
    [field: SerializeField] public float DiscardXVelocity { get; private set; } = 4.0f;
    [field: SerializeField] public float DiscardYVelocity { get; private set; } = 4.0f;
    [field: SerializeField] public float DiscardGravitation { get; private set; } = -4.0f;
    [field: SerializeField] public float DiscardRotation { get; private set; } = 45.0f;
    [Header("Idle")]
    [field: SerializeField] public Vector2 IdleAmplitude { get; private set; } = Vector2.one;
    [field: SerializeField] public float IdleAnimationSpeed { get; private set; } = 1.0f;
    [field: SerializeField] public AnimationCurve IdleCurve { get; private set; }
}
