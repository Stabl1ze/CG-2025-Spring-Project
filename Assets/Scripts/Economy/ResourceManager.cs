using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [System.Serializable]
    public class ResourceData
    {
        public ResourceNode.ResourceType type;
        public int amount;
        public int maxCapacity;
    }

    [SerializeField] private List<ResourceData> resources = new List<ResourceData>();

    private Dictionary<ResourceNode.ResourceType, ResourceData> resourceDict = new Dictionary<ResourceNode.ResourceType, ResourceData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeResources();
    }

    private void InitializeResources()
    {
        resourceDict.Clear();
        foreach (var res in resources)
        {
            resourceDict[res.type] = res;
        }
    }

    public bool HasEnoughResources(ResourceNode.ResourceType type, int amount)
    {
        if (resourceDict.TryGetValue(type, out ResourceData data))
        {
            return data.amount >= amount;
        }
        return false;
    }

    public bool SpendResources(ResourceNode.ResourceType type, int amount)
    {
        if (!HasEnoughResources(type, amount)) return false;

        resourceDict[type].amount -= amount;
        UpdateUI();
        return true;
    }

    public void AddResources(ResourceNode.ResourceType type, int amount)
    {
        if (resourceDict.TryGetValue(type, out ResourceData data))
        {
            data.amount = Mathf.Min(data.amount + amount, data.maxCapacity);
            UpdateUI();
        }
    }

    public int GetResourceAmount(ResourceNode.ResourceType type)
    {
        if (resourceDict.TryGetValue(type, out ResourceData data))
        {
            return data.amount;
        }
        return 0;
    }

    private void UpdateUI()
    {
        UIManager.Instance?.UpdateResourceDisplay(resourceDict);
    }
}