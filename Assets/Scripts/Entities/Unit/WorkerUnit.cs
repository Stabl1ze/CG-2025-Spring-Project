using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;

public class WorkerUnit : UnitBase
{
    [Header("Worker Settings")]
    [SerializeField] private int resourceCarryCapacity = 10;
    [SerializeField] private float collectionRate = 1f;
    [SerializeField] private float fetchRange = 1f;

    [Header("Visual Effects")]
    [SerializeField] private MeshRenderer bodyRenderer;

    private Color originalColor;

    private bool isCollecting = false;
    private bool isDelivering = false;
    private int currentAmount = 0;
    private ResourceManager.ResourceType currentType = ResourceManager.ResourceType.LineR;

    private ResourceNode currentResourceNode;
    private MainBase targetBase;
    private bool shouldContinueCollecting = false;

    protected override void Awake()
    {
        base.Awake();
        if (bodyRenderer != null)
            originalColor = bodyRenderer.material.color;
    }

    public override void ReceiveCommand(Vector3 targetPosition, GameObject targetObject)
    {
        if (isEnemy) return; // Enemy check
        if (targetObject == null)
        {
            base.ReceiveCommand(targetPosition, targetObject);
            return;
        }

        // Reset when new command is received
        CancelCollection();
        isDelivering = false;
        shouldContinueCollecting = false;
        currentResourceNode = null;
        targetBase = null;
        
        ResourceNode node = targetObject.GetComponentInParent<ResourceNode>();
        MainBase mainBase = targetObject.GetComponentInParent<MainBase>();

        if (targetObject != null && node != null)
        {
            // Start collecting when click on resource
            currentResourceNode = node;
            this.targetPosition = node.transform.position;
            isMoving = true;
            isCollecting = false;
            shouldContinueCollecting = true; // Enable continuous collection
        }
        else if (targetObject != null && mainBase != null)
        {
            // Set base as target for delivery
            targetBase = mainBase;
            this.targetPosition = mainBase.transform.position;
            isMoving = true;
            isDelivering = true;
        }
        else  // Normally move
            base.ReceiveCommand(targetPosition, targetObject);

    }

    protected override void MoveToTarget()
    {
        if (currentResourceNode != null && !isCollecting && !isDelivering)
        {
            // Check if in range of resource node
            if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
                               new Vector2(targetPosition.x, targetPosition.z)) <= fetchRange)
            {
                Debug.Log(currentResourceNode);
                StartCollecting();
                return;
            }
        }
        else if (targetBase != null && isDelivering)
        {
            // Check if in range of base
            if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
                               new Vector2(targetPosition.x, targetPosition.z)) <= fetchRange)
            {
                DeliverResources();
                return;
            }
        }

        base.MoveToTarget();
    }

    #region Collecting Utils
    private void StartCollecting()
    {
        // Check if node depleted while moving
        if (currentResourceNode == null)
            return;
        Debug.Log(1111);
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

        // Collect 
        if (currentResourceNode == null)
            return;
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

        // Reset collect status
        currentAmount = 0;
        targetBase = null;

        // Check if we should continue collecting
        if (shouldContinueCollecting && currentResourceNode != null)
        {
            ReturnToResourceNode();
        }

        UpdateVisuals();
        return delivered;
    }

    private void ReturnToBase()
    {
        MainBase mainBase = FindObjectOfType<MainBase>();
        if (mainBase != null)
        {
            targetPosition = mainBase.transform.position;
            targetBase = mainBase;
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
                    bodyRenderer.material.color = Color.black;
                if (currentType == ResourceManager.ResourceType.FaceR)
                    bodyRenderer.material.color = Color.blue;
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