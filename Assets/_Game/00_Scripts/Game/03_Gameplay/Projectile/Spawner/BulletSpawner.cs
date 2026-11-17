
using UnityEngine;
using Slafurry.Utils.Pooling;

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

    private GenericPool<Projectile> projectilePool;
    private Transform projectilePoolParent;
    private bool poolReady;
    private bool ownsPoolParent;
    private Character ownerCharacter;

    public Character OwnerCharacter
    {
        set => ownerCharacter = value;
    }

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
        SpawnProjectile(firePoint.position, firePoint.rotation, speedOverride, damageOverride);
    }

    protected void SpawnProjectile(
        Vector3 position,
        Quaternion rotation)
    {
        SpawnProjectile(
            position,
            rotation,
            DefaultProjectileSpeed
        );
    }

    protected void SpawnProjectile(
        Vector3 position,
        Quaternion rotation,
        float speedOverride)
    {
        SpawnProjectile(position, rotation, speedOverride, 0f);
    }

    protected void SpawnProjectile(
        Vector3 position,
        Quaternion rotation,
        float speedOverride,
        float damageOverride)
    {
        if (!poolReady || projectilePool == null)
            return;

        Projectile projectile = projectilePool.Get();

        if (projectile == null)
            return;

        projectile.SetOwner(ownerCharacter);
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
