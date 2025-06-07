using UnityEngine;

public class MeleeUnit : UnitBase
{
    [Header("Melee Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackRotationSpeed = 720f;

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

        if (attackTarget == null && !isMoving)
            FindNearestEnemy();

        if (attackTarget != null && !isAttacking)
            HandleAttackAI();

        if (isAttacking)
            HandleAttackAnimation();
        else if (attackTarget != null && !isMoving)
            if (Vector3.Distance(transform.position, attackTarget.transform.position) <= attackRange)
                StartAttack();
    }

    public override void ReceiveCommand(Vector3 targetPosition, GameObject targetObject)
    {
        if (isEnemy) return;

        isAttacking = false;
        attackTarget = null;

        if (targetObject != null)
        {
            bool targetIsEnemy = false;

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
                attackTarget = targetObject;
                this.targetPosition = GetAttackPosition(targetObject.transform.position);
                isMoving = true;
                return;
            }
        }

        base.ReceiveCommand(targetPosition, targetObject);
    }

    protected override void MoveToTarget()
    {
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
        Vector3 attackDirection = (transform.position - enemyPosition).normalized;
        return enemyPosition + attackDirection * (attackRange * 0.2f);
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

        float rotationAmount = attackRotationSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up, rotationAmount);

        if (attackProgress >= 1f)
        {
            CompleteAttack();
        }
    }

    private void CompleteAttack()
    {
        isAttacking = false;
        transform.rotation = originalRotation;

        if (attackTarget != null && Vector3.Distance(transform.position, attackTarget.transform.position) <= attackRange)
        {
            if (attackTarget.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(attackDamage);
            }
        }

        if (attackTarget != null)
        {
            StartAttack();
        }
    }

    private void HandleAttackAI()
    {
        float distanceToTarget = Vector3.Distance(transform.position, attackTarget.transform.position);

        if (distanceToTarget <= attackRange)
        {
            StartAttack();
        }
        else if (distanceToTarget <= sensingRange)
        {
            targetPosition = GetAttackPosition(attackTarget.transform.position);
            isMoving = true;
        }
        else
        {
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

        if (!target.TryGetComponent<IDamageable>(out damageable))
            return false;

        var unit = target.GetComponent<UnitBase>();
        if (unit != null && unit.IsEnemy == isEnemy)
            return false;

        var building = target.GetComponent<BuildingBase>();
        if (building != null && building.IsEnemy == isEnemy)
            return false;

        return true;
    }

}