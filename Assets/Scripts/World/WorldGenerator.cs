using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using static WorldGenerator.Clearing;

public class WorldGenerator : MonoBehaviour
{
    public static WorldGenerator Instance { get; private set; }

    [Header("Generation Settings")]
    [SerializeField] private Terrain terrainPrefab;
    [SerializeField] private int mapSize = 100;
    [SerializeField] private Vector2 spawnCenter = new(0, -36);

    [Header("Boundary Settings")]
    [SerializeField] private GameObject boundaryWallPrefab; 
    [SerializeField] private GameObject waterPlanePrefab;
    [SerializeField] private float boundaryHeight = 10f;
    [SerializeField] private float waterLevel = -0.5f;

    [Header("Resource Nodes Prefab")]
    [SerializeField] private GameObject treePrefab;
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private GameObject facePrefab;
    [SerializeField] private GameObject cubePrefab;

    [Header("Enemies Prefab")]
    public GameObject[] enemyPrefab;

    [Header("Fog of War")]
    [SerializeField] private FogOfWarSystem fogOfWarSystem;

    Vector3 spawnCenterV3 = new(0, 0, -36);

    [System.Serializable]
    public class Clearing
    {
        public Vector2 center;
        public float radius;
        public ClearingSize size;
        public bool isSpawn;

        public enum ClearingSize { Small = 5, Medium = 8, Large = 12 }
    }

    [Header("Clearing Generation")]
    [SerializeField] private int numS = 3;
    [SerializeField] private int numM = 3;
    [SerializeField] private int numL = 3;

    private Terrain generatedTerrain;

    [Header("Starting Units")]
    [SerializeField] private WorkerUnit workerPrefab;
    [SerializeField] private MainBase basePrefab;
    [SerializeField] private ResourceDepot depotPrefab;

    [Header("Ground Material Settings")]
    [SerializeField] private Material groundMaterial;
    [SerializeField] private Color color1 = Color.white;
    [SerializeField] private Color color2 = Color.black;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float groundSize = 100f;

    private int difficulty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    public void GenerateNewWorld()
    {
        difficulty = PlayerPrefs.GetInt("GameDifficulty", 0);
        // Debug.Log(difficulty);

        StartCoroutine(GenerateWorldRoutine());
    }

    private IEnumerator GenerateWorldRoutine()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("OverWorld");

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        GenerateBaseTerrain();

        List<Clearing> clearings = GenerateClearings();

        SpawnEnemies(clearings);

        FillTrees(clearings);

        SpawnStartingUnits();

        fogOfWarSystem.Initialize(clearings, spawnCenter, (float)ClearingSize.Medium, mapSize);

        GenerateResourcesInClearings(clearings);

        BuildingManager.Instance.UpdateDepotList();
    }

    private void GenerateBaseTerrain()
    {
        if (terrainPrefab == null)
        {
            Debug.LogError("Terrain prefab is not assigned!");
            return;
        }

        // 实例化Terrain
        generatedTerrain = Instantiate(terrainPrefab, spawnCenter, Quaternion.identity);
        generatedTerrain.name = "GeneratedTerrain";
        generatedTerrain.transform.position = new Vector3(-mapSize / 2f, 0, -mapSize / 2f);
        generatedTerrain.terrainData.size = new Vector3(mapSize, 1, mapSize);

        // 应用棋盘格材质
        ApplyCheckerboardMaterial();

        // 生成水体
        GenerateWaterBoundary();

        // 生成空气墙
        GenerateBoundaryWalls();
    }

    private void ApplyCheckerboardMaterial()
    {
        // 创建棋盘格材质
        Material checkerMat = new Material(Shader.Find("Standard"));
        checkerMat.name = "CheckerboardMaterial";

        // 创建小尺寸纹理
        int textureSize = 8; // 小纹理即可
        Texture2D texture = new Texture2D(textureSize, textureSize)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point
        };

        // 填充棋盘格图案
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                bool isColor1 = ((x / (textureSize / 2) + y / (textureSize / 2)) % 2) == 0;
                texture.SetPixel(x, y, isColor1 ? color1 : color2);
            }
        }
        texture.Apply();

        // 设置材质属性
        checkerMat.mainTexture = texture;
        checkerMat.mainTextureScale = new Vector2(groundSize / cellSize, groundSize / cellSize);

        // 应用材质到地形
        generatedTerrain.materialTemplate = checkerMat;
    }

    // 预留的公开接口
    public int GetMapSize() => mapSize;
    public Vector3 GetSpawnCenter() => spawnCenter;

    #region Clearing Generation System
    public List<Clearing> GenerateClearings()
    {
        List<Clearing> clearings = new()
        {
            // 1. 添加出生点空地（中型）
            new Clearing
            {
                center = spawnCenter,
                size = ClearingSize.Medium,
                radius = (float)ClearingSize.Medium,
                isSpawn = true
            }
        };

        int smallCount = 0;
        int mediumCount = 1; // 出生点已占一个
        int largeCount = 0;

        // 2. 不断尝试生成，直到达到各自目标数量
        int maxAttempts = 10000;
        int attempts = 0;

        while ((smallCount < numS || mediumCount < numM || largeCount < numL) && attempts < maxAttempts)
        {
            attempts++;

            // 随机在地图范围内挑选一个点
            float half = mapSize / 2f;
            float x = Random.Range(-half, half);
            float z = Random.Range(-half, half);
            Vector2 candidate = new Vector2(x, z);

            // 决定该点所在的 z 区间（三级划分）
            float chosenRadius;
            ClearingSize chosenSize;

            if (z < -20f)
            {
                // 第一级：只生成小空地
                if (smallCount >= numS)
                    continue; // 小空地已经够了，跳过
                chosenSize = ClearingSize.Small;
                chosenRadius = (float)ClearingSize.Small;
            }
            else if (z >= -20f && z < 10f)
            {
                // 第二级：80% 中，20% 小
                float r = Random.value;
                if (r < 0.8f)
                {
                    if (mediumCount >= numM)
                        continue;
                    chosenSize = ClearingSize.Medium;
                    chosenRadius = (float)ClearingSize.Medium;
                }
                else
                {
                    if (smallCount >= numS)
                        continue;
                    chosenSize = ClearingSize.Small;
                    chosenRadius = (float)ClearingSize.Small;
                }
            }
            else // z >= 10f && z <= 50f
            {
                // 第三级：60% 大，40% 中
                float r = Random.value;
                if (r < 0.6f)
                {
                    if (largeCount >= numL)
                        continue;
                    chosenSize = ClearingSize.Large;
                    chosenRadius = (float)ClearingSize.Large;
                }
                else
                {
                    if (mediumCount >= numM)
                        continue;
                    chosenSize = ClearingSize.Medium;
                    chosenRadius = (float)ClearingSize.Medium;
                }
            }

            // 3. 检查是否在地图边界内（圆要完整在地图内）
            if (!IsWithinMap(candidate, chosenRadius, mapSize))
                continue;

            // 4. 检查与现有空地是否重叠
            if (CheckOverlap(clearings, candidate, chosenRadius))
                continue;

            // 5. 通过以上条件，则添加该空地并更新计数
            clearings.Add(new Clearing
            {
                center = candidate,
                radius = chosenRadius,
                size = chosenSize,
                isSpawn = false
            });

            switch (chosenSize)
            {
                case ClearingSize.Small:
                    smallCount++;
                    break;
                case ClearingSize.Medium:
                    mediumCount++;
                    break;
                case ClearingSize.Large:
                    largeCount++;
                    break;
            }
        }

        if (attempts >= maxAttempts)
        {
            Debug.LogWarning("已达到最大尝试次数，可能无法生成满足条件的所有空地。");
        }

        return clearings;
    }

    private bool IsWithinMap(Vector2 point, float radius, float mapSize)
    {
        float half = mapSize / 2f;
        // 圆心到四条边的距离都要 >= 半径
        if (point.x - radius < -half || point.x + radius > half)
            return false;
        if (point.y - radius < -half || point.y + radius > half)
            return false;
        return true;
    }

    private bool CheckOverlap(List<Clearing> clearings, Vector2 point, float radius)
    {
        foreach (var clearing in clearings)
        {
            float dist = Vector2.Distance(point, clearing.center);
            if (dist < (radius + clearing.radius))
                return true;
        }
        return false;
    }
    #endregion

    #region Tree Filling System
    private void FillTrees(List<Clearing> clearings)
    {
        if (TreeManager.Instance != null)
        {
            TreeManager.Instance.ClearAllTrees();
        }

        GameObject treesParent = new("Trees");

        // Cache tree mats
        GameObject objTemp = Instantiate(treePrefab);
        TreeNode treeTemp = objTemp.GetComponent<TreeNode>();
        TreeManager.Instance.SetTreeMaterials(treeTemp);
        Destroy(objTemp);

        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                Vector3 position = new(x - mapSize / 2, 0.5f, y - mapSize / 2);
                if (!IsInAnyClearing(clearings, position))
                {
                    GameObject tree = Instantiate(treePrefab, position, Quaternion.identity, treesParent.transform);

                    TreeNode treeNode = tree.GetComponent<TreeNode>();
                    if (treeNode == null)
                    {
                        Debug.LogWarning($"Tree prefab at {position} does not have ResourceNode component!");
                        continue;
                    }

                    if (TreeManager.Instance != null)
                    {
                        TreeManager.Instance.RegisterTree(treeNode, position);
                    }
                }
            }
        }
    }
    #endregion

    private bool IsInAnyClearing(List<Clearing> clearings, Vector3 position)
    {
        Vector2 pos = new(position.x, position.z);
        foreach (var clearing in clearings)
        {
            if (Vector2.Distance(pos, clearing.center) <= clearing.radius)
            {
                return true;
            }
        }
        return false;
    }

    #region Starting Buildings and Units
    private void SpawnStartingUnits()
    {
        if (basePrefab == null || workerPrefab == null)
        {
            Debug.LogError("Base or Worker prefab not assigned!");
            return;
        }

        var mainBaseObject = Instantiate(basePrefab, spawnCenterV3, Quaternion.identity);
        mainBaseObject.TryGetComponent<MainBase>(out var mainBase);
        mainBase.CompleteConstruction();

        Vector3 depotPos = spawnCenterV3 + new Vector3(0f, 0.5f, 3f);
        var depotObject = Instantiate(depotPrefab, depotPos, Quaternion.identity);
        depotObject.TryGetComponent<ResourceDepot>(out var depot);
        depot.CompleteConstruction();

        Vector3 worker1Pos = spawnCenterV3 + new Vector3(-3f, 0.5f, 3f);
        var unit1 = Instantiate(workerPrefab, worker1Pos, Quaternion.identity);
        Vector3 worker2Pos = spawnCenterV3 + new Vector3(3f, 0.5f, 3f);
        var unit2 = Instantiate(workerPrefab, worker2Pos, Quaternion.identity);

        UnitManager.Instance.RegisterUnit(unit1);
        UnitManager.Instance.RegisterUnit(unit2);
    }
    #endregion

    #region Resource Generation in Clearings
    private void GenerateResourcesInClearings(List<Clearing> clearings)
    {
        if (facePrefab == null || cubePrefab == null)
        {
            Debug.LogWarning("Face or Cube prefabs not assigned!");
            return;
        }

        GameObject resourcesParent = new("ClearingResources");

        foreach (var clearing in clearings)
        {
            if (clearing.isSpawn) continue;

            List<GameObject> resources = new();

            switch (clearing.size)
            {
                case ClearingSize.Small:
                    resources.AddRange(SpawnResourceInClearing(linePrefab, clearing, 1, resourcesParent.transform));
                    break;

                case ClearingSize.Medium:
                    resources.AddRange(SpawnResourceInClearing(linePrefab, clearing, 2, resourcesParent.transform));
                    resources.AddRange(SpawnResourceInClearing(facePrefab, clearing, 1, resourcesParent.transform));
                    break;

                case ClearingSize.Large:
                    resources.AddRange(SpawnResourceInClearing(facePrefab, clearing, 2, resourcesParent.transform));
                    resources.AddRange(SpawnResourceInClearing(cubePrefab, clearing, 2, resourcesParent.transform));
                    break;
            }

            fogOfWarSystem.RegisterClearingResources(clearing, resources);
        }
    }

    private List<GameObject> SpawnResourceInClearing(GameObject prefab, Clearing clearing, int count, Transform parent)
    {
        List<GameObject> spawnedResources = new();
        Vector2 center = clearing.center;
        float minRadius = clearing.radius * 0.3f;
        float maxRadius = clearing.radius * 0.7f;

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(minRadius, maxRadius);

            Vector2 offset = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                Mathf.Sin(angle * Mathf.Deg2Rad) * distance);
            Vector3 position = new Vector3(
                center.x + offset.x,
                0.5f,
                center.y + offset.y);

            var resource = Instantiate(prefab, position, Quaternion.identity, parent);
            resource.SetActive(false);
            spawnedResources.Add(resource);
        }

        return spawnedResources;
    }
    #endregion

    #region Enemy Spawn
    private void SpawnEnemies(List<Clearing> clearings)
    {
        if (enemyPrefab == null || enemyPrefab.Length < 4)
        {
            Debug.LogWarning("Enemy prefabs not properly assigned!");
            return;
        }

        GameObject enemiesParent = new("Enemies");

        foreach (var clearing in clearings)
        {
            if (clearing.isSpawn)
                continue;

            (int type0Count, int type1Count, int type2or3Count) = GetEnemyCountsByClearingSize(clearing.size);
            int totalCount = type0Count + type1Count + type2or3Count;
            SpawnEnemiesInClearing(clearing, type0Count, 0, totalCount, 0, enemiesParent.transform);
            SpawnEnemiesInClearing(clearing, type1Count, type0Count, totalCount, 1, enemiesParent.transform);

            if (type2or3Count > 0)
            {
                int enemyType = Random.Range(2, 4); // Randomly choose between 2 and 3
                SpawnEnemiesInClearing(clearing, type2or3Count, type0Count + type1Count, totalCount, enemyType, enemiesParent.transform);
            }
        }
    }

    private (int type0Count, int type1Count, int type2or3Count) GetEnemyCountsByClearingSize(ClearingSize size)
    {
        return size switch
        {
            ClearingSize.Small => (2, 0, 0),
            ClearingSize.Medium => (2, 2, 0),
            ClearingSize.Large => (2, 2, 2),
            _ => (0, 0, 0)
        };
    }

    private void SpawnEnemiesInClearing(Clearing clearing, int count, int prevCount, int totalCount, int enemyType, Transform parent)
    {
        if (count <= 0) return;

        List<GameObject> enemies = new();

        float radius = clearing.radius * 0.3f;
        Vector3 center = new(clearing.center.x, 0.5f, clearing.center.y);

        for (int i = 0; i < count; i++)
        {
            float angle = prevCount * (360f / totalCount);
            ++prevCount;
            Vector3 position = center + Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;

            var enemy = Instantiate(enemyPrefab[enemyType], position, Quaternion.identity, parent);
            enemies.Add(enemy);
            if (enemy.TryGetComponent<UnitBase>(out var unit))
            {
                unit.SetEnemy();
                unit.SetDifficulty(difficulty);
                unit.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"Enemy prefab at {position} does not have UnitBase component!");
            }
        }

        fogOfWarSystem.RegisterClearingEnemies(clearing, enemies);
    }
    #endregion

    #region Boundary Settings
    private void GenerateWaterBoundary()
    {
        if (waterPlanePrefab == null)
        {
            Debug.LogWarning("Water plane prefab not assigned!");
            return;
        }

        GameObject waterParent = new("WaterBoundary");
        float waterSize = mapSize * 1.2f;
        GameObject waterPlane = Instantiate(waterPlanePrefab, Vector3.zero, Quaternion.identity, waterParent.transform);
        waterPlane.transform.localScale = new Vector3(waterSize, 1, waterSize);
        waterPlane.transform.position = new Vector3(0, waterLevel, 0);
    }

    private void GenerateBoundaryWalls()
    {
        if (boundaryWallPrefab == null)
        {
            Debug.LogWarning("Boundary wall prefab not assigned!");
            return;
        }

        GameObject boundaryParent = new("BoundaryWalls");

        float halfSize = mapSize / 2f;
        float wallLength = mapSize + 2f; 

        CreateWall(boundaryParent.transform, new Vector3(0, boundaryHeight / 2, halfSize),
            new Vector3(wallLength, boundaryHeight, 0.1f));
        CreateWall(boundaryParent.transform, new Vector3(0, boundaryHeight / 2, -halfSize),
            new Vector3(wallLength, boundaryHeight, 0.1f));
        CreateWall(boundaryParent.transform, new Vector3(halfSize, boundaryHeight / 2, 0),
            new Vector3(0.1f, boundaryHeight, wallLength));
        CreateWall(boundaryParent.transform, new Vector3(-halfSize, boundaryHeight / 2, 0),
            new Vector3(0.1f, boundaryHeight, wallLength));
    }

    private void CreateWall(Transform parent, Vector3 position, Vector3 scale)
    {
        GameObject wall = Instantiate(boundaryWallPrefab, position, Quaternion.identity, parent);
        wall.transform.localScale = scale;

        if (wall.GetComponent<Collider>() == null)
        {
            wall.AddComponent<BoxCollider>();
        }
    }
    #endregion
}