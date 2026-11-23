using UnityEngine;

// Applies an EnemyData asset to every data-driven component on this enemy
// at spawn. Runs BEFORE other Awakes (-100) so EnemyMovement/EnemyShooter/
// EnemyLifecycle read the applied values during their own initialization.
//
// data == null → components keep their own Inspector values (existing
// prefabs work unchanged).
[DefaultExecutionOrder(-100)]
public class EnemyDataBinder : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private EnemyData data;

    [SerializeField, Tooltip("Push data onto components during Awake. Uncheck to call Apply() yourself.")]
    private bool applyOnAwake = true;

    public EnemyData Data => data;

    private void Awake()
    {
        if (applyOnAwake)
            Apply();
    }

    [ContextMenu("Apply Data Now")]
    public void Apply()
    {
        if (data == null)
            return;

        float mult = ResolveEnemyStatMultiplier();

        // Health — Set* methods are event-safe in any Awake order.
        Health health = GetComponentInChildren<Health>();

        if (health != null)
        {
            health.SetMaxHealth(data.maxHealth * mult);
            health.SetDefensePower(data.defensePower * mult);
        }

        // Weapon — read per shot, order-free.
        EnemyPatternSpawner spawner =
            GetComponentInChildren<EnemyPatternSpawner>(true);

        if (spawner != null)
        {
            spawner.ApplyProjectileStats(
                data.projectileDamage * mult,
                data.projectileSpeed);
        }

        // Movement — must land before its Awake derives timers/orbit values.
        EnemyMovement movement = GetComponentInChildren<EnemyMovement>();

        if (movement != null)
            movement.ApplyData(data);

        // Shooting — must land before its Awake builds rng/active patterns.
        EnemyShooter shooter = GetComponentInChildren<EnemyShooter>();

        if (shooter != null)
            shooter.ApplyData(data);

        // Lifecycle — must land before its Awake sets SuppressOwnExit.
        EnemyLifecycle lifecycle = GetComponentInChildren<EnemyLifecycle>();

        if (lifecycle != null)
            lifecycle.ApplyData(data);
    }

    // Enemy Scaling lives on the current LevelData (per-level difficulty);
    // missing system/level → 1 (unchanged).
    private static float ResolveEnemyStatMultiplier()
    {
        LevelSystem levelSystem = LevelSystem.Instance;
        LevelData level =
            levelSystem != null ? levelSystem.CurrentLevel : null;

        return level != null
            ? Mathf.Max(0.01f, level.EnemyStatMultiplier)
            : 1f;
    }
}
