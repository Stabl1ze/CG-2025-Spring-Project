using UnityEngine;
using UnityEngine.UI;

public class GameConfig : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button backButton;

    [Header("Configuration Options")]
    [SerializeField] private int difficulty;

    private void Awake()
    {
        startGameButton.onClick.AddListener(StartGame);
        backButton.onClick.AddListener(BackToMenu);
    }

    private void StartGame()
    {
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