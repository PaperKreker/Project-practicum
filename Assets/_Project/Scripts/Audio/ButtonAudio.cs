using UnityEngine;
using UnityEngine.UI;

public class ButtonAudio : MonoBehaviour
{
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(PlayButtonSound);
    }

    private void OnDestroy()
    {
        _button.onClick.RemoveListener(PlayButtonSound);
    }

    private void PlayButtonSound()
    {
        AudioManager.Instance.Play("button_click", Random.Range(0.9f, 1.1f));
    }
}
