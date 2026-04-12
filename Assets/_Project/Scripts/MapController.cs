using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MapController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _nodeContainer;
    [SerializeField] private GameObject _nodeButtonPrefab;

    [Header("HUD")]
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _actText;

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[MapController] GameManager not found!");
            return;
        }

        UpdateHUD();
        BuildMapUI();
    }

    private void UpdateHUD()
    {
        var run = GameManager.Instance.Run;
        if (_hpText) _hpText.text = $"HP: {run.PlayerHp} / {run.PlayerMaxHp}";
        if (_goldText) _goldText.text = $"Gold: {run.Gold}";
        if (_actText) _actText.text = $"Act {GameManager.Instance.CurrentActIndex + 1}";
    }

    private void BuildMapUI()
    {
        var map = GameManager.Instance.CurrentMap;
        var run = GameManager.Instance.Run;
        int cur = run.CurrentNodeIndex;

        // Which nodes are reachable right now
        MapNode currentNode = map.GetNode(cur);
        var reachable = run.CurrentNodeCompleted
            ? new HashSet<int>(currentNode.NextNodeIndices)
            : new HashSet<int> { cur };

        foreach (var node in map.Nodes)
        {
            GameObject go = Instantiate(_nodeButtonPrefab, _nodeContainer);
            var label = go.GetComponentInChildren<TMP_Text>();
            var button = go.GetComponent<Button>();

            string nodeLabel = node.Type switch
            {
                NodeType.Battle => node.Enemy != null ? node.Enemy.EnemyName : "Враг",
                NodeType.Shop => "Магазин",
                NodeType.Rest => "Отдых",
                _ => "?",
            };
            if (label) label.text = $"[{node.Index + 1}] {nodeLabel}";

            bool interactable = reachable.Contains(node.Index);
            button.interactable = interactable;

            // Capture for lambda
            int capturedIndex = node.Index;
            button.onClick.AddListener(() => OnNodeClicked(capturedIndex));
        }
    }

    private void OnNodeClicked(int nodeIndex)
    {
        GameManager.Instance.EnterNode(nodeIndex);
    }
}
