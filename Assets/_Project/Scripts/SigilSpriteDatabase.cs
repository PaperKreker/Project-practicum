using UnityEngine;

[CreateAssetMenu(fileName = "SigilSpriteDatabase", menuName = "Scriptable Objects/SigilSpriteDatabase")]
public class SigilSpriteDatabase : ScriptableObject
{
    [System.Serializable]
    public struct SigilSprite
    {
        public string SigilName;   // must match Sigil.SpriteKey
        public Sprite Sprite;
    }

    public SigilSprite[] Entries;

    [Tooltip("Shown when no entry matches the sigil's SpriteKey.")]
    public Sprite Placeholder;

    public Sprite GetSprite(string sigilKey)
    {
        foreach (var entry in Entries)
            if (entry.SigilName == sigilKey)
                return entry.Sprite;

        return Placeholder;
    }
}
