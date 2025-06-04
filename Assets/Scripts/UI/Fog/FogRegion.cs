using UnityEngine;

public class FogRegion : MonoBehaviour
{
    [SerializeField] private GameObject fogCover; // 迷雾视觉表现
    private bool isRevealed = false;
    private Vector2 center;
    private float radius;

    public Vector2 Center => center; // 公共属性供外部访问

    public void Initialize(Vector2 center, float radius, GameObject fogPrefab)
    {
        this.center = center;
        this.radius = radius;

        // 创建迷雾视觉
        fogCover = Instantiate(fogPrefab,
            new Vector3(center.x, 0.1f, center.y),
            Quaternion.identity,
            transform);

        fogCover.transform.localScale = Vector3.one * radius * 2f;
        SetRevealed(false);
    }

    public void SetRevealed(bool revealed)
    {
        isRevealed = revealed;
        fogCover.SetActive(!revealed);
    }

    public bool Contains(Vector2 point)
    {
        return Vector2.Distance(point, center) <= radius;
    }
}