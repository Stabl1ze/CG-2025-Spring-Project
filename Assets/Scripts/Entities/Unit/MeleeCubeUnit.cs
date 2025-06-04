using UnityEngine;

public class MeleeCube : UnitBase
{
    [Header("Melee Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackRotationSpeed = 720f; // ÿ����ת����

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

    // ���ӻ�������Χ
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (!isSelected) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}