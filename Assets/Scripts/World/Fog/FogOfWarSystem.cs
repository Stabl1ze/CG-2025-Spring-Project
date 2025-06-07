using System.Collections.Generic;
using UnityEngine;
using static WorldGenerator;

public class FogOfWarSystem : MonoBehaviour
{
    public static FogOfWarSystem Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private GameObject fogPrefab;

    private List<FogRegion> fogRegions = new List<FogRegion>();
    private List<Clearing> allClearings = new List<Clearing>();
    private HashSet<Clearing> unrevealedClearings = new();
    private Dictionary<Clearing, List<GameObject>> clearingResources = new();
    private Vector2 spawnCenter;
    private float spawnRadius;
    private int mapSize;
    private const float cellSize = 1f;

    private HashSet<Vector2Int> passableCells = new();
    private HashSet<FogRegion> revealedRegions = new();

    private void Start()
    {
        TreeManager.OnTreeRemoved += OnTreeRemoved;
    }

    private void OnDestroy()
    {
        TreeManager.OnTreeRemoved -= OnTreeRemoved;
    }

    public void Initialize(List<Clearing> clearings, Vector2 spawnCenter, float spawnRadius, int mapSize)
    {
        this.spawnCenter = spawnCenter;
        this.spawnRadius = spawnRadius;
        this.mapSize = mapSize;
        allClearings = new List<Clearing>(clearings);

        unrevealedClearings.Clear();
        foreach (var clearing in clearings)
            if (!clearing.isSpawn)
                unrevealedClearings.Add(clearing);

        InitializePassableCells();
        ClearAllFogRegions();

        foreach (var clearing in clearings)
        {
            if (!clearing.isSpawn)
            {
                var fogObj = new GameObject($"FogRegion_{clearing.center}");
                var fogRegion = fogObj.AddComponent<FogRegion>();
                fogRegion.Initialize(clearing.center, clearing.radius, fogPrefab);
                fogRegions.Add(fogRegion);
            }
        }

        UpdateFogState();
    }

    private void InitializePassableCells()
    {
        passableCells.Clear();

        int halfSize = mapSize / 2;
        for (int x = -halfSize; x <= halfSize; x++)
        {
            for (int y = -halfSize; y <= halfSize; y++)
            {
                Vector2 worldPos = new Vector2(x, y);
                Vector2Int gridPos = WorldToGrid(worldPos);

                if (IsInAnyClearing(worldPos) && !HasTreeAt(gridPos))
                {
                    passableCells.Add(gridPos);
                }
            }
        }
    }

    private void ClearAllFogRegions()
    {
        foreach (var fogRegion in fogRegions)
        {
            if (fogRegion != null && fogRegion.gameObject != null)
            {
                DestroyImmediate(fogRegion.gameObject);
            }
        }
        fogRegions.Clear();
        revealedRegions.Clear();
    }

    private void OnTreeRemoved(Vector2Int removedTreePosition)
    {
        passableCells.Add(removedTreePosition);

        UpdateFogState();
    }

    public void RegisterClearingResources(Clearing clearing, List<GameObject> resources)
    {
        if (!clearingResources.ContainsKey(clearing))
            clearingResources.Add(clearing, new List<GameObject>());
        clearingResources[clearing].AddRange(resources);

        if (unrevealedClearings.Contains(clearing))
            SetResourcesVisibility(clearing, false);
    }

    private void SetResourcesVisibility(Clearing clearing, bool visible)
    {
        if (clearingResources.TryGetValue(clearing, out var resources))
            foreach (var resource in resources)
                if (resource != null)
                    resource.SetActive(visible);
    }

    private void UpdateFogState()
    {
        HashSet<Vector2Int> reachableCells = PerformGlobalFloodFill();

        foreach (var fogRegion in fogRegions)
        {
            if (revealedRegions.Contains(fogRegion)) continue;

            if (IsRegionConnected(fogRegion, reachableCells))
            {
                fogRegion.SetRevealed(true);
                revealedRegions.Add(fogRegion);

                // 找到对应的空地并显示资源
                var clearing = FindClearingByCenter(fogRegion.Center);
                if (clearing != null && unrevealedClearings.Remove(clearing))
                {
                    SetResourcesVisibility(clearing, true);
                }
            }
        }
    }

    private HashSet<Vector2Int> PerformGlobalFloodFill()
    {
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        HashSet<Vector2Int> spawnGrids = GetGridsInCircle(spawnCenter, spawnRadius);
        foreach (var grid in spawnGrids)
        {
            if (passableCells.Contains(grid) && !visited.Contains(grid))
            {
                visited.Add(grid);
                queue.Enqueue(grid);
            }
        }

        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            foreach (var dir in directions)
            {
                Vector2Int neighbor = current + dir;

                if (passableCells.Contains(neighbor) && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return visited;
    }

    private bool IsRegionConnected(FogRegion fogRegion, HashSet<Vector2Int> reachableCells)
    {
        HashSet<Vector2Int> regionGrids = GetGridsInCircle(fogRegion.Center, fogRegion.Radius);

        foreach (var grid in regionGrids)
        {
            if (reachableCells.Contains(grid))
            {
                return true;
            }
        }

        return false;
    }

    private HashSet<Vector2Int> GetGridsInCircle(Vector2 center, float radius)
    {
        HashSet<Vector2Int> grids = new HashSet<Vector2Int>();

        int minX = Mathf.FloorToInt((center.x - radius) / cellSize);
        int maxX = Mathf.CeilToInt((center.x + radius) / cellSize);
        int minY = Mathf.FloorToInt((center.y - radius) / cellSize);
        int maxY = Mathf.CeilToInt((center.y + radius) / cellSize);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector2 worldPos = GridToWorld(gridPos);

                if (Vector2.Distance(worldPos, center) <= radius)
                {
                    grids.Add(gridPos);
                }
            }
        }

        return grids;
    }

    private bool HasTreeAt(Vector2Int gridPosition)
    {
        return TreeManager.Instance != null && TreeManager.Instance.HasTreeAt(gridPosition);
    }

    private bool IsInAnyClearing(Vector2 position)
    {
        foreach (var clearing in allClearings)
        {
            if (Vector2.Distance(position, clearing.center) <= clearing.radius)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsInUnrevealedClearing(Vector3 position)
    {
        Vector2 pos = new(position.x, position.z);
        foreach (var clearing in unrevealedClearings)
        {
            if (Vector2.Distance(pos, clearing.center) <= clearing.radius)
            {
                return true;
            }
        }
        return false;
    }

    private Clearing FindClearingByCenter(Vector2 center)
    {
        foreach (var clearing in allClearings)
        {
            if (Vector2.Distance(clearing.center, center) < 0.1f)
            {
                return clearing;
            }
        }
        return null;
    }

    private Vector2Int WorldToGrid(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cellSize),
            Mathf.RoundToInt(worldPos.y / cellSize));
    }

    private Vector2 GridToWorld(Vector2Int gridPos)
    {
        return new Vector2(
            gridPos.x * cellSize,
            gridPos.y * cellSize);
    }
}