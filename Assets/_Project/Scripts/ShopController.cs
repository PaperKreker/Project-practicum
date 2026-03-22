using UnityEngine;
using TMPro;

// Placeholder for shop UI
public class ShopController : MonoBehaviour
{
    [SerializeField] private TMP_Text _goldText;

    private void Start()
    {
        if (GameManager.Instance == null) return;
        if (_goldText)
            _goldText.text = $"Gold: {GameManager.Instance.Run.Gold}";
    }

    public void OnLeaveShopClicked()
    {
        GameManager.Instance.OnShopExited();
    }
}
