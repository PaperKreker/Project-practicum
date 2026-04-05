using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _sigilContainer;
    [SerializeField] private GameObject _sigilSlotPrefab;

    [Header("HUD")]
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _rerollCostText;
    [SerializeField] private Button _rerollButton;
    [SerializeField] private Button _leaveButton;

    private List<Sigil> _offered = new List<Sigil>();
    private int _rerollCost = 1;
    private const int OfferedCount = 3;

    private void Start()
    {
        if (GameManager.Instance == null) return;
        RollOffers();
        RefreshHUD();
    }

    public void OnRerollClicked()
    {
        var run = GameManager.Instance.Run;
        if (run.Gold < _rerollCost) return;

        run.Gold -= _rerollCost;
        _rerollCost++;
        RollOffers();
        RefreshHUD();
    }

    public void OnLeaveShopClicked()
    {
        GameManager.Instance.OnShopExited();
    }

    private void RollOffers()
    {
        // Clear old slots
        foreach (Transform child in _sigilContainer)
            Destroy(child.gameObject);
        _offered.Clear();

        var run = GameManager.Instance.Run;
        var all = SigilDatabase.All();

        // Remove already owned sigils from pool
        var owned = new HashSet<string>();
        foreach (var s in run.ActiveSigils)
            owned.Add(s.Name);

        var pool = all.FindAll(s => !owned.Contains(s.Name));

        // Pick OfferedCount random sigils
        for (int i = 0; i < OfferedCount && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            _offered.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        foreach (var sigil in _offered)
            SpawnSlot(sigil);

        RefreshSlots();
    }

    private void SpawnSlot(Sigil sigil)
    {
        GameObject go = Instantiate(_sigilSlotPrefab, _sigilContainer);
        var slot = go.GetComponent<SigilSlot>();
        if (slot == null) slot = go.AddComponent<SigilSlot>();
        slot.Setup(sigil, this);
    }

    public void OnSigilPurchased(Sigil sigil)
    {
        var run = GameManager.Instance.Run;
        if (run.Gold < sigil.Cost) return;
        if (run.ActiveSigils.Count >= RunData.MaxSigils) return;

        run.Gold -= sigil.Cost;
        run.ActiveSigils.Add(sigil);

        _offered.Remove(sigil);

        // Remove slot from UI
        foreach (Transform child in _sigilContainer)
        {
            var slot = child.GetComponent<SigilSlot>();
            if (slot != null && slot.Sigil == sigil)
            {
                child.SetParent(null);

                child.gameObject.SetActive(false);

                Destroy(child.gameObject);
                break;
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(_sigilContainer as RectTransform);
        RefreshHUD();
        RefreshSlots();
    }

    private void RefreshHUD()
    {
        var run = GameManager.Instance.Run;
        if (_goldText) _goldText.text = $"Gold: {run.Gold}";
        if (_rerollCostText) _rerollCostText.text = $"Reroll: {_rerollCost}g";
        if (_rerollButton) _rerollButton.interactable = run.Gold >= _rerollCost;

        bool full = run.ActiveSigils.Count >= RunData.MaxSigils;
        if (_leaveButton) _leaveButton.interactable = true;

        // Disable buy buttons if no gold or slots full
        RefreshSlots();
    }

    private void RefreshSlots()
    {
        if (GameManager.Instance == null) return;
        var run = GameManager.Instance.Run;
        bool full = run.ActiveSigils.Count >= RunData.MaxSigils;

        foreach (Transform child in _sigilContainer)
        {
            var slot = child.GetComponent<SigilSlot>();
            if (slot == null) continue;
            bool canAfford = run.Gold >= slot.Sigil.Cost;
            slot.SetBuyable(canAfford && !full);
        }
    }
}
