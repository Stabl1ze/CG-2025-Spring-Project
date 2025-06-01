// TestSelectable.cs
using UnityEngine;

public class TestSelectable : MonoBehaviour, ICommandable
{
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private Material deselectedMaterial;

    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = deselectedMaterial;
    }

    public void OnSelect()
    {
        meshRenderer.material = selectedMaterial;
    }

    public void OnDeselect()
    {
        meshRenderer.material = deselectedMaterial;
    }

    public void OnDoubleClick()
    {

    }

    public void ReceiveCommand(Vector3 targetPosition, GameObject targetObject)
    {

    }
}