public class CubeNode : ResourceNode
{
    protected override void DepleteNode() 
    {
        OnDeselect();

        if (SelectionManager.Instance != null)
            SelectionManager.Instance.DeselectThis(this);

        if (gameObject != null)
            Destroy(gameObject);
    }
}