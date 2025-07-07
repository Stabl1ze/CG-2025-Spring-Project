using UnityEngine;
using TMPro;

public class FloatingTooltip : MonoBehaviour
{
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private Vector2 offset = new Vector2(0, 30f);

    private RectTransform rectTransform;
    private bool isShowing = false;

    private void Awake()
    {
        rectTransform = tooltipPanel.GetComponent<RectTransform>();
        HideTooltip();
    }

    public void ShowTooltip(string text, Vector3 position)
    {
        tooltipText.text = text;
        tooltipPanel.SetActive(true);
        rectTransform.position = position + (Vector3)offset;
        isShowing = true;
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
        isShowing = false;
    }

    public void UpdatePosition(Vector3 position)
    {
        if (isShowing)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent.GetComponent<RectTransform>(),
                position + (Vector3)offset,
                null,
                out Vector2 localPoint);

            rectTransform.anchoredPosition = localPoint;
        }
    }
}