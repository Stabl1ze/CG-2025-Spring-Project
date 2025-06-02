using UnityEngine;

public class MeleeCube : UnitBase
{
    [Header("Melee Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackRotationSpeed = 720f; // 每秒旋转度数

    private float lastAttackTime;
    private GameObject attackTarget;
    private bool isAttacking = false;
    private Quaternion originalRotation;
    private float attackProgress;

    protected override void Update()
    {
        base.Update();

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
        if (isEnemy) return; // Enemy check

        // 重置攻击状态
        isAttacking = false;
        attackTarget = null;

        if (targetObject != null && targetObject.CompareTag("Enemy"))
        {
            // 攻击命令
            attackTarget = targetObject;
            this.targetPosition = GetAttackPosition(targetObject.transform.position);
            isMoving = true;
        }
        else
        {
            // 移动命令
            base.ReceiveCommand(targetPosition, targetObject);
        }
    }

    protected override void MoveToTarget()
    {
        base.MoveToTarget();

        // 到达目标位置后检查是否有攻击目标
        if (!isMoving && attackTarget != null)
        {
            if (Vector3.Distance(transform.position, attackTarget.transform.position) <= attackRange)
            {
                StartAttack();
            }
            else
            {
                // 目标移动了，重新计算攻击位置
                targetPosition = GetAttackPosition(attackTarget.transform.position);
                isMoving = true;
            }
        }
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

    // 可视化攻击范围
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (!isSelected) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}