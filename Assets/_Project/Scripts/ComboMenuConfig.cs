using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ComboMenuConfig", menuName = "UI/Combo Menu Config")]
public class ComboMenuConfig : ScriptableObject
{
    [Header("Card Prefab")]
    [Tooltip("Префаб CardView — тот же, что используется в руке игрока")]
    public GameObject CardPrefab;

    [Header("Layout")]
    [Tooltip("Масштаб миниатюр карт в поп-апе")]
    public float PreviewCardScale = 0.55f;
    [Tooltip("Горизонтальный отступ между картами в поп-апе")]
    public float PreviewCardSpacing = 60f;
    [Tooltip("Задержка появления поп-апа (сек)")]
    public float PopupDelay = 0.15f;
    [Tooltip("Время анимации появления/скрытия поп-апа")]
    public float PopupAnimTime = 0.12f;

    [Header("Colors")]
    public Color RowNormalBg = new Color(0.10f, 0.10f, 0.12f, 0.92f);
    public Color RowHoverBg = new Color(0.18f, 0.16f, 0.10f, 0.98f);
    public Color DamageColor = new Color(1.00f, 0.82f, 0.28f, 1.00f);
    public Color MultiplierColor = new Color(0.40f, 0.90f, 0.55f, 1.00f);
    public Color PopupBgColor = new Color(0.08f, 0.08f, 0.10f, 0.97f);

    [Header("Combo Definitions")]
    [Tooltip("Описания и примеры для каждой комбинации. Заполняются автоматически при сбросе.")]
    public List<ComboEntryConfig> Entries = new();

    [ContextMenu("Reset to Defaults")]
    public void ResetToDefaults()
    {
        Entries = new List<ComboEntryConfig>
        {
            new ComboEntryConfig
            {
                Type = ComboType.High,
                DisplayName = "Старшая карта",
                Description = "Засчитывается только наивысший ранг из выбранных карт.",
                ExampleCards = new List<CardPreviewData>
                {
                    new CardPreviewData { Suit = Suit.Sun,  Rank = Rank.Ace }
                }
            },
            new ComboEntryConfig
            {
                Type = ComboType.Pair,
                DisplayName = "Пара",
                Description = "2 карты одного ранга. Обе засчитываются.",
                ExampleCards = new List<CardPreviewData>
                {
                    new CardPreviewData { Suit = Suit.Fire, Rank = Rank.King },
                    new CardPreviewData { Suit = Suit.Moon, Rank = Rank.King }
                }
            },
            new ComboEntryConfig
            {
                Type = ComboType.TwoPair,
                DisplayName = "Две пары",
                Description = "2 разные пары. Засчитываются все 4 карты.",
                ExampleCards = new List<CardPreviewData>
                {
                    new CardPreviewData { Suit = Suit.Sun,   Rank = Rank.Queen },
                    new CardPreviewData { Suit = Suit.Stone, Rank = Rank.Queen },
                    new CardPreviewData { Suit = Suit.Fire,  Rank = Rank.Eight },
                    new CardPreviewData { Suit = Suit.Moon,  Rank = Rank.Eight }
                }
            },
            new ComboEntryConfig
            {
                Type = ComboType.Set,
                DisplayName = "Тройка",
                Description = "3 карты одного ранга. Все три засчитываются.",
                ExampleCards = new List<CardPreviewData>
                {
                    new CardPreviewData { Suit = Suit.Stone, Rank = Rank.Seven },
                    new CardPreviewData { Suit = Suit.Fire,  Rank = Rank.Seven },
                    new CardPreviewData { Suit = Suit.Sun,   Rank = Rank.Seven }
                }
            },
            new ComboEntryConfig
            {
                Type = ComboType.Straight,
                DisplayName = "Стрит",
                Description = "5 карт с последовательными рангами, любые масти.",
                ExampleCards = new List<CardPreviewData>
                {
                    new CardPreviewData { Suit = Suit.Stone, Rank = Rank.Five },
                    new CardPreviewData { Suit = Suit.Fire,  Rank = Rank.Six  },
                    new CardPreviewData { Suit = Suit.Moon,  Rank = Rank.Seven},
                    new CardPreviewData { Suit = Suit.Sun,   Rank = Rank.Eight},
                    new CardPreviewData { Suit = Suit.Stone, Rank = Rank.Nine }
                }
            },
            new ComboEntryConfig
            {
                Type = ComboType.Flush,
                DisplayName = "Флеш",
                Description = "5 карт одной масти, любые ранги.",
                ExampleCards = new List<CardPreviewData>
                {
                    new CardPreviewData { Suit = Suit.Moon, Rank = Rank.Two   },
                    new CardPreviewData { Suit = Suit.Moon, Rank = Rank.Five  },
                    new CardPreviewData { Suit = Suit.Moon, Rank = Rank.Eight },
                    new CardPreviewData { Suit = Suit.Moon, Rank = Rank.Jack  },
                    new CardPreviewData { Suit = Suit.Moon, Rank = Rank.King  }
                }
            },
            new ComboEntryConfig
            {
                Type = ComboType.FullHouse,
                DisplayName = "Фулл-хаус",
                Description = "3 карты одного ранга + 2 карты другого ранга.",
                ExampleCards = new List<CardPreviewData>
                {
                    new CardPreviewData { Suit = Suit.Fire,  Rank = Rank.Ten },
                    new CardPreviewData { Suit = Suit.Moon,  Rank = Rank.Ten },
                    new CardPreviewData { Suit = Suit.Stone, Rank = Rank.Ten },
                    new CardPreviewData { Suit = Suit.Sun,   Rank = Rank.Six },
                    new CardPreviewData { Suit = Suit.Fire,  Rank = Rank.Six }
                }
            },
            new ComboEntryConfig
            {
                Type = ComboType.FOK,
                DisplayName = "Каре",
                Description = "4 карты одного ранга. Все засчитываются.",
                ExampleCards = new List<CardPreviewData>
                {
                    new CardPreviewData { Suit = Suit.Stone, Rank = Rank.Nine },
                    new CardPreviewData { Suit = Suit.Fire,  Rank = Rank.Nine },
                    new CardPreviewData { Suit = Suit.Moon,  Rank = Rank.Nine },
                    new CardPreviewData { Suit = Suit.Sun,   Rank = Rank.Nine }
                }
            },
            new ComboEntryConfig
            {
                Type = ComboType.StraightFlush,
                DisplayName = "Стрит-флеш",
                Description = "5 карт одной масти с последовательными рангами.",
                ExampleCards = new List<CardPreviewData>
                {
                    new CardPreviewData { Suit = Suit.Fire, Rank = Rank.Three },
                    new CardPreviewData { Suit = Suit.Fire, Rank = Rank.Four  },
                    new CardPreviewData { Suit = Suit.Fire, Rank = Rank.Five  },
                    new CardPreviewData { Suit = Suit.Fire, Rank = Rank.Six   },
                    new CardPreviewData { Suit = Suit.Fire, Rank = Rank.Seven }
                }
            },
            new ComboEntryConfig
            {
                Type = ComboType.RoyalFlush,
                DisplayName = "Роял-флеш",
                Description = "10 J Q K A одной масти. Максимальная комбинация.",
                ExampleCards = new List<CardPreviewData>
                {
                    new CardPreviewData { Suit = Suit.Sun, Rank = Rank.Ten   },
                    new CardPreviewData { Suit = Suit.Sun, Rank = Rank.Jack  },
                    new CardPreviewData { Suit = Suit.Sun, Rank = Rank.Queen },
                    new CardPreviewData { Suit = Suit.Sun, Rank = Rank.King  },
                    new CardPreviewData { Suit = Suit.Sun, Rank = Rank.Ace   }
                }
            }
        };
    }
}

[System.Serializable]
public class ComboEntryConfig
{
    public ComboType Type;
    public string DisplayName;
    [TextArea(1, 3)]
    public string Description;
    public List<CardPreviewData> ExampleCards = new();
}

[System.Serializable]
public class CardPreviewData
{
    public Suit Suit;
    public Rank Rank;
}
