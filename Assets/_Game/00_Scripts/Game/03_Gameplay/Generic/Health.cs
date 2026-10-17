
using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField, Min(1f)]
    private float maxHealth = 100f;

    [SerializeField]
    private bool startAtFullHealth = true;

    private float currentHealth;
    private bool isDead;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthPercentage => maxHealth > 0f
        ? currentHealth / maxHealth
        : 0f;

    public bool IsDead => isDead;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);

        if (startAtFullHealth)
            currentHealth = maxHealth;
        else
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        isDead = currentHealth <= 0f;
    }

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        float previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        if (!Mathf.Approximately(previousHealth, currentHealth))
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetMaxHealth(float newMaxHealth, bool refillHealth = true)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);

        if (refillHealth)
            currentHealth = maxHealth;
        else
            currentHealth = Mathf.Min(currentHealth, maxHealth);

        isDead = currentHealth <= 0f;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (isDead)
            OnDeath?.Invoke();
    }

    public void RestoreFullHealth()
    {
        if (isDead)
            return;

        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        OnDeath?.Invoke();

        Debug.Log($"[{gameObject.name}] has been defeated.", this);
    }
}
