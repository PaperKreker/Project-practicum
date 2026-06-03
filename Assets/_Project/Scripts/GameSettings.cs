using UnityEngine;

public enum HandSortMode
{
    RANK,
    SUIT
}

public static class HandSortModeExtensions
{
    public static string ToRussianString(this HandSortMode handSortMode)
    {
        return handSortMode switch
        {
            HandSortMode.RANK => "Ранг",
            HandSortMode.SUIT => "Масть",
            _ => throw new System.NotImplementedException()
        };
    }

    public static HandSortMode Cycle(this HandSortMode handSortMode)
    {
        return handSortMode switch
        {
            HandSortMode.RANK => HandSortMode.SUIT,
            HandSortMode.SUIT => HandSortMode.RANK,
            _ => throw new System.NotImplementedException()
        };
    }
}

public static class GameSettings
{
    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        set
        {
            PlayerPrefs.SetFloat("MasterVolume", Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat("MusicVolume", 1.0f);
        set
        {
            PlayerPrefs.SetFloat("MusicVolume", Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat("SfxVolume", 1.0f);
        set
        {
            PlayerPrefs.SetFloat("SfxVolume", Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }

    public static int ScreenMode
    {
        get => PlayerPrefs.GetInt("ScreenMode", 0);
        set
        {
            PlayerPrefs.SetInt("ScreenMode", value);
            PlayerPrefs.Save();
        }
    }

    public static int ResolutionIndex
    {
        get => PlayerPrefs.GetInt("ResolutionIndex", -1);
        set
        {
            PlayerPrefs.SetInt("ResolutionIndex", value);
            PlayerPrefs.Save();
        }
    }

    public static HandSortMode HandSortMode
    {
        get => (HandSortMode)PlayerPrefs.GetInt("HandSortMode", (int)HandSortMode.RANK);
        set
        {
            PlayerPrefs.SetInt("HandSortMode", (int)value);
            PlayerPrefs.Save();
        }
    }
}
