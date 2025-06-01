using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using static UnityEditor.Progress;

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

    private ProductionBuilding currentProductionBuilding;
    private List<GameObject> activeButtons = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (productionPanel != null)
            productionPanel.SetActive(false);
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
        // �ؼ���飺ȷ�����б�Ҫ��UIԪ�ض��ѷ���
        if (productionPanel == null || productionButtonParent == null ||
            productionButtonPrefab == null || queueCountText == null)
        {
            Debug.LogError("UI elements not assigned in UIManager! Please check the inspector.");
            return;
        }

        currentProductionBuilding = building;
        productionPanel.SetActive(true);

        // ����ɰ�ť
        ClearProductionButtons();

        // �����°�ť
        var items = building.ProductionQueue.GetAvailableItems();
        for (int i = 0; i < items.Length; i++)
        {
            int index = i; // �հ�����

            // ʵ������ť
            GameObject buttonObj = Instantiate(productionButtonPrefab, productionButtonParent);
            buttonObj.SetActive(true);
            activeButtons.Add(buttonObj);
            buttonObj.transform.SetAsLastSibling();

            // ���ð�ť�ı�
            TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
            buttonText.text = items[i].unitPrefab.name;

            // ��ӵ���¼�
            Button button = buttonObj.GetComponent<Button>();
            button.onClick.AddListener(() => OnProductionButtonClicked(index));
        }

        UpdateQueueDisplay();
    }

    private void OnProductionButtonClicked(int itemIndex)
    {
        if (currentProductionBuilding != null && currentProductionBuilding.IsBuilt())
        {
            if (currentProductionBuilding.ProductionQueue.TryAddToQueue(itemIndex))
            {
                UpdateQueueDisplay();
            }
            else
            {
                Debug.Log("Failed to add to production queue");
            }
        }
    }

    public void HideProductionMenu()
    {
        if (productionPanel != null)
        {
            productionPanel.SetActive(false);
        }

        currentProductionBuilding = null;
        ClearProductionButtons();
    }

    private void ClearProductionButtons()
    {
        foreach (GameObject button in activeButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        activeButtons.Clear();
    }

    private void UpdateQueueDisplay()
    {
        if (currentProductionBuilding != null && queueCountText != null)
        {
            queueCountText.text = $"Queue: {currentProductionBuilding.ProductionQueue.GetQueueCount()}";
        }
    }

    private void OnGUI()
    {
        if (GameManager.Instance.GetDebugStatus())
        {
            GUILayout.BeginArea(new Rect(Screen.width - 210, 10, 200, 200));
            GUILayout.Label("Debug Tools");

            if (GUILayout.Button("Add 100 Gold"))
            {
                ResourceManager.Instance.AddResources(ResourceNode.ResourceType.Gold, 100);
            }

            if (GUILayout.Button("Remove 150 Gold"))
            {
                ResourceManager.Instance.SpendResources(ResourceNode.ResourceType.Gold, 150);
            }

            if (GUILayout.Button("Add 100 Wood"))
            {
                ResourceManager.Instance.AddResources(ResourceNode.ResourceType.Wood, 100);
            }

            if (currentProductionBuilding != null && GUILayout.Button("Complete Current Production"))
            {
                // ������ɵ�ǰ������Ŀ�ĵ��Է���
                currentProductionBuilding.ProductionQueue.CompleteCurrentItem();
            }

            GUILayout.EndArea();
        }
    }
}