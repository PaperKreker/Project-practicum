using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    [SerializeField] private float _moveSpeed = 50f;
    [SerializeField] private float _lifetime = 2f;

    private Color startColor;

    private void Start()
    {
        startColor = _text.color;
        StartCoroutine(Animate());
    }

    public void SetText(string value)
    {
        _text.text = value;
    }

    public void SetPosition(Vector3 screenPosition)
    {
        transform.position = screenPosition;
    }

    private IEnumerator Animate()
    {
        float time = 0f;

        while (time < _lifetime)
        {
            time += Time.deltaTime;

            transform.position += Vector3.up * _moveSpeed * Time.deltaTime;

            float alpha = Mathf.Lerp(1f, 0f, time / _lifetime);
            _text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            yield return null;
        }

        Destroy(gameObject);
    }
}