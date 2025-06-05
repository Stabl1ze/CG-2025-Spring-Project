using UnityEngine;

public class MeleeUnit : UnitBase
{
    [Header("Melee Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackRotationSpeed = 720f; // ÿ����ת����

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
            // ���û�е�ǰĿ���Ŀ������Ч����Ѱ����Ŀ��
            if (attackTarget == null || !attackTarget.activeInHierarchy)
            {
                FindNearestEnemy();
            }

            // �����Ŀ���Ҳ��ڹ����У������ƶ�/�����߼�
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
            // ����Ƿ��ڹ�����Χ��
            if (Vector3.Distance(transform.position, attackTarget.transform.position) <= attackRange)
            {
                StartAttack();
            }
        }
    }

    public override void ReceiveCommand(Vector3 targetPosition, GameObject targetObject)
    {
        if (isEnemy) return; // �����ǵ�������Ӧ����

        // ���ù���״̬
        isAttacking = false;
        attackTarget = null;

        if (targetObject != null)
        {
            // ���Ŀ���Ƿ��ǵ���
            bool targetIsEnemy = false;

            // ֻ��UnitBase��BuildingBase�����ж�
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
                // ��������
                attackTarget = targetObject;
                this.targetPosition = GetAttackPosition(targetObject.transform.position);
                isMoving = true;
                return;
            }
        }

        // �ƶ�����
        base.ReceiveCommand(targetPosition, targetObject);
    }

    protected override void MoveToTarget()
    {
        // ����Ŀ��λ�ú����Ƿ��й���Ŀ��
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
        // ���㹥��λ�ã�����λ�ü�ȥ����������Թ������룩
        Vector3 attackDirection = (transform.position - enemyPosition).normalized;
        return enemyPosition + attackDirection * (attackRange * 0.2f); // ��΢�������
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

        // ��ת��������
        float rotationAmount = attackRotationSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up, rotationAmount);

        // �������
        if (attackProgress >= 1f)
        {
            CompleteAttack();
        }
    }

    private void CompleteAttack()
    {
        isAttacking = false;
        transform.rotation = originalRotation;

        // Ӧ���˺�
        if (attackTarget != null && Vector3.Distance(transform.position, attackTarget.transform.position) <= attackRange)
        {
            if (attackTarget.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(attackDamage);
            }
        }

        // ���Ŀ����Ȼ���ڣ���������
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
            // �ڹ�����Χ����ֱ�ӹ���
            StartAttack();
        }
        else if (distanceToTarget <= sensingRange)
        {
            // �ڸ�֪��Χ�ڵ����ڹ�����Χ�ڣ����ƶ�����Ŀ��
            targetPosition = GetAttackPosition(attackTarget.transform.position);
            isMoving = true;
        }
        else
        {
            // ������֪��Χ�����Ŀ��
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
            // ����Ƿ�����Ч���ˣ��Ǽ�����λ/������
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

        // ����Ƿ�ɹ���
        if (!target.TryGetComponent<IDamageable>(out damageable))
            return false;

        // ����Ƿ��Ǽ�����λ
        var unit = target.GetComponent<UnitBase>();
        if (unit != null && unit.IsEnemy == isEnemy)
            return false;

        // ����Ƿ��Ǽ�������
        var building = target.GetComponent<BuildingBase>();
        if (building != null && building.IsEnemy == isEnemy)
            return false;

        return true;
    }

}