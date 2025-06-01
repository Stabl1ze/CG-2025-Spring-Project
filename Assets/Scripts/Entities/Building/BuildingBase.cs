using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Outline))]
public class BuildingBase : MonoBehaviour, ISelectable
{
    [Header("Building Settings")]
    [SerializeField] protected int buildCost = 100;
    [SerializeField] protected float buildTime = 10f;
    [SerializeField] protected float health = 100f;

    // Selection visual
    private GameObject selectionIndicator;
    private Color selectionColor = Color.green;
    private readonly float indicatorRadius = 1.0f;
    private readonly float indicatorHeightOffset = 0.1f;

    protected bool isSelected = false;
    protected bool isBuilt = false;
    public int BuildCost => buildCost;

    protected virtual void Awake()
    {
        // Set select indicator
        CreateCircleIndicator();
        selectionIndicator.SetActive(false);
    }

    protected virtual void Start()
    {
        StartConstruction();
    }

    protected virtual void StartConstruction()
    {
        // TODO: ADD ANIMATION
        Invoke(nameof(CompleteConstruction), buildTime);
    }

    protected virtual void CompleteConstruction()
    {
        isBuilt = true;
    }
    public virtual bool IsBuilt()
    {
        return isBuilt;
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

    public virtual void TakeDamage(float amount)
    {
        if (!isBuilt) return;

        health -= amount;
        if (health <= 0)
        {
            DestroyBuilding();
        }
    }

    protected virtual void DestroyBuilding()
    {
        Debug.Log($"{gameObject.name} destroyed");
        Destroy(gameObject);
    }

    // 在Scene视图中绘制Gizmos
    protected virtual void OnDrawGizmosSelected()
    {
        if (!isSelected) return;

        var renderer = GetComponent<Renderer>();
        if (renderer == null) return;

        float bottomY = transform.position.y - renderer.bounds.extents.y;
        Vector3 indicatorPos = new Vector3(
            transform.position.x,
            bottomY + indicatorHeightOffset,
            transform.position.z
        );

        Gizmos.color = selectionColor;
        Gizmos.DrawWireSphere(indicatorPos, indicatorRadius);
    }

    // Visualize selection
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
