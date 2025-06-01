using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private MainMenuController mainMenuController;
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        startButton.onClick.AddListener(OpenConfigPanel);
        settingsButton.onClick.AddListener(OpenSettingsPanel);
        quitButton.onClick.AddListener(QuitGame);
    }

    private void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    public void OpenConfigPanel()
    {
        mainMenuController.ShowConfig();
    }

    public void OpenSettingsPanel()
    {
        mainMenuController.ShowSettings();
    }

    public void QuitGame()
    {
        PlayButtonSound();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}