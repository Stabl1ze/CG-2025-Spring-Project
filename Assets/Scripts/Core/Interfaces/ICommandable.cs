using UnityEngine;

public interface ICommandable
{
    void ReceiveCommand(Vector3 targetPosition, GameObject targetObject);
}