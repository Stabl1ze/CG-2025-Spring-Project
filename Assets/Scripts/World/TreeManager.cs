using System.Collections.Generic;
using UnityEngine;
using System;

public class TreeManager : MonoBehaviour
{
    public static TreeManager Instance { get; private set; }

    // �¼�������������ʱ����
    public static event Action<Vector2Int> OnTreeRemoved;

    // �洢������ľ��λ�ã��������꣩
    private HashSet<Vector2Int> treePositions = new();

    // �洢��ľResourceNode������
    private Dictionary<Vector2Int, ResourceNode> treeNodes = new();

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

    public void RegisterTree(ResourceNode treeNode, Vector3 worldPosition)
    {
        Vector2Int gridPos = WorldToGrid(new Vector2(worldPosition.x, worldPosition.z));
        treePositions.Add(gridPos);
        treeNodes[gridPos] = treeNode;

        // ������Դ�ڵ�������¼�
        if (treeNode != null)
        {
            // ͨ���������Ƿ�������������ľ�Ƴ�
            StartCoroutine(MonitorTreeNode(treeNode, gridPos));
        }
    }

    private System.Collections.IEnumerator MonitorTreeNode(ResourceNode treeNode, Vector2Int gridPos)
    {
        while (treeNode != null && treeNode.gameObject != null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // ��ľ�ѱ�����
        OnTreeDestroyed(gridPos);
    }

    private void OnTreeDestroyed(Vector2Int gridPos)
    {
        if (treePositions.Contains(gridPos))
        {
            treePositions.Remove(gridPos);
            treeNodes.Remove(gridPos);

            // �����¼�֪ͨս������ϵͳ
            OnTreeRemoved?.Invoke(gridPos);
        }
    }

    public void RemoveTree(Vector3 worldPosition)
    {
        Vector2Int gridPos = WorldToGrid(new Vector2(worldPosition.x, worldPosition.z));
        OnTreeDestroyed(gridPos);
    }

    public bool HasTreeAt(Vector2Int gridPosition)
    {
        return treePositions.Contains(gridPosition);
    }

    public HashSet<Vector2Int> GetAllTreePositions()
    {
        return new HashSet<Vector2Int>(treePositions);
    }

    public ResourceNode GetTreeNodeAt(Vector2Int gridPosition)
    {
        treeNodes.TryGetValue(gridPosition, out ResourceNode node);
        return node;
    }

    public void ClearAllTrees()
    {
        // ֹͣ����Э��
        StopAllCoroutines();

        treePositions.Clear();
        treeNodes.Clear();
    }

    private Vector2Int WorldToGrid(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / cellSize),
            Mathf.RoundToInt(worldPos.y / cellSize));
    }
}