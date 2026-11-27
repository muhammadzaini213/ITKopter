
using UnityEngine;
using Slafurry.Utils.Pooling;
using Slafurry.Systems.Audio;

public abstract class BulletSpawner : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField]
    private Projectile projectilePrefab;

    [SerializeField, Min(0.01f)]
    private float projectileSpeed = 15f;

    [SerializeField, Min(0f)]
    private float projectileDamage = 10f;

    [SerializeField]
    private LayerMask targetMask;

    [Header("Pooling")]
    [SerializeField, Min(0)]
    private int prewarmCount = 30;

    [SerializeField, Min(1)]
    private int maxPoolSize = 300;

    [Header("Shoot SFX")]
    [SerializeField]
    private SFXReference shootSfx;

    private GenericPool<Projectile> projectilePool;
    private Transform projectilePoolParent;
    private bool poolReady;
    private bool ownsPoolParent;
    private Character ownerCharacter;

    // Hierarchy of the shooter (Counter Mechanics), resolved lazily on the
    // first spawn: player bullets from their Character, enemy bullets from
    // the EnemyDataBinder on the prefab root. Unknown → null → neutral.
    private CharacterType? attackerType;
    private bool attackerTypeCached;

    public Character OwnerCharacter
    {
        set => ownerCharacter = value;
    }

    /// <summary>Forces the hierarchy of this spawner's bullets (e.g. the Annihilator's chosen type). Null = normal resolution.</summary>
    public CharacterType? AttackerTypeOverride { get; set; }

    protected bool PoolReady => poolReady;
    protected float DefaultProjectileSpeed => projectileSpeed;

    protected virtual void Awake()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError(
                "[BulletSpawner] Projectile prefab is not assigned.",
                this
            );
            return;
        }

        PoolParent existingPoolParent =
            Object.FindFirstObjectByType<PoolParent>();

        if (existingPoolParent != null)
        {
            projectilePoolParent = existingPoolParent.transform;
        }
        else
        {
            GameObject poolContainer = new GameObject(
                $"{gameObject.name}_ProjectilePool"
            );

            projectilePoolParent = poolContainer.transform;
            ownsPoolParent = true;
        }

        projectilePool = new GenericPool<Projectile>(
            projectilePrefab,
            projectilePoolParent,
            defaultCapacity: Mathf.Max(1, prewarmCount),
            maxSize: Mathf.Max(1, maxPoolSize)
        );

        projectilePool.Prewarm(prewarmCount);
        poolReady = true;
    }

    public abstract void Fire(Transform firePoint);

    public virtual void Fire(
        Transform firePoint,
        float speedOverride)
    {
        Fire(firePoint, speedOverride, 0f);
    }

    public virtual void Fire(
        Transform firePoint,
        float speedOverride,
        float damageOverride)
    {
        if (firePoint == null)
            return;

        PlayShootSFX();
        SpawnProjectile(firePoint.position, firePoint.rotation, speedOverride, damageOverride);
    }

    // Called by subclasses that override Fire directly so every fire path
    // plays the shot sound exactly once per trigger (not per projectile).
    protected void PlayShootSFX()
    {
        if (shootSfx != null)
            Audio.PlaySFX2D(shootSfx);
    }

    protected Projectile SpawnProjectile(
        Vector3 position,
        Quaternion rotation)
    {
        return SpawnProjectile(
            position,
            rotation,
            DefaultProjectileSpeed
        );
    }

    protected Projectile SpawnProjectile(
        Vector3 position,
        Quaternion rotation,
        float speedOverride)
    {
        return SpawnProjectile(position, rotation, speedOverride, 0f);
    }

    protected Projectile SpawnProjectile(
        Vector3 position,
        Quaternion rotation,
        float speedOverride,
        float damageOverride)
    {
        if (!poolReady || projectilePool == null)
            return null;

        Projectile projectile = projectilePool.Get();

        if (projectile == null)
            return null;

        projectile.SetOwner(ownerCharacter);
        projectile.SetAttackerType(ResolveAttackerType());
        projectile.SetReturnCallback(ReturnProjectile);

        float finalDamage = damageOverride > 0f
            ? damageOverride
            : projectileDamage;

        projectile.Initialize(
            position,
            rotation,
            Mathf.Max(0.01f, speedOverride),
            finalDamage,
            targetMask
        );

        return projectile;
    }

    // External config (EnemyDataBinder). Damage <= 0 or speed <= 0 keeps
    // the current Inspector value.
    public void ApplyProjectileStats(float damage, float speed)
    {
        if (damage > 0f)
            projectileDamage = damage;

        if (speed > 0f)
            projectileSpeed = speed;
    }

    private CharacterType? ResolveAttackerType()
    {
        if (AttackerTypeOverride.HasValue)
            return AttackerTypeOverride;

        // Player bullets: the owning Character (its type never changes).
        if (ownerCharacter != null)
            return ownerCharacter.CharacterType;

        // Enemy bullets: hierarchy lives on the EnemyData asset.
        if (!attackerTypeCached)
        {
            EnemyDataBinder binder =
                GetComponentInParent<EnemyDataBinder>();

            if (binder != null && binder.Data != null)
                attackerType = binder.Data.characterType;

            attackerTypeCached = true;
        }

        return attackerType;
    }

    private void ReturnProjectile(Projectile projectile)
    {
        if (projectilePool == null || projectile == null)
            return;

        projectilePool.Release(projectile);
    }

    protected virtual void OnDestroy()
    {
        poolReady = false;

        if (projectilePool != null)
        {
            projectilePool.Clear();
            projectilePool = null;
        }

        if (ownsPoolParent && projectilePoolParent != null)
            Destroy(projectilePoolParent.gameObject);
    }
}
