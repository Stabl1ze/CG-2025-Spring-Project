using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    public enum ResourceType { LineR, FaceR, CubeR }

    [System.Serializable]
    public class ResourcePack
    {
        public ResourceType type;
        public int amount;
    }

    [System.Serializable]
    public class ResourceData
    {
        public ResourceType type;
        public int amount;
        public int maxCapacity;
    }

    [SerializeField] private List<ResourceData> resources = new();

    private Dictionary<ResourceType, ResourceData> resourceDict = new();

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

    public bool HasEnoughResources(ResourceType type, int amount)
    {
        if (resourceDict.TryGetValue(type, out ResourceData data))
        {
            return data.amount >= amount;
        }
        return false;
    }

    public bool SpendResources(ResourceType type, int amount)
    {
        if (!HasEnoughResources(type, amount)) return false;

        resourceDict[type].amount -= amount;
        UpdateUI();
        return true;
    }

    public void AddResources(ResourceType type, int amount)
    {
        if (resourceDict.TryGetValue(type, out ResourceData data))
        {
            data.amount = Mathf.Min(data.amount + amount, data.maxCapacity);
            UpdateUI();
        }
    }

    public int GetResourceAmount(ResourceType type)
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