using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ResourceManager;


public class ProductionQueue : MonoBehaviour
{
    [System.Serializable]
    public class ProductionItem
    {
        public Sprite iconPrefab;
        public GameObject unitPrefab;
        public float productionTime;
        public List<ResourcePack> costs = new();
    }

    [SerializeField] private ProductionBuilding building;
    [SerializeField] private ProductionItem[] productionItems;
    [SerializeField] private int maxQueueSize = 5;

    private Queue<ProductionItem> queue = new();
    public event Action OnQueueChanged;
    private bool isProducing = false;

    public bool CanAddToQueue()
    {
        return queue.Count < maxQueueSize;
    }

    public bool TryAddToQueue(int itemIndex)
    {
        if (!building.IsBuilt()) return false;
        if (itemIndex < 0 || itemIndex >= productionItems.Length) return false;
        if (!CanAddToQueue()) return false;

        ProductionItem item = productionItems[itemIndex];

        // 检查资源是否足够
        Debug.Log(item.costs);
        foreach (var cost in item.costs)
        {
            if (!ResourceManager.Instance.HasEnoughResources(cost.type, cost.amount))
                return false;
        }

        // 扣除资源
        foreach (var cost in item.costs)
        {
            ResourceManager.Instance.SpendResources(cost.type, cost.amount);
        }

        queue.Enqueue(item);

        if (!isProducing)
        {
            StartCoroutine(ProductionCoroutine());
        }

        OnQueueChanged?.Invoke();
        return true;
    }

    private IEnumerator ProductionCoroutine()
    {
        isProducing = true;

        while (queue.Count > 0)
        {
            ProductionItem currentItem = queue.Peek();

            yield return new WaitForSeconds(currentItem.productionTime);

            GameObject newUnit = Instantiate(currentItem.unitPrefab, building.SpawnPoint.position, building.SpawnPoint.rotation);

            // Move to rally point if available
            if (building.RallyPoint != null && newUnit.TryGetComponent<UnitBase>(out var unitBase))
            {
                unitBase.ReceiveCommand(building.RallyPoint.position, null);
            }

            queue.Dequeue();
            OnQueueChanged?.Invoke();
        }

        isProducing = false;
    }

    public int GetQueueCount()
    {
        return queue.Count;
    }

    public int GetQueueMax()
    {
        return maxQueueSize;
    }

    public Queue<ProductionItem> GetQueueItems()
    {
        return queue;
    }

    public bool IsProducing()
    {
        return isProducing;
    }

    public ProductionItem[] GetAvailableItems()
    {
        return productionItems;
    }
}