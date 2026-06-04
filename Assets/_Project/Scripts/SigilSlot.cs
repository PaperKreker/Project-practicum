using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SigilSlot : MonoBehaviour
{
    [SerializeField] private Image _icon;

    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descText;
    [SerializeField] private TMP_Text _costText;
    [SerializeField] private TMP_Text _typeText;
    [SerializeField] private Button _buyButton;

    public Sigil Sigil { get; private set; }
    private ShopController _shop;

    public void Setup(Sigil sigil, ShopController shop, int displayedCost, SigilSpriteDatabase spriteDatabase)
    {
        Sigil = sigil;
        _shop = shop;

        _icon.sprite = spriteDatabase.GetSprite(sigil.SpriteKey);

        _nameText.text = sigil.Name;
        _descText.text = sigil.Description;
        _costText.text = $"{sigil.Cost}g";
        _typeText.text = sigil.Type.ToFriendlyString();

        _buyButton.onClick.AddListener(OnBuyClicked);
    }

    public void SetBuyable(bool canBuy)
    {
        _buyButton.interactable = canBuy;
    }

    private void OnBuyClicked()
    {
        _shop.OnSigilPurchased(Sigil);
    }
}
