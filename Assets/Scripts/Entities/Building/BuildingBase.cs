using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Outline))]
public class BuildingBase : MonoBehaviour, ISelectable, ICommandable, IDamageable
{
    [Header("Building Settings")]
    [SerializeField] protected float HP = 100f;
    [SerializeField] protected float maxHP = 100f;
    [SerializeField] protected bool isEnemy = false;
    [SerializeField] protected int buildCost = 100;
    [SerializeField] protected float buildTime = 10f;

    [Header("Health Bar Settings")]
    [SerializeField] private GameObject healthBarPrefab; // 血条预制件
    [SerializeField] private Vector3 healthBarOffset = new(0, -40f, 0); // 向下偏移10像素

    // 血条实例
    private GameObject healthBarInstance;
    private Slider healthBarSlider;
    private static Canvas uiCanvas; // 统一的UI画布

    // Selection visual
    private GameObject selectionIndicator;
    private Color selectionColor = Color.green;
    private readonly float indicatorRadius = 1.0f;
    private readonly float indicatorHeightOffset = 0.1f;

    protected bool isSelected = false;
    protected bool isBuilt = false;
    protected float constructionProgress = 0f;

    public bool IsEnemy => isEnemy;
    public int BuildCost => buildCost;

    protected virtual void Awake()
    {
        // 初始化UI画布
        if (uiCanvas == null)
        {
            uiCanvas = GameObject.FindGameObjectWithTag("MainCanvas").GetComponent<Canvas>();
            if (uiCanvas == null)
            {
                Debug.LogError("MainCanvas not found! Make sure there is a Canvas with tag 'MainCanvas' in the scene.");
            }
        }

        // 创建血条
        CreateHealthBar();

        // 设置选择指示器
        CreateCircleIndicator();
        selectionIndicator.SetActive(false);

        gameObject.tag = isEnemy ? "Enemy" : "Ally";
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
        // 销毁建筑时同步销毁血条实例
        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance);
        }
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
        ShowHealthBar(false); // 建造完成后隐藏血条
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
    }

    public virtual void OnDeselect()
    {
        isSelected = false;
        selectionIndicator.SetActive(false);
        ShowHealthBar(false);
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
        ShowHealthBar(true); // 受伤时显示血条

        if (HP <= 0f)
            Destroy(gameObject);
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

    // 创建血条实例
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

        // 将世界坐标转换为屏幕坐标
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position);

        // 应用偏移量
        RectTransform rectTransform = healthBarSlider.GetComponent<RectTransform>();
        rectTransform.position = screenPosition + healthBarOffset;
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
}