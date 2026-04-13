using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Setup")]
    [SerializeField] private AudioBank audioBank;

    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 10;
    [SerializeField] private int maxPoolSize = 30;

    private List<AudioSource> _pool = new List<AudioSource>();
    private Queue<AudioSource> _available = new Queue<AudioSource>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioBank.Init();
            InitPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateSource();
        }
    }

    private AudioSource CreateSource()
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;

        _pool.Add(source);
        _available.Enqueue(source);

        return source;
    }

    private AudioSource GetSource()
    {
        if (_available.Count > 0)
            return _available.Dequeue();

        if (_pool.Count < maxPoolSize)
            return CreateSource();

        return _pool[0];
    }

    private void ReturnToPool(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        source.pitch = 1f;

        _available.Enqueue(source);
    }

    public void Play(string key, float pitch = 1.0f)
    {
        var clip = audioBank.GetClip(key);

        if (clip == null)
            return;

        var source = GetSource();

        source.clip = clip;
        source.pitch = pitch;
        source.Play();

        StartCoroutine(ReturnAfterPlay(source, clip.length / Mathf.Abs(pitch)));
    }

    private System.Collections.IEnumerator ReturnAfterPlay(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(source);
    }
}