using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class WorkerUnit : UnitBase
{
    [Header("Worker Settings")]
    [SerializeField] private int resourceCarryCapacity = 10;
    [SerializeField] private float collectionRate = 1f;
    [SerializeField] private int collectionAbility = 1;
    [SerializeField] private float fetchRange = 1f;

    private bool isCollecting = false;
    private bool isDelivering = false;
    private int currentResources = 0;
    private ResourceNode currentResourceNode;
    private MainBase targetBase;
    private bool shouldContinueCollecting = false;

    public override void ReceiveCommand(Vector3 targetPosition, GameObject targetObject)
    {
        // Reset when new command is received
        CancelCollection();

        if (targetObject != null && targetObject.TryGetComponent<ResourceNode>(out ResourceNode node))
        {
            // Start collecting when click on resource
            currentResourceNode = node;
            this.targetPosition = node.transform.position;
            isMoving = true;
            isCollecting = false;
            shouldContinueCollecting = true; // Enable continuous collection
        }
        else if (targetObject != null && targetObject.TryGetComponent<MainBase>(out MainBase mainBase))
        {
            // Set base as target for delivery
            targetBase = mainBase;
            this.targetPosition = mainBase.transform.position;
            isMoving = true;
        }
        else
        {
            // Normal move
            base.ReceiveCommand(targetPosition, targetObject);
            currentResourceNode = null;
            targetBase = null;
            shouldContinueCollecting = false; // Disable continuous collection
        }
    }

    protected override void MoveToTarget()
    {
        base.MoveToTarget();
        if (!isMoving && currentResourceNode != null && !isCollecting && !isDelivering)
        {
            // Check if in range of resource node
            if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
                               new Vector2(targetPosition.x, targetPosition.z)) <= fetchRange)
            {
                StartCollecting();
            }
        }
        else if (!isMoving && targetBase != null && isDelivering)
        {
            // Check if in range of base
            if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
                               new Vector2(targetPosition.x, targetPosition.z)) <= fetchRange)
            {
                DeliverResources();
            }
        }
    }

    #region Collecting Utils
    private void StartCollecting()
    {
        // Check if node depleted while moving
        if (currentResourceNode == null)
            return;

        isCollecting = true;
        InvokeRepeating(nameof(CollectResource), collectionRate, collectionRate);
    }

    private void CollectResource()
    {
        // Click on resource nodes when holding resource
        if (currentResources >= resourceCarryCapacity)
        {
            CancelCollection();
            ReturnToBase();
            return;
        }

        // Collect 
        int collected = currentResourceNode.Collect(collectionAbility);
        if (collected > 0)
        {
            currentResources += collected;
        }
        else
        {
            CancelCollection();
            ReturnToBase();
        }

        if (currentResourceNode == null || currentResources >= resourceCarryCapacity)
        {
            CancelCollection();
            ReturnToBase();
        }
    }

    private void CancelCollection()
    {
        isCollecting = false;
        CancelInvoke(nameof(CollectResource));
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
    #endregion

    #region Easy AI For Repeatly Collection
    public int DeliverResources()
    {
        int delivered = currentResources;
        currentResources = 0;
        ResourceManager.Instance.AddResources(ResourceNode.ResourceType.Gold, delivered);
        UIManager.Instance.ShowFloatingText(targetBase.transform.position, $"+{delivered} Gold", Color.yellow);

        // Reset base reference
        MainBase deliveredBase = targetBase;
        targetBase = null;

        // Check if we should continue collecting
        if (shouldContinueCollecting && currentResourceNode != null)
        {
            ReturnToResourceNode();
        }

        return delivered;
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
}