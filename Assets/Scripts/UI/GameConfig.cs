using UnityEngine;
using UnityEngine.UI;

public class GameConfig : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button backButton;

    [Header("Configuration Options")]
    [SerializeField] private int difficulty;

    [Header("Victory Conditions")]
    [SerializeField] private Toggle defeatEnemiesToggle;
    [SerializeField] private Toggle buildBeaconToggle;

    public static bool UseDefeatEnemiesCondition { get; private set; }
    public static int EnemiesToDefeat { get; private set; }
    public static bool UseBuildBeaconCondition { get; private set; }

    private void Awake()
    {
        startGameButton.onClick.AddListener(StartGame);
        backButton.onClick.AddListener(BackToMenu);
    }

    private void StartGame()
    {
        UseDefeatEnemiesCondition = defeatEnemiesToggle.isOn;
        if (UseDefeatEnemiesCondition)
            EnemiesToDefeat = 18;
        else
            EnemiesToDefeat = 0;
        UseBuildBeaconCondition = buildBeaconToggle.isOn;

        ApplyConfiguration();
        FindObjectOfType<MainMenuController>().StartGame();
    }

    private void BackToMenu()
    {
        FindObjectOfType<MainMenuController>().ShowMainMenu();
    }

    private void ApplyConfiguration()
    {

    }
}