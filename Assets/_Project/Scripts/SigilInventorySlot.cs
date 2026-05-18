using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class SigilInventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject _tooltip;
    [SerializeField] private UIElementFitter _tooltipFitter;
    [SerializeField] private TMP_Text _tooltipName;
    [SerializeField] private TMP_Text _tooltipType;
    [SerializeField] private TMP_Text _tooltipDesc;

    private Sigil _sigil;

    public void Setup(Sigil sigil)
    {
        _sigil = sigil;
        _tooltipName.text = sigil.Name;
        _tooltip.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _tooltip.SetActive(true);
        _tooltipFitter.SetPosition(_tooltip.transform.position);
        _tooltipName.text = _sigil.Name;
        _tooltipType.text = _sigil.Type.ToFriendlyString();
        _tooltipDesc.text = _sigil.Description;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _tooltip.SetActive(false);
    }
}