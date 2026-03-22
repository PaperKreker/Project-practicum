using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckView : MonoBehaviour
{
    [SerializeField] private HandController _handController;
    [SerializeField] private TextMeshProUGUI _remainingText;
    [SerializeField] private Image _topCard;

    private Deck _deck;

    private void OnEnable()
    {
        _handController.OnInit += Init;
    }

    private void OnDisable()
    {
        _handController.OnInit -= Init;
        if (_deck != null)
        {
            _deck.OnRefresh -= Refresh;
        }
    }

    private void Init(Deck deck)
    {
        _deck = deck;
        _deck.OnRefresh += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        int remaining = _deck.Remaining;
        _remainingText.text = remaining.ToString();
        _topCard.enabled = remaining != 0;
    }
}
