using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class TutorialPopup : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] public TMP_Text TitleText;
    [SerializeField] public TMP_Text BodyText;
    [SerializeField] public TMP_Text ButtonLabel;
    [SerializeField] public Button DismissButton;

    private void Awake()
    {
        DismissButton.onClick.AddListener(OnDismissClicked);
        gameObject.SetActive(false);
    }

    public void Show(string title, string body, string buttonLabel = "Понятно")
    {
        TitleText.text = title;
        BodyText.text = body;
        ButtonLabel.text = buttonLabel;
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(AnimateIn());
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnDismissClicked()
    {
        Hide();
        TutorialManager.Instance?.UnblockGameplay();
        TutorialManager.Instance?.OnPopupDismissedInternal();
    }

    private IEnumerator AnimateIn()
    {
        float elapsed = 0f;
        const float duration = 0.15f;
        Vector3 start = new Vector3(1f, 0.88f, 1f);

        transform.localScale = start;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(start, Vector3.one,
                Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}
