using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attach to the SigilSlotPrefab.
// Requires: TMP_Text "NameText", TMP_Text "DescText", TMP_Text "CostText", Button "BuyButton"
public class SigilSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _descText;
    [SerializeField] private TMP_Text _costText;
    [SerializeField] private TMP_Text _typeText;
    [SerializeField] private Button _buyButton;

    public Sigil Sigil { get; private set; }
    private ShopController _shop;

    public void Setup(Sigil sigil, ShopController shop, int displayedCost)
    {
        Sigil = sigil;
        _shop = shop;

        if (_nameText) _nameText.text = sigil.Name;
        if (_descText) _descText.text = sigil.Description;
        if (_costText) _costText.text = $"{displayedCost}g";
        if (_typeText) _typeText.text = sigil.Type.ToFriendlyString();

        if (_buyButton)
            _buyButton.onClick.AddListener(OnBuyClicked);
    }

    public void SetBuyable(bool canBuy)
    {
        if (_buyButton) _buyButton.interactable = canBuy;
    }

    private void OnBuyClicked()
    {
        _shop.OnSigilPurchased(Sigil);
    }
}
