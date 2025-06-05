using UnityEngine;
using System.Collections;

public class MainBase : ProductionBuilding
{
    [Header("Main Base Settings")]
    [SerializeField] private float selfHealing = 10f; // 每秒恢复量
    [SerializeField] private float healInterval = 1f; // 恢复间隔(秒)

    private float healTimer = 0f;

    public Vector3 GetDepositPoint()
    {
        return transform.position;
    }

    protected override void Update()
    {
        base.Update();

        if (IsBuilt() && GetCurrentHP() < GetMaxHP())
        {
            healTimer += Time.deltaTime;
            if (healTimer >= healInterval)
            {
                healTimer = 0f;
                HealSelf();
            }
        }
    }

    private void HealSelf()
    {
        float newHP = GetCurrentHP() + selfHealing;
        SetHP(Mathf.Min(newHP, GetMaxHP()));
        UpdateHealthBar();

        // 如果被选中则更新UI
        if (UIManager.Instance?.buildingUI.CurrentBuilding == this)
        {
            UIManager.Instance.UpdateBuildingHP(this);
        }
    }
}