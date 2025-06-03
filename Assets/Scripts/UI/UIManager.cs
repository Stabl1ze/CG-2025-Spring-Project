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

    [Header("Production UI")]
    [SerializeField] private GameObject productionPanel;
    [SerializeField] private Transform productionButtonParent;
    [SerializeField] private GameObject productionButtonPrefab;
    [SerializeField] private TMP_Text queueCountText;

    [Header("Queue Visualization")]
    [SerializeField] private Transform queueIconsParent;
    [SerializeField] private GameObject queueItemPrefab;
    [SerializeField] private float queueIconSpacing = 60f;
    [SerializeField] private Vector2 queueStartPosition = new(0, -100);
    [SerializeField] private GameObject floatingTextPrefab;

    private ProductionBuilding currentProductionBuilding;
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

    public void ShowProductionMenu(ProductionBuilding building)
    {
        if (productionPanel == null || productionButtonParent == null ||
            productionButtonPrefab == null || queueCountText == null)
        {
            Debug.LogError("UI elements not assigned in UIManager! Please check the inspector.");
            return;
        }

        currentProductionBuilding = building;
        productionPanel.SetActive(true);
        ClearProductionUI();

        // ���Ķ��б仯�¼�
        building.ProductionQueue.OnQueueChanged += UpdateQueueDisplay;

        // ����������ť
        var items = building.ProductionQueue.GetAvailableItems();
        for (int i = 0; i < items.Length; i++)
        {
            CreateProductionButton(i, items[i]);
        }

        UpdateQueueDisplay();
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
            if (currentProductionBuilding.ProductionQueue.TryAddToQueue(index))
            {
                // �¼����Զ�����UpdateQueueDisplay
            }
            else
            {
                // ShowFloatingText(productionPanel.transform.position, "Not enough resources!", Color.red);
            }
        });
    }

    private void UpdateQueueDisplay()
    {
        if (currentProductionBuilding == null) return;

        // ���¶��м����ı�
        int count = currentProductionBuilding.ProductionQueue.GetQueueCount();
        queueCountText.text = $"Queue: {count}";

        // ���¶��п��ӻ�
        UpdateQueueVisualization();
    }

    private void UpdateQueueVisualization()
    {
        // �������ͼ��
        foreach (var icon in queueVisualItems)
        {
            if (icon != null) Destroy(icon);
        }
        queueVisualItems.Clear();

        // ��ȡ��ǰ����
        var queue = currentProductionBuilding.ProductionQueue.GetQueueItems().ToArray();

        // ������ͼ��
        for (int i = 0; i < queue.Length; i++)
        {
            var item = queue[i];
            var icon = Instantiate(queueItemPrefab, queueIconsParent);
            icon.SetActive(true);

            // ����ͼ��λ��
            RectTransform rect = icon.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(
                queueStartPosition.x + i * queueIconSpacing,
                queueStartPosition.y
            );

            // ����ͼ��ͼ����ı�
            var image = icon.GetComponent<Image>();
            var text = icon.GetComponentInChildren<TMP_Text>();

            // ���ص�λͼ��
            var iconSprite = Resources.Load<Sprite>($"Icons/{item.unitPrefab.name}");
            if (iconSprite != null) image.sprite = iconSprite;

            if (text != null) text.text = item.unitPrefab.name;

            queueVisualItems.Add(icon);
        }
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

    public void HideProductionMenu()
    {
        if (currentProductionBuilding != null)
        {
            currentProductionBuilding.ProductionQueue.OnQueueChanged -= UpdateQueueDisplay;
        }

        productionPanel?.SetActive(false);
        currentProductionBuilding = null;
        ClearProductionUI();
    }

    public void ShowFloatingText(Vector3 position, string text, Color color)
    {
        if (floatingTextPrefab == null) return;

        GameObject floatingText = Instantiate(floatingTextPrefab, position, Quaternion.identity);
        TMP_Text textComponent = floatingText.GetComponent<TMP_Text>();
        textComponent.text = text;
        textComponent.color = color;
        Destroy(floatingText, 2f);
    }

    // Debug Mode Tests
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
}