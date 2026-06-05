using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryController : MonoBehaviour
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
            _summaryText.text = $"Забег завершён!\n"
                                + $"Здоровье: {run.PlayerHp} / {run.PlayerMaxHp}\n"
                                + $"Золото: {run.Gold}";
        }
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
