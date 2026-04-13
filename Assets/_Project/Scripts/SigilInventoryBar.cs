using System.Collections.Generic;
using UnityEngine;

public class SigilInventoryBar : MonoBehaviour
{
    [SerializeField] private Transform _slotContainer;
    [SerializeField] private GameObject _slotPrefab;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        foreach (Transform child in _slotContainer)
            Destroy(child.gameObject);

        var sigils = GameManager.Instance?.Run.ActiveSigils ?? new List<Sigil>();

        foreach (var sigil in sigils)
        {
            var go = Instantiate(_slotPrefab, _slotContainer);
            go.GetComponent<SigilInventorySlot>().Setup(sigil);
        }
    }
}