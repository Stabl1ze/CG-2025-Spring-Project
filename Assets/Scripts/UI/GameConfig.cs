using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameConfig : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button backButton;

    [Header("Difficulty Options")]
    [SerializeField] private Slider difficultySlider;
    [SerializeField] private TextMeshProUGUI difficultyDescriptionText;
    [SerializeField]
    private string[] difficultyDescriptions = new string[]
{
        "Normal - Default enemy stats",
        "Hard - Enemies have +25% HP",
        "Brutal - Enemies have +50% HP and +20% speed"
};

    [Header("Victory Options")]
    [SerializeField] private Toggle defeatEnemiesToggle;
    [SerializeField] private Toggle buildBeaconToggle;

    public int Difficulty { get; private set; } = 0;
    public static bool UseDefeatEnemiesCondition { get; private set; }
    public static int EnemiesToDefeat { get; private set; }
    public static bool UseBuildBeaconCondition { get; private set; }

    private void Awake()
    {
        startGameButton.onClick.AddListener(StartGame);
        backButton.onClick.AddListener(BackToMenu);

        difficultySlider.minValue = 0;
        difficultySlider.maxValue = 2;
        difficultySlider.wholeNumbers = true;
        difficultySlider.value = Difficulty;

        difficultySlider.onValueChanged.AddListener(OnDifficultyChanged);
        UpdateDifficultyDescription();
    }

    private void OnDifficultyChanged(float value)
    {
        Difficulty = (int)value;
        UpdateDifficultyDescription();
    }

    private void UpdateDifficultyDescription()
    {
        if (difficultyDescriptionText != null && Difficulty >= 0 && Difficulty < difficultyDescriptions.Length)
        {
            difficultyDescriptionText.text = difficultyDescriptions[Difficulty];
        }
    }

    private void StartGame()
    {
        UseDefeatEnemiesCondition = defeatEnemiesToggle.isOn;
        if (UseDefeatEnemiesCondition)
            EnemiesToDefeat = 36;
        else
            EnemiesToDefeat = 0;
        UseBuildBeaconCondition = buildBeaconToggle.isOn;

        if (!UseDefeatEnemiesCondition && !UseBuildBeaconCondition)
            return;

        ApplyConfiguration();

        FindObjectOfType<MainMenuController>().StartGame();
    }

    private void BackToMenu()
    {
        FindObjectOfType<MainMenuController>().ShowMainMenu();
    }

    private void ApplyConfiguration()
    {
        PlayerPrefs.SetInt("GameDifficulty", Difficulty);
        PlayerPrefs.Save();
    }
}