using UnityEngine;
using TMPro;

public class ResourceUI : MonoBehaviour, IUIComponent
{
    [SerializeField] private GameObject resourcePanel;
    [SerializeField] private TMP_Text lineRText;
    [SerializeField] private TMP_Text faceRText;
    [SerializeField] private TMP_Text cubeRText;

    public bool IsActive => resourcePanel != null && resourcePanel.activeSelf;

    public void Initialize()
    {
        resourcePanel?.SetActive(true);
    }

    public void Show()
    {
        resourcePanel?.SetActive(true);
    }

    public void Hide()
    {
        resourcePanel?.SetActive(false);
    }

    public void UpdateDisplay()
    {
        // Can be called if needed for refresh
    }

    public void UpdateResourceDisplay(ResourceManager.ResourceType type, int amount, int maxCapacity)
    {
        switch (type)
        {
            case ResourceManager.ResourceType.LineR:
                lineRText.text = $"{amount}/{maxCapacity}";
                break;
            case ResourceManager.ResourceType.FaceR:
                faceRText.text = $"{amount}/{maxCapacity}";
                break;
            case ResourceManager.ResourceType.CubeR:
                cubeRText.text = $"{amount}/{maxCapacity}";
                break;
        }
    }
}