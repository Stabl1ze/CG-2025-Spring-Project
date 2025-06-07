using UnityEngine;

public class Projectile : MonoBehaviour
{
    private GameObject target;
    private int damage;
    private float speed = 20f;

    public void SetTarget(GameObject target, int damage)
    {
        this.target = target;
        this.damage = damage;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 direction = (target.transform.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.transform.position) < 0.5f)
        {
            if (target.TryGetComponent<IDamageable>(out var dmg))
            {
                dmg.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}
