using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyView : MonoBehaviour
{
    [SerializeField] private BattleController _battleController;
    [SerializeField] private Image _graphics;

    [Header("Hit animation")]
    [SerializeField] private float _animationDuration = 0.5f;
    [SerializeField] private float _shakeAmplitude = 2.0f;

    [Header("Death animation")]
    [SerializeField] private Material _deathMaterial;
    [SerializeField] private float _deathScale = 0.6f;
    [SerializeField] private float _deathDuration = 1.0f;

    private RectTransform _rectTransform;
    private Material _material;
    private Vector3 _initialPosition;

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
    }
    private void OnDisable()
    {
        _battleController.OnEnemyHit -= Hit;
        _battleController.OnBattleEnd -= EndBattle;
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

    IEnumerator AnimateHit()
    {
        float startTime = Time.time;
        while (Time.time < startTime + _animationDuration)
        {
            float t = 1.0f - (Time.time - startTime) / _animationDuration;

            float amplitude = _shakeAmplitude * t;
            _rectTransform.anchoredPosition = _initialPosition + new Vector3(
                Random.Range(-amplitude, amplitude),
                Random.Range(-amplitude, amplitude));

            _material.SetFloat("_Damage", t);

            yield return new WaitForEndOfFrame();
        }
    }

    IEnumerator AnimateDeath()
    {
        _material = Instantiate(_deathMaterial);
        _graphics.material = _material;

        float startTime = Time.time;
        while (Time.time < startTime + _deathDuration)
        {
            float t = 1.0f - (Time.time - startTime) / _deathDuration;

            _material.SetFloat("_Transition", t);
            _graphics.transform.localScale = Vector3.one * Mathf.Lerp(_deathScale, 1.0f, t);

            yield return new WaitForEndOfFrame();
        }

        gameObject.SetActive(false);
    }
}
