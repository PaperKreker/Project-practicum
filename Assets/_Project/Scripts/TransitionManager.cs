using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    public event Action OnTransitionStart;
    public static TransitionManager Instance { get; private set; }

    [SerializeField] private UITransition _UITransition;
    [SerializeField] private Image _overlay;
    private bool _isTransitioning;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void LoadScene(string sceneName)
    {
        if (_isTransitioning)
            return;

        StartCoroutine(AnimateTransition(sceneName));
    }

    IEnumerator AnimateTransition(string sceneName)
    {
        _isTransitioning = true;
        OnTransitionStart?.Invoke();
        yield return _UITransition.AnimateTransition(UITransition.TransitionType.Hide);
        _overlay.enabled = true;
        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return _UITransition.AnimateTransition(UITransition.TransitionType.Show);
        _overlay.enabled = false;
        _isTransitioning = false;
    }
}
