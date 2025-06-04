using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RawImage))]
public class MiniMap : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("Camera Settings")]
    [SerializeField] private Camera minimapCamera;
    [SerializeField] private CameraController mainCameraController;

    [Header("UI Elements")]
    [SerializeField] private RectTransform cameraViewIndicator;
    [SerializeField] private RectTransform minimapRect;

    private const float MapSize = 100f; // 100x100 units
    private const float CameraHeight = 100f;

    private void Awake()
    {
        // 设置小地图相机
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = MapSize / 2f;
        minimapCamera.transform.position = new Vector3(0, CameraHeight, 0);
        minimapCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    private void Update()
    {
        UpdateCameraViewIndicator();
    }

    private void UpdateCameraViewIndicator()
    {
        if (mainCameraController == null || cameraViewIndicator == null) return;

        // 获取主相机视野范围的地面投影
        Vector3[] worldCorners = GetMainCameraViewCorners();
        Vector2[] minimapCorners = new Vector2[4];

        // 转换为小地图上的坐标
        for (int i = 0; i < 4; i++)
        {
            minimapCorners[i] = WorldToMinimapPosition(worldCorners[i]);
        }

        // 更新指示器位置和形状
        UpdateIndicatorShape(minimapCorners);
    }

    private Vector3[] GetMainCameraViewCorners()
    {
        Camera mainCam = mainCameraController.GetComponent<Camera>();
        Vector3[] corners = new Vector3[4];

        // 获取相机视锥体的四个角
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0, 0, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            corners[0] = hit.point;

        ray = mainCam.ViewportPointToRay(new Vector3(0, 1, 0));
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            corners[1] = hit.point;

        ray = mainCam.ViewportPointToRay(new Vector3(1, 1, 0));
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            corners[2] = hit.point;

        ray = mainCam.ViewportPointToRay(new Vector3(1, 0, 0));
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            corners[3] = hit.point;

        return corners;
    }

    private void UpdateIndicatorShape(Vector2[] corners)
    {
        // 计算中心点
        Vector2 center = (corners[0] + corners[1] + corners[2] + corners[3]) / 4f;

        // 计算旋转角度 (基于第一条边)
        Vector2 dir = corners[1] - corners[0];
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // 计算大小 (取对角线长度)
        float width = Vector2.Distance(corners[0], corners[3]);
        float height = Vector2.Distance(corners[0], corners[1]);

        // 更新指示器
        cameraViewIndicator.position = center;
        cameraViewIndicator.sizeDelta = new Vector2(width, height);
        cameraViewIndicator.localEulerAngles = new Vector3(0, 0, angle);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        HandleMinimapClick(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        HandleMinimapClick(eventData);
    }

    private void HandleMinimapClick(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            minimapRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            // 转换为世界坐标
            Vector2 normalizedPoint = Rect.PointToNormalized(
                minimapRect.rect,
                localPoint);

            Vector3 worldPos = new Vector3(
                (normalizedPoint.x - 0.5f) * MapSize,
                0,
                (normalizedPoint.y - 0.5f) * MapSize);

            // 移动主相机看向该位置
            mainCameraController.FocusOnTarget(worldPos);
        }
    }

    private Vector2 WorldToMinimapPosition(Vector3 worldPosition)
    {
        // 将世界坐标转换为小地图上的局部坐标
        float x = (worldPosition.x / MapSize + 0.5f) * minimapRect.rect.width;
        float y = (worldPosition.z / MapSize + 0.5f) * minimapRect.rect.height;

        return new Vector2(x, y);
    }
}