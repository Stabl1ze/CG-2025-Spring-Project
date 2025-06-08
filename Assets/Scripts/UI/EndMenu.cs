using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button quitButton;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text defeatCountText;
    [SerializeField] private TMP_Text producedCountText;

    private void Awake()
    {
        bool playerWon = PlayerPrefs.GetInt("LastGameResult", 0) == 1;
        resultText.text = playerWon ? "VICTORY!!!" : "DEFEAT...";
        int defeatCount = PlayerPrefs.GetInt("TotalEnemiesDefeated", 0);
        defeatCountText.text = $"Total Enemy Defeated: {defeatCount}";
        int prodCount = PlayerPrefs.GetInt("TotalUnitProduced", 0);
        producedCountText.text = $"Total Unit Produced: {prodCount}";

        quitButton.onClick.AddListener(QuitGame);

        Time.timeScale = 1.0f;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}