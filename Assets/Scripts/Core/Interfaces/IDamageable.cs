public interface IDamageable
{
    void TakeDamage(float damage);
    void ShowHealthBar(bool show);
    void UpdateHealthBar();
    float GetCurrentHP();
    float GetMaxHP();
    void SetHP(float hp);
    void SetMaxHP(float max);
}