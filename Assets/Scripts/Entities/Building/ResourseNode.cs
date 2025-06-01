using UnityEngine;

public class ResourceNode : MonoBehaviour, ISelectable
{
    [Header("Resource Settings")]
    [SerializeField] private ResourceType resourceType = ResourceType.Gold;
    [SerializeField] private int resourceAmount = 100;
    [SerializeField] private float collectionRadius = 2f;

    // Selection visual
    private GameObject selectionIndicator;
    private Color selectionColor = Color.green;
    private readonly float indicatorRadius = 1.0f;
    private readonly float indicatorHeightOffset = 0.1f;

    protected bool isSelected = false;
    public enum ResourceType { Gold, Wood, Food }

    protected virtual void Awake()
    {
        // Set select indicator
        CreateCircleIndicator();
        selectionIndicator.SetActive(false);
    }

    #region ISelectable Implementation
    public virtual void OnSelect()
    {
        isSelected = true;
        selectionIndicator.SetActive(true);
    }

    public virtual void OnDeselect()
    {
        isSelected = false;
        selectionIndicator.SetActive(false);
    }

    public virtual void OnDoubleClick()
    {
        CameraController.Instance?.FocusOnTarget(transform.position);
    }

    public virtual Vector2 GetXZ()
    {
        return new(transform.position.x, transform.position.z);
    }
    #endregion

    public Vector3 GetCollectionPoint()
    {
        Vector2 randomCircle = Random.onUnitSphere * collectionRadius;
        return transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    public int Collect(int amount)
    {
        int collected = Mathf.Min(amount, resourceAmount);
        resourceAmount -= collected;

        if (resourceAmount <= 0)
        {
            DepleteNode();
        }

        return collected;
    }

    private void DepleteNode()
    {
        Debug.Log($"{resourceType} node depleted");
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, collectionRadius);
    }

    private void CreateCircleIndicator()
    {
        // 创建空游戏对象作为指示器父对象
        selectionIndicator = new GameObject("SelectionIndicator");
        selectionIndicator.transform.SetParent(transform);

        // 计算位置（单位底部上方0.1f）
        var renderer = GetComponent<Renderer>();
        float bottomY = renderer != null ?
            (transform.position.y - renderer.bounds.extents.y) :
            transform.position.y;

        selectionIndicator.transform.localPosition = new Vector3(0, bottomY - transform.position.y + indicatorHeightOffset, 0);

        // 创建圆形
        int segments = 32;
        LineRenderer lineRenderer = selectionIndicator.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = segments + 1;
        lineRenderer.material = new Material(Shader.Find("Unlit/Color")) { color = selectionColor };

        // 设置圆形顶点
        float angle = 0f;
        for (int i = 0; i < segments + 1; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * indicatorRadius;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * indicatorRadius;
            lineRenderer.SetPosition(i, new Vector3(x, 0, z));
            angle += 360f / segments;
        }
    }
}