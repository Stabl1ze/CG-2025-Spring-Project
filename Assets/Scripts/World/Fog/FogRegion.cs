using UnityEngine;

public class FogRegion : MonoBehaviour
{
    [SerializeField] private GameObject fogCover;
    private bool isRevealed = false;
    private Vector2 center;
    private float radius;

    public Vector2 Center => center;
    public float Radius => radius;

    public void Initialize(Vector2 center, float radius, GameObject fogPrefab)
    {
        this.center = center;
        this.radius = radius;

        fogCover = Instantiate(fogPrefab,
            new Vector3(center.x, 1.1f, center.y),
            Quaternion.identity,
            transform);

        fogCover.transform.localScale = new(2.1f * radius, 0.001f, 2.1f * radius);
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