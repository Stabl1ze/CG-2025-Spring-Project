using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEditor.Search;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Resource Display")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text woodText;
    [SerializeField] private TMP_Text foodText;

    [Header("Production UI")]
    [SerializeField] private GameObject productionPanel;
    [SerializeField] private Transform productionButtonParent;
    [SerializeField] private GameObject productionButtonPrefab;
    [SerializeField] private TMP_Text queueCountText;

    [Header("Queue Visualization")]
    [SerializeField] private Transform queueVisualizationParent;
    [SerializeField] private GameObject queueItemPrefab;
    [SerializeField] private float queueItemSpacing = 30f;
    [SerializeField] private GameObject floatingTextPrefab;

    private ProductionBuilding currentProductionBuilding;
    private readonly List<GameObject> activeButtons = new();
    private readonly List<GameObject> queueVisualItems = new();
    private Coroutine queueUpdateCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        productionPanel?.SetActive(false);
    }

    public void UpdateResourceDisplay(Dictionary<ResourceNode.ResourceType, ResourceManager.ResourceData> resources)
    {
        foreach (var resource in resources)
        {
            switch (resource.Key)
            {
                case ResourceNode.ResourceType.Gold:
                    goldText.text = $"Gold: {resource.Value.amount}/{resource.Value.maxCapacity}";
                    break;
                case ResourceNode.ResourceType.Wood:
                    woodText.text = $"Wood: {resource.Value.amount}/{resource.Value.maxCapacity}";
                    break;
                case ResourceNode.ResourceType.Food:
                    foodText.text = $"Food: {resource.Value.amount}/{resource.Value.maxCapacity}";
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

        // Create production buttons
        var items = building.ProductionQueue.GetAvailableItems();
        for (int i = 0; i < items.Length; i++)
        {
            CreateProductionButton(i, items[i]);
        }

        UpdateQueueDisplay();
        queueUpdateCoroutine = StartCoroutine(UpdateQueueVisualization());
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

        if (costText != null)
        {
            string costString = string.Join(" ",
                Array.ConvertAll(item.costs, cost => $"{cost.amount} {cost.type}"));
            costText.text = costString;
        }

        button.onClick.AddListener(() =>
        {
            if (currentProductionBuilding.ProductionQueue.TryAddToQueue(index))
            {
                UpdateQueueDisplay();
            }
            else
            {
                ShowFloatingText(productionPanel.transform.position, "Not enough resources!", Color.red);
            }
        });
    }

    private IEnumerator UpdateQueueVisualization()
    {
        while (productionPanel.activeSelf && currentProductionBuilding != null)
        {
            UpdateQueueDisplay();
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void UpdateQueueDisplay()
    {
        if (currentProductionBuilding == null) return;

        // Update queue count text
        int count = currentProductionBuilding.ProductionQueue.GetQueueCount();
        queueCountText.text = $"Queue: {count}";

        // Update queue visualization
        ClearQueueVisualization();

        var queueItems = currentProductionBuilding.ProductionQueue.GetQueueItems().ToArray();
        for (int i = 0; i < queueItems.Length; i++)
        {
            CreateQueueVisualItem(i, queueItems[i]);
        }
    }

    private void CreateQueueVisualItem(int index, ProductionQueue.ProductionItem item)
    {
        var queueItem = Instantiate(queueItemPrefab, queueVisualizationParent);
        queueItem.GetComponent<Image>().sprite = item.iconPrefab;
        queueItem.transform.localPosition = new Vector3(0, -index * queueItemSpacing, 0);
        queueVisualItems.Add(queueItem);

        if (index == 0 && currentProductionBuilding.ProductionQueue.IsProducing())
        {
            StartCoroutine(UpdateQueueItemProgress(queueItem, item.productionTime));
        }
    }

    private IEnumerator UpdateQueueItemProgress(GameObject queueItem, float productionTime)
    {
        float timer = 0;
        Image progressBar = queueItem.transform.Find("ProgressBar")?.GetComponent<Image>();
        if (progressBar == null) yield break;

        while (timer < productionTime && queueItem != null)
        {
            timer += Time.deltaTime;
            progressBar.fillAmount = timer / productionTime;
            yield return null;
        }

        if (queueItem != null)
        {
            progressBar.fillAmount = 1f;
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
        productionPanel?.SetActive(false);
        currentProductionBuilding = null;

        if (queueUpdateCoroutine != null)
        {
            StopCoroutine(queueUpdateCoroutine);
            queueUpdateCoroutine = null;
        }

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

        if (GUILayout.Button("Add 100 Gold"))
        {
            ResourceManager.Instance?.AddResources(ResourceNode.ResourceType.Gold, 100);
        }

        if (GUILayout.Button("Remove 150 Gold"))
        {
            ResourceManager.Instance?.SpendResources(ResourceNode.ResourceType.Gold, 150);
        }

        if (GUILayout.Button("Add 100 Wood"))
        {
            ResourceManager.Instance?.AddResources(ResourceNode.ResourceType.Wood, 100);
        }

        GUILayout.EndArea();
    }
}