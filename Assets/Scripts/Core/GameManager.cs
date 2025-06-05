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

    private void InitializeGame()
    {
        CurrentGameState = GameState.Playing;
        DebugLog("All systems initialized");
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