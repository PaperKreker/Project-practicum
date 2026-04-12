using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UITransition : MonoBehaviour
{
    [SerializeField] private Canvas _renderCanvas;
    [SerializeField] private Image _overlay;
    [SerializeField] private Image _image;
    [SerializeField] private float _animationTime;
    [SerializeField] private float _transitionScale;

    private Camera _targetCamera;
    private GameObject _ignoreCanvas;
    private GameObject _targetCanvas;
    private Material _material;
    private bool _isLerping = false;

    void Start()
    {
        _material = Instantiate(_image.material);
        _image.material = _material;
        _image.enabled = false;
    }

    private void OnDestroy()
    {
        Destroy(_material);
    }

    [EasyButtons.Button]
    public void PlayTransition(TransitionType transitionType)
    {
        StartCoroutine(AnimateTransition(transitionType));   
    }

    public IEnumerator AnimateTransition(TransitionType transitionType)
    {
        if (TransitionProxy.Instance == null)
        {
            _overlay.enabled = false;
            yield break;
        }

        _targetCamera = TransitionProxy.Instance.TargetCamera;
        _ignoreCanvas = TransitionProxy.Instance.IgnoreCanvas;
        _targetCanvas = TransitionProxy.Instance.TargetCanvas;

        _isLerping = true;
        if (transitionType == TransitionType.Hide)
        {
            _image.enabled = false;
            yield return GetFrame();
            yield return AnimateHide();
            transform.localScale = Vector3.one;
        }
        else
        {
            yield return AnimateShow();
        }
    }

    private IEnumerator GetFrame()
    {
        yield return new WaitForEndOfFrame();

        RectTransform rectTransform = (RectTransform)transform;
        Vector2 size = rectTransform.rect.size * _renderCanvas.transform.localScale.x;
        Vector2Int sizeInt = new Vector2Int((int)size.x, (int)size.y);

        _ignoreCanvas.SetActive(false);
        _targetCanvas.SetActive(true);
        _overlay.enabled = false;

        _image.sprite = TakeScreenshot(sizeInt, rectTransform, Color.red);
        Sprite chromaKey = TakeScreenshot(sizeInt, rectTransform, Color.green);
        _material.SetTexture("_GreenTex", chromaKey.texture);


        _ignoreCanvas.SetActive(true);
        _targetCanvas.SetActive(true);
        _targetCamera.Render();
    }

    private Sprite PrepareSprite(Vector2Int size, RectTransform rectTransform)
    {
        Texture2D screenTex = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
        screenTex.filterMode = FilterMode.Point;

        Vector2 pos = rectTransform.position;
        Rect rect = new Rect(0, 0, size.x, size.y);
        screenTex.ReadPixels(rect, 0, 0);
        screenTex.Apply();
        return Sprite.Create(screenTex, new Rect(0, 0, size.x, size.y), new Vector2(0.5f, 0.5f));
    }

    private Sprite TakeScreenshot(Vector2Int size, RectTransform rectTransform, Color cameraColor)
    {
        Color initialColor = _targetCamera.backgroundColor;
        _targetCamera.backgroundColor = cameraColor;
        _targetCamera.Render();
        Sprite sprite = PrepareSprite(size, rectTransform);
        _targetCamera.backgroundColor = initialColor;
        return sprite;
    }

    private IEnumerator AnimateHide()
    {
        string transitionKey = "_Transition";
        float ticker = 0;
        _material.SetFloat(transitionKey, 1);
        _image.enabled = true;
        _targetCanvas.SetActive(false);
        
        while (ticker < _animationTime && _isLerping)
        {
            yield return new WaitForEndOfFrame();

            ticker += Time.unscaledDeltaTime;
            float t = ticker / _animationTime;

            _material.SetFloat(transitionKey, 1 - t);
        }
        if (_isLerping)
        {
            _material.SetFloat(transitionKey, 0);
            _image.enabled = false;
        }

        _isLerping = false;
    }

    private IEnumerator AnimateShow()
    {
        string transitionKey = "_Transition";
        float ticker = 0;
        _overlay.enabled = true;
        _overlay.material.SetFloat(transitionKey, 0);
        yield return new WaitForFixedUpdate();
        while (ticker < _animationTime && _isLerping)
        {
            yield return new WaitForEndOfFrame();

            float t = ticker / _animationTime;
            _overlay.material.SetFloat(transitionKey, t);

            ticker += Time.unscaledDeltaTime;
        }
        _overlay.enabled = false;
        _isLerping = false;
    }

    public enum TransitionType
    {
        Show,
        Hide,
    }
}
