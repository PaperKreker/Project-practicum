using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsMenuController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Volume Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;

    [Header("Screen")]
    [SerializeField] private TMP_Dropdown screenModeDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    private Resolution[] _resolutions;


    private void Start()
    {
        BuildResolutionDropdown();
        BuildScreenModeDropdown();
        LoadSettings();

        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        screenModeDropdown.onValueChanged.AddListener(OnScreenModeChanged);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        settingsPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }


    private void LoadSettings()
    {
        masterVolumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
        sfxVolumeSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
        musicVolumeSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
        screenModeDropdown.SetValueWithoutNotify(GameSettings.ScreenMode);

        int savedRes = GameSettings.ResolutionIndex;
        if (savedRes >= 0 && savedRes < _resolutions.Length)
            resolutionDropdown.SetValueWithoutNotify(savedRes);
        else
            resolutionDropdown.SetValueWithoutNotify(GetCurrentResolutionIndex());
    }

    private void BuildResolutionDropdown()
    {
        var seen = new HashSet<string>();
        var unique = new List<Resolution>();

        foreach (var r in Screen.resolutions)
        {
            string key = $"{r.width}x{r.height}";
            if (seen.Add(key))
                unique.Add(r);
        }

        _resolutions = unique.ToArray();

        resolutionDropdown.ClearOptions();
        var options = new List<TMP_Dropdown.OptionData>();
        foreach (var r in _resolutions)
            options.Add(new TMP_Dropdown.OptionData($"{r.width} × {r.height}"));

        resolutionDropdown.AddOptions(options);
    }

    private void BuildScreenModeDropdown()
    {
        screenModeDropdown.ClearOptions();
        screenModeDropdown.AddOptions(new List<string>
        {
            "Оконный",
            "Без рамки",
            "Полный экран"
        });
    }

    private int GetCurrentResolutionIndex()
    {
        for (int i = 0; i < _resolutions.Length; i++)
        {
            if (_resolutions[i].width == Screen.currentResolution.width &&
                _resolutions[i].height == Screen.currentResolution.height)
                return i;
        }
        return _resolutions.Length - 1;
    }

    private void OnMasterVolumeChanged(float value)
    {
        GameSettings.MasterVolume = value;
        AudioManager.Instance?.ApplyAllVolumes();
    }

    private void OnSfxVolumeChanged(float value)
    {
        GameSettings.SfxVolume = value;
        AudioManager.Instance?.ApplySfxVolume();
    }

    private void OnMusicVolumeChanged(float value)
    {
        GameSettings.MusicVolume = value;
        AudioManager.Instance?.ApplyMusicVolume();
    }

    private void OnScreenModeChanged(int index)
    {
        GameSettings.ScreenMode = index;
        ApplyScreenMode(index);
    }

    private void OnResolutionChanged(int index)
    {
        GameSettings.ResolutionIndex = index;
        ApplyResolution(index);
    }

    private void ApplyScreenMode(int index)
    {
        FullScreenMode mode = index switch
        {
            0 => FullScreenMode.Windowed,
            1 => FullScreenMode.FullScreenWindow,  // Borderless
            2 => FullScreenMode.ExclusiveFullScreen,
            _ => FullScreenMode.Windowed
        };

        Screen.fullScreenMode = mode;
    }

    private void ApplyResolution(int index)
    {
        if (index < 0 || index >= _resolutions.Length) return;

        var r = _resolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode);
    }
}
