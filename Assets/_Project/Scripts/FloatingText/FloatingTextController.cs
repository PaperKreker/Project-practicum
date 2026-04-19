using UnityEngine;

public class FloatingTextController : MonoBehaviour
{
    public static FloatingTextController Instance;

    [SerializeField] private GameObject _floatingTextPrefab;
    [SerializeField] private Transform _container;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowText(string text, Vector3 screenPosition)
    {
        GameObject obj = Instantiate(_floatingTextPrefab, _container);
        FloatingText floatingText = obj.GetComponent<FloatingText>();

        floatingText.SetText(text);
        floatingText.SetPosition(screenPosition);
    }
}