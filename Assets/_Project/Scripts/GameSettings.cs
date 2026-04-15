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
    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat("MusicVolume", 1.0f);
        set {
            PlayerPrefs.SetFloat("MusicVolume", value);
            PlayerPrefs.Save();
        }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat("SfxVolume", 1.0f);
        set {
            PlayerPrefs.SetFloat("SfxVolume", value);
            PlayerPrefs.Save();
        }
    }

    public static HandSortMode HandSortMode
    {
        get => (HandSortMode)PlayerPrefs.GetInt("HandSortMode", (int)HandSortMode.RANK);
        set {
            PlayerPrefs.SetInt("HandSortMode", (int)value);
            PlayerPrefs.Save();
        }
    }
}
