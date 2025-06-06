using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ConstructionUI : MonoBehaviour, IUIComponent
{
    [Header("Construction UI")]
    [SerializeField] private GameObject constructionPanel;
    [SerializeField] private Transform constructionButtonParent;
    [SerializeField] private GameObject constructionButtonPrefab;
    [SerializeField] private List<BuildingBase> buildingList;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Color validPlacementColor = new(0, 1, 0, 0.5f);
    [SerializeField] private Color invalidPlacementColor = new(1, 0, 0, 0.5f);

    public bool IsActive => constructionPanel != null && constructionPanel.activeSelf;

    private readonly List<GameObject> activeButtons = new();
    private BuildingBase currentGhostBuilding;
    private BuildingBase selectedBuildingPrefab;
    private WorkerUnit currentWorker;
    private bool isInBuildMode;
    private Renderer[] ghostRenderers;

    public void Initialize()
    {
        constructionPanel?.SetActive(false);
    }

    public void Show()
    {
        constructionPanel?.SetActive(true);
    }

    public void Hide()
    {
        constructionPanel?.SetActive(false);
        ClearBuildingButtons();
        CancelGhostBuilding();
    }

    public void UpdateDisplay()
    {
        return;
    }

    public void SetPreviousWorker(WorkerUnit worker)
    {
        currentWorker = worker;
    }

    public void SetConstructableBuildings(List<BuildingBase> buildingList)
    {
        this.buildingList = buildingList;
    }

    public void AddConstructableBuilding(BuildingBase building)
    {
        buildingList.Add(building);
    }

    public void ShowConstructionPanel()
    {
        CreateBuildingButton();
        Show();
    }

    private void CreateBuildingButton()
    {
        ClearBuildingButtons();
        var tempList = buildingList.ToArray();
        for (int i = 0; i < tempList.Length; ++i)
        {
            var buttonObj = Instantiate(constructionButtonPrefab, constructionButtonParent);
            var button = buttonObj.GetComponent<Button>();
            var text = buttonObj.GetComponentInChildren<TMP_Text>();
            var costText = buttonObj.transform.Find("CostText")?.GetComponent<TMP_Text>();

            text.text = tempList[i].gameObject.name;

            int index = i;
            button.onClick.AddListener(() => OnBuildingButtonClicked(index));

            activeButtons.Add(buttonObj);
            buttonObj.SetActive(true);
            buttonObj.transform.SetAsLastSibling();
        }
    }

    // In ConstructionUI.cs, modify the OnBuildingButtonClicked method:
    private void OnBuildingButtonClicked(int buildingIndex)
    {
        if (buildingIndex < 0 || buildingIndex >= buildingList.Count) return;

        // If already in build mode with this building, do nothing
        if (isInBuildMode)
        {
            // Cancel current ghost building if any
            CancelGhostBuilding();
            return;
        }

        var costs = buildingList[buildingIndex].GetCosts();
        if (costText != null && costs != null)
        {
            costText.text = "Cost: ";
            foreach (var cost in costs)
            {
                costText.text += $"{cost.type}:{cost.amount} ";
            }
        }

        selectedBuildingPrefab = buildingList[buildingIndex];
        CreateGhostBuilding(selectedBuildingPrefab);

        isInBuildMode = true;
        InputManager.IsInBuildMode = true;
    }

    private void CreateGhostBuilding(BuildingBase buildingPrefab)
    {
        currentGhostBuilding = Instantiate(buildingPrefab);
        currentGhostBuilding.ShowHealthBar(false);
        ghostRenderers = currentGhostBuilding.GetComponentsInChildren<Renderer>();

        // 设置半透明材质
        foreach (var renderer in ghostRenderers)
        {
            renderer.material = new Material(ghostMaterial);
        }

        // 禁用碰撞体和功能脚本
        var colliders = currentGhostBuilding.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        var buildingScripts = currentGhostBuilding.GetComponents<MonoBehaviour>();
        foreach (var script in buildingScripts)
        {
            script.enabled = false;
        }

        StartCoroutine(GhostBuildingFollowMouse());
    }

    private IEnumerator GhostBuildingFollowMouse()
    {
        while (currentGhostBuilding != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                currentGhostBuilding.transform.position = hit.point;

                bool isValidPosition = CheckPlacementValidity();

                UpdateGhostColor(isValidPosition);

                if (Input.GetMouseButtonDown(0) && isValidPosition)
                {
                    TryPlaceBuilding();
                    yield break;
                }
            }
            if (Input.GetMouseButtonDown(1))
            {
                CancelGhostBuilding();
                yield break;
            }

            yield return null;
        }
    }

    private bool CheckPlacementValidity()
    {
        if (currentGhostBuilding == null) return false;
        foreach (var cost in selectedBuildingPrefab.GetCosts())
        {
            if (!ResourceManager.Instance.HasEnoughResources(cost.type, cost.amount))
            {
                return false;
            }
        }

        Collider[] collidersBuilding = Physics.OverlapBox(
            currentGhostBuilding.transform.position,
            currentGhostBuilding.transform.localScale / 2,
            currentGhostBuilding.transform.rotation,
            LayerMask.GetMask("Buildings")
        );

        Collider[] collidersResource = Physics.OverlapBox(
            currentGhostBuilding.transform.position,
            currentGhostBuilding.transform.localScale / 2,
            currentGhostBuilding.transform.rotation,
            LayerMask.GetMask("Resources")
        );

        return collidersBuilding.Length == 0 && collidersResource.Length == 0;
    }

    private void UpdateGhostColor(bool isValid)
    {
        if (ghostRenderers == null) return;

        Color color = isValid ? validPlacementColor : invalidPlacementColor;
        foreach (var renderer in ghostRenderers)
        {
            renderer.material.color = color;
        }
    }

    private void TryPlaceBuilding()
    {
        if (currentGhostBuilding == null || selectedBuildingPrefab == null) return;

        // 检查资源是否足够
        foreach (var cost in selectedBuildingPrefab.GetCosts())
        {
            if (!ResourceManager.Instance.HasEnoughResources(cost.type, cost.amount))
            {
                Debug.Log("Not enough resources to build!");
                CancelGhostBuilding();
                return;
            }
        }

        // 消耗资源
        foreach (var cost in selectedBuildingPrefab.GetCosts())
        {
            ResourceManager.Instance.SpendResources(cost.type, cost.amount);
        }

        var realBuilding = Instantiate(selectedBuildingPrefab, currentGhostBuilding.transform.position, currentGhostBuilding.transform.rotation);
        BuildingManager.Instance.OnBuildingPlaced(realBuilding);
        currentWorker.ReceiveCommand(realBuilding.transform.position, realBuilding.gameObject);

        isInBuildMode = false;
        InputManager.IsInBuildMode = false;

        Destroy(currentGhostBuilding.gameObject);
        currentGhostBuilding = null;
        selectedBuildingPrefab = null;
    }

    private void CancelGhostBuilding()
    {
        if (currentGhostBuilding != null)
        {
            Destroy(currentGhostBuilding.gameObject);
            currentGhostBuilding = null;
        }
        selectedBuildingPrefab = null;
        isInBuildMode = false;
        InputManager.IsInBuildMode = false;
    }

    private void ClearBuildingButtons()
    {
        foreach (var button in activeButtons)
        {
            if (button != null) Destroy(button);
        }
        activeButtons.Clear();
    }
}