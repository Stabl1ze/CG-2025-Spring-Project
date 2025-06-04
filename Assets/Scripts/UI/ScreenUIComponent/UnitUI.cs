using UnityEngine;
using TMPro;
using System;

public class UnitUI : MonoBehaviour, IUIComponent
{
    [SerializeField] private GameObject unitPanel;
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private TMP_Text unitHPText;

    public UnitBase CurrentUnit { get; private set; }
    public bool IsActive => unitPanel != null && unitPanel.activeSelf;

    public void Initialize()
    {
        unitPanel?.SetActive(false);
    }

    public void Show()
    {
        unitPanel?.SetActive(true);
    }

    public void Hide()
    {
        unitPanel?.SetActive(false);
        CurrentUnit = null;
    }

    public void UpdateDisplay()
    {
        if (CurrentUnit != null)
        {
            UpdateUnitHP(CurrentUnit);
        }
    }

    public void ShowUnitPanel(UnitBase unit)
    {
        CurrentUnit = unit;
        unitNameText.text = $"{unit.gameObject.name}";
        UpdateUnitHP(unit);
        Show();
    }

    public void UpdateUnitHP(UnitBase unit)
    {
        unitHPText.text = $"HP: {Math.Round(unit.GetCurrentHP())}/{unit.GetMaxHP()}";
    }
}