using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public static class CoroutineUtils
{
    public static IEnumerator WhenAll(MonoBehaviour owner, List<Coroutine> routines)
    {
        int completed = 0;

        foreach (var r in routines)
        {
            owner.StartCoroutine(Wrap(r, () => completed++));
        }

        yield return new WaitUntil(() => completed == routines.Count);
    }

    private static IEnumerator Wrap(Coroutine routine, System.Action onComplete)
    {
        yield return routine;
        onComplete?.Invoke();
    }
}
