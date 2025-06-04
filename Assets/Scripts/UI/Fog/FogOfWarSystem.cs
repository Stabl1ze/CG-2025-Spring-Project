using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static WorldGenerator;

public class FogOfWarSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject fogPrefab;

    private List<FogRegion> fogRegions = new List<FogRegion>();
    private Vector2 spawnCenter;
    private float spawnRadius;
    private int mapSize;
    private const float cellSize = 1f; // �̶������СΪ1

    public void Initialize(List<Clearing> clearings, Vector2 spawnCenter, float spawnRadius, int mapSize)
    {
        this.spawnCenter = spawnCenter;
        this.spawnRadius = spawnRadius;
        this.mapSize = mapSize;

        // Ϊ���зǳ�ʼ�ص�յش�������
        foreach (var clearing in clearings)
        {
            if (Vector2.Distance(clearing.center, spawnCenter) > spawnRadius + 0.1f)
            {
                var fogObj = new GameObject($"FogRegion_{clearing.center}");
                var fogRegion = fogObj.AddComponent<FogRegion>();
                fogRegion.Initialize(clearing.center, clearing.radius, fogPrefab);
                fogRegions.Add(fogRegion);
            }
        }

        UpdateFogState(clearings);
    }

    private void UpdateFogState(List<Clearing> clearings)
    {
        // ��ȡ���п�ͨ�и��ӣ��յ��ڵĸ��ӣ�
        var passableCells = GetPassableCells(clearings);

        foreach (var fogRegion in fogRegions)
        {
            if (IsConnectedToSpawn(fogRegion, passableCells))
            {
                fogRegion.SetRevealed(true);
            }
        }
    }

    private HashSet<Vector2Int> GetPassableCells(List<Clearing> clearings)
    {
        HashSet<Vector2Int> passableCells = new HashSet<Vector2Int>();

        // ������ͼ�ϵ����и���
        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                Vector3 worldPosition = new Vector3(x - mapSize / 2, 0, y - mapSize / 2);
                Vector2 worldPos2D = new Vector2(worldPosition.x, worldPosition.z);

                // ������λ���Ƿ����κοյ���
                if (IsInAnyClearing(clearings, worldPos2D))
                {
                    Vector2Int gridPos = WorldToGrid(worldPos2D);
                    passableCells.Add(gridPos);
                }
            }
        }

        return passableCells;
    }

    private bool IsInAnyClearing(List<Clearing> clearings, Vector2 position)
    {
        foreach (var clearing in clearings)
        {
            if (Vector2.Distance(position, clearing.center) <= clearing.radius)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsConnectedToSpawn(FogRegion fogRegion, HashSet<Vector2Int> passableCells)
    {
        // ʹ�ù�����������㷨�����ͨ��
        var startCell = WorldToGrid(fogRegion.Center);
        var targetCell = WorldToGrid(spawnCenter);

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(startCell);
        visited.Add(startCell);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            // ��������ʼ������
            if (Vector2.Distance(GridToWorld(current), spawnCenter) <= spawnRadius)
            {
                return true;
            }

            // ���4�����ڸ���
            foreach (var dir in new Vector2Int[] {
                Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left })
            {
                var neighbor = current + dir;

                if (passableCells.Contains(neighbor) && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
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