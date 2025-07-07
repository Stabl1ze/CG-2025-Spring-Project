using UnityEngine;
using TMPro;

public class VictoryUI : MonoBehaviour, IUIComponent
{
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TMP_Text enemyText;
    [SerializeField] private TMP_Text beaconText;

    public bool IsActive => victoryPanel != null && victoryPanel.activeSelf;

    public void Initialize()
    {
        victoryPanel?.SetActive(true);
    }

    public void Show()
    {
        victoryPanel?.SetActive(true);
    }

    public void Hide()
    {
        victoryPanel?.SetActive(false);
    }

    public void UpdateDisplay()
    {
        // Can be called if needed for refresh
    }

    public void SetConditionActive(bool enemy, bool beacon)
    {
        enemyText.gameObject.SetActive(enemy);
        beaconText.gameObject.SetActive(beacon);
    }

    public void UpdateVictoryDisplay(int enemy, bool beacon)
    {
        int beaconBuilt = beacon ? 1 : 0;
        enemyText.text = $"Enemy Defeated: {enemy}/36";
        beaconText.text = $"Beacon Built: {beaconBuilt}/1";
    }
}