using UnityEngine;

public interface ISelectable
{
    void OnSelect();
    void OnDeselect();
    void OnDoubleClick();
    Vector2 GetXZ();
}
