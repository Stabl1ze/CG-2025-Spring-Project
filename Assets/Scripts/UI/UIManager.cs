using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Resource Display")]
    [SerializeField] private GameObject resourcePanel;
    [SerializeField] private TMP_Text lineRText;
    [SerializeField] private TMP_Text faceRText;
    [SerializeField] private TMP_Text cubeRText;

    [Header("Unit UI")]
    [SerializeField] private GameObject unitPanel;
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private TMP_Text unitHPText;

    [Header("Building UI")]
    [SerializeField] private GameObject productionPanel;
    [SerializeField] private TMP_Text buildingNameText;
    [SerializeField] private TMP_Text buildingHPText;
    [SerializeField] private Transform productionButtonParent;
    [SerializeField] private GameObject productionButtonPrefab;
    [SerializeField] private TMP_Text queueCountText;

    [Header("Resource UI")]
    [SerializeField] private GameObject resourceNodePanel;
    [SerializeField] private TMP_Text resourceNameText;
    [SerializeField] private TMP_Text resourceAmountText;
    [SerializeField] private TMP_Text resourceTypeText;

    [Header("Queue Visualization")]
    [SerializeField] private Transform queueIconsParent;
    [SerializeField] private GameObject queueItemPrefab;
    [SerializeField] private float queueIconSpacing = 60f;
    [SerializeField] private Vector2 queueStartPosition = new(0, -100);
    [SerializeField] private GameObject floatingTextPrefab;

    public BuildingBase currentBuilding;
    public ProductionBuilding currentPBuilding;
    public UnitBase currentUnit;
    public ResourceNode currentResourceNode;
    private readonly List<GameObject> activeButtons = new();
    private readonly List<GameObject> queueVisualItems = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        resourcePanel?.SetActive(true);
        productionPanel?.SetActive(false);
        unitPanel?.SetActive(false);
        resourceNodePanel?.SetActive(false);
    }

    public void UpdateResourceDisplay(Dictionary<ResourceManager.ResourceType, ResourceManager.ResourceData> resources)
    {
        foreach (var resource in resources)
        {
            switch (resource.Key)
            {
                case ResourceManager.ResourceType.LineR:
                    lineRText.text = $"{resource.Value.amount}/{resource.Value.maxCapacity}";
                    break;
                case ResourceManager.ResourceType.FaceR:
                    faceRText.text = $"{resource.Value.amount}/{resource.Value.maxCapacity}";
                    break;
                case ResourceManager.ResourceType.CubeR:
                    cubeRText.text = $"{resource.Value.amount}/{resource.Value.maxCapacity}";
                    break;
            }
        }
    }

    #region Resource Node Panel
    public void ShowResourceNodePanel(ResourceNode resourceNode)
    {
        if (resourceNodePanel == null || resourceAmountText == null || resourceTypeText == null)
        {
            Debug.LogError("UI elements not assigned in UIManager! Please check the inspector.");
            return;
        }

        currentResourceNode = resourceNode;
        resourceNodePanel.SetActive(true);
        resourceNameText.text = $"{resourceNode.gameObject.name}";
        UpdateResourceNodeDisplay(resourceNode);
    }

    public void UpdateResourceNodeDisplay(ResourceNode resourceNode)
    {
        resourceAmountText.text = $"Amount: {resourceNode.GetResourceAmount()}";
        resourceTypeText.text = $"Type: {resourceNode.GetResourceType()}";
    }

    public void HideResourceNodePanel()
    {
        resourceNodePanel?.SetActive(false);
        currentResourceNode = null;
    }
    #endregion

    #region Unit Panel
    public void ShowUnitPanel(UnitBase unit)
    {
        if (unitPanel == null || unitHPText == null)
        {
            Debug.LogError("UI elements not assigned in UIManager! Please check the inspector.");
            return;
        }

        currentUnit = unit;
        unitPanel.SetActive(true);
        unitNameText.text = $"{unit.gameObject.name}";
        UpdateUnitHP(unit);
    }

    public void UpdateUnitHP(UnitBase unit)
    {
        unitHPText.text = $"HP: {Math.Round(unit.GetCurrentHP())}/{unit.GetMaxHP()}";
    }

    public void HideUnitPanel()
    {
        unitPanel?.SetActive(false);
        currentUnit = null;
    }
    #endregion

    #region Building Panel
    public void ShowBuildingPanel(BuildingBase building)
    {
        if (productionPanel == null || productionButtonParent == null ||
            productionButtonPrefab == null || queueCountText == null)
        {
            Debug.LogError("UI elements not assigned in UIManager! Please check the inspector.");
            return;
        }

        currentBuilding = building;
        productionPanel.SetActive(true);
        ClearProductionUI();
        Debug.Log(building.gameObject);
        buildingNameText.text = $"{building.gameObject.name}";
        UpdateBuildingHP(building);

        // If production building
        if (building is ProductionBuilding pBuilding)
        {
            currentPBuilding = pBuilding;
            pBuilding.ProductionQueue.OnQueueChanged += UpdateQueueDisplay;

            // Create production buttons
            var items = pBuilding.ProductionQueue.GetAvailableItems();
            for (int i = 0; i < items.Length; i++)
            {
                CreateProductionButton(i, items[i]);
            }

            UpdateQueueDisplay();
        }
    }

    private void CreateProductionButton(int index, ProductionQueue.ProductionItem item)
    {
        var buttonObj = Instantiate(productionButtonPrefab, productionButtonParent);
        var button = buttonObj.GetComponent<Button>();
        var text = buttonObj.GetComponentInChildren<TMP_Text>();
        var costText = buttonObj.transform.Find("CostText")?.GetComponent<TMP_Text>();

        text.text = item.unitPrefab.name;
        activeButtons.Add(buttonObj);
        buttonObj.SetActive(true);
        buttonObj.transform.SetAsLastSibling();

        button.onClick.AddListener(() =>
        {
            if (currentPBuilding.ProductionQueue.TryAddToQueue(index))
            {
                // Queue display will update via event
            }
            else
            {
                // ShowFloatingText(productionPanel.transform.position, "Not enough resources!", Color.red);
            }
        });
    }

    private void UpdateQueueDisplay()
    {
        if (currentPBuilding == null) return;

        // Update queue count text
        int count = currentPBuilding.ProductionQueue.GetQueueCount();
        queueCountText.text = $"Queue: {count}";

        // Update queue visualization
        UpdateQueueVisualization();
    }

    private void UpdateQueueVisualization()
    {
        // Clear existing icons
        foreach (var icon in queueVisualItems)
        {
            if (icon != null) Destroy(icon);
        }
        queueVisualItems.Clear();

        // Get current queue
        var queue = currentPBuilding.ProductionQueue.GetQueueItems().ToArray();

        // Create new icons
        for (int i = 0; i < queue.Length; i++)
        {
            var item = queue[i];
            var icon = Instantiate(queueItemPrefab, queueIconsParent);
            icon.SetActive(true);

            // Set icon position
            RectTransform rect = icon.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(
                queueStartPosition.x + i * queueIconSpacing,
                queueStartPosition.y
            );

            // Set icon image and text
            var image = icon.GetComponent<Image>();
            var text = icon.GetComponentInChildren<TMP_Text>();

            // Load icon sprite
            var iconSprite = Resources.Load<Sprite>($"Icons/{item.unitPrefab.name}");
            if (iconSprite != null) image.sprite = iconSprite;

            if (text != null) text.text = item.unitPrefab.name;

            queueVisualItems.Add(icon);
        }
    }

    public void UpdateBuildingHP(BuildingBase building)
    {
        buildingHPText.text = $"HP: {Math.Round(building.GetCurrentHP())}/{building.GetMaxHP()}";
    }

    private void ClearProductionUI()
    {
        ClearProductionButtons();
        ClearQueueVisualization();
    }

    private void ClearProductionButtons()
    {
        foreach (var button in activeButtons)
        {
            if (button != null) Destroy(button);
        }
        activeButtons.Clear();
    }

    private void ClearQueueVisualization()
    {
        foreach (var item in queueVisualItems)
        {
            if (item != null) Destroy(item);
        }
        queueVisualItems.Clear();
    }

    public void HideBuildingPanel()
    {
        if (currentPBuilding != null)
        {
            currentPBuilding.ProductionQueue.OnQueueChanged -= UpdateQueueDisplay;
        }

        productionPanel?.SetActive(false);
        currentBuilding = null;
        currentPBuilding = null;
        ClearProductionUI();
    }
    #endregion

    #region Debug Mode Test
    private void OnGUI()
    {
        if (GameManager.Instance?.GetDebugStatus() != true) return;

        GUILayout.BeginArea(new Rect(Screen.width - 210, 10, 200, 200));
        GUILayout.Label("Debug Tools");

        if (GUILayout.Button("Add 100 LineR"))
        {
            ResourceManager.Instance?.AddResources(ResourceManager.ResourceType.LineR, 100);
        }

        if (GUILayout.Button("Remove 150 LineR"))
        {
            ResourceManager.Instance?.SpendResources(ResourceManager.ResourceType.LineR, 150);
        }

        if (GUILayout.Button("Add 100 CubeR"))
        {
            ResourceManager.Instance?.AddResources(ResourceManager.ResourceType.CubeR, 100);
        }

        GUILayout.EndArea();
    }
    #endregion
}