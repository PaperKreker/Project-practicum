using UnityEngine;

public class TransitionProxy : MonoBehaviour
{
    [field: SerializeField] public Camera TargetCamera { get; private set; }
    [field: SerializeField] public GameObject IgnoreCanvas { get; private set; }
    [field: SerializeField] public GameObject TargetCanvas { get; private set; }
    public static TransitionProxy Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("Transition proxy should be only one!");
        }
        Instance = this;
    }
}
