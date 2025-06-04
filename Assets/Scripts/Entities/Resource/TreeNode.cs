using UnityEngine;

public class TreeNode : ResourceNode
{
    protected override void DepleteNode() 
    {
        OnDeselect();
        if (SelectionManager.Instance != null)
            SelectionManager.Instance.DeselectThis(this);
        if (TreeManager.Instance != null)
            TreeManager.Instance.RemoveTree(transform.position);
        if (gameObject != null)
            Destroy(gameObject);
    }
}