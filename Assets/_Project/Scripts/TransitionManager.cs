using System.Collections;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [SerializeField] private UITransition _UITransition;
    [SerializeField] private Image _overlay;

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

    public void LoadScene(string sceneName)
    {
        StartCoroutine(AnimateTransition(sceneName));
    }

    IEnumerator AnimateTransition(string sceneName)
    {
        yield return _UITransition.AnimateTransition(UITransition.TransitionType.Hide);
        _overlay.enabled = true;
        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return _UITransition.AnimateTransition(UITransition.TransitionType.Show);
    }
}
