using UnityEngine;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private TMP_InputField _seedInput;

    // ── Existing game modes ───────────────────────────────────────────

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

    // ── Tutorial ──────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается кнопкой «Обучение» в главном меню.
    /// </summary>
    public void OnTutorialClicked()
    {
        // Ensure TutorialManager exists (it's DontDestroyOnLoad,
        // but might not be in the scene if the player launched directly).
        if (TutorialManager.Instance == null)
        {
            GameObject go = new GameObject("TutorialManager");
            go.AddComponent<TutorialManager>();
        }

        TutorialManager.Instance.StartTutorial();
    }

    // ── Helpers ───────────────────────────────────────────────────────

    // -1 = random seed
    private int ParseSeed()
    {
        if (_seedInput == null) return -1;
        string text = _seedInput.text.Trim();
        if (string.IsNullOrEmpty(text)) return -1;
        return int.TryParse(text, out int seed) ? Mathf.Abs(seed) : Mathf.Abs(text.GetHashCode());
    }
}