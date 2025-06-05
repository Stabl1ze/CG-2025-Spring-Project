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
        backToGameButton.onClick.AddListener(QuitGame);
        escPanel?.SetActive(false);
    }

    public void Show()
    {
        escPanel?.SetActive(true);
    }

    public void Hide()
    {
        escPanel?.SetActive(false);
    }

    public void UpdateDisplay()
    {
        return;
    }

    private void BackToMenu()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("MainMenu");
    }

    private void BackToGame()
    {
        Hide();
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}