using UnityEngine;

public class TransitionAudio : MonoBehaviour
{
    [SerializeField] private TransitionManager _manager;
    [SerializeField] private string _transitionSound;

    private void OnEnable()
    {
        _manager.OnTransitionStart += PlaySound;
    }

    private void OnDisable()
    {
        _manager.OnTransitionStart -= PlaySound;
    }

    private void PlaySound()
    {
        AudioManager.Instance.Play(_transitionSound, Random.Range(0.9f, 1.1f));
    }
}
