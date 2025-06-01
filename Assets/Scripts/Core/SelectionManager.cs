using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
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

    public void DeselectAll()
    {
        foreach (ISelectable selectable in selectedObjects)
            selectable.OnDeselect();
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

        // Count each type
        int buildings = 0;
        int units = 0;
        int resources = 0;

        foreach (ISelectable selectable in selectablesInBox)
        {
            if (selectable is BuildingBase) buildings++;
            else if (selectable is UnitBase) units++;
            else if (selectable is ResourceNode) resources++;
        }

        // Determine majority type
        System.Type majorityType = null;
        int maxCount = Mathf.Max(buildings, units, resources);

        if (maxCount == buildings) majorityType = typeof(BuildingBase);
        else if (maxCount == units) majorityType = typeof(UnitBase);
        else if (maxCount == resources) majorityType = typeof(ResourceNode);

        // Select all of the majority type
        foreach (ISelectable selectable in selectablesInBox)
        {
            if ((majorityType == typeof(BuildingBase) && selectable is BuildingBase) ||
                (majorityType == typeof(UnitBase) && selectable is UnitBase) ||
                (majorityType == typeof(ResourceNode) && selectable is ResourceNode))
            {
                selectable.OnSelect();
                AddToSelection(selectable);
            }
        }
    }
}
