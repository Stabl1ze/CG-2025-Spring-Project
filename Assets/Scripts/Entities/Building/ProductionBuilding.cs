using UnityEngine;

public class ProductionBuilding : BuildingBase
{

    [Header("Production Settings")]
    [SerializeField] private ProductionQueue productionQueue;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform rallyPoint;

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

    public void SetRallyPoint(Transform newRallyPoint)
    {
        rallyPoint = newRallyPoint;
    }
}