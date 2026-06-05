using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    [SerializeField] private TMP_Text _summaryText;

    private void Start()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance == null || GameManager.Instance.Run == null)
            return;

        GameManager.Instance.DeleteSave();
        RunData run = GameManager.Instance.Run;

        if (_summaryText != null)
        {
            _summaryText.text = $"Вы умерли на акте {GameManager.Instance.CurrentActIndex + 1}\n"
                                + $"Золота собрано: {run.Gold}";
        }
    }

    public void OnRetryClicked()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            DifficultyLevel difficulty = GameManager.Instance.Run?.Difficulty ?? DifficultyLevel.Normal;
            GameManager.Instance.StartNewRun(difficulty, -1, false);
            return;
        }

        SceneManager.LoadScene("MainMenu");
    }

    public void OnMainMenuClicked()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMainMenu(false);
            return;
        }

        SceneManager.LoadScene("MainMenu");
    }
}
