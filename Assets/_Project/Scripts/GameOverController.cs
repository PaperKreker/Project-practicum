using UnityEngine;
using TMPro;

public class GameOverController : MonoBehaviour
{
    [SerializeField] private TMP_Text _summaryText;

    private void Start()
    {
        if (GameManager.Instance == null) return;
        var run = GameManager.Instance.Run;
        if (_summaryText)
            _summaryText.text = $"Вы умерли на акте {GameManager.Instance.CurrentActIndex + 1}\n"
                              + $"Золота собрано: {run.Gold}";
    }

    public void OnRetryClicked()
    {
        DifficultyLevel difficulty = GameManager.Instance?.Run?.Difficulty ?? DifficultyLevel.Normal;
        GameManager.Instance.StartNewRun(difficulty);
    }

    public void OnMainMenuClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu();
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
