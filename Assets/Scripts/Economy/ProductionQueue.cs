using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProductionQueue : MonoBehaviour
{
    [System.Serializable]
    public class ProductionItem
    {
        public Sprite iconPrefab;
        public GameObject unitPrefab;
        public float productionTime;
        public ResourceCost[] costs;
    }

    [System.Serializable]
    public class ResourceCost
    {
        public ResourceNode.ResourceType type;
        public int amount;
    }

    [SerializeField] private ProductionBuilding building;
    [SerializeField] private ProductionItem[] productionItems;
    [SerializeField] private int maxQueueSize = 5;

    private Queue<ProductionItem> queue = new();
    private bool isProducing = false;

    public bool CanAddToQueue()
    {
        return queue.Count < maxQueueSize;
    }

    public bool TryAddToQueue(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= productionItems.Length) return false;
        if (!CanAddToQueue()) return false;

        ProductionItem item = productionItems[itemIndex];

        // 检查资源是否足够
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

            Debug.Log($"Completed producing: {currentItem.unitPrefab.name}");
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