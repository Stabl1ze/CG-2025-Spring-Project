using UnityEngine;

public class RangedUnit : UnitBase
{
    [Header("Ranged Settings")]
    [SerializeField] private float baseAttackRange = 6f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int baseAttackDamage = 8;
    [SerializeField] private GameObject projectilePrefab; 
    [SerializeField] private Transform firePoint;

    [Header("Night Debuff Settings")]
    [SerializeField] private float nightRangeDebuff = 2f;
    [SerializeField] private int nightDamageDebuff = 2;

    [Header("AI Settings")]
    [SerializeField] private float sensingRange = 10f;

    private float lastAttackTime;
    private GameObject attackTarget = null;
    private bool isMoveAttacking = false;
    private float currentAttackRange;
    private int currentAttackDamage;

    protected override void Awake()
    {
        base.Awake();
        currentAttackRange = baseAttackRange;
        currentAttackDamage = baseAttackDamage;
    }

    protected override void Update()
    {
        base.Update();

        if ((attackTarget == null && !isMoving) || isMoveAttacking)
            FindNearestEnemy();

        if (attackTarget != null)
            HandleAttackAI();

        if (!isMoving && attackTarget != null)
        {
            float distance = Vector3.Distance(transform.position, attackTarget.transform.position);
            if (distance <= currentAttackRange && Time.time - lastAttackTime >= attackCooldown)
            {
                TurnToTarget();
                if (Quaternion.Angle(transform.rotation, Quaternion.LookRotation(attackTarget.transform.position - transform.position)) < 5f)
                {
                    Attack();
                }
            }
        }
    }

    public override void ReceiveCommand(Vector3 targetPosition, GameObject targetObject)
    {
        if (isEnemy) return;

        attackTarget = null;
        isMoveAttacking = false;

        if (targetObject == null) isMoveAttacking = true;
        else
        {
            bool targetIsEnemy = false;

            var unit = targetObject.GetComponent<UnitBase>();
            var building = targetObject.GetComponent<BuildingBase>();

            if (unit != null) targetIsEnemy = unit.IsEnemy;
            if (building != null) targetIsEnemy = building.IsEnemy;

            if (targetIsEnemy)
            {
                attackTarget = targetObject;
                isMoving = true;
                this.targetPosition = targetObject.transform.position;
                return;
            }
        }

        base.ReceiveCommand(targetPosition, targetObject);
    }

    private void TurnToTarget()
    {
        Vector3 direction = (attackTarget.transform.position - transform.position).normalized;
        direction.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    public void ApplyNightDebuff(bool isNight)
    {
        if (isNight)
        {
            currentAttackRange = Mathf.Max(1f, baseAttackRange - nightRangeDebuff);
            currentAttackDamage = Mathf.Max(1, baseAttackDamage - nightDamageDebuff);
        }
        else
        {
            currentAttackRange = baseAttackRange;
            currentAttackDamage = baseAttackDamage;
        }
    }

    private void Attack()
    {
        lastAttackTime = Time.time;

        if (projectilePrefab != null && firePoint != null && attackTarget != null)
        {
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Projectile p = proj.GetComponent<Projectile>();
            p.SetTarget(attackTarget, currentAttackDamage);
        }
    }

    private void HandleAttackAI()
    {
        if (attackTarget == null) return;

        float distance = Vector3.Distance(transform.position, attackTarget.transform.position);
        if (distance > currentAttackRange && distance <= sensingRange)
        {
            targetPosition = attackTarget.transform.position;
            isMoving = true;
        }
        else if (distance <= currentAttackRange)
        {
            isMoving = false;
        }
        else
        {
            attackTarget = null;
        }
    }

    private void FindNearestEnemy()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, sensingRange);
        float closestDistance = Mathf.Infinity;
        GameObject closest = null;

        foreach (var col in colliders)
        {
            if (IsValidEnemyTarget(col.gameObject, out var _))
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);

                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closest = col.gameObject;
                }
            }
        }

        attackTarget = closest;
    }

    private bool IsValidEnemyTarget(GameObject target, out IDamageable damageable)
    {
        damageable = null;
        if (!target.TryGetComponent(out damageable)) return false;

        var unit = target.GetComponent<UnitBase>();
        if (unit != null && unit.IsEnemy == isEnemy) return false;

        var building = target.GetComponent<BuildingBase>();
        if (building != null && building.IsEnemy == isEnemy) return false;

        return true;
    }
    protected override void MoveToTarget()
    {
        if (attackTarget != null)
        {
            float distance = Vector3.Distance(transform.position, attackTarget.transform.position);

            if (distance <= currentAttackRange)
            {
                isMoving = false;
                return;
            }

            targetPosition = attackTarget.transform.position;
            base.MoveToTarget();
        }
        else
        {
            base.MoveToTarget();
        }
    }

}
