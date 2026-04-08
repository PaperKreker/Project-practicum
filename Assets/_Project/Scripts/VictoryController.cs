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
            _summaryText.text = $"Забег завершён!\n"
                              + $"Здоровье: {run.PlayerHp} / {run.PlayerMaxHp}\n"
                              + $"Золото: {run.Gold}";
    }

    public void OnMainMenuClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
