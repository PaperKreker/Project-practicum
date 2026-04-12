using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System;

public static class CoroutineUtils
{
    public static IEnumerator WhenAll(MonoBehaviour owner, List<Coroutine> routines, Action OnStepComplete = null)
    {
        int completed = 0;

        foreach (var r in routines)
        {
            owner.StartCoroutine(Wrap(r, () =>
            {
                completed++;
                OnStepComplete?.Invoke();
            }));
        }

        yield return new WaitUntil(() => completed == routines.Count);
    }

    private static IEnumerator Wrap(Coroutine routine, Action onComplete)
    {
        yield return routine;
        onComplete?.Invoke();
    }
}
