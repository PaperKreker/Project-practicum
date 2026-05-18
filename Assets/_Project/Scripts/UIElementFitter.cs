using UnityEngine;
using UnityEngine.UI;

public class UIElementFitter : MonoBehaviour
{
    private Camera _mainCamera;
    private RectTransform _rectTransform;
    private RectTransform _canvasRect;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _rectTransform = (RectTransform)transform;
        _canvasRect = (RectTransform)(GetComponentInParent<Canvas>().rootCanvas.transform);
    }

    public void SetPosition(Vector2 worldPosition)
    {
        Vector2 clampedPosition = ClampToCanvas(worldPosition);
        _rectTransform.position = clampedPosition;
    }

    private Vector2 ClampToCanvas(Vector2 pos)
    {
        Vector2 elementSize = _rectTransform.rect.size * _canvasRect.localScale;
        Vector2 canvasSize = _canvasRect.rect.size * _canvasRect.localScale;
        Vector2 pivot = _rectTransform.pivot;

        float minX = elementSize.x * pivot.x;
        float maxX = canvasSize.x - elementSize.x * (1 - pivot.x);

        float minY = elementSize.y * pivot.y;
        float maxY = canvasSize.y - elementSize.y * (1 - pivot.y);

        float clampedX = Mathf.Clamp(pos.x, minX, maxX);
        float clampedY = Mathf.Clamp(pos.y, minY, maxY);

        return new Vector2(clampedX, clampedY);
    }
}
