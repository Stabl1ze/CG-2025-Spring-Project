using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using static UnityEditor.Progress;

public class ConstructionUI : MonoBehaviour, IUIComponent
{
    [Header("Construction UI")]
    [SerializeField] private GameObject constructionPanel;
    [SerializeField] private Transform constructionButtonParent;
    [SerializeField] private GameObject constructionButtonPrefab;
    [SerializeField] private MainBase testbase;

    public bool IsActive => constructionPanel != null && constructionPanel.activeSelf;
    private BuildingBase[] buildingList;
    private readonly List<GameObject> activeButtons = new();

    public void Initialize()
    {
        constructionPanel?.SetActive(false);
    }

    public void Show()
    {
        constructionPanel?.SetActive(true);
    }

    public void Hide()
    {
        constructionPanel?.SetActive(false);
        ClearBuildingButtons();
    }

    public void UpdateDisplay()
    {
        return;
    }

    public void SetConstructableBuildings(BuildingBase[] buildingList)
    {
        this.buildingList = buildingList;
        this.buildingList[0] = testbase;
    }

    public void ShowConstructionPanel()
    {
        CreateBuildingButton();
        Show();
    }

    private void CreateBuildingButton()
    {
        for (int i = 0; i < buildingList.Length; ++i)
        {
            var buttonObj = Instantiate(constructionButtonPrefab, constructionButtonParent);
            var button = buttonObj.GetComponent<Button>();
            var text = buttonObj.GetComponentInChildren<TMP_Text>();
            var costText = buttonObj.transform.Find("CostText")?.GetComponent<TMP_Text>();

            text.text = this.buildingList[i].gameObject.name;
            activeButtons.Add(buttonObj);
            buttonObj.SetActive(true);
            buttonObj.transform.SetAsLastSibling();
        }
    }

    private void ClearBuildingButtons()
    {
        foreach (var button in activeButtons)
        {
            if (button != null) Destroy(button);
        }
        activeButtons.Clear();
    }
}