using UnityEngine;

public class CardAudio : MonoBehaviour
{
    [SerializeField] private AnimatedButton _button;
    [SerializeField] private CardView _cardView;
    [SerializeField] private string _pickSound;
    [SerializeField] private string _hitSound;
    [SerializeField] private string _discardSound;

    private void OnEnable()
    {
        _button.OnClick.AddListener(PlayPickSound);
        _cardView.OnAttackHit += PlayHitSound;
        _cardView.OnDiscard += PlayDiscardSound;
    }

    private void OnDisable()
    {
        _button.OnClick.RemoveListener(PlayPickSound);
        _cardView.OnAttackHit -= PlayHitSound;
        _cardView.OnDiscard -= PlayDiscardSound;
    }

    private void PlayPickSound()
    {
        AudioManager.Instance.Play(_pickSound, Random.Range(0.8f, 1.2f));
    }

    private void PlayHitSound()
    {
        AudioManager.Instance.Play(_hitSound, Random.Range(0.8f, 1.2f));
    }

    private void PlayDiscardSound()
    {
        AudioManager.Instance.Play(_discardSound, Random.Range(0.8f, 1.2f));
    }
}
