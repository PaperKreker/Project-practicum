using UnityEngine;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TMP_InputField _seedInput;

    public void OnNewGameClicked()
    {
        GameManager.Instance.StartNewRun(playerMaxHp: 100, seed: ParseSeed());
    }

    public void OnNewGameHard()
    {
        GameManager.Instance.StartNewRun(playerMaxHp: 95, seed: ParseSeed());
    }

    public void OnNewGameDemon()
    {
        GameManager.Instance.StartNewRun(playerMaxHp: 90, seed: ParseSeed());
    }

    // -1 = random seed
    private int ParseSeed()
    {
        if (_seedInput == null) return -1;
        string text = _seedInput.text.Trim();
        if (string.IsNullOrEmpty(text)) return -1;
        return int.TryParse(text, out int seed) ? Mathf.Abs(seed) : Mathf.Abs(text.GetHashCode());
    }
}
