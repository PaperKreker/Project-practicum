using UnityEngine;
using UnityEngine.UI;

public class TransitionAnimation : MonoBehaviour
{
    [Range(0, 1)]
    [SerializeField] private float _transition;
    [SerializeField] private Image _image;
    private Material _material;

    private void Awake()
    {
        _material = Instantiate(_image.material);
        _image.material = _material;
    }

    private void Update()
    {
        _material.SetFloat("_Transition", _transition);
    }

    private void OnDestroy()
    {
        Destroy(_material);
    }
}
