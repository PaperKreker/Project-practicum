using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TMP_InputField _seedInput;
    [SerializeField] private SettingsMenuController _settingsMenu;
    [SerializeField] private Button _singleNewGameButton;
    [SerializeField] private Button _saveNewGameButton;
    [SerializeField] private Button _continueButton;

    private void Start()
    {
        Application.targetFrameRate = 120;

        ResolveMenuButtons();
        WireContinueButton();
        RefreshContinueButton();
    }

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

    public void OnContinueClicked()
    {
        GameManager.Instance?.LoadSavedRun();
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

    private void ResolveMenuButtons()
    {
        if (_singleNewGameButton == null)
            _singleNewGameButton = FindSceneButton("New game button");

        if (_saveNewGameButton == null)
            _saveNewGameButton = FindSceneButton("New game save button");

        if (_continueButton == null)
            _continueButton = FindSceneButton("Continue button");
    }

    private Button FindSceneButton(string objectName)
    {
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (Button button in buttons)
        {
            if (button.gameObject.name == objectName && button.gameObject.scene.IsValid())
                return button;
        }

        return null;
    }

    private void WireContinueButton()
    {
        if (_continueButton == null)
            return;

        _continueButton.onClick.RemoveListener(OnContinueClicked);
        _continueButton.onClick.AddListener(OnContinueClicked);
    }

    private void RefreshContinueButton()
    {
        bool hasSave = GameManager.Instance != null && GameManager.Instance.HasSave();

        if (_singleNewGameButton != null)
            _singleNewGameButton.gameObject.SetActive(!hasSave);

        if (_saveNewGameButton != null)
            _saveNewGameButton.gameObject.SetActive(hasSave);

        if (_continueButton == null)
            return;

        _continueButton.gameObject.SetActive(hasSave);
        _continueButton.interactable = hasSave;
    }
}
