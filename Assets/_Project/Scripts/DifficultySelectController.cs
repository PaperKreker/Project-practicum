using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DifficultySelectController : MonoBehaviour
{
    [SerializeField] private RectTransform _panel;
    [SerializeField] private Button _normalButton;
    [SerializeField] private Button _hardButton;
    [SerializeField] private Button _demonButton;
    [SerializeField] private Button _backButton;

    private void Start()
    {
        _normalButton.onClick.AddListener(() => OnDifficultySelected(DifficultyLevel.Normal));
        _hardButton.onClick.AddListener(() => OnDifficultySelected(DifficultyLevel.Hard));
        _demonButton.onClick.AddListener(() => OnDifficultySelected(DifficultyLevel.Demon));

        _backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnDifficultySelected(DifficultyLevel level)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StartNewRun(level);
    }

    private void OnBackClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
            return;
        }

        SceneManager.LoadScene("MainMenu");
    }
}
