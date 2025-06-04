using System.Collections.Generic;
using UnityEngine;
using static WorldGenerator;

public class FogOfWarSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject fogPrefab;

    private List<FogRegion> fogRegions = new List<FogRegion>();
    private List<Clearing> allClearings = new List<Clearing>();
    private Vector2 spawnCenter;
    private float spawnRadius;
    private int mapSize;
    private const float cellSize = 1f; // 固定网格大小为1

    // 存储所有可通行网格位置（初始化后只增不减）
    private HashSet<Vector2Int> passableCells = new HashSet<Vector2Int>();

    // 存储已揭示的迷雾区域
    private HashSet<FogRegion> revealedRegions = new HashSet<FogRegion>();

    private void Start()
    {
        // 订阅砍树事件
        TreeManager.OnTreeRemoved += OnTreeRemoved;
    }

    private void OnDestroy()
    {
        // 取消订阅
        TreeManager.OnTreeRemoved -= OnTreeRemoved;
    }

    public void Initialize(List<Clearing> clearings, Vector2 spawnCenter, float spawnRadius, int mapSize)
    {
        this.spawnCenter = spawnCenter;
        this.spawnRadius = spawnRadius;
        this.mapSize = mapSize;
        this.allClearings = new List<Clearing>(clearings);

        // 初始化可通行网格
        InitializePassableCells();

        // 清理之前的迷雾区域
        ClearAllFogRegions();

        // 为所有非初始地点空地创建迷雾
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

        // 计算地图边界（假设地图以(0,0)为中心）
        int halfSize = mapSize / 2;
        for (int x = -halfSize; x <= halfSize; x++)
        {
            for (int y = -halfSize; y <= halfSize; y++)
            {
                Vector2 worldPos = new Vector2(x, y);
                Vector2Int gridPos = WorldToGrid(worldPos);

                // 检查是否在任何空地内且没有树
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
        // 树被移除后，该位置变为可通行
        passableCells.Add(removedTreePosition);

        // 重新计算迷雾状态
        UpdateFogState();
    }

    private void UpdateFogState()
    {
        // 执行全局洪水填充，获取所有可达位置
        HashSet<Vector2Int> reachableCells = PerformGlobalFloodFill();

        foreach (var fogRegion in fogRegions)
        {
            // 跳过已揭示的区域
            if (revealedRegions.Contains(fogRegion)) continue;

            // 检查该区域是否与出生点区域连通
            if (IsRegionConnected(fogRegion, reachableCells))
            {
                fogRegion.SetRevealed(true);
                revealedRegions.Add(fogRegion);
            }
        }
    }

    private HashSet<Vector2Int> PerformGlobalFloodFill()
    {
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        // 添加出生点区域的所有可通行网格作为起点
        HashSet<Vector2Int> spawnGrids = GetGridsInCircle(spawnCenter, spawnRadius);
        foreach (var grid in spawnGrids)
        {
            if (passableCells.Contains(grid) && !visited.Contains(grid))
            {
                visited.Add(grid);
                queue.Enqueue(grid);
            }
        }

        // 洪水填充
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
        // 获取该迷雾区域覆盖的所有网格
        HashSet<Vector2Int> regionGrids = GetGridsInCircle(fogRegion.Center, fogRegion.Radius);

        // 检查区域内的任何网格是否可达
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

        // 计算边界
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