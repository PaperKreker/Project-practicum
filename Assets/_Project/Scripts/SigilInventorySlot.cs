using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class SigilInventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private GameObject _tooltip;
    [SerializeField] private TMP_Text _tooltipName;
    [SerializeField] private TMP_Text _tooltipType;
    [SerializeField] private TMP_Text _tooltipDesc;

    private Sigil _sigil;

    public void Setup(Sigil sigil)
    {
        _sigil = sigil;
        if (_nameText) _nameText.text = sigil.Name;
        if (_tooltip) _tooltip.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_tooltip == null) return;
        _tooltip.SetActive(true);
        if (_tooltipName) _tooltipName.text = _sigil.Name;
        if (_tooltipType) _tooltipType.text = _sigil.Type.ToFriendlyString();
        if (_tooltipDesc) _tooltipDesc.text = _sigil.Description;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_tooltip) _tooltip.SetActive(false);
    }
}