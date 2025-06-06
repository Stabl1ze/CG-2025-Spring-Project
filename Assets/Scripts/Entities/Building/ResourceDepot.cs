using UnityEngine;

public class ResourceDepot : BuildingBase
{
    #region ICommandable Implementation
    public override void ReceiveCommand(Vector3 targetPosition, GameObject targetObject)
    {
        if (isEnemy) return; // Enemy check
    }
    #endregion
}