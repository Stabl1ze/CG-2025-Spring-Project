using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    private HashSet<ISelectable> selectedObjects = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddToSelection(ISelectable selectable)
    {
        selectedObjects.Add(selectable);
    }

    public void DeselectThis(ISelectable selectable)
    {
        selectedObjects.Remove(selectable);
    }

    public void DeselectAll()
    {
        foreach (ISelectable selectable in selectedObjects)
            selectable?.OnDeselect();
        selectedObjects.Clear();
    }

    public bool HasSelection()
    {
        return selectedObjects.Count > 0;
    }

    public void IssueCommand(Vector3 targetPosition, GameObject targetObject)
    {
        foreach (ISelectable selectable in selectedObjects)
        {
            if (selectable is ICommandable commandable)
            {
                commandable.ReceiveCommand(targetPosition, targetObject);
            }
        }
    }

    public void BoxSelect(HashSet<ISelectable> selectablesInBox)
    {
        if (selectablesInBox.Count == 0) return;
        
        // Select all of the unit
        foreach (ISelectable selectable in selectablesInBox)
        {
            if (selectable is UnitBase)
            {
                selectable.OnSelect();
                AddToSelection(selectable);
            }
        }
    }

    public void TypeSelect(UnitBase typeUnit)
    {
        var units = UnitManager.Instance.GetSameTypeFriendlyUnits(typeUnit);
        foreach (var unit in units)
        {
            unit.OnSelect();
            AddToSelection(unit);
        }
    }
}
