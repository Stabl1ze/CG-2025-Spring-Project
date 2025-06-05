using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class CheckerboardGround : MonoBehaviour
{
    [Header("Checkerboard Settings")]
    [SerializeField] private Color color1 = Color.white;
    [SerializeField] private Color color2 = Color.black;
    [SerializeField] private float cellSize = 1f; // ÿ������1��λ
    [SerializeField] private float groundSize = 100f; // �����ܴ�С100��λ

    private void Start()
    {
        GenerateCheckerboardMaterial();
        AdjustGroundScale();
    }

    private void GenerateCheckerboardMaterial()
    {
        // ����С�ߴ������ɣ�����Ҫ������
        int textureSize = 8; // �㹻С������ߴ�
        Texture2D texture = new Texture2D(textureSize, textureSize)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point
        };

        // �����������̸�ͼ��
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                bool isColor1 = ((x / (textureSize / 2) + y / (textureSize / 2)) % 2) == 0;
                texture.SetPixel(x, y, isColor1 ? color1 : color2);
            }
        }
        texture.Apply();

        // ������Ҫ��ƽ�̴���
        float tiles = groundSize / cellSize;

        // ��������
        Material material = new Material(Shader.Find("Standard"))
        {
            mainTexture = texture,
            mainTextureScale = new Vector2(tiles, tiles) // �ؼ�����
        };

        GetComponent<Renderer>().material = material;
    }

    private void AdjustGroundScale()
    {
        // �����������ʵ�ʴ�С
        transform.localScale = new Vector3(groundSize, 1, groundSize);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && GetComponent<Renderer>().material != null)
        {
            GenerateCheckerboardMaterial();
            AdjustGroundScale();
        }
    }
#endif
}