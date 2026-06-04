using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ComboMenuController : MonoBehaviour
{

    [Header("Panel")]
    [SerializeField] private GameObject _panelRoot;

    [Header("Container")]
    [SerializeField] private Transform _rowContainer;
    [SerializeField] private GameObject _rowPrefab;

    [Header("Popup")]
    [SerializeField] private GameObject _popup;
    [SerializeField] private TMP_Text _popupTitle;
    [SerializeField] private TMP_Text _popupDesc;
    [SerializeField] private Transform _popupCardContainer;

    [Header("Config")]
    [SerializeField] private ComboMenuConfig _config;

    [Header("Keys")]
    [SerializeField] private Button _openButton;
    [SerializeField] private Key _toggleKey = Key.H;

    private bool _isOpen;
    private readonly List<GameObject> _spawnedRows = new();
    private readonly List<GameObject> _spawnedCards = new();
    private Coroutine _popupDelayRoutine;

    #region Unity Lifecycle

    private void Awake()
    {
        BuildRows();

        if (_panelRoot != null) _panelRoot.SetActive(false);
        if (_popup != null) _popup.SetActive(false);
    }

    private void Start()
    {
        if (_openButton != null)
            _openButton.onClick.AddListener(Toggle);
    }

    private void OnDestroy()
    {
        if (_openButton != null)
            _openButton.onClick.RemoveListener(Toggle);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[_toggleKey].wasPressedThisFrame)
            Toggle();
    }

    #endregion

    #region Public API

    public void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }

    public void Open()
    {
        _isOpen = true;
        if (_panelRoot != null) _panelRoot.SetActive(true);
        if (_openButton != null) _openButton.interactable = false;
    }

    public void Close()
    {
        _isOpen = false;
        if (_panelRoot != null) _panelRoot.SetActive(false);
        HidePopup();
        StartCoroutine(ReenableOpenButton());
    }

    private System.Collections.IEnumerator ReenableOpenButton()
    {
        yield return null;
        if (_openButton != null) _openButton.interactable = true;
    }

    #endregion

    #region Build Rows

    private void BuildRows()
    {
        if (_rowPrefab == null || _rowContainer == null || _config == null) return;

        foreach (var go in _spawnedRows) if (go) Destroy(go);
        _spawnedRows.Clear();

        foreach (var entry in _config.Entries)
        {
            var row = Instantiate(_rowPrefab, _rowContainer);
            _spawnedRows.Add(row);

            SetChildText(row, "NameLabel", entry.DisplayName);
            SetChildText(row, "DamageLabel", GetBaseDamage(entry.Type).ToString());

            WireHover(row, entry);
        }
    }

    private static void SetChildText(GameObject root, string childName, string value)
    {
        var child = root.transform.Find(childName);
        if (child == null) return;
        var tmp = child.GetComponent<TMP_Text>();
        if (tmp != null) tmp.text = value;
    }

    #endregion

    #region Hover / Popup

    private void WireHover(GameObject row, ComboEntryConfig entry)
    {
        var trigger = row.AddComponent<EventTrigger>();

        var onEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        onEnter.callback.AddListener(_ =>
        {
            if (_popupDelayRoutine != null) StopCoroutine(_popupDelayRoutine);
            _popupDelayRoutine = StartCoroutine(ShowPopupDelayed(row, entry));
        });
        trigger.triggers.Add(onEnter);

        var onExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        onExit.callback.AddListener(_ =>
        {
            if (_popupDelayRoutine != null)
            {
                StopCoroutine(_popupDelayRoutine);
                _popupDelayRoutine = null;
            }
            HidePopup();
        });
        trigger.triggers.Add(onExit);
    }

    private IEnumerator ShowPopupDelayed(GameObject row, ComboEntryConfig entry)
    {
        float delay = _config != null ? _config.PopupDelay : 0.15f;
        yield return new WaitForSecondsRealtime(delay);
        ShowPopup(row, entry);
    }

    private Coroutine _positionRoutine;

    private void ShowPopup(GameObject row, ComboEntryConfig entry)
    {
        if (_popup == null) return;

        if (_popupTitle != null) _popupTitle.text = entry.DisplayName;
        if (_popupDesc != null) _popupDesc.text = entry.Description;

        _popup.SetActive(true);

        SpawnPreviewCards(entry);

        if (_positionRoutine != null) StopCoroutine(_positionRoutine);
        _positionRoutine = StartCoroutine(PositionNextFrame(row));
    }

    private IEnumerator PositionNextFrame(GameObject row)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        PositionPopupNextToRow(row);
        _positionRoutine = null;
    }

    private void HidePopup()
    {
        if (_popup != null) _popup.SetActive(false);
        ClearPreviewCards();
    }

    private void SpawnPreviewCards(ComboEntryConfig entry)
    {
        ClearPreviewCards();
        if (_popupCardContainer == null || _config?.CardPrefab == null) return;

        foreach (var cd in entry.ExampleCards)
        {
            var go = Instantiate(_config.CardPrefab, _popupCardContainer);
            go.transform.localScale = Vector3.one * _config.PreviewCardScale;

            var cv = go.GetComponent<CardView>();
            if (cv != null)
            {
                cv.enabled = false;
                cv.enabled = true;
                cv.Setup(new Card(cd.Suit, cd.Rank), Vector2.zero, Vector2.zero);
                cv.SetInteractable(false);
                cv.enabled = false;
            }

            var ab = go.GetComponent<AnimatedButton>();
            if (ab != null) ab.interactable = false;

            var rt = go.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = Vector2.zero;

            _spawnedCards.Add(go);
        }
    }

    private void ClearPreviewCards()
    {
        foreach (var c in _spawnedCards) if (c) Destroy(c);
        _spawnedCards.Clear();
    }

    private void PositionPopupNextToRow(GameObject row)
    {
        var popupRect = _popup.GetComponent<RectTransform>();
        var rowRect = row.GetComponent<RectTransform>();
        if (popupRect == null || rowRect == null) return;

        Canvas canvas = _popup.GetComponentInParent<Canvas>()?.rootCanvas;
        if (canvas == null) return;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : canvas.worldCamera;

        var canvasRect = (RectTransform)canvas.transform;
        Vector2 popupSize = popupRect.rect.size;
        Vector2 canvasSize = canvasRect.rect.size;

        var corners = new Vector3[4];
        rowRect.GetWorldCorners(corners);

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, corners[1]);
        screenPos.x -= popupSize.x + 8f;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, cam, out Vector2 local);

        if (local.x < -canvasSize.x / 2f)
        {
            screenPos = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            screenPos.x += 8f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, cam, out local);
        }

        float halfPW = popupSize.x / 2f;
        float halfPH = popupSize.y / 2f;
        float halfCW = canvasSize.x / 2f;
        float halfCH = canvasSize.y / 2f;

        local.x = Mathf.Clamp(local.x, -halfCW + halfPW, halfCW - halfPW);
        local.y = Mathf.Clamp(local.y, -halfCH + halfPH, halfCH - halfPH);

        popupRect.anchoredPosition = local;
    }

    #endregion

    #region Data Helpers

    private static int GetBaseDamage(ComboType t) => t switch
    {
        ComboType.High => 4,
        ComboType.Pair => 12,
        ComboType.TwoPair => 24,
        ComboType.Set => 48,
        ComboType.FOK => 150,
        ComboType.Straight => 60,
        ComboType.Flush => 72,
        ComboType.FullHouse => 96,
        ComboType.StraightFlush => 210,
        ComboType.RoyalFlush => 320,
        _ => 0
    };

    private static string GetCardCountLabel(ComboType t) => t switch
    {
        ComboType.High => "1",
        ComboType.Pair => "2",
        ComboType.TwoPair => "4",
        ComboType.Set => "3",
        ComboType.FOK => "4",
        ComboType.Straight => "5",
        ComboType.Flush => "5",
        ComboType.FullHouse => "5",
        ComboType.StraightFlush => "5",
        ComboType.RoyalFlush => "5",
        _ => "—"
    };

    #endregion
}
