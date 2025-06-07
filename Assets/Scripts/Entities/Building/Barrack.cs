using UnityEngine;

public class Barrack : ProductionBuilding
{
    #region ICommandable Implementation
    public override void ReceiveCommand(Vector3 targetPosition, GameObject targetObject)
    {
        if (isEnemy) return; // Enemy check
    }
    #endregion
}