using UnityEngine;

public class ConstructionQueue : MonoBehaviour
{
    public bool TryAddToQueue(BuildingBase building)
    {
        // Check if resources are sufficient
        foreach (var cost in building.GetCosts())
        {
            if (!ResourceManager.Instance.HasEnoughResources(cost.type, cost.amount))
                return false;
        }

        // Spend resources
        foreach (var cost in building.GetCosts())
        {
            ResourceManager.Instance.SpendResources(cost.type, cost.amount);
        }

        return true;
    }
}