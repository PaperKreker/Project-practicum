using UnityEngine;

[CreateAssetMenu(fileName = "ButtonPreset", menuName = "Scriptable Objects/ButtonPreset")]
public class ButtonPreset : ScriptableObject
{
    public AnimationCurve animationCurve;
    public bool playSound = true;
    public AudioClip clickSound;
    public AudioClip enterSound;
    public float animationTime;
    public float amplitude;
}
