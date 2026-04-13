using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AudioBank", menuName = "Audio/Audio Bank")]
public class AudioBank : ScriptableObject
{
    [System.Serializable]
    public class SoundEntry
    {
        public string key;
        public AudioClip clip;
    }

    public List<SoundEntry> sounds = new List<SoundEntry>();

    private Dictionary<string, AudioClip> _soundDict;

    public void Init()
    {
        _soundDict = new Dictionary<string, AudioClip>();

        foreach (var sound in sounds)
        {
            if (!_soundDict.ContainsKey(sound.key))
            {
                _soundDict.Add(sound.key, sound.clip);
            }
        }
    }

    public AudioClip GetClip(string key)
    {
        if (_soundDict == null)
            Init();

        if (_soundDict.TryGetValue(key, out var clip))
            return clip;

        Debug.LogWarning($"Sound with key '{key}' not found!");
        return null;
    }
}