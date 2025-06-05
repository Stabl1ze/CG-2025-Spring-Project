using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using static WorldGenerator.Clearing;

public class WorldGenerator : MonoBehaviour
{
    public static WorldGenerator Instance { get; private set; }

    [Header("Terrain Prefab")]
    [SerializeField] private Terrain terrainPrefab;

    [Header("Generation Settings")]
    [SerializeField] private int mapSize = 100;
    [SerializeField] private Vector2 spawnCenter = new(0, -36);

    [Header("Enemy")]
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
    [SerializeField] private GameObject treePrefab;

    private Terrain generatedTerrain;

    [Header("Starting Units")]
    [SerializeField] private WorkerUnit workerPrefab;
    [SerializeField] private MainBase basePrefab;

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
        StartCoroutine(GenerateWorldRoutine());
    }

    private IEnumerator GenerateWorldRoutine()
    {
        // ���س���
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("OverWorld");

        // �ȴ������������
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        GenerateBaseTerrain();

        // ���ɿյ�
        List<Clearing> clearings = GenerateClearings();

        // ���ɵ���
        SpawnEnemies(clearings);

        // �����ľ
        FillTrees(clearings);

        SpawnStartingUnits();

        // ��ʼ��ս������ϵͳ
        if (fogOfWarSystem != null)
        {
            fogOfWarSystem.Initialize(clearings, spawnCenter, (float)ClearingSize.Medium, mapSize);
        }
        else
        {
            Debug.LogWarning("FogOfWarSystem not assigned to WorldGenerator!");
        }
    }

    private void GenerateBaseTerrain()
    {
        if (terrainPrefab == null)
        {
            Debug.LogError("Terrain prefab is not assigned!");
            return;
        }

        // ʵ����Terrain
        generatedTerrain = Instantiate(terrainPrefab, spawnCenter, Quaternion.identity);
        generatedTerrain.name = "GeneratedTerrain";

        // �����ξ��У�ԭ�������ģ�
        generatedTerrain.transform.position = new Vector3(-mapSize * 0.7f, 0, -mapSize * 0.7f);

        // ����Terrain�ߴ�
        generatedTerrain.terrainData.size = new Vector3(mapSize * 1.4f, 1, mapSize * 1.4f);
    }

    // Ԥ���Ĺ����ӿ�
    public int GetMapSize() => mapSize;
    public Vector3 GetSpawnCenter() => spawnCenter;

    #region Clearing Generation System
    public List<Clearing> GenerateClearings()
    {
        List<Clearing> clearings = new()
        {
            // 1. ��ӳ�����յأ����ͣ�
            new Clearing
            {
                center = spawnCenter,
                size = ClearingSize.Medium,
                radius = (float)ClearingSize.Medium,
                isSpawn = true
            }
        };

        int smallCount = 0;
        int mediumCount = 1; // ��������ռһ��
        int largeCount = 0;

        // 2. ���ϳ������ɣ�ֱ���ﵽ����Ŀ������
        int maxAttempts = 10000;
        int attempts = 0;

        while ((smallCount < numS || mediumCount < numM || largeCount < numL) && attempts < maxAttempts)
        {
            attempts++;

            // ����ڵ�ͼ��Χ����ѡһ����
            float half = mapSize / 2f;
            float x = Random.Range(-half, half);
            float z = Random.Range(-half, half);
            Vector2 candidate = new Vector2(x, z);

            // �����õ����ڵ� z ���䣨�������֣�
            float chosenRadius;
            ClearingSize chosenSize;

            if (z < -20f)
            {
                // ��һ����ֻ����С�յ�
                if (smallCount >= numS)
                    continue; // С�յ��Ѿ����ˣ�����
                chosenSize = ClearingSize.Small;
                chosenRadius = (float)ClearingSize.Small;
            }
            else if (z >= -20f && z < 10f)
            {
                // �ڶ�����80% �У�20% С
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
                // ��������60% ��40% ��
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

            // 3. ����Ƿ��ڵ�ͼ�߽��ڣ�ԲҪ�����ڵ�ͼ�ڣ�
            if (!IsWithinMap(candidate, chosenRadius, mapSize))
                continue;

            // 4. ��������пյ��Ƿ��ص�
            if (CheckOverlap(clearings, candidate, chosenRadius))
                continue;

            // 5. ͨ����������������Ӹÿյز����¼���
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
            Debug.LogWarning("�Ѵﵽ����Դ����������޷������������������пյء�");
        }

        return clearings;
    }

    private bool IsWithinMap(Vector2 point, float radius, float mapSize)
    {
        float half = mapSize / 2f;
        // Բ�ĵ������ߵľ��붼Ҫ >= �뾶
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
        // ����֮ǰ����ľ����
        if (TreeManager.Instance != null)
        {
            TreeManager.Instance.ClearAllTrees();
        }

        GameObject treesParent = new("Trees");

        for (int x = 0; x < mapSize; x++)
        {
            for (int y = 0; y < mapSize; y++)
            {
                Vector3 position = new(x - mapSize / 2, 0, y - mapSize / 2);
                // ����Ƿ����κοյ���
                if (!IsInAnyClearing(clearings, position))
                {
                    GameObject tree = Instantiate(treePrefab, position, Quaternion.identity, treesParent.transform);

                    // ȷ����ľԤ������ResourceNode���
                    ResourceNode treeResourceNode = tree.GetComponent<ResourceNode>();
                    if (treeResourceNode == null)
                    {
                        Debug.LogWarning($"Tree prefab at {position} does not have ResourceNode component!");
                        continue;
                    }

                    // ע����ľ��TreeManager
                    if (TreeManager.Instance != null)
                    {
                        TreeManager.Instance.RegisterTree(treeResourceNode, position);
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
        mainBase.SetHP(mainBase.GetMaxHP());
        mainBase.CompleteConstruction();

        Vector3 worker1Pos = spawnCenterV3 + new Vector3(-3f, 0.5f, 3f);
        Instantiate(workerPrefab, worker1Pos, Quaternion.identity);
        Vector3 worker2Pos = spawnCenterV3 + new Vector3(3f, 0.5f, 3f);
        Instantiate(workerPrefab, worker2Pos, Quaternion.identity);
    }
    #endregion

    #region Enemy Spawn
    private void SpawnEnemies(List<Clearing> clearings)
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("Enemy melee prefab not assigned!");
            return;
        }

        GameObject enemiesParent = new("Enemies");

        foreach (var clearing in clearings)
        {
            if (clearing.isSpawn || enemyPrefab == null)
                continue;

            int enemyCount = GetEnemyCountByClearingSize(clearing.size);
            SpawnEnemiesInClearing(clearing, enemyCount, enemiesParent.transform);
        }
    }

    // ���ݿյش�Сȷ��������Ŀ
    private int GetEnemyCountByClearingSize(ClearingSize size)
    {
        return size switch
        {
            ClearingSize.Small => 1,
            ClearingSize.Medium => 2,
            ClearingSize.Large => 4,
            _ => 0
        };
    }

    // �ڿյ��ھ������ɵ���
    private void SpawnEnemiesInClearing(Clearing clearing, int count, Transform parent)
    {
        float radius = clearing.radius * 0.3f; // ʹ��70%�İ뾶ȷ�������ڿյ���
        Vector3 center = new(clearing.center.x, 0.5f, clearing.center.y);

        for (int i = 0; i < count; i++)
        {
            // ������ȷֲ��ĽǶ�
            float angle = i * (360f / count);
            Vector3 position = center + Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;

            // ʵ�������˲�����Ϊ�ж�
            var enemy = Instantiate(enemyPrefab[0], position, Quaternion.identity, parent);
            if (enemy.TryGetComponent<UnitBase>(out var unit))
            {
                unit.SetEnemy();
            }
            else
            {
                Debug.LogWarning($"Enemy prefab at {position} does not have UnitBase component!");
            }
        }
    }
    #endregion
}