using UnityEngine;
using TMPro;

public class VictoryController : MonoBehaviour
{
    [SerializeField] private TMP_Text _summaryText;

    private void Start()
    {
        if (GameManager.Instance == null) return;
        var run = GameManager.Instance.Run;
        if (_summaryText)
            _summaryText.text = $"Run complete!\n"
                              + $"Final HP: {run.PlayerHp} / {run.PlayerMaxHp}\n"
                              + $"Gold: {run.Gold}";
    }

    public void OnMainMenuClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
