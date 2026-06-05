using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private GameObject _openButtonRoot;
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private Button _openButton;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _saveExitButton;
    [SerializeField] private Button _exitWithoutSaveButton;

    private bool _isOpen;
    private float _previousTimeScale = 1f;

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ResolveReferences();
        WireButtons();
        Close();
    }

    private void Update()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            Toggle();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnwireButtons();

        if (_isOpen)
            Time.timeScale = _previousTimeScale;
    }

    public void Toggle()
    {
        if (_isOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        if (_root == null)
            return;

        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        _root.SetActive(true);

        if (_openButtonRoot != null)
            _openButtonRoot.SetActive(false);

        _isOpen = true;

        if (_statusText != null)
            _statusText.text = string.Empty;
    }

    public void Close()
    {
        if (_root != null)
            _root.SetActive(false);

        if (_openButtonRoot != null)
            _openButtonRoot.SetActive(true);

        Time.timeScale = _previousTimeScale;
        _isOpen = false;
    }

    private void ResolveReferences()
    {
        if (_root == null)
            _root = FindChildObject("PauseMenu");

        if (_openButtonRoot == null)
            _openButtonRoot = FindChildObject("PauseOpenButton");

        if (_statusText == null)
            _statusText = FindChildComponent<TMP_Text>("StatusText");

        if (_openButton == null)
            _openButton = FindChildComponent<Button>("PauseOpenButton");

        if (_resumeButton == null)
            _resumeButton = FindChildComponent<Button>("ResumeButton");

        if (_saveButton == null)
            _saveButton = FindChildComponent<Button>("SaveButton");

        if (_saveExitButton == null)
            _saveExitButton = FindChildComponent<Button>("SaveExitButton");

        if (_exitWithoutSaveButton == null)
            _exitWithoutSaveButton = FindChildComponent<Button>("ExitButton");
    }

    private void WireButtons()
    {
        if (_openButton != null)
            _openButton.onClick.AddListener(Open);

        if (_resumeButton != null)
            _resumeButton.onClick.AddListener(Close);

        if (_saveButton != null)
            _saveButton.onClick.AddListener(OnSaveClicked);

        if (_saveExitButton != null)
            _saveExitButton.onClick.AddListener(OnSaveAndExitClicked);

        if (_exitWithoutSaveButton != null)
            _exitWithoutSaveButton.onClick.AddListener(OnExitWithoutSaveClicked);
    }

    private void UnwireButtons()
    {
        if (_openButton != null)
            _openButton.onClick.RemoveListener(Open);

        if (_resumeButton != null)
            _resumeButton.onClick.RemoveListener(Close);

        if (_saveButton != null)
            _saveButton.onClick.RemoveListener(OnSaveClicked);

        if (_saveExitButton != null)
            _saveExitButton.onClick.RemoveListener(OnSaveAndExitClicked);

        if (_exitWithoutSaveButton != null)
            _exitWithoutSaveButton.onClick.RemoveListener(OnExitWithoutSaveClicked);
    }

    private void OnSaveClicked()
    {
        bool saved = GameManager.Instance != null && GameManager.Instance.SaveRun();

        if (_statusText != null)
            _statusText.text = saved ? "Игра сохранена" : "Сейчас нельзя сохранить";
    }

    private void OnSaveAndExitClicked()
    {
        if (GameManager.Instance == null || !GameManager.Instance.SaveRun())
        {
            if (_statusText != null)
                _statusText.text = "Сейчас нельзя сохранить";
            return;
        }

        Time.timeScale = 1f;
        HideForSceneTransition();
        GameManager.Instance?.ReturnToMainMenu();
    }

    private void OnExitWithoutSaveClicked()
    {
        Time.timeScale = 1f;
        HideForSceneTransition();
        GameManager.Instance?.ReturnToMainMenu();
    }

    private void HideForSceneTransition()
    {
        if (_root != null)
            _root.SetActive(false);

        if (_openButtonRoot != null)
            _openButtonRoot.SetActive(false);

        _isOpen = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Map" || scene.name == "Battle" || scene.name == "Shop")
            return;

        Time.timeScale = 1f;
        Destroy(gameObject);
    }

    private GameObject FindChildObject(string objectName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == objectName)
                return child.gameObject;
        }

        return null;
    }

    private T FindChildComponent<T>(string objectName) where T : Component
    {
        GameObject child = FindChildObject(objectName);
        return child != null ? child.GetComponent<T>() : null;
    }
}
