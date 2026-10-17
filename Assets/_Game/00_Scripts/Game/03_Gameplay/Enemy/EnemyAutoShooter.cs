
using UnityEngine;

public class EnemyAutoShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BulletSpawner bulletSpawner;
    [SerializeField] private Firepoint firePoint;

    [Header("Firing Settings")]
    [SerializeField, Min(0.01f)] private float fireInterval = 1f;

    [Header("Bullet Settings")]
    [SerializeField, Min(0f)] private float bulletSpeed = 5f;

    private float fireTimer;
    private Health health;
    private bool hasLoggedMissingReference;

    private void Awake()
    {
        if (bulletSpawner == null)
        {
            Debug.LogError(
                "[EnemyAutoShooter] Bullet Spawner is not assigned.",
                this);
        }

        if (firePoint == null)
        {
            firePoint = GetComponentInChildren<Firepoint>(true);
        }

        if (firePoint == null)
        {
            Debug.LogError(
                "[EnemyAutoShooter] Fire Point was not found.",
                this);
        }

        health = GetComponent<Health>();

        if (health == null)
        {
            Debug.LogWarning(
                "[EnemyAutoShooter] Health component is missing.",
                this);
        }
    }

    private void Update()
    {
        if (!CanShoot())
        {
            fireTimer = 0f;
            return;
        }

        fireTimer += Time.deltaTime;

        if (fireTimer < fireInterval)
            return;

        fireTimer -= fireInterval;

        bulletSpawner.Fire(
            firePoint.transform,
            bulletSpeed
        );
    }

    private bool CanShoot()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return false;

        if (bulletSpawner == null || firePoint == null)
        {
            if (!hasLoggedMissingReference)
            {
                Debug.LogError(
                    "[EnemyAutoShooter] Required references are missing.",
                    this);

                hasLoggedMissingReference = true;
            }

            return false;
        }

        if (health != null && health.IsDead)
            return false;

        return true;
    }

    private void OnDisable()
    {
        fireTimer = 0f;
    }
}
