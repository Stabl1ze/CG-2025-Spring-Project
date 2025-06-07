using UnityEngine;
using System.Collections;

public class Ranged2Unit : UnitBase
{
    [Header("Ranged2 Settings")]
    [SerializeField] private float attackRange = 6f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int attackDamage = 8;
    [SerializeField] private GameObject projectilePrefab; 
    [SerializeField] private Transform firePoint;

    [Header("AI Settings")]
    [SerializeField] private float sensingRange = 10f;

    private float lastAttackTime;
    private GameObject attackTarget;

    protected override void Update()
    {
        base.Update();


        if (attackTarget == null && !isMoving)
            FindNearestEnemy();

        if (attackTarget != null)
            HandleEnemyAI();

        if (!isMoving && attackTarget != null)
        {
            float distance = Vector3.Distance(transform.position, attackTarget.transform.position);
            if (distance <= attackRange && Time.time - lastAttackTime >= attackCooldown)
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
        if (targetObject != null)
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
    private void Attack()
    {
        lastAttackTime = Time.time;

        if (projectilePrefab != null && firePoint != null && attackTarget != null)
        {
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Projectile p = proj.GetComponent<Projectile>();
            p.SetTarget(attackTarget, attackDamage);
            StartCoroutine(ShootSecondProjectile());
        }
    }

    private void HandleEnemyAI()
    {
        if (attackTarget == null) return;

        float distance = Vector3.Distance(transform.position, attackTarget.transform.position);
        if (distance > attackRange && distance <= sensingRange)
        {
            targetPosition = attackTarget.transform.position;
            isMoving = true;
        }
        else if (distance <= attackRange)
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

            if (distance <= attackRange)
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
    private IEnumerator ShootSecondProjectile()
    {
        yield return new WaitForSeconds(0.2f); 

        if (firePoint != null && attackTarget != null)
        {
            GameObject proj2 = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Projectile p2 = proj2.GetComponent<Projectile>();
            p2.SetTarget(attackTarget, attackDamage);
        }
    }
}
