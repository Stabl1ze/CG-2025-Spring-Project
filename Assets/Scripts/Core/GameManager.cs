using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game Settings")]
    [SerializeField] bool debugMode = false;

    [Header("Subsystems")]
    [SerializeField] private InputManager inputManager;
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private CameraController cameraController;

    private bool beaconBuilt = false;
    private int enemiesDefeated = 0;

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

        if (UnitManager.Instance != null)
            UnitManager.Instance.OnEnemyDestroyed -= CheckEnemiesDefeated;
    }

    private void InitializeGame()
    {
        CurrentGameState = GameState.Playing;
        beaconBuilt = false;
        enemiesDefeated = 0;

        BuildingManager.Instance.OnBuildingConstructed += CheckBeaconBuilt;
        UnitManager.Instance.OnEnemyDestroyed += CheckEnemiesDefeated;

        DebugLog("All systems initialized");
    }

    private void CheckBeaconBuilt(BuildingBase building)
    {
        if (!GameConfig.UseBuildBeaconCondition) return;

        if (building is Beacon)
        {
            beaconBuilt = true;
            CheckVictoryConditions();
        }
    }

    private void CheckEnemiesDefeated()
    {
        if (!GameConfig.UseDefeatEnemiesCondition) return;

        enemiesDefeated++;
        if (enemiesDefeated >= GameConfig.EnemiesToDefeat)
        {
            CheckVictoryConditions();
        }
    }

    private void CheckVictoryConditions()
    {
        bool victory = true;

        if (GameConfig.UseDefeatEnemiesCondition && enemiesDefeated < GameConfig.EnemiesToDefeat)
            victory = false;

        if (GameConfig.UseBuildBeaconCondition && !beaconBuilt)
            victory = false;

        if (victory)
        {
            NotifyGameOver(true);
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
    }


}