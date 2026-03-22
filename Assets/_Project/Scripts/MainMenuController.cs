using UnityEngine;
public class MainMenuController : MonoBehaviour
{
    public void OnNewGameClicked()
    {
        GameManager.Instance.StartNewRun();
    }

    public void OnNewGameHard()
    {
        GameManager.Instance.StartNewRun(playerMaxHp: 95);
    }

    public void OnNewGameDemon()
    {
        GameManager.Instance.StartNewRun(playerMaxHp: 90);
    }
}
