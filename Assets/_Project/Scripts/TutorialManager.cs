using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }
    public bool IsTutorialActive { get; private set; }
    public bool IsWaitingForDismiss { get; private set; }

    public event Action OnTutorialEnded;

    private TutorialPopup _popup;
    private BattleController _battleController;
    private BattleView _battleView;

    private TutorialTrigger _waitingForTrigger = TutorialTrigger.None;
    private int _stepIndex = 0;

    private bool _cardsDealt;
    private bool _firstSelectDone;
    private bool _firstAttackDone;
    private bool _battleWon;

    public static EnemyData TutorialEnemy => new EnemyData
    {
        EnemyName = "Волк",
        Tier = EnemyTier.Regular,
        MaxHp = 60,
        AttackDamage = 8,
        AttackCoinsPerRound = 3,
        GoldReward = 0,
        EffectType = EnemyEffectType.None,
    };
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartTutorial()
    {
        IsTutorialActive = true;
        ResetFlags();
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadScene("Battle");
        else
            SceneManager.LoadScene("Battle");
    }

    public void EndTutorial()
    {
        IsTutorialActive = false;
        _popup = null;
        _battleController = null;
        _battleView = null;
        OnTutorialEnded?.Invoke();
        SceneManager.LoadScene("MainMenu");
    }

    public void RegisterBattleScene(BattleController bc, BattleView bv, TutorialPopup popup)
    {
        _battleController = bc;
        _battleView = bv;
        _popup = popup;

        var hand = bc.GetComponentInChildren<HandController>(true);
        if (hand != null)
            hand.OnSelectionChanged += OnSelectionChanged;

        bc.OnBattleEnd += OnBattleEnded;
        bc.OnRefreshAll += OnBattleRefreshed;

        ShowStep(0);
    }

    public void OnPopupDismissed()
    {
        IsWaitingForDismiss = false;
        SetGameplayBlocked(false);

        if (_waitingForTrigger == TutorialTrigger.None)
        {
            ShowStep(_stepIndex);
        }
    }

    public void NotifyAttackExecuted()
    {
        if (_firstAttackDone) return;
        _firstAttackDone = true;
        TryFireTrigger(TutorialTrigger.AfterFirstAttack);
    }

    public void UnblockGameplay() => SetGameplayBlocked(false);

    private void OnBattleRefreshed()
    {
        if (_cardsDealt) return;
        _cardsDealt = true;
    }

    private void OnSelectionChanged()
    {
        if (_firstSelectDone) return;
        var hand = _battleController?.GetComponentInChildren<HandController>(true);
        if (hand == null || hand.GetSelectedCards().Count == 0) return;
        _firstSelectDone = true;
        TryFireTrigger(TutorialTrigger.FirstSelection);
    }

    private void OnBattleEnded(bool playerWon)
    {
        if (playerWon)
        {
            _battleWon = true;
            TryFireTrigger(TutorialTrigger.BattleWon);
        }
        else
        {
            StartCoroutine(RestartAfterLoss());
        }
    }
    private void TryFireTrigger(TutorialTrigger trigger)
    {
        if (IsWaitingForDismiss) return;
        if (_waitingForTrigger != trigger) return;

        _waitingForTrigger = TutorialTrigger.None;
        ShowStep(_stepIndex);
    }

    private void ShowStep(int index)
    {
        _stepIndex = index + 1;

        switch (index)
        {
            case 0:
                Show(
                    "Добро пожаловать!",
                    "Вы начинаете путешествие со <b>100 очками здоровья</b>.\n\n" +
                    "Ваша цель — победить всех врагов на карте.\n" +
                    "Если здоровье упадёт до нуля — забег заканчивается.",
                    "Далее",
                    afterDismiss: TutorialTrigger.None
                );
                break;

            case 1:
                Show(
                    "Ваша рука",
                    "В начале боя из колоды <b>52 карт</b> вам раздаётся рука.\n\n" +
                    "Каждая карта имеет <b>масть</b> (Камень, Огонь, Солнце, Луна) и <b>ранг</b> (2–Туз).\n\n" +
                    "Нажмите на карты чтобы выбрать их.",
                    "Понятно",
                    afterDismiss: TutorialTrigger.FirstSelection
                );
                break;

            case 2:
                Show(
                    "Комбинации",
                    "Выбранные карты образуют <b>комбинацию</b> — как в покере.\n\n" +
                    "Пара, стрит, флеш — чем сильнее комбинация, тем больше урона.\n\n" +
                    "Теперь нажмите <b>«Атаковать»</b>!",
                    "Понятно",
                    afterDismiss: TutorialTrigger.AfterFirstAttack
                );
                break;

            case 3:
                Show(
                    "Атака",
                    "У вас есть <b>3 атаки за раунд</b>.\n\n" +
                    "После каждой атаки вы добираете карты из колоды.\n" +
                    "Когда все атаки израсходованы — враг бьёт в ответ.",
                    "Понятно",
                    afterDismiss: TutorialTrigger.None
                );
                break;

            case 4:
                Show(
                    "Сброс карт",
                    "Не нравятся карты? Выберите их и нажмите <b>«Сбросить»</b>.\n\n" +
                    "По умолчанию у вас <b>3 сброса за бой</b>.\n" +
                    "Используйте сброс чтобы улучшить руку перед атакой.",
                    "Понятно",
                    afterDismiss: TutorialTrigger.None
                );
                break;

            case 5:
                Show(
                    "Способность врага",
                    "Многие враги применяют <b>особые правила</b>, усложняющие бой.\n\n" +
                    "Описание эффекта отображается рядом с врагом.\n\n" +
                    "В этом бою враг без эффектов — тренируйтесь спокойно!",
                    "Понятно",
                    afterDismiss: TutorialTrigger.BattleWon
                );
                break;

            case 6:
                Show(
                    "Карта и магазин",
                    "Вы победили!\n\n" +
                    "На <b>карте акта</b> выбирайте следующий узел: бой, магазин или отдых.\n\n" +
                    "<b>Магазин</b> позволяет тратить золото на <b>Сигилы</b> — " +
                    "мощные пассивные улучшения на весь забег.",
                    "Завершить обучение",
                    afterDismiss: TutorialTrigger.None
                );
                _onNextDismiss = EndTutorial;
                break;

            default:
                break;
        }
    }


    private Action _onNextDismiss;

    private void Show(string title, string body, string btn, TutorialTrigger afterDismiss)
    {
        _waitingForTrigger = afterDismiss;
        IsWaitingForDismiss = true;
        SetGameplayBlocked(true);
        _popup.Show(title, body, btn);
    }

    public void OnPopupDismissedInternal()
    {
        IsWaitingForDismiss = false;
        SetGameplayBlocked(false);

        if (_onNextDismiss != null)
        {
            var cb = _onNextDismiss;
            _onNextDismiss = null;
            cb.Invoke();
            return;
        }

        if (_waitingForTrigger == TutorialTrigger.None)
        {
            ShowStep(_stepIndex);
        }
    }

    private void SetGameplayBlocked(bool blocked)
    {
        _battleView?.SetButtonsBlocked(blocked);
    }

    private void ResetFlags()
    {
        _stepIndex = 0;
        _waitingForTrigger = TutorialTrigger.None;
        IsWaitingForDismiss = false;
        _cardsDealt = false;
        _firstSelectDone = false;
        _firstAttackDone = false;
        _battleWon = false;
        _onNextDismiss = null;
    }

    private IEnumerator RestartAfterLoss()
    {
        _popup.Show(
            "Не беда!",
            "В обучении можно проигрывать.\nДавайте попробуем ещё раз.",
            "Попробовать снова"
        );
        IsWaitingForDismiss = true;
        yield return new WaitUntil(() => !IsWaitingForDismiss);
        ResetFlags();
        StartTutorial();
    }
}


public enum TutorialTrigger
{
    None,
    FirstSelection,
    AfterFirstAttack,
    BattleWon,
}
