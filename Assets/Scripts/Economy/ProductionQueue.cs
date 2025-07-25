using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ProductionQueue : MonoBehaviour
{
    [System.Serializable]
    public class ProductionItem
    {
        public Sprite iconPrefab;
        public GameObject unitPrefab;
        public float productionTime;
        public List<ResourceManager.ResourcePack> costs = new();
    }

    [SerializeField] private ProductionBuilding building;
    [SerializeField] private ProductionItem[] productionItems;
    [SerializeField] private int maxQueueSize = 5;

    private Coroutine currentProductionCoroutine;
    private float currentProductionProgress = 0f;
    public float CurrentProductionProgress => currentProductionProgress;
    public float CurrentProductionTime => queue.Count > 0 ? queue.Peek().productionTime : 0f;

    private Queue<ProductionItem> queue = new();
    public event Action OnQueueChanged;
    private bool isProducing = false;

    public bool CanAddToQueue()
    {
        return queue.Count < maxQueueSize;
    }

    public bool TryAddToQueue(int itemIndex)
    {
        if (building.IsEnemy || !building.IsBuilt) return false;
        if (itemIndex < 0 || itemIndex >= productionItems.Length) return false;
        if (!CanAddToQueue()) return false;

        ProductionItem item = productionItems[itemIndex];

        foreach (var cost in item.costs)
        {
            if (!ResourceManager.Instance.HasEnoughResources(cost.type, cost.amount))
                return false;
        }

        foreach (var cost in item.costs)
        {
            ResourceManager.Instance.SpendResources(cost.type, cost.amount);
        }

        queue.Enqueue(item);

        if (!isProducing)
        {
            currentProductionCoroutine = StartCoroutine(ProductionCoroutine());
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
            currentProductionProgress = 0f;

            float timer = 0f;
            while (timer < currentItem.productionTime)
            {
                timer += Time.deltaTime;
                currentProductionProgress = timer / currentItem.productionTime;
                OnQueueChanged?.Invoke();
                yield return null;
            }

            GameObject newUnit = Instantiate(currentItem.unitPrefab, building.SpawnPoint.position, building.SpawnPoint.rotation);

            if (newUnit.TryGetComponent<UnitBase>(out var unitBase))
            {
                UnitManager.Instance.RegisterUnit(unitBase);

                // Move to rally point if available
                if (building.RallyPoint != null)
                    unitBase.ReceiveCommand(building.RallyPoint.position, null);
            }

            queue.Dequeue();
            currentProductionProgress = 0f;
            OnQueueChanged?.Invoke();
        }

        isProducing = false;
        currentProductionCoroutine = null;
    }

    public bool CancelCurrentProduction()
    {
        if (!isProducing || queue.Count == 0) return false;

        ProductionItem currentItem = queue.Peek();

        foreach (var cost in currentItem.costs)
        {
            int refundAmount = Mathf.RoundToInt(cost.amount);
            if (refundAmount > 0)
            {
                ResourceManager.Instance.AddResources(cost.type, refundAmount);
            }
        }

        queue.Dequeue();

        if (currentProductionCoroutine != null)
        {
            StopCoroutine(currentProductionCoroutine);
            currentProductionCoroutine = null;
        }

        currentProductionProgress = 0f;
        isProducing = false;

        if (queue.Count > 0)
        {
            currentProductionCoroutine = StartCoroutine(ProductionCoroutine());
        }

        OnQueueChanged?.Invoke();
        return true;
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