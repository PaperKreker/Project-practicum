using UnityEngine;

[CreateAssetMenu(fileName = "EnemySpriteDatabase", menuName = "Scriptable Objects/EnemySpriteDatabase")]
public class EnemySpriteDatabase : ScriptableObject
{
    [System.Serializable]
    public struct EnemySprite
    {
        public string EnemyName;   // must match EnemyData.EnemyName exactly
        public Sprite Sprite;
    }

    public EnemySprite[] Entries;

    public Sprite GetSprite(string enemyName)
    {
        foreach (var entry in Entries)
            if (entry.EnemyName == enemyName)
                return entry.Sprite;
        return null;
    }
}