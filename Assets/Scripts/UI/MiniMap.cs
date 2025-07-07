using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RawImage))]
public class MiniMap : MonoBehaviour, IPointerClickHandler
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
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = MapSize / 2f;
        minimapCamera.transform.position = new Vector3(0, CameraHeight, 0);
        minimapCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            minimapRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            // �����λ��ת��ΪС��ͼUV����(0-1)
            Vector2 normalizedPoint = Rect.PointToNormalized(
                minimapRect.rect,
                localPoint);

            Debug.Log(normalizedPoint);

            // ת����С��ͼ����ӿ�����
            Vector3 viewportPoint = new Vector3(
                normalizedPoint.x,
                normalizedPoint.y,
                minimapCamera.nearClipPlane);

            // ��С��ͼ�����������
            Ray miniMapRay = minimapCamera.ViewportPointToRay(viewportPoint);

            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(miniMapRay, out float distance))
            {
                Vector3 worldPoint = miniMapRay.GetPoint(distance);
                mainCameraController.FocusOnTarget(worldPoint);
            }
        }
    }
}