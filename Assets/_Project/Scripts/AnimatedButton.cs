using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class AnimatedButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerClickHandler, IPointerUpHandler
{
    [SerializeField] ButtonPreset preset;
    [SerializeField] Transform graphics;
    public bool interactable = true;
    public Action OnButtonMove;
    public UnityEvent OnClick;
    public UnityEvent OnMouseEnter;
    public UnityEvent OnMouseExit;
    Vector3 targetScale;
    Vector3 startScale;

    private void Awake()
    {
        if (graphics == null)
        {
            graphics = transform;
        }
    }

    public void SetInteractive(bool val)
    {
        interactable = val;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactable) return;
        Click();
    }

    public void Click()
    {
        SetTargetSize(Vector3.one);
        OnClick?.Invoke();
        PlaySound(preset.clickSound);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!interactable) return;
        PointerDown();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PointerDown()
    {
        SetTargetSize(Vector3.one * (2 - preset.amplitude));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!interactable) return;
        PointerUp();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PointerUp()
    {
        SetTargetSize(Vector3.one);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!interactable) return;
        PointerEnter();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PointerEnter()
    {
        SetTargetSize(Vector3.one * preset.amplitude);
        PlaySound(preset.enterSound);
        OnMouseEnter?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!interactable) return;
        PointerExit();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PointerExit()
    {
        SetTargetSize(Vector3.one);
        OnMouseExit?.Invoke();
    }

    public void PlaySound(AudioClip audioClip)
    {
        if (audioClip == null || !preset.playSound) return;
        //TODO
    }

    private void SetTargetSize(Vector3 target)
    {
        targetScale = target;
        startScale = graphics.localScale;
        StartCoroutine(Lerp());
    }
    IEnumerator Lerp()
    {
        float ticker = 0;
        while (ticker < preset.animationTime)
        {
            yield return new WaitForEndOfFrame();
            OnButtonMove?.Invoke();
            ticker += Time.unscaledDeltaTime;
            float localTicker = Mathf.Min(ticker, preset.animationTime);
            graphics.localScale = Vector3.LerpUnclamped(startScale, targetScale, preset.animationCurve.Evaluate(localTicker / preset.animationTime));
        }
    }
}
