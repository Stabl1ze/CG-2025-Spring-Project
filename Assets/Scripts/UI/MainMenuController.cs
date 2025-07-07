using UnityEngine;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject configPanel;

    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        Time.timeScale = 1f;
        ShowMainMenu();
    }

    private void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    public void ShowMainMenu()
    {
        PlayButtonSound();
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        configPanel.SetActive(false);
    }

    public void ShowSettings()
    {
        PlayButtonSound();
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
        configPanel.SetActive(false);
    }

    public void ShowConfig()
    {
        PlayButtonSound();
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        configPanel.SetActive(true);
    }

    public void StartGame()
    {
        PlayButtonSound();
        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        WorldGenerator worldGenerator = FindObjectOfType<WorldGenerator>();
        if (worldGenerator == null)
        {
            GameObject generatorObj = new("WorldGenerator");
            worldGenerator = generatorObj.AddComponent<WorldGenerator>();
        }

        worldGenerator.GenerateNewWorld();

        yield return null;
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