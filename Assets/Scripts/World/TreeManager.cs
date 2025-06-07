using System.Collections.Generic;
using UnityEngine;
using System;

public class TreeManager : MonoBehaviour
{
    public static TreeManager Instance { get; private set; }
    public static event Action<Vector2Int> OnTreeRemoved;
    private HashSet<Vector2Int> treePositions = new();
    private Dictionary<Vector2Int, TreeNode> treeNodes = new();
    private HashSet<Vector2Int> markedTreePositions = new();
    private Dictionary<Vector2Int, TreeNode> markedTreeNodes = new();
    private List<Material> treeMat = new();

    private const float cellSize = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    public void SetTreeMaterials(TreeNode treeNode)
    {
        treeMat.Clear(); 
        var renderers = treeNode.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
            treeMat.Add(new Material(renderer.sharedMaterial));
    }

    public void RegisterTree(TreeNode treeNode, Vector3 worldPosition)
    {
        Vector2Int gridPos = WorldToGrid(new Vector2(worldPosition.x, worldPosition.z));
        treePositions.Add(gridPos);
        treeNodes[gridPos] = treeNode;

        if (treeNode != null)
        {
            StartCoroutine(MonitorTreeNode(treeNode, gridPos));
        }
    }

    private System.Collections.IEnumerator MonitorTreeNode(TreeNode treeNode, Vector2Int gridPos)
    {
        while (treeNode != null && treeNode.gameObject != null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        OnTreeDestroyed(gridPos);
    }

    private void OnTreeDestroyed(Vector2Int gridPos)
    {
        if (treePositions.Contains(gridPos))
        {
            treePositions.Remove(gridPos);
            treeNodes.Remove(gridPos);
            OnTreeRemoved?.Invoke(gridPos);
        }

        gridPos.y -= 1; // Match with mark system
        if (markedTreePositions.Contains(gridPos))
        {
            markedTreePositions.Remove(gridPos);
            markedTreeNodes.Remove(gridPos);
        }
    }

    public bool HasTreeAt(Vector2Int gridPosition)
    {
        return treePositions.Contains(gridPosition);
    }

    public HashSet<Vector2Int> GetAllTreePositions()
    {
        return new HashSet<Vector2Int>(treePositions);
    }

    public TreeNode GetTreeNodeAt(Vector2Int gridPosition)
    {
        treeNodes.TryGetValue(gridPosition, out TreeNode node);
        return node;
    }

    public void ClearAllTrees()
    {
        StopAllCoroutines();

        treePositions.Clear();
        treeNodes.Clear();
    }

    #region Tree Mark System
    public bool IsTreeMarked(Vector2Int gridPos)
    {
        return markedTreePositions.Contains(gridPos);
    }

    private void SetTreeColor(TreeNode treeNode, bool mark)
    {
        var renderers = treeNode.GetComponentsInChildren<Renderer>();
        if (mark)
            foreach (var renderer in renderers)
                renderer.material.color = Color.yellow;
        else
            for (int i = 0; i < renderers.Length; ++i)
                renderers[i].material = treeMat[i];
    }

    public void ToggleMarkTree(TreeNode treeNode, Vector3 worldPosition, bool mark)
    {
        Vector2Int gridPos = WorldToGrid(new Vector2(worldPosition.x, worldPosition.z));
        if (mark)
        {
            if (!markedTreePositions.Contains(gridPos))
            {
                markedTreePositions.Add(gridPos);
                markedTreeNodes[gridPos] = treeNode;
                SetTreeColor(treeNode, mark);
            }
        }
        else
        {
            if (markedTreePositions.Contains(gridPos))
            {
                markedTreePositions.Remove(gridPos);
                markedTreeNodes.Remove(gridPos);
                SetTreeColor(treeNode, mark);
            }
        }
        // Debug.Log($"{treeNode} has be {mark}");
    }

    public TreeNode GetNearestMarkedTree(Vector3 position)
    {
        if (markedTreeNodes.Count == 0) return null;

        TreeNode nearestNode = null;
        float minDistance = float.MaxValue;
        Vector2 pos = new(position.x, position.z);

        foreach (var pair in markedTreeNodes)
        {
            if (pair.Value == null) continue;

            Vector2 treePos = new(pair.Value.transform.position.x, pair.Value.transform.position.z);
            float distance = Vector2.Distance(pos, treePos);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestNode = pair.Value;
            }
        }

        return nearestNode;
    }
    #endregion

    private Vector2Int WorldToGrid(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cellSize),
            Mathf.RoundToInt(worldPos.y / cellSize));
    }
}