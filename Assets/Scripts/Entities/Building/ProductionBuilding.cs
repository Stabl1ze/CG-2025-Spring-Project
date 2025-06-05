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

    public override void CompleteConstruction()
    {
        base.CompleteConstruction();
        if (productionQueue != null)
            productionQueue.enabled = true;
    }

    #region ICommandable Implementation
    public override void ReceiveCommand(Vector3 targetPosition, GameObject targetObject)
    {
        if (isEnemy) return; // Enemy check
        this.rallyPoint.position = targetPosition;
    }
    #endregion
}