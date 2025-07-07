using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EscUI : MonoBehaviour, IUIComponent
{
    [Header("Esc UI")]
    [SerializeField] private GameObject escPanel;
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private Button backToGameButton;
    [SerializeField] private Button quitGameButton;

    public bool IsActive => escPanel != null && escPanel.activeSelf;

    public void Initialize()
    {
        backToMenuButton.onClick.AddListener(BackToMenu);
        backToMenuButton.gameObject.SetActive(false);
        backToGameButton.onClick.AddListener(BackToGame);
        quitGameButton.onClick.AddListener(QuitGame);
        escPanel?.SetActive(false);
    }

    public void Show()
    {
        escPanel?.SetActive(true);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameManager.GameState.Paused);
        }
    }

    public void Hide()
    {
        escPanel?.SetActive(false);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameManager.GameState.Playing);
        }
    }

    public void UpdateDisplay()
    {
        return;
    }

    private void BackToMenu()
    {
        StopAllCoroutines();

        UnitManager.Instance.CleanUpUnits();

        if (GameManager.Instance != null)
            GameManager.Instance.ChangeGameState(GameManager.GameState.MainMenu);
        Time.timeScale = 1f;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainMenu");
        asyncLoad.allowSceneActivation = true;
        asyncLoad.completed += (operation) =>
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                Destroy(gameManager.gameObject);
            }

            Debug.Log("Main menu loaded successfully");
        };

        Resources.UnloadUnusedAssets();

        Hide();
    }

    private void BackToGame()
    {
        Hide();
    }

    private void QuitGame()
    {
        Debug.Log("Quitting game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}