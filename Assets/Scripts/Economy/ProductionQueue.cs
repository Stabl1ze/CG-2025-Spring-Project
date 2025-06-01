using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductionQueue : MonoBehaviour
{
    [System.Serializable]
    public class ProductionItem
    {
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

    [SerializeField] private ProductionItem[] productionItems;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private int maxQueueSize = 5;

    private Queue<ProductionItem> queue = new Queue<ProductionItem>();
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

    public void CompleteCurrentItem()
    {
        while (queue.Count > 0)
        {
            ProductionItem currentItem = queue.Peek();
            Instantiate(currentItem.unitPrefab, spawnPoint.position, spawnPoint.rotation);
            queue.Dequeue();
        }
        Debug.Log("Current item complete");
    }

    private IEnumerator ProductionCoroutine()
    {
        isProducing = true;

        while (queue.Count > 0)
        {
            ProductionItem currentItem = queue.Peek();

            // 这里可以触发生产开始事件
            Debug.Log($"Started producing: {currentItem.unitPrefab.name}");

            yield return new WaitForSeconds(currentItem.productionTime);

            // 生产完成
            Instantiate(currentItem.unitPrefab, spawnPoint.position, spawnPoint.rotation);
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

    public ProductionItem[] GetAvailableItems()
    {
        return productionItems;
    }
}