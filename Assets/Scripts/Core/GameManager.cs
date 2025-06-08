using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game Settings")]
    [SerializeField] bool debugMode = false;

    [Header("Scene Settings")]
    [SerializeField] private string gameplaySceneName = "OverWorld";
    [SerializeField] private string endMenuSceneName = "EndMenu";
    [SerializeField] private float gameOverDelay = 3f;

    [Header("Subsystems")]
    [SerializeField] private InputManager inputManager;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private CameraController cameraController;

    private bool beaconBuilt = false;
    private int enemiesDefeated = 0;
    private bool victory = false;

    public enum GameState { MainMenu, Playing, Paused, GameOver }
    public GameState CurrentGameState { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeGame();
    }

    private void OnDestroy()
    {
        if (BuildingManager.Instance != null)
            BuildingManager.Instance.OnBuildingConstructed -= CheckBeaconBuilt;
    }

    private void InitializeGame()
    {
        CurrentGameState = GameState.Playing;
        beaconBuilt = false;
        enemiesDefeated = 0;

        BuildingManager.Instance.OnBuildingConstructed += CheckBeaconBuilt;

        UIManager.Instance.SetCondItionActive(GameConfig.UseDefeatEnemiesCondition, GameConfig.UseBuildBeaconCondition);

        DebugLog("All systems initialized");
    }

    public void CheckBeaconBuilt(BuildingBase building)
    {
        if (!GameConfig.UseBuildBeaconCondition) return;

        if (building is Beacon)
        {
            // Debug.Log("Beacon Built");
            beaconBuilt = true;
            UIManager.Instance.UpdateVictoryPanel(enemiesDefeated, beaconBuilt);
            CheckVictoryConditions();
        }
    }

    public void CheckEnemiesDefeated()
    {
        if (victory) return;

        enemiesDefeated++;
        // Debug.Log($"{enemiesDefeated} enemy defeated");

        if (!GameConfig.UseDefeatEnemiesCondition) return;

        UIManager.Instance.UpdateVictoryPanel(enemiesDefeated, beaconBuilt);

        if (enemiesDefeated >= GameConfig.EnemiesToDefeat)
            CheckVictoryConditions();
 
    }

    private void CheckVictoryConditions()
    {
        victory = true;

        if (GameConfig.UseDefeatEnemiesCondition && enemiesDefeated < GameConfig.EnemiesToDefeat)
            victory = false;

        if (GameConfig.UseBuildBeaconCondition && !beaconBuilt)
            victory = false;

        if (victory)
        {
            NotifyGameOver(true);
        }
    }

    public void CheckTimeLimit(int currentDay, int totalDays)
    {
        if (currentDay > totalDays)
        {
            Debug.Log("Ê±¼äºÄ¾¡£¬ÓÎÏ·Ê§°Ü");
            NotifyGameOver(false);
        }
    }

    public void ChangeGameState(GameState newState)
    {
        CurrentGameState = newState;
        DebugLog($"Game state changed to: {newState}");
        
        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
        }
    }

    private void SaveGameState(bool playerWon)
    {
        PlayerPrefs.SetInt("LastGameResult", playerWon ? 1 : 0);
        PlayerPrefs.SetInt("TotalEnemiesDefeated", enemiesDefeated);
        PlayerPrefs.SetInt("TotalUnitProduced", UnitManager.Instance.ProducedUnitCount);
        PlayerPrefs.Save();
    }

    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log("[GameManager] " + message);
        }
    }

    public bool GetDebugStatus()
    {
        return debugMode;
    }

    public void NotifyGameOver(bool playerWon)
    {
        ChangeGameState(GameState.GameOver);
        DebugLog($"Game Over - Player {(playerWon ? "won" : "lost")}");

        SaveGameState(playerWon);

        Invoke(nameof(LoadEndMenu), gameOverDelay);
        
        StartCoroutine(UnloadGameSceneResources(playerWon));
    }

    private void LoadEndMenu()
    {
        SceneManager.LoadScene(endMenuSceneName, LoadSceneMode.Single);
    }

    private System.Collections.IEnumerator UnloadGameSceneResources(bool playerWon)
    {
        StopAllCoroutines();

        UnitManager.Instance.CleanUpUnits();

        if (SceneManager.sceneCount > 1)
        {
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(gameplaySceneName);
            while (!unloadOp.isDone)
            {
                yield return null;
            }
        }

        // Debug.Log($"Memory Before GC: {System.GC.GetTotalMemory(false) / 1024 / 1024}MB");
        System.GC.Collect();
        // Debug.Log($"Memory After GC: {System.GC.GetTotalMemory(true) / 1024 / 1024}MB");
        Resources.UnloadUnusedAssets();
    }
}