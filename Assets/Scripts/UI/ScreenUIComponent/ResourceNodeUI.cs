using UnityEngine;
using TMPro;

public class ResourceNodeUI : MonoBehaviour, IUIComponent
{
    [SerializeField] private GameObject resourceNodePanel;
    [SerializeField] private TMP_Text resourceNameText;
    [SerializeField] private TMP_Text resourceAmountText;
    [SerializeField] private TMP_Text resourceTypeText;

    public ResourceNode CurrentResourceNode { get; private set; }
    public bool IsActive => resourceNodePanel != null && resourceNodePanel.activeSelf;

    public void Initialize()
    {
        resourceNodePanel?.SetActive(false);
    }

    public void Show()
    {
        resourceNodePanel?.SetActive(true);
    }

    public void Hide()
    {
        resourceNodePanel?.SetActive(false);
        CurrentResourceNode = null;
    }

    public void UpdateDisplay()
    {
        if (CurrentResourceNode != null)
        {
            UpdateResourceNodeDisplay(CurrentResourceNode);
        }
    }

    public void ShowResourceNodePanel(ResourceNode resourceNode)
    {
        CurrentResourceNode = resourceNode;
        resourceNameText.text = $"{resourceNode.gameObject.name}";
        UpdateResourceNodeDisplay(resourceNode);
        Show();
    }

    public void UpdateResourceNodeDisplay(ResourceNode resourceNode)
    {
        resourceAmountText.text = $"Amount: {resourceNode.GetResourceAmount()}";
        resourceTypeText.text = $"Type: {resourceNode.GetResourceType()}";
    }
}