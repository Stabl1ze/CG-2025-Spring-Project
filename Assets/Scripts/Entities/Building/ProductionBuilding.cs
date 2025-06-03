using UnityEngine;

public class ProductionBuilding : BuildingBase
{

    [Header("Production Settings")]
    [SerializeField] private ProductionQueue productionQueue;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform rallyPoint;

    private readonly float rallyPointRadius = 1f;
    private readonly bool showRallyPoint = false;

    public ProductionQueue ProductionQueue => productionQueue;
    public Transform SpawnPoint => spawnPoint;
    public Transform RallyPoint => rallyPoint;

    protected override void Awake()
    {
        base.Awake();

        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }

        if (rallyPoint == null)
        {
            rallyPoint = transform;
        }

        if (productionQueue == null)
        {
            productionQueue = GetComponentInChildren<ProductionQueue>();
        }
    }

    protected override void CompleteConstruction()
    {
        base.CompleteConstruction();
        if (productionQueue != null)
            productionQueue.enabled = true;
    }

    public override void OnSelect()
    {
        base.OnSelect();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowProductionMenu(this);
        }
    }

    public override void OnDeselect()
    {
        base.OnDeselect();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideProductionMenu();
        }
    }

    #region ICommandable Implementation
    public override void ReceiveCommand(Vector3 targetPosition, GameObject targetObject)
    {
        if (isEnemy) return; // Enemy check
        this.rallyPoint.position = targetPosition;
    }
    #endregion

    private void OnDrawGizmos()
    {
        if (showRallyPoint && rallyPoint != null)
        {
            // 绘制绿色圆圈表示集结点
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(rallyPoint.position, rallyPointRadius);

            // 绘制从建筑到集结点的连线
            Gizmos.color = Color.green;
            Gizmos.DrawLine(spawnPoint.position, rallyPoint.position);

            // 在集结点位置绘制一个向上的箭头
            Vector3 arrowTip = rallyPoint.position + Vector3.up * 0.5f;
            Gizmos.DrawLine(rallyPoint.position, arrowTip);
            Gizmos.DrawLine(arrowTip, arrowTip + Quaternion.Euler(0, 135, 0) * Vector3.forward * 0.3f);
            Gizmos.DrawLine(arrowTip, arrowTip + Quaternion.Euler(0, 225, 0) * Vector3.forward * 0.3f);
        }
    }
}