
using System;
using UnityEngine;
using Slafurry.Systems.Audio;

public class Health : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField, Min(1f)]
    private float maxHealth = 100f;

    [SerializeField]
    private bool startAtFullHealth = true;

    [Header("Death SFX")]
    [SerializeField] private SFXReference deathSfx;

    [Header("Death VFX")]
    [SerializeField] private GameObject deathVfxPrefab;

    [Header("Defense Settings")]
    [SerializeField, Min(0f)]
    private float defensePower = 0f;

    private float currentHealth;
    private bool isDead;
    private Shield shield;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float DefensePower => defensePower;

    public float IncomingDamageMultiplier { get; set; } = 1f;

    public Shield Shield => shield;

    public float HealthPercentage => maxHealth > 0f
        ? currentHealth / maxHealth
        : 0f;

    public bool IsDead => isDead;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDamaged;
    public event Action OnDeath;

    // Set by consumers that want to play their own death sequence first
    // (EnemyLifecycle: spin+shrink, then SpawnDeathVFX()). Default false.
    public bool DeferDeathVFX { get; set; }

    // Static: fires for ANY Health in the scene with the actual HP loss
    // (after shield/defense). Fires with 0 when a hit is fully blocked —
    // consumers: damage popup systems, analytics.
    public static event Action<Health, float> OnAnyDamaged;

    // Static: fires for ANY Health on actual HP gain (popup systems).
    public static event Action<Health, float> OnAnyHealed;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        defensePower = Mathf.Max(0f, defensePower);

        currentHealth = startAtFullHealth
            ? maxHealth
            : Mathf.Clamp(currentHealth, 0f, maxHealth);

        isDead = currentHealth <= 0f;
    }

    public void SetDefensePower(float newDefensePower)
    {
        defensePower = Mathf.Max(0f, newDefensePower);
    }

    public void AssignShield(Shield newShield) => shield = newShield;

    public void RemoveShield() => shield = null;

    public void TakeDamage(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        if (shield != null)
            shield.Absorb(ref amount);

        if (amount <= 0f)
        {
            // Fully blocked by shield — HP unchanged, but show a "0".
            OnAnyDamaged?.Invoke(this, 0f);
            return;
        }

        amount *= IncomingDamageMultiplier;

        float finalDamage = Mathf.Max(0f, amount - defensePower);

        if (finalDamage <= 0f)
        {
            // Fully blocked by defense — HP unchanged, but show a "0".
            OnAnyDamaged?.Invoke(this, 0f);
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);

        OnDamaged?.Invoke();
        OnAnyDamaged?.Invoke(this, finalDamage);
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
        {
            OnAnyHealed?.Invoke(this, currentHealth - previousHealth);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    public void SetMaxHealth(
        float newMaxHealth,
        bool refillHealth = true)
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

        if (deathSfx != null)
            Audio.PlaySFX2D(deathSfx);

        if (!DeferDeathVFX)
            SpawnDeathVFX();

        OnDeath?.Invoke();

        Debug.Log($"[{gameObject.name}] has been defeated.", this);
    }

    // Spawns the death VFX at the entity's current position. Safe to call
    // once even when DeferDeathVFX is on (callers decide the timing).
    public void SpawnDeathVFX()
    {
        if (deathVfxPrefab != null)
            Instantiate(deathVfxPrefab, transform.position, Quaternion.identity);
    }
}
