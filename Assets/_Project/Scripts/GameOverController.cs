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
            _summaryText.text = $"You fell at Act {GameManager.Instance.CurrentActIndex + 1}\n"
                              + $"Gold collected: {run.Gold}";
    }

    public void OnRetryClicked()
    {
        GameManager.Instance.StartNewRun();
    }

    public void OnMainMenuClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
