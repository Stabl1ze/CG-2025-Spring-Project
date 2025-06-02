using UnityEngine;
using System.Collections;

public class MainBase : ProductionBuilding
{
    [Header("Main Base Settings")]
    [SerializeField] protected float selfHealing = 10f;

    public Vector3 GetDepositPoint()
    {
        return transform.position;
    }
}