using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float panSpeed = 20f;
    [SerializeField] private float panBorderThickness = 10f;
    [SerializeField] private Vector2 panLimitMin = new(-50, -50);
    [SerializeField] private Vector2 panLimitMax = new(50, 50);
    [SerializeField] private float fixedHeight = 30f;
    [SerializeField] Vector3 targetPoint = new(0, 0, -40);

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 20f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 30f;

    [Header("Rotation Settings")]
    [SerializeField] private float horizontalRotationSpeed = 100f;
    [SerializeField] private float defaultVerticalAngle = 45f;

    private Quaternion defaultRotation;
    private Camera cam;
    public static CameraController Instance { get; private set; }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        Vector3 cameraPosition = new(0, fixedHeight, 0)
        {
            z = targetPoint.z - (fixedHeight / Mathf.Tan(defaultVerticalAngle))
        };

        transform.position = cameraPosition;

        transform.LookAt(targetPoint);
        defaultRotation = transform.rotation;
        cam.fieldOfView = (minZoom + maxZoom) / 2f;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentGameState != GameManager.GameState.Playing)
            return;

        HandleMovement();
        HandleZoom();
        HandleRotation();
    }

    private void HandleMovement()
    {
        Vector3 pos = transform.position;

        if (Input.GetKey(KeyCode.W)) pos += Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * panSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) pos -= Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * panSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.A)) pos -= Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized * panSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) pos += Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized * panSpeed * Time.deltaTime;

        if (Input.mousePosition.y >= Screen.height - panBorderThickness)
            pos += Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * panSpeed * Time.deltaTime;
        if (Input.mousePosition.y <= panBorderThickness)
            pos -= Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * panSpeed * Time.deltaTime;
        if (Input.mousePosition.x >= Screen.width - panBorderThickness)
            pos += Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized * panSpeed * Time.deltaTime;
        if (Input.mousePosition.x <= panBorderThickness)
            pos -= Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized * panSpeed * Time.deltaTime;

        Vector3 groundHitPoint = GetGroundHitPoint();

        // 检查地面交点是否超出限制范围
        if (groundHitPoint.x < panLimitMin.x)
            pos.x += panLimitMin.x - groundHitPoint.x;
        if (groundHitPoint.x > panLimitMax.x)
            pos.x -= groundHitPoint.x - panLimitMax.x;
        if (groundHitPoint.z < panLimitMin.y)
            pos.z += panLimitMin.y - groundHitPoint.z;
        if (groundHitPoint.z > panLimitMax.y)
            pos.z -= groundHitPoint.z - panLimitMax.y;

        pos.y = fixedHeight;
        transform.position = pos;
    }

    private Vector3 GetGroundHitPoint()
    {
        // 从相机中心发射射线检测地形交点
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            return hit.point;
        }

        // 如果没有检测到交点，返回相机正下方的地面点
        return new Vector3(transform.position.x, 0, transform.position.z);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        float newZoom = cam.fieldOfView - scroll * zoomSpeed;
        cam.fieldOfView = Mathf.Clamp(newZoom, minZoom, maxZoom);
    }

    private void HandleRotation()
    {
        float keyboardRotation = 0f;
        if (Input.GetKey(KeyCode.E)) keyboardRotation = -1f;
        if (Input.GetKey(KeyCode.Q)) keyboardRotation = 1f;

        if (keyboardRotation != 0f)
        {
            RotateAroundFocusPoint(keyboardRotation * horizontalRotationSpeed * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            transform.rotation = defaultRotation;
        }
    }

    private void RotateAroundFocusPoint(float horizontalInput)
    {
        // 从相机中心发射射线检测地形交点
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            Vector3 focusPoint = hit.point;
            float currentDistance = Vector3.Distance(transform.position, focusPoint);

            // 保存当前垂直角度
            float currentVerticalAngle = transform.eulerAngles.x;

            // 水平旋转
            transform.RotateAround(focusPoint, Vector3.up, horizontalInput);

            // 保持相机高度不变
            Vector3 newPosition = transform.position;
            newPosition.y = fixedHeight;
            transform.position = newPosition;

            // 调整距离以保持与焦点距离不变
            Vector3 direction = (transform.position - focusPoint).normalized;
            transform.position = focusPoint + direction * currentDistance;

            // 恢复垂直角度
            Vector3 euler = transform.eulerAngles;
            transform.eulerAngles = new Vector3(currentVerticalAngle, euler.y, euler.z);
        }
        else
        {
            // 如果没有检测到交点，使用默认旋转方式
            transform.Rotate(0, horizontalInput, 0, Space.World);
        }
    }

    public void SetCameraLimits(Vector2 min, Vector2 max)
    {
        panLimitMin = min;
        panLimitMax = max;
    }

    public void FocusOnTarget(Vector3 position)
    {
        Debug.Log("Enable Focus");
    }
}