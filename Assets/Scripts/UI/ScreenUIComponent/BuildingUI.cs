using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class BuildingUI : MonoBehaviour, IUIComponent
{
    [Header("Building UI")]
    [SerializeField] private GameObject productionPanel;
    [SerializeField] private TMP_Text buildingNameText;
    [SerializeField] private TMP_Text buildingHPText;
    [SerializeField] private Transform productionButtonParent;
    [SerializeField] private GameObject productionButtonPrefab;
    [SerializeField] private Button cancelProductionButton;
    [SerializeField] private TMP_Text queueCountText;

    [Header("Queue Visualization")]
    [SerializeField] private Transform queueIconsParent;
    [SerializeField] private GameObject queueItemPrefab;
    [SerializeField] private float queueIconSpacing = 60f;
    [SerializeField] private Vector2 queueStartPosition = new(0, -100);
    [SerializeField] private Image progressBarFill;
    [SerializeField] private Slider progressBarSlider;

    [Header("Tooltip Settings")]
    [SerializeField] private FloatingTooltip tooltip;
    [SerializeField] private float tooltipDelay = 0.5f;
    private float hoverTimer;
    private GameObject currentHoveredButton;

    public BuildingBase CurrentBuilding { get; private set; }
    public ProductionBuilding CurrentPBuilding { get; private set; }
    public bool IsActive => productionPanel != null && productionPanel.activeSelf;

    private readonly List<GameObject> activeButtons = new();
    private readonly List<GameObject> queueVisualItems = new();

    private void Update()
    {
        if (currentHoveredButton != null)
        {
            hoverTimer += Time.deltaTime;
            if (hoverTimer >= tooltipDelay && tooltip != null)
                tooltip.UpdatePosition(currentHoveredButton.transform.position);
        }
    }

    public void Initialize()
    {
        productionPanel?.SetActive(false);
    }

    public void Show()
    {
        productionPanel?.SetActive(true);
    }

    public void Hide()
    {
        productionPanel?.SetActive(false);
        if (CurrentPBuilding != null)
        {
            CurrentPBuilding.ProductionQueue.OnQueueChanged -= UpdateQueueDisplay;
        }
        if (cancelProductionButton != null)
        {
            cancelProductionButton.onClick.RemoveAllListeners();
            cancelProductionButton.gameObject.SetActive(false);
        }
        ClearProductionUI();
        CurrentBuilding = null;
        CurrentPBuilding = null;
    }

    public void UpdateDisplay()
    {
        if (CurrentBuilding != null)
            UpdateBuildingHP(CurrentBuilding);

        if (CurrentPBuilding != null)
            UpdateQueueDisplay();
    }

    public void ShowBuildingPanel(BuildingBase building)
    {
        CurrentBuilding = building;
        buildingNameText.text = $"{building.gameObject.name}";
        UpdateBuildingHP(building);

        if (building is ProductionBuilding pBuilding)
        {
            queueCountText.gameObject.SetActive(true);
            CurrentPBuilding = pBuilding;
            pBuilding.ProductionQueue.OnQueueChanged += UpdateQueueDisplay;
            var items = pBuilding.ProductionQueue.GetAvailableItems();
            for (int i = 0; i < items.Length; i++)
                CreateProductionButton(i, items[i]);

            SetupCancelButton();
        }
        else
        {
            queueCountText.gameObject.SetActive(false);
            if (cancelProductionButton != null)
                cancelProductionButton.gameObject.SetActive(false);
        }

        Show();
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

        // 添加事件触发器
        var eventTrigger = buttonObj.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        // 鼠标进入事件
        var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((e) => OnButtonPointerEnter(buttonObj, item));
        eventTrigger.triggers.Add(pointerEnter);

        // 鼠标离开事件
        var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((e) => OnButtonPointerExit());
        eventTrigger.triggers.Add(pointerExit);

        button.onClick.AddListener(() =>
        {
            if (CurrentPBuilding.ProductionQueue.TryAddToQueue(index))
            {
                // Queue display will update via event
            }
            else
            {
                // ShowFloatingText(productionPanel.transform.position, "Not enough resources!", Color.red);
            }
        });
    }

    private void OnButtonPointerEnter(GameObject button, ProductionQueue.ProductionItem item)
    {
        currentHoveredButton = button;
        hoverTimer = 0f;

        // 构建造价信息字符串
        string costText = "Cost:\n";
        foreach (var cost in item.costs)
        {
            costText += $"{cost.type}: {cost.amount}\n";
        }

        // 显示工具提示
        if (tooltip != null)
        {
            tooltip.ShowTooltip(costText, button.transform.position);
        }
    }

    private void OnButtonPointerExit()
    {
        currentHoveredButton = null;
        hoverTimer = 0f;

        // 隐藏工具提示
        if (tooltip != null)
        {
            tooltip.HideTooltip();
        }
    }

    private void SetupCancelButton()
    {
        if (cancelProductionButton != null)
        {
            cancelProductionButton.gameObject.SetActive(true);
            cancelProductionButton.onClick.RemoveAllListeners();
            cancelProductionButton.onClick.AddListener(CancelCurrentProduction);
            UpdateCancelButtonState();
        }
    }

    private void UpdateQueueDisplay()
    {
        if (CurrentPBuilding == null) return;

        int count = CurrentPBuilding.ProductionQueue.GetQueueCount();
        queueCountText.text = $"Queue: {count}/{CurrentPBuilding.ProductionQueue.GetQueueMax()}";

        UpdateProgressBar();

        UpdateQueueVisualization();

        UpdateCancelButtonState();
    }

    private void CancelCurrentProduction()
    {
        if (CurrentPBuilding?.ProductionQueue != null && CurrentPBuilding.ProductionQueue.IsProducing())
            CurrentPBuilding.ProductionQueue.CancelCurrentProduction();
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
        var queue = CurrentPBuilding.ProductionQueue.GetQueueItems().ToArray();

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

            // Load icon sprite
            var iconSprite = Resources.Load<Sprite>($"Icons/{item.unitPrefab.name}");
            if (iconSprite != null) image.sprite = iconSprite;
            queueVisualItems.Add(icon);
        }
    }

    private void UpdateProgressBar()
    {
        if (CurrentPBuilding == null || progressBarFill == null || progressBarSlider == null) return;

        bool isProducing = CurrentPBuilding.ProductionQueue.IsProducing();
        progressBarSlider.gameObject.SetActive(isProducing);

        float progress = CurrentPBuilding.ProductionQueue.CurrentProductionProgress;
        progressBarSlider.value = progress;
        progressBarFill.color = Color.Lerp(Color.red, Color.green, progress);
    }

    private void UpdateCancelButtonState()
    {
        if (cancelProductionButton != null && CurrentPBuilding?.ProductionQueue != null)
        {
            bool canCancel = CurrentPBuilding.ProductionQueue.IsProducing();
            cancelProductionButton.interactable = canCancel;

            var buttonText = cancelProductionButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = canCancel ? "Cancel" : "Invalid";
            }
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
}