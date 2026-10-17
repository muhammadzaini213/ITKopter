
using UnityEngine;
using Slafurry.Utils.Pooling;

public class PlayerAutoShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform firePoint;

    [SerializeField]
    private Projectile projectilePrefab;

    private Transform projectilePoolParent;

    [Header("Firing Settings")]
    [SerializeField, Min(0.01f)]
    private float fireInterval = 0.2f;

    [SerializeField, Min(0.01f)]
    private float projectileSpeed = 15f;

    [SerializeField, Min(0f)]
    private float projectileDamage = 10f;

    [Header("Pooling")]
    [SerializeField, Min(0)]
    private int prewarmCount = 30;

    [SerializeField, Min(1)]
    private int maxPoolSize = 300;

    [Header("Target Detection")]
    [SerializeField]
    private LayerMask targetMask;

    private GenericPool<Projectile> projectilePool;
    private float fireTimer;
    private bool poolReady;

    private void Awake()
    {
        projectilePoolParent = Object.FindFirstObjectByType<PoolParent>()?.transform;
    }

    private void Start()
    {
        InitializePool();
    }

    private void Update()
    {
        if (!poolReady || firePoint == null || projectilePrefab == null)
            return;

        fireTimer += Time.deltaTime;

        while (fireTimer >= fireInterval)
        {
            fireTimer -= fireInterval;
            FireProjectile();
        }
    }

    private void InitializePool()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError(
                "[PlayerAutoShooter] Projectile prefab is not assigned.",
                this
            );

            return;
        }

        if (projectilePoolParent == null)
        {
            GameObject poolContainer = new GameObject(
                "PlayerProjectilePool"
            );

            projectilePoolParent = poolContainer.transform;
        }

        projectilePool = new GenericPool<Projectile>(
            projectilePrefab,
            projectilePoolParent,
            defaultCapacity: Mathf.Max(1, prewarmCount),
            maxSize: Mathf.Max(1, maxPoolSize)
        );

        projectilePool.Prewarm(prewarmCount);

        poolReady = true;
        fireTimer = 0f;
    }

    private void FireProjectile()
    {
        if (firePoint == null)
            return;

        Projectile projectile = projectilePool.Get();

        projectile.SetReturnCallback(ReturnProjectile);

        projectile.Initialize(
            firePoint.position,
            firePoint.rotation,
            projectileSpeed,
            projectileDamage,
            targetMask
        );
    }

    private void ReturnProjectile(Projectile projectile)
    {
        if (projectilePool == null || projectile == null)
            return;

        projectilePool.Release(projectile);
    }

    private void OnDestroy()
    {
        poolReady = false;

        if (projectilePool != null)
        {
            projectilePool.Clear();
            projectilePool = null;
        }
    }
}
