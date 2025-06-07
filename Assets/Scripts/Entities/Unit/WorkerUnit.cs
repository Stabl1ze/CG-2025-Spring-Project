using UnityEngine;

public class WorkerUnit : UnitBase
{
    [Header("Worker Settings")]
    [SerializeField] private int resourceCarryCapacity = 10;
    [SerializeField] private float collectionRate = 1f;
    [SerializeField] private float fetchRange = 1.5f;

    [Header("Visual Effects")]
    [SerializeField] private MeshRenderer bodyRenderer;

    private Color originalColor;

    private bool isCollecting = false;
    private bool isDelivering = false;
    private bool isChopping = false;
    private bool isConstructing = false;
    private int currentAmount = 0;
    private ResourceManager.ResourceType currentType = ResourceManager.ResourceType.LineR;

    private ResourceNode currentResourceNode;
    private BuildingBase constructionTarget;

    protected override void Awake()
    {
        base.Awake();
        if (bodyRenderer != null)
            originalColor = bodyRenderer.material.color;
    }

    public override void ReceiveCommand(Vector3 targetPosition, GameObject targetObject)
    {
        if (isEnemy) return; // Enemy check
        if (isConstructing) constructionTarget.RemoveWorker(this);

        if (targetObject == null)  // Normally move to position
        {
            base.ReceiveCommand(targetPosition, targetObject);
            return;
        }
        
        // Reset when new command is received
        CancelCollection();
        isDelivering = false;
        isChopping = false;
        isConstructing = false;
        currentResourceNode = null;
        constructionTarget = null;

        ResourceNode node = targetObject.GetComponentInParent<ResourceNode>();
        ResourceDepot depot = targetObject.GetComponentInParent<ResourceDepot>();
        BuildingBase building = targetObject.GetComponentInParent<BuildingBase>();

        if (node != null)
        {
            currentResourceNode = node;
            this.targetPosition = node.transform.position;
            isMoving = true;
            isChopping = node is TreeNode;
            return;
        }

        if (building != null)
        {
            this.targetPosition = building.transform.position;
            isMoving = true;

            if (!building.IsBuilt)
            {
                isConstructing = true;
                constructionTarget = building;
                return;
            }
            else if (depot != null)
            {
                isDelivering = true;
                return;
            }
        }

        base.ReceiveCommand(targetPosition, targetObject);
    }

    protected override void MoveToTarget()
    {
        if (currentResourceNode == null && isChopping && !isCollecting && !isDelivering)
        {
            FindNextTree();
            return;
        }

        if (currentResourceNode != null && AbleToFetch() && !isCollecting && !isDelivering)
        {
            StartCollecting();
            return;
        }

        if (isDelivering && AbleToFetch())
        {
            DeliverResources();
            return;
        }

        if (isConstructing && AbleToFetch())
        {
            constructionTarget.AssignWorker(this);
            return;
        }

        base.MoveToTarget();
    }

    private bool AbleToFetch()
    {
        return Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
                               new Vector2(targetPosition.x, targetPosition.z)) <= fetchRange;
    }

    #region ISelectable Implements
    public override void OnSelect()
    {
        isSelected = true;
        selectionIndicator.SetActive(true);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowUnitPanel(this);
            UIManager.Instance.ShowConstructionPanel();
            UIManager.Instance.constructionUI.SetPreviousWorker(this);
        }
    }

    public override void OnDeselect()
    {
        isSelected = false;
        selectionIndicator.SetActive(false);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideUnitPanel();
            UIManager.Instance.HideConstructionPanel();
        }
    }
    #endregion

    #region Collecting Utils
    private void StartCollecting()
    {
        // Check if node depleted while moving
        if (currentResourceNode == null)
            return;
        targetPosition = transform.position;
        isMoving = false;
        isCollecting = true;
        InvokeRepeating(nameof(CollectResource), collectionRate, collectionRate);
    }

    private void CollectResource()
    {
        // Click on resource nodes when holding resource
        if (currentAmount >= resourceCarryCapacity)
        {
            CancelCollection();
            ReturnToBase();
            return;
        }

        if (currentResourceNode == null)
        {
            CancelCollection();
            if (isChopping) FindNextTree();
            return;
        }
        ResourceManager.ResourcePack pack = currentResourceNode.Collect();
        if (pack.amount > 0)
        {
            currentAmount += pack.amount;
            currentType = pack.type;
        }
        CancelCollection();
        ReturnToBase();

        UpdateVisuals();
    }

    private void CancelCollection()
    {
        isCollecting = false;
        CancelInvoke(nameof(CollectResource));
    }
    #endregion

    #region Easy AI For Repeatly Collection
    public int DeliverResources()
    {
        targetPosition = transform.position;
        isMoving = false;

        int delivered = currentAmount;
        ResourceManager.Instance.AddResources(currentType, delivered);

        currentAmount = 0;
        UpdateVisuals();

        if (currentResourceNode != null)
        {
            ReturnToResourceNode();
            return delivered;
        }

        FindNextTree();
        return delivered;
    }

    private void FindNextTree()
    {
        ResourceNode nextTree = TreeManager.Instance.GetNearestMarkedTree(transform.position);
        if (nextTree != null)
        {
            currentResourceNode = nextTree;
            targetPosition = nextTree.transform.position;
            isMoving = true;
            isDelivering = false;
        }
    }

    private void ReturnToBase()
    {
        BuildingBase nearestBase = BuildingManager.Instance.GetNearestDepot(transform.position);
        if (nearestBase != null)
        {
            targetPosition = nearestBase.transform.position;
            isMoving = true;
            isDelivering = true;
        }
    }

    private void ReturnToResourceNode()
    {
        // Check if node depleted while moving
        if (currentResourceNode != null)
        {
            targetPosition = currentResourceNode.transform.position;
            isMoving = true;
            isDelivering = false;
        }
    }
    #endregion

    #region Visualization
    private void UpdateVisuals()
    {
        bool isCarrying = currentAmount > 0;
        if (bodyRenderer != null)
        {
            if (isCarrying)
            {
                if (currentType == ResourceManager.ResourceType.LineR)
                    bodyRenderer.material.color = new Color(96, 74, 52);
                if (currentType == ResourceManager.ResourceType.FaceR)
                    bodyRenderer.material.color = new Color(255, 231, 0);
                if (currentType == ResourceManager.ResourceType.CubeR)
                    bodyRenderer.material.color = Color.cyan;
            }
            else
            {
                bodyRenderer.material.color = originalColor;
            }
        }
    }
    #endregion
}