using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class CheckerboardGround : MonoBehaviour
{
    [Header("Checkerboard Settings")]
    [SerializeField] private Color color1 = Color.white;
    [SerializeField] private Color color2 = Color.black;
    [SerializeField] private float cellSize = 1f; // 每个格子1单位
    [SerializeField] private float groundSize = 100f; // 地面总大小100单位

    private void Start()
    {
        GenerateCheckerboardMaterial();
        AdjustGroundScale();
    }

    private void GenerateCheckerboardMaterial()
    {
        // 创建小尺寸纹理即可（不需要大纹理）
        int textureSize = 8; // 足够小的纹理尺寸
        Texture2D texture = new Texture2D(textureSize, textureSize)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point
        };

        // 创建基础棋盘格图案
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                bool isColor1 = ((x / (textureSize / 2) + y / (textureSize / 2)) % 2) == 0;
                texture.SetPixel(x, y, isColor1 ? color1 : color2);
            }
        }
        texture.Apply();

        // 计算需要的平铺次数
        float tiles = groundSize / cellSize;

        // 创建材质
        Material material = new Material(Shader.Find("Standard"))
        {
            mainTexture = texture,
            mainTextureScale = new Vector2(tiles, tiles) // 关键设置
        };

        GetComponent<Renderer>().material = material;
    }

    private void AdjustGroundScale()
    {
        // 调整地面对象实际大小
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