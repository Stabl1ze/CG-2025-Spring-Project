using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSettings : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Resolution Settings")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;

    [Header("Controls")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        // Volume settings
        masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // Resolution settings
        fullscreenToggle.isOn = Screen.fullScreen;
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(new System.Collections.Generic.List<string>
        {
            "1920x1080",
            "1600x900",
            "1366x768",
            "1280x720"
        });

        // Button event
        applyButton.onClick.AddListener(ApplySettings);
        resetButton.onClick.AddListener(ResetSettings);
        backButton.onClick.AddListener(BackToMenu);
    }

    private void ApplySettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolumeSlider.value);
        PlayerPrefs.SetFloat("MusicVolume", musicVolumeSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolumeSlider.value);
        PlayerPrefs.Save();

        Screen.fullScreen = fullscreenToggle.isOn;
        string[] res = resolutionDropdown.options[resolutionDropdown.value].text.Split('x');
        if (res.Length == 2)
        {
            int width = int.Parse(res[0]);
            int height = int.Parse(res[1]);
            Screen.SetResolution(width, height, fullscreenToggle.isOn);
        }

        Debug.Log("Settings applied and saved");
    }
    private void ResetSettings()
    {
        masterVolumeSlider.value = 1f;
        musicVolumeSlider.value = 1f;
        sfxVolumeSlider.value = 1f;

        Debug.Log("Settings reset to default");
    }

    private void BackToMenu()
    {
        FindObjectOfType<MainMenuController>().ShowMainMenu();
    }

    public void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = value;
    }
}