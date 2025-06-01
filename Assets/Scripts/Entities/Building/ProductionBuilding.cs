using UnityEngine;

public class ProductionBuilding : BuildingBase
{
    [SerializeField] private ProductionQueue productionQueue;
    [SerializeField] private Transform rallyPoint;

    public ProductionQueue ProductionQueue => productionQueue;
    public Transform RallyPoint => rallyPoint;

    protected override void Awake()
    {
        base.Awake();

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

    public void SetRallyPoint()
    {

    }
}