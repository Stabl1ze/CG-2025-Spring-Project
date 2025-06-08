using System.Collections.Generic;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    public static UnitManager Instance { get; private set; }

    public delegate void EnemyDestroyedHandler();

    private List<UnitBase> activeUnits = new();
    private List<UnitBase> friendlyUnits = new();
    private List<UnitBase> enemyUnits = new();
    private List<UnitBase> fightingUnits = new();
    private List<RangedUnit> allRangedUnits = new();

    private int producedUnitCount = -2;
    public int ProducedUnitCount => producedUnitCount;

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
        activeUnits.Add(unit);

        if (unit.IsEnemy)
            enemyUnits.Add(unit);
        else
        {
            friendlyUnits.Add(unit);
            ++producedUnitCount;
            if (unit is not WorkerUnit)
                fightingUnits.Add(unit);
        }
            
        if (unit is RangedUnit rangedUnit)
            allRangedUnits.Add(rangedUnit);

        unit.OnDestroyed += () =>
        {
            UnregisterUnit(unit);
            // Debug.Log(unit.IsEnemy);
        };
    }

    private void UnregisterUnit(UnitBase unit)
    {
        if (unit.IsEnemy)
            enemyUnits.Remove(unit);
        else
            friendlyUnits.Remove(unit);
        if (unit is RangedUnit rangedUnit)
            allRangedUnits.Remove(rangedUnit);
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

    public void UpdateNightDebuff(bool isNight)
    {
        foreach (var rangedUnit in allRangedUnits)
            if (rangedUnit != null)
                rangedUnit.ApplyNightDebuff(isNight);
    }

    public List<UnitBase> GetFriendlyUnits()
    {
        return new List<UnitBase>(friendlyUnits);
    }

    public List<UnitBase> GetFightingUnits()
    {
        return new List<UnitBase>(fightingUnits);
    }

    public List<UnitBase> GetEnemyUnits()
    {
        return new List<UnitBase>(enemyUnits);
    }

    public List<UnitBase> GetSameTypeFriendlyUnits(UnitBase unit)
    {
        List<UnitBase> sameTypeUnits = new List<UnitBase>();

        if (unit == null || unit.IsEnemy)
            return sameTypeUnits;

        System.Type unitType = unit.GetType();

        foreach (UnitBase friendlyUnit in friendlyUnits)
        {
            if (friendlyUnit != null && friendlyUnit.GetType() == unitType)
            {
                sameTypeUnits.Add(friendlyUnit);
            }
        }

        return sameTypeUnits;
    }

    public int GetFriendlyUnitCount()
    {
        return friendlyUnits.Count;
    }

    public int GetEnemyUnitCount()
    {
        return enemyUnits.Count;
    }

    public void CleanUpUnits()
    {
        foreach (var unit in activeUnits)
        {
            if (unit != null)
                Destroy(unit.gameObject);
        }
        activeUnits.Clear();
    }
}