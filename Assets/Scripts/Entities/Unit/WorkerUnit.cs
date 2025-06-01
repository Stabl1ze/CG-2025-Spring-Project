using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class WorkerUnit : UnitBase
{
    [Header("Worker Settings")]
    [SerializeField] private int resourceCarryCapacity = 10;
    [SerializeField] private float collectionRate = 1f;
    [SerializeField] private int collectionAbility = 1;

    private bool isCollecting = false;
    private int currentResources = 0;
    private ResourceNode currentResourceNode;

    public override void ReceiveCommand(Vector3 targetPosition, GameObject targetObject)
    {
        CancelInvoke(nameof(CollectResource));

        if (targetObject != null && targetObject.TryGetComponent<ResourceNode>(out ResourceNode node))
        {
            currentResourceNode = node;
            this.targetPosition = node.GetCollectionPoint();
            isMoving = true;
            isCollecting = false;
        }
        else
        {
            base.ReceiveCommand(targetPosition, targetObject);
            currentResourceNode = null;
        }
    }

    protected override void MoveToTarget()
    {
        base.MoveToTarget();
        if (!isMoving && currentResourceNode != null && !isCollecting)
        {
            StartCollecting();
        }
    }

    private void StartCollecting()
    {
        if (currentResourceNode == null) return;

        isCollecting = true;
        InvokeRepeating(nameof(CollectResource), collectionRate, collectionRate);
    }

    private void CollectResource()
    {
        if (currentResourceNode == null || currentResources >= resourceCarryCapacity)
        {
            CancelCollection();
            return;
        }

        int collected = currentResourceNode.Collect(collectionAbility);
        if (collected > 0)
        {
            currentResources += collected;
        }
        else
        {
            CancelCollection();
            Debug.Log($"{gameObject.name}: Resource depleted", this);
        }
    }

    private void CancelCollection()
    {
        CancelInvoke(nameof(CollectResource));
        isCollecting = false;
        if (currentResources > 0)
        {
            ReturnToBase();
        }
    }

    private void ReturnToBase()
    {
        // TODO
        base.ReceiveCommand(Vector3.zero, null);
    }
}