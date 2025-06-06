using UnityEngine;

public class ResourceNode : MonoBehaviour, ISelectable
{
    [Header("Resource Settings")]
    [SerializeField] protected ResourceManager.ResourceType resourceType 
        = ResourceManager.ResourceType.LineR;
    [SerializeField] protected int resourceAmount = 100;
    [SerializeField] protected int collectAmount = 2;

    [Header("Collision Settings")]
    [SerializeField] protected float collisionRadius = 2.0f;
    [SerializeField] protected LayerMask collisionLayerMask;

    // Selection visual
    protected GameObject selectionIndicator;
    protected Color selectionColor = Color.green;
    protected readonly float indicatorRadius = 1.0f;
    protected readonly float indicatorHeightOffset = 0.1f;

    protected bool isSelected = false;

    protected virtual void Awake()
    {
        // Set select indicator
        CreateCircleIndicator();
        selectionIndicator.SetActive(false);

        gameObject.TryGetComponent<SphereCollider>(out var collider);
        if (collider == null)
        {
            collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = collisionRadius;
        }
        gameObject.AddComponent<Rigidbody>().useGravity = false;
        collider.isTrigger = true;
    }

    protected void OnTriggerStay(Collider other)
    {
        if ((collisionLayerMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        float otherRadius = GetOtherCollisionRadius(other);
        float totalRadius = collisionRadius + otherRadius;

        Vector3 otherPos = other.transform.position;
        Vector3 myPos = transform.position;
        Vector3 direction = new Vector3(otherPos.x - myPos.x, 0, otherPos.z - myPos.z);

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector3.forward;
        }

        float distance = direction.magnitude;
        direction.Normalize(); 

        if (distance < totalRadius)
        {
            float overlap = totalRadius - distance;
            Vector3 pushVector = 0.5f * overlap * direction;
            Rigidbody otherRb = other.attachedRigidbody;
            if (otherRb != null)
                otherRb.position += pushVector;
            else
                other.transform.position += pushVector;
        }
    }

    #region ISelectable Implementation
    public virtual void OnSelect()
    {
        isSelected = true;
        selectionIndicator.SetActive(true);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowResourceNodePanel(this);
        }
    }

    public virtual void OnDeselect()
    {
        isSelected = false;
        selectionIndicator.SetActive(false);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideResourceNodePanel();
        }
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

    public int GetResourceAmount()
    {
        return resourceAmount;
    }

    public ResourceManager.ResourceType GetResourceType()
    {
        return resourceType;
    }

    public Vector3 GetCollectionPoint()
    {
        return transform.position;
    }

    public ResourceManager.ResourcePack Collect()
    {
        int collected = Mathf.Min(collectAmount, resourceAmount);
        resourceAmount -= collected;

        if (resourceAmount <= 0) DepleteNode();

        ResourceManager.ResourcePack pack = new()
        {
            type = resourceType,
            amount = collected
        };
        return pack;
    }

    protected virtual void DepleteNode()
    {
        OnDeselect();
        SelectionManager.Instance.DeselectThis(this);
        if (gameObject != null)
            Destroy(gameObject);
        Debug.Log($"{resourceType} node depleted");
    }

    protected void CreateCircleIndicator()
    {
        selectionIndicator = new GameObject("SelectionIndicator");
        selectionIndicator.transform.SetParent(transform);

        var renderer = GetComponent<Renderer>();
        float bottomY = renderer != null ?
            (transform.position.y - renderer.bounds.extents.y) :
            transform.position.y;

        selectionIndicator.transform.localPosition = new Vector3(0, bottomY - transform.position.y + indicatorHeightOffset, 0);

        int segments = 32;
        LineRenderer lineRenderer = selectionIndicator.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = segments + 1;
        lineRenderer.material = new Material(Shader.Find("Unlit/Color")) { color = selectionColor };

        float angle = 0f;
        for (int i = 0; i < segments + 1; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * indicatorRadius;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * indicatorRadius;
            lineRenderer.SetPosition(i, new Vector3(x, 0, z));
            angle += 360f / segments;
        }
    }

    #region Hitbox
    protected float GetOtherCollisionRadius(Collider other)
    {
        other.TryGetComponent<UnitBase>(out var unit);
        if (unit != null) return unit.GetCollisionRadius();

        return 1.0f; // Ä¬ÈÏÖµ
    }

    public float GetCollisionRadius()
    {
        return collisionRadius;
    }
    #endregion
}