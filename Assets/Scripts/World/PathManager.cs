using System.Collections.Generic;
using UnityEngine;
using static WorldGenerator;

public class PathManager : MonoBehaviour
{
    public static PathManager Instance { get; private set; }

    private HashSet<Vector2Int> passableCells = new();
    private float cellSize = 1f;
    private int mapSize;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void Initialize(int mapSize)
    {
        this.mapSize = mapSize;
        passableCells.Clear();
    }

    public void UpdatePassableCells(List<Clearing> clearings)
    {
        passableCells.Clear();
        int halfSize = mapSize / 2;

        for (int x = -halfSize; x <= halfSize; x++)
        {
            for (int y = -halfSize; y <= halfSize; y++)
            {
                Vector2 worldPos = new Vector2(x, y);
                Vector2Int gridPos = WorldToGrid(worldPos);

                if (IsInAnyClearing(worldPos, clearings) && !TreeManager.Instance.HasTreeAt(gridPos))
                {
                    passableCells.Add(gridPos);
                }
            }
        }
    }

    public void AddPassableCell(Vector2Int gridPosition)
    {
        passableCells.Add(gridPosition);
    }

    public void RemovePassableCell(Vector2Int gridPosition)
    {
        passableCells.Remove(gridPosition);
    }

    public bool IsCellPassable(Vector2Int gridPosition)
    {
        return passableCells.Contains(gridPosition);
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        // Simple BFS pathfinding - can be replaced with A* for better performance
        var path = new List<Vector2Int>();
        var queue = new Queue<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        var parent = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
        };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == end)
            {
                // Reconstruct path
                while (current != start)
                {
                    path.Add(current);
                    current = parent[current];
                }
                path.Reverse();
                return path;
            }

            foreach (var dir in directions)
            {
                var neighbor = current + dir;

                if (passableCells.Contains(neighbor) && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    parent[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return path; // Empty if no path found
    }

    private bool IsInAnyClearing(Vector2 position, List<Clearing> clearings)
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

    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cellSize),
            Mathf.RoundToInt(worldPos.y / cellSize));
    }

    public Vector2 GridToWorld(Vector2Int gridPos)
    {
        return new Vector2(
            gridPos.x * cellSize,
            gridPos.y * cellSize);
    }
}