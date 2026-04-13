using UnityEngine;

public class BattleAudio : MonoBehaviour
{
    [SerializeField] private BattleController _battleController;

    private void OnEnable()
    {
        _battleController.OnEnemyLastHit += PlayEnemyHit;
    }

    private void OnDisable()
    {
        _battleController.OnEnemyLastHit -= PlayEnemyHit;
    }

    private void PlayEnemyHit()
    {
        var state = _battleController.GetCurrentState();
        if (state.enemyData == null) return;

        string sound = "";
        switch (state.enemyData.EnemyName)
        {
            case "Альфа волк":
                sound = "hit_alpha_wolf";
                break;
            case "Волк":
                sound = "hit_wolf";
                break;
            case "Ворон":
                sound = "hit_raven";
                break;
            case "Лис":
                sound = "hit_fox";
                break;
            case "Василиск":
                sound = "hit_basilisk";
                break;
            case "Скарабей":
                sound = "hit_scarab";
                break;
            case "Минотавр":
                sound = "hit_minotaur";
                break;
            case "Паук":
                sound = "hit_spider";
                break;
            case "Амальгам":
                sound = "hit_amalgam";
                break;
            default:
                sound = "card_hit";
                break;
        }

        AudioManager.Instance.Play(sound, Random.Range(0.8f, 1.2f));
    }
}
