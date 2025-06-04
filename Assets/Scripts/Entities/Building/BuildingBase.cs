using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static ResourceManager;

[RequireComponent(typeof(Outline))]
public class BuildingBase : MonoBehaviour, ISelectable, ICommandable, IDamageable
{
    [Header("Building Settings")]
    [SerializeField] protected float HP = 100f;
    [SerializeField] protected float maxHP = 100f;
    [SerializeField] protected bool isEnemy = false;
    [SerializeField] protected List<ResourcePack> costs = new();
    [SerializeField] protected float buildTime = 10f;

    [Header("Health Bar Settings")]
    [SerializeField] private GameObject healthBarPrefab; // Ѫ��Ԥ�Ƽ�
    [SerializeField] private Vector3 healthBarOffset = new(0, -40f, 0); // ����ƫ��10����

    [Header("Collision Settings")]
    [SerializeField] private float collisionRadius = 0.5f; // ��λ��ײ�뾶
    [SerializeField] private LayerMask collisionLayerMask; // ��Ҫ�����ײ�Ĳ�

    // Ѫ��ʵ��
    private GameObject healthBarInstance;
    private Slider healthBarSlider;
    private static Canvas uiCanvas; // ͳһ��UI����

    // Selection visual
    private GameObject selectionIndicator;
    private Color selectionColor = Color.green;
    private readonly float indicatorRadius = 1.0f;
    private readonly float indicatorHeightOffset = 0.1f;

    protected bool isSelected = false;
    protected bool isBuilt = false;
    protected float constructionProgress = 0f;

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

    protected virtual void Start()
    {
        StartConstruction();
    }

    protected virtual void Update()
    {
        if (!isBuilt)
        {
            UpdateConstruction();
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
        if ((collisionLayerMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        // ��ǰ��ȡ��ײ�뾶 (�����ظ�����)
        float otherRadius = GetOtherCollisionRadius(other);
        float totalRadius = collisionRadius + otherRadius;

        // ����ˮƽ�������� (����Y��)
        Vector3 otherPos = other.transform.position;
        Vector3 myPos = transform.position;
        Vector3 direction = new Vector3(otherPos.x - myPos.x, 0, otherPos.z - myPos.z);

        // �������������
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

    protected virtual void StartConstruction()
    {
        HP = 0f;
        constructionProgress = 0f;
        ShowHealthBar(true);
    }

    protected virtual void UpdateConstruction()
    {
        if (constructionProgress < 1f)
        {
            constructionProgress += Time.deltaTime / buildTime;
            HP = Mathf.Lerp(0f, maxHP, constructionProgress);
            UpdateHealthBar();
            if (UIManager.Instance != null && UIManager.Instance.currentBuilding == this)
            {
                UIManager.Instance.UpdateBuildingHP(this);
            }
            if (constructionProgress >= 1f)
            {
                CompleteConstruction();
            }
        }
    }

    protected virtual void CompleteConstruction()
    {
        isBuilt = true;
        HP = maxHP;
        UpdateHealthBar();
        ShowHealthBar(false); // ������ɺ�����Ѫ��
    }

    public virtual bool IsBuilt()
    {
        return isBuilt;
    }

    #region ISelectable Implementation
    public virtual void OnSelect()
    {
        isSelected = true;
        selectionIndicator.SetActive(true);
        ShowHealthBar(true);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowBuildingPanel(this);
        }
    }

    public virtual void OnDeselect()
    {
        isSelected = false;
        selectionIndicator.SetActive(false);
        if (isBuilt)
            ShowHealthBar(false);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideBuildingPanel();
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
        if (isEnemy) return;
    }
    #endregion

    #region IDamageable Implementation
    public virtual void TakeDamage(float damage)
    {
        if (!isBuilt) return;

        if (HP >= maxHP)
            HP = maxHP;

        HP -= damage;
        UpdateHealthBar();
        ShowHealthBar(true);

        if (UIManager.Instance != null && UIManager.Instance.currentBuilding == this)
        {
            UIManager.Instance.UpdateBuildingHP(this);
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
        if (healthBarInstance != null)
        {
            healthBarInstance.SetActive(show);
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

    // Visualize selection
    private void CreateCircleIndicator()
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