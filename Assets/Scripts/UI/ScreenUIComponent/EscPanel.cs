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
        backToGameButton.onClick.AddListener(BackToGame);
        quitGameButton.onClick.AddListener(QuitGame); // 修正了这里，之前错误地添加了BackToGame
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
        // 1. 重置游戏状态和时间缩放
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameManager.GameState.Playing);
        }
        Time.timeScale = 1f; // 确保时间缩放重置

        // 2. 卸载当前场景的资源（可选）
        Resources.UnloadUnusedAssets();

        // 3. 异步加载主菜单
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainMenu");
        asyncLoad.allowSceneActivation = true;

        // 4. 添加加载完成回调
        asyncLoad.completed += (operation) =>
        {
            // 确保所有单例对象被正确处理
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                Destroy(gameManager.gameObject);
            }

            Debug.Log("Main menu loaded successfully");
        };

        // 5. 立即隐藏ESC面板
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