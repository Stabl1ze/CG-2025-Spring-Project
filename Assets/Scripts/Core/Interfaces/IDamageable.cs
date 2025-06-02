public interface IDamageable
{
    void TakeDamage(float damage);
    void ShowHealthBar(bool show);
    void UpdateHealthBar();
    float GetCurrentHP();
    float GetMaxHP();
}