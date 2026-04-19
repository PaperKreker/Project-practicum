using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlashAnimation : MonoBehaviour
{
    [SerializeField] private Image _graphics;
    [SerializeField] private List<Sprite> _frames;
    [SerializeField] private float _animationDuration = 0.4f;

    public void PlayAnimation()
    {
        StartCoroutine(AnimateParticle());
    }

    IEnumerator AnimateParticle()
    {
        _graphics.enabled = true;
        float ticker = 0.0f;
        while (ticker < _animationDuration)
        {
            int frameIndex = Mathf.FloorToInt(ticker / _animationDuration * _frames.Count);
            _graphics.sprite = _frames[frameIndex];
            ticker += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        _graphics.enabled = false;
    }
}
