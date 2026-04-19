using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyView : MonoBehaviour
{
    [SerializeField] private BattleController _battleController;
    [SerializeField] private Image _graphics;
    [SerializeField] private EnemySpriteDatabase _spriteDatabase;
    [SerializeField] private EnemyAnimationConfig _animationConfig;
    [SerializeField] private SlashAnimation _slashAnimation;
    [SerializeField] private string _slashSound;

    private RectTransform _rectTransform;
    private Material _material;
    private Vector3 _initialPosition;
    private bool _isMoving = false;

    private void Start()
    {
        _material = Instantiate(_graphics.material);
        _graphics.material = _material;

        _rectTransform = (RectTransform)transform;
        _initialPosition = _rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        _battleController.OnEnemyHit += Hit;
        _battleController.OnBattleEnd += EndBattle;
        _battleController.OnRefreshAll += ApplyEnemySprite;
        _battleController.OnEnemyAttack += Attack;
    }
    private void OnDisable()
    {
        _battleController.OnEnemyHit -= Hit;
        _battleController.OnBattleEnd -= EndBattle;
        _battleController.OnRefreshAll -= ApplyEnemySprite;
        _battleController.OnEnemyAttack -= Attack;
    }

    private void Update()
    {
        if (!_isMoving)
        {
            PlayIdleAnimation();
        }
    }

    private void PlayIdleAnimation()
    {
        _rectTransform.anchoredPosition = Vector2.Lerp(
               _rectTransform.anchoredPosition,
               GetIdlePosition(),
               Time.deltaTime / _animationConfig.IdleAnimationSmooth);
    }

    private Vector3 GetIdlePosition()
    {
        float siblingAddition = (float)transform.GetSiblingIndex() / transform.parent.childCount;
        float t = (Time.time * _animationConfig.IdleAnimationSpeed + siblingAddition) % 1.0f;
        Vector3 shift = Vector2.Lerp(
            -_animationConfig.IdleAmplitude,
            _animationConfig.IdleAmplitude,
            _animationConfig.IdleCurve.Evaluate(t));
        return _initialPosition + shift;
    }

    private void ApplyEnemySprite()
    {
        if (_spriteDatabase == null) return;
        var state = _battleController.GetCurrentState();
        if (state.enemyData == null) return;

        Sprite sprite = _spriteDatabase.GetSprite(state.enemyData.EnemyName);
        if (sprite != null)
            _graphics.sprite = sprite;

        _battleController.OnRefreshAll -= ApplyEnemySprite;
    }

    private void Attack()
    {
        _battleController.AddAnimationToWait(
            StartCoroutine(AnimateAttack()));
    }

    private void Hit()
    {
        StartCoroutine(AnimateHit());
    }

    private void EndBattle(bool playerWon)
    {
        if (playerWon)
        {
            Coroutine animation = StartCoroutine(AnimateDeath());
            _battleController.AddAnimationToWait(animation);
        }
    }

    private IEnumerator AnimateHit()
    {
        _isMoving = true;
        float startTime = Time.time;
        while (Time.time < startTime + _animationConfig.AnimationDuration)
        {
            float t = 1.0f - (Time.time - startTime) / _animationConfig.AnimationDuration;

            float amplitude = _animationConfig.ShakeAmplitude * t;
            _rectTransform.anchoredPosition = _initialPosition + new Vector3(
                Random.Range(-amplitude, amplitude),
                Random.Range(-amplitude, amplitude));

            _material.SetFloat("_Damage", t);

            yield return new WaitForEndOfFrame();
        }
        _isMoving = false;
    }

    private IEnumerator AnimateAttack()
    {
        yield return new WaitForSeconds(_animationConfig.AttackDelay);

        _isMoving = true;
        float time = _animationConfig.AttackDuration;
        Vector3 initialPosition = _rectTransform.position;
        Vector3 targetPosition = _slashAnimation.transform.position;
        StartCoroutine(SlashDelayed(_animationConfig.AttackSlashDelay));
        while (time > 0.0f)
        {
            float t = 1 - time / _animationConfig.AttackDuration;
            float eval = _animationConfig.AttackCurve.Evaluate(t);
            _rectTransform.position = Vector2.LerpUnclamped(
                initialPosition,
                targetPosition,
                eval);
            time -= Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        _isMoving = false;
    }

    private IEnumerator SlashDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioManager.Instance.Play(_slashSound, Random.Range(0.8f, 1.2f));
        _slashAnimation.PlayAnimation();
    }

    IEnumerator AnimateDeath()
    {
        _isMoving = true;
        _material = Instantiate(_animationConfig.DeathMaterial);
        _graphics.material = _material;

        float startTime = Time.time;
        while (Time.time < startTime + _animationConfig.DeathDuration)
        {
            float t = 1.0f - (Time.time - startTime) / _animationConfig.DeathDuration;

            _material.SetFloat("_Transition", t);
            _graphics.transform.localScale = Vector3.one * Mathf.Lerp(_animationConfig.DeathScale, 1.0f, t);

            yield return new WaitForEndOfFrame();
        }
        _isMoving = false;

        gameObject.SetActive(false);
    }
}
