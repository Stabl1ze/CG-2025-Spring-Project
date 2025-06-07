using System.Collections.Generic;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }

    public delegate void EnemyDestroyedHandler();
    public event EnemyDestroyedHandler OnEnemyDestroyed;

    private List<UnitBase> friendlyUnits = new();
    private List<UnitBase> enemyUnits = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public void RegisterUnit(UnitBase unit)
    {
        if (unit.IsEnemy)
        {
            enemyUnits.Add(unit);
        }
        else
        {
            friendlyUnits.Add(unit);
        }

        unit.OnDestroyed += () =>
        {
            UnregisterUnit(unit);
            if (unit.IsEnemy)
            {
                OnEnemyDestroyed?.Invoke();
            }
        };
    }

    private void UnregisterUnit(UnitBase unit)
    {
        if (unit.IsEnemy)
        {
            enemyUnits.Remove(unit);
        }
        else
        {
            friendlyUnits.Remove(unit);
        }
    }

    public List<UnitBase> GetEnemyInRange(Vector3 position, float range, bool isEnemy)
    {
        List<UnitBase> targetUnits = isEnemy ? friendlyUnits : enemyUnits;
        List<UnitBase> unitsInRange = new();

        foreach (UnitBase unit in targetUnits)
        {
            if (unit == null) continue;

            float distance = Vector3.Distance(position, unit.transform.position);
            if (distance <= range)
            {
                unitsInRange.Add(unit);
            }
        }

        return unitsInRange;
    }

    public List<UnitBase> GetFriendlyUnits()
    {
        return new List<UnitBase>(friendlyUnits);
    }

    public List<UnitBase> GetEnemyUnits()
    {
        return new List<UnitBase>(enemyUnits);
    }

    public int GetFriendlyUnitCount()
    {
        return friendlyUnits.Count;
    }

    public int GetEnemyUnitCount()
    {
        return enemyUnits.Count;
    }
}