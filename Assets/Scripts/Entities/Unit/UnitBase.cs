using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Outline))]
public class UnitBase : MonoBehaviour, ISelectable, ICommandable, IDamageable
{
    [Header("Unit Settings")]
    [SerializeField] protected float HP = 100f;
    [SerializeField] protected float maxHP = 100f;
    [SerializeField] protected bool isEnemy = false;

    [Header("Health Bar Settings")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private Vector3 healthBarOffset = new(0, -40f, 0);

    [Header("Collision Settings")]
    [SerializeField] private float collisionRadius = 1.5f; // ������ײ�뾶
    [SerializeField] private LayerMask collisionLayerMask; // ��Ҫ�����ײ�Ĳ�

    // Ѫ��ʵ��
    private GameObject healthBarInstance;
    private Slider healthBarSlider;
    private static Canvas uiCanvas; // ͳһ��UI����

    // Move speed settings
    protected float moveSpeed = 5f;
    protected float rotationSpeed = 10f;
    
    // Selection visual
    private GameObject selectionIndicator;
    private Color selectionColor = Color.green;
    private readonly float indicatorRadius = 1.0f;
    private readonly float indicatorHeightOffset = 0.1f;

    protected bool isSelected = false;
    protected bool isMoving = false;
    protected Vector3 targetPosition;

    public bool IsEnemy => isEnemy;

    protected virtual void Awake()
    {
        // ��ʼ��UI����
        if (uiCanvas == null)
        {
            uiCanvas = GameObject.FindGameObjectWithTag("MainCanvas").GetComponent<Canvas>();
            if (uiCanvas == null)
            {
                Debug.LogError("MainCanvas not found! Make sure there is a Canvas with tag 'MainCanvas' in the scene.");
            }
        }

        // ����Ѫ��
        CreateHealthBar();

        // ����ѡ��ָʾ��
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

    protected virtual void Update()
    {
        if (isMoving)
        {
            MoveToTarget();
        }
        UpdateHealthBarPosition();
    }

    protected virtual void OnDestroy()
    {
        // ���ٽ���ʱͬ������Ѫ��ʵ��
        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isMoving) return;
        if ((collisionLayerMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        Vector3 otherPos = other.transform.position;
        Vector3 myPos = transform.position;
        Vector3 direction = new(otherPos.x - myPos.x, 0, otherPos.z - myPos.z);
        if (direction.sqrMagnitude < Mathf.Epsilon)
            direction = Vector3.forward;

        float distance = direction.magnitude;
        Vector3 normalizedDirection = direction / distance; 

        float overlap = (collisionRadius + GetOtherCollisionRadius(other)) - distance;
        if (overlap > 0)
        {
            Vector3 pushVector = -0.5f * overlap * normalizedDirection;
            transform.position += pushVector;
        }
    }

    protected virtual void MoveToTarget()
    {
        Vector2 targetXY = new(targetPosition.x, targetPosition.z),
            transformXY = new(transform.position.x, transform.position.z);
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // ����Y�᲻��
        if (Vector3.Distance(targetXY, transformXY) > 0.5f)
        {
            transform.position += direction * moveSpeed * Time.deltaTime;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            isMoving = false;
        }
    }

    #region ISelectable Implementation
    public virtual void OnSelect()
    {
        isSelected = true;
        selectionIndicator.SetActive(true);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowUnitPanel(this);
        }
    }

    public virtual void OnDeselect()
    {
        isSelected = false;
        selectionIndicator.SetActive(false);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideUnitPanel();
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

    #region ICommandable Implementation
    public virtual void ReceiveCommand(Vector3 targetPosition, GameObject targetObject)
    {
        if (isEnemy) return; // Enemy check
        isMoving = true;
        this.targetPosition = targetPosition;
    }
    #endregion

    #region IDamageable Implementation
    public virtual void TakeDamage(float damage)
    {
        if (HP >= maxHP)
            HP = maxHP;

        HP -= damage;
        UpdateHealthBar();
        ShowHealthBar(true); // ����ʱ��ʾѪ��

        if (UIManager.Instance != null && UIManager.Instance.currentUnit == this)
        {
            UIManager.Instance.UpdateUnitHP(this);
        }

        if (HP <= 0f)
        {
            OnDeselect();
            SelectionManager.Instance.DeselectThis(this);
            Destroy(gameObject);
        }
    }

    public void ShowHealthBar(bool show)
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.gameObject.SetActive(show);
        }
    }

    public void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.value = HP;
        }
    }

    public float GetCurrentHP()
    {
        return HP;
    }

    public float GetMaxHP()
    {
        return maxHP;
    }
    #endregion

    // ����Ѫ��ʵ��
    private void CreateHealthBar()
    {
        if (healthBarPrefab == null || uiCanvas == null) return;

        healthBarInstance = Instantiate(healthBarPrefab, uiCanvas.transform);
        healthBarSlider = healthBarInstance.GetComponentInChildren<Slider>();

        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHP;
            healthBarSlider.value = HP >= maxHP ? maxHP : HP;
            healthBarInstance.SetActive(true);
        }
        else
        {
            Debug.LogError("HealthBarPrefab is missing Slider component!");
        }
    }

    private void UpdateHealthBarPosition()
    {
        if (healthBarInstance == null || healthBarSlider == null) return;

        // ����������ת��Ϊ��Ļ����
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position);

        // Ӧ��ƫ����
        RectTransform rectTransform = healthBarSlider.GetComponent<RectTransform>();
        rectTransform.position = screenPosition + healthBarOffset;
    }

    // ��Scene��ͼ�л���Gizmos
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
        // ��������Ϸ������Ϊָʾ��������
        selectionIndicator = new GameObject("SelectionIndicator");
        selectionIndicator.transform.SetParent(transform);

        // ����λ�ã���λ�ײ��Ϸ�0.1f��
        var renderer = GetComponent<Renderer>();
        float bottomY = renderer != null ?
            (transform.position.y - renderer.bounds.extents.y) :
            transform.position.y;

        selectionIndicator.transform.localPosition = new Vector3(0, bottomY - transform.position.y + indicatorHeightOffset, 0);

        // ����Բ��
        int segments = 32;
        LineRenderer lineRenderer = selectionIndicator.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = segments + 1;
        lineRenderer.material = new Material(Shader.Find("Unlit/Color")) { color = selectionColor };

        // ����Բ�ζ���
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
    private float GetOtherCollisionRadius(Collider other)
    {
        var unit = other.GetComponent<UnitBase>();
        if (unit != null) return unit.GetCollisionRadius();

        var building = other.GetComponent<BuildingBase>();
        if (building != null) return building.GetCollisionRadius();

        var resource = other.GetComponent<ResourceNode>();
        if (resource != null) return resource.GetCollisionRadius();

        return 1.0f; // Ĭ��ֵ
    }

    // ��ȡ��ײ�뾶
    public float GetCollisionRadius()
    {
        return collisionRadius;
    }
    #endregion
}