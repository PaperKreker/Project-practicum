using UnityEngine;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TMP_InputField _seedInput;
    [SerializeField] private SettingsMenuController _settingsMenu;

    public void OnNewGameClicked()
    {
        OpenDifficultySelection();
    }

    public void OnNewGameHard()
    {
        OpenDifficultySelection();
    }

    public void OnNewGameDemon()
    {
        OpenDifficultySelection();
    }

    public void OnTutorialClicked()
    {
        if (TutorialManager.Instance == null)
        {
            GameObject go = new GameObject("TutorialManager");
            go.AddComponent<TutorialManager>();
        }

        TutorialManager.Instance.StartTutorial();
    }

    public void OnSettingsClicked()
    {
        if (_settingsMenu != null)
            _settingsMenu.OpenSettings();
        else
            Debug.LogWarning("MainMenuController: SettingsMenuController не назначен в Inspector.");
    }

    private int ParseSeed()
    {
        if (_seedInput == null) return -1;
        string text = _seedInput.text.Trim();
        if (string.IsNullOrEmpty(text)) return -1;
        return int.TryParse(text, out int seed) ? Mathf.Abs(seed) : Mathf.Abs(text.GetHashCode());
    }

    private void OpenDifficultySelection()
    {
        GameManager.Instance.OpenDifficultySelection(ParseSeed());
    }
}
