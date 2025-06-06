using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    private List<BuildingBase> depots = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void OnBuildingPlaced(BuildingBase newBuilding)
    {
        // Check if the placed building is a depot
        if (newBuilding.IsDepot && !depots.Contains(newBuilding))
            depots.Add(newBuilding);
    }

    public void OnBuildingDestroyed(BuildingBase destroyedBuilding)
    {
        if (depots.Contains(destroyedBuilding))
            depots.Remove(destroyedBuilding);
    }

    public void UpdateDepotList()
    {
        depots.Clear();
        var allBuildings = FindObjectsOfType<BuildingBase>();

        foreach (var building in allBuildings)
            if (building.IsDepot)
                depots.Add(building);
    }

    public BuildingBase GetNearestDepot(Vector3 position)
    {
        if (depots.Count == 0) return null;

        BuildingBase nearest = null;
        float minDistance = float.MaxValue;

        foreach (var depot in depots)
        {
            if (depot == null) continue;

            float distance = Vector3.Distance(position, depot.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = depot;
            }
        }

        return nearest;
    }

    // For debugging purposes
    public int GetDepotCount()
    {
        return depots.Count;
    }
}