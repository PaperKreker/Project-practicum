using UnityEngine;

public class ShopAudio : MonoBehaviour
{
    [SerializeField] private ShopController _shopController;
    [SerializeField] private string _buySound;
    [SerializeField] private string _rerollSound;

    private void OnEnable()
    {
        _shopController.OnBuy += PlayBuySound;
        _shopController.OnReroll += PlayReroll;
    }

    private void OnDisable()
    {
        _shopController.OnBuy -= PlayBuySound;
        _shopController.OnReroll -= PlayReroll;
    }

    private void PlayBuySound()
    {
        AudioManager.Instance.Play(_buySound, Random.Range(0.8f, 1.2f));
    }

    private void PlayReroll()
    {
        AudioManager.Instance.Play(_rerollSound, Random.Range(0.8f, 1.2f));
    }
}
