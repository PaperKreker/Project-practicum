using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ShopController : MonoBehaviour
{
    public event Action OnReroll;
    public event Action OnBuy;

    [Header("References")]
    [SerializeField] private Transform _sigilContainer;
    [SerializeField] private GameObject _sigilSlotPrefab;
    [SerializeField] private SigilInventoryBar _inventoryBar;

    [Header("HUD")]
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _rerollCostText;
    [SerializeField] private Button _rerollButton;

    private List<Sigil> _offered = new List<Sigil>();
    private int _rerollCost = GameBalance.BaseShopRerollCost;
    private int _rerollsSpent;
    private const int DefaultOfferedCount = 3;

    private void Start()
    {
        if (GameManager.Instance == null) return;
        RefreshRerollCost();
        RollOffers();
        RefreshHUD();
    }

    public void OnRerollClicked()
    {
        var run = GameManager.Instance.Run;
        if (run.Gold < _rerollCost) return;

        run.Gold -= _rerollCost;
        _rerollsSpent++;
        RefreshRerollCost();
        OnReroll?.Invoke();
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

        // Use run's Rng for reproducibility
        int offeredCount = GetOfferedCount();
        for (int i = 0; i < offeredCount && pool.Count > 0; i++)
        {
            int idx = run.Rng.Next(pool.Count);
            _offered.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        foreach (var sigil in _offered)
            SpawnSlot(sigil);

        RefreshSlots();
    }

    private void SpawnSlot(Sigil sigil)
    {
        var go = Instantiate(_sigilSlotPrefab, _sigilContainer);
        var slot = go.GetComponent<SigilSlot>();
        if (slot == null) slot = go.AddComponent<SigilSlot>();
        slot.Setup(sigil, this, GetSigilCost(sigil));
    }

    public void OnSigilPurchased(Sigil sigil)
    {
        var run = GameManager.Instance.Run;
        int price = GetSigilCost(sigil);
        if (run.Gold < price) return;
        if (run.ActiveSigils.Count >= RunData.MaxSigils) return;

        run.Gold -= price;
        run.ActiveSigils.Add(sigil);
        _offered.Remove(sigil);

        foreach (Transform child in _sigilContainer)
        {
            var slot = child.GetComponent<SigilSlot>();
            if (slot != null && slot.Sigil == sigil)
            {
                Destroy(child.gameObject);
                break;
            }
        }

        RefreshHUD();
        RefreshSlots();

        OnBuy?.Invoke();
        _inventoryBar?.Refresh();
    }

    private void RefreshHUD()
    {
        var run = GameManager.Instance.Run;
        if (_goldText) _goldText.text = $"Золото: {run.Gold}";
        if (_rerollCostText) _rerollCostText.text = $"Стоимость: {_rerollCost}g";
        if (_rerollButton) _rerollButton.interactable = run.Gold >= _rerollCost;
        RefreshSlots();
    }

    private void RefreshSlots()
    {
        var run = GameManager.Instance.Run;
        bool full = run.ActiveSigils.Count >= RunData.MaxSigils;

        foreach (Transform child in _sigilContainer)
        {
            var slot = child.GetComponent<SigilSlot>();
            if (slot == null) continue;
            slot.SetBuyable(!full && run.Gold >= GetSigilCost(slot.Sigil));
        }
    }

    private int GetSigilCost(Sigil sigil)
    {
        return GameBalance.GetSigilCost(sigil, GameManager.Instance.Run.Difficulty);
    }

    private void RefreshRerollCost()
    {
        if (GameManager.Instance == null || GameManager.Instance.Run == null)
            return;

        _rerollCost = GameBalance.GetShopRerollCost(_rerollsSpent, GameManager.Instance.Run.Difficulty);
    }

    private int GetOfferedCount()
    {
        if (GameManager.Instance == null || GameManager.Instance.Run == null)
            return DefaultOfferedCount;

        return GameBalance.GetShopOfferCount(GameManager.Instance.Run.Difficulty);
    }
}
