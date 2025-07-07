using UnityEngine;
using System.Collections;
public class Melee2Unit : UnitBase
{
    [Header("Melee2 Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int attackDamage = 10;

    [Header("Attack Animation Settings")]
    [SerializeField] private float expandDuration = 0.1f;
    [SerializeField] private float shrinkDuration = 0.4f;
    [SerializeField] private float maxScaleMultiplier = 2f;

    [Header("AI Settings")]
    [SerializeField] private float sensingRange = 5f;

    private float lastAttackTime;
    private GameObject attackTarget = null;
    private bool isAttacking = false;
    private bool isMoveAttacking = false;
    private Vector3 originalScale;

    protected override void Update()
    {
        base.Update();

        if ((attackTarget == null && !isMoving) || isMoveAttacking)
            FindNearestEnemy();

        if (attackTarget != null && !isAttacking)
            HandleAttackAI();

        if (isAttacking)
            return;
        else if (attackTarget != null && !isMoving)
        {
            if (Vector3.Distance(transform.position, attackTarget.transform.position) <= attackRange)
                StartAttack();
        }
    }

    public override void ReceiveCommand(Vector3 targetPosition, GameObject targetObject)
    {
        if (isEnemy) return;

        attackTarget = null;
        isAttacking = false;
        isMoveAttacking = false;

        if (targetObject == null) isMoveAttacking = true;
        else
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
        {
            base.MoveToTarget();
        }
    }

    private Vector3 GetAttackPosition(Vector3 enemyPosition)
    {
        Vector3 attackDirection = (transform.position - enemyPosition).normalized;
        return enemyPosition + attackDirection * (attackRange * 0.2f);
    }

    private void StartAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown || isAttacking) return;

        StartCoroutine(PlayAttackAnimation());
    }

    private void ApplyKnockbackAndDamage()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
        foreach (var hitCollider in hitColliders)
        {
            if (IsValidEnemyTarget(hitCollider.gameObject, out var damageable))
            {
                Vector3 targetPosition = hitCollider.transform.position;
                targetPosition.y = transform.position.y;

                Vector3 knockbackDir = (targetPosition - transform.position).normalized;
                Vector3 knockbackTarget = transform.position + knockbackDir * (attackRange + 0.5f);

                Vector3 finalTarget = new Vector3(knockbackTarget.x, hitCollider.transform.position.y, knockbackTarget.z);
                StartCoroutine(SmoothKnockback(hitCollider.transform, finalTarget, 0.2f));

                damageable.TakeDamage(attackDamage);
            }
        }
    }
    private IEnumerator SmoothKnockback(Transform target, Vector3 destination, float duration)
    {
        if (target == null) yield return null;

        float time = 0f;
        Vector3 start = target.position;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            Vector3 nextPos = Vector3.Lerp(start, destination, t);
            target.position = new Vector3(nextPos.x, start.y, nextPos.z);
            yield return null;
        }
        target.position = new Vector3(destination.x, start.y, destination.z);
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

    private IEnumerator PlayAttackAnimation()
    {
        isAttacking = true;
        isMoving = false;
        lastAttackTime = Time.time;

        originalScale = transform.localScale;
        float timer = 0f;

        // 快速放大
        while (timer < expandDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / expandDuration);
            float scale = Mathf.Lerp(1f, maxScaleMultiplier, t);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        ApplyKnockbackAndDamage(); // 撞開 & 傷害發生在最大時刻

        // 緩慢縮小
        timer = 0f;
        while (timer < shrinkDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / shrinkDuration);
            float scale = Mathf.Lerp(maxScaleMultiplier, 1f, t);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        transform.localScale = originalScale;
        isAttacking = false;

    }
}
