using UnityEngine;

public class MeleeUnit : UnitBase
{
    [Header("Melee Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackRotationSpeed = 720f; // 每秒旋转度数

    [Header("AI Settings")]
    [SerializeField] private float sensingRange = 5f;

    private float lastAttackTime;
    private GameObject attackTarget;
    private bool isAttacking = false;
    private Quaternion originalRotation;
    private float attackProgress;

    protected override void Update()
    {
        base.Update();

        if (isEnemy)
        {
            // 如果没有当前目标或目标已无效，则寻找新目标
            if (attackTarget == null || !attackTarget.activeInHierarchy)
            {
                FindNearestEnemy();
            }

            // 如果有目标且不在攻击中，则处理移动/攻击逻辑
            if (attackTarget != null && !isAttacking)
            {
                HandleEnemyAI();
            }
        }

        if (isAttacking)
        {
            HandleAttackAnimation();
        }
        else if (attackTarget != null && !isMoving)
        {
            // 检查是否在攻击范围内
            if (Vector3.Distance(transform.position, attackTarget.transform.position) <= attackRange)
            {
                StartAttack();
            }
        }
    }

    public override void ReceiveCommand(Vector3 targetPosition, GameObject targetObject)
    {
        if (isEnemy) return; // 自身是敌人则不响应命令

        // 重置攻击状态
        isAttacking = false;
        attackTarget = null;

        if (targetObject != null)
        {
            // 检查目标是否是敌人
            bool targetIsEnemy = false;

            // 只对UnitBase和BuildingBase进行判断
            var unitTarget = targetObject.GetComponent<UnitBase>();
            var buildingTarget = targetObject.GetComponent<BuildingBase>();

            if (unitTarget != null)
            {
                targetIsEnemy = unitTarget.IsEnemy;
            }
            else if (buildingTarget != null)
            {
                targetIsEnemy = buildingTarget.IsEnemy;
            }

            if (targetIsEnemy)
            {
                // 攻击命令
                attackTarget = targetObject;
                this.targetPosition = GetAttackPosition(targetObject.transform.position);
                isMoving = true;
                return;
            }
        }

        // 移动命令
        base.ReceiveCommand(targetPosition, targetObject);
    }

    protected override void MoveToTarget()
    {
        // 到达目标位置后检查是否有攻击目标
        if (attackTarget != null)
        {
            if (Vector3.Distance(transform.position, attackTarget.transform.position) <= attackRange)
            {
                StartAttack();
            }
            else
            {
                targetPosition = GetAttackPosition(attackTarget.transform.position);
                isMoving = true;
                base.MoveToTarget();
            }
        }
        else
            base.MoveToTarget();
    }

    private Vector3 GetAttackPosition(Vector3 enemyPosition)
    {
        // 计算攻击位置（敌人位置减去攻击方向乘以攻击距离）
        Vector3 attackDirection = (transform.position - enemyPosition).normalized;
        return enemyPosition + attackDirection * (attackRange * 0.2f); // 稍微留点余地
    }

    private void StartAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;

        isAttacking = true;
        isMoving = false;
        originalRotation = transform.rotation;
        attackProgress = 0f;
        lastAttackTime = Time.time;
    }

    private void HandleAttackAnimation()
    {
        attackProgress += Time.deltaTime;

        // 旋转攻击动画
        float rotationAmount = attackRotationSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up, rotationAmount);

        // 动画完成
        if (attackProgress >= 1f)
        {
            CompleteAttack();
        }
    }

    private void CompleteAttack()
    {
        isAttacking = false;
        transform.rotation = originalRotation;

        // 应用伤害
        if (attackTarget != null && Vector3.Distance(transform.position, attackTarget.transform.position) <= attackRange)
        {
            if (attackTarget.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(attackDamage);
            }
        }

        // 如果目标仍然存在，继续攻击
        if (attackTarget != null)
        {
            StartAttack();
        }
    }

    private void HandleEnemyAI()
    {
        float distanceToTarget = Vector3.Distance(transform.position, attackTarget.transform.position);

        if (distanceToTarget <= attackRange)
        {
            // 在攻击范围内则直接攻击
            StartAttack();
        }
        else if (distanceToTarget <= sensingRange)
        {
            // 在感知范围内但不在攻击范围内，则移动靠近目标
            targetPosition = GetAttackPosition(attackTarget.transform.position);
            isMoving = true;
        }
        else
        {
            // 超出感知范围则放弃目标
            attackTarget = null;
        }
    }

    private void FindNearestEnemy()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, sensingRange);
        float closestDistance = Mathf.Infinity;
        GameObject closestEnemy = null;

        foreach (var hitCollider in hitColliders)
        {
            // 检查是否是有效敌人（非己方单位/建筑）
            if (IsValidEnemyTarget(hitCollider.gameObject, out var damageable))
            {
                float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = hitCollider.gameObject;
                }
            }
        }

        attackTarget = closestEnemy;
    }

    private bool IsValidEnemyTarget(GameObject target, out IDamageable damageable)
    {
        damageable = null;

        // 检查是否可攻击
        if (!target.TryGetComponent<IDamageable>(out damageable))
            return false;

        // 检查是否是己方单位
        var unit = target.GetComponent<UnitBase>();
        if (unit != null && unit.IsEnemy == isEnemy)
            return false;

        // 检查是否是己方建筑
        var building = target.GetComponent<BuildingBase>();
        if (building != null && building.IsEnemy == isEnemy)
            return false;

        return true;
    }

}