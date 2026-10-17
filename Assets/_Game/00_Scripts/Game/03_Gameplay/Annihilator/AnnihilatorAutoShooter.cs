
using UnityEngine;

public class AnnihilatorAutoShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BulletSpawner bulletSpawner;
    [SerializeField] private Firepoint firePoint;

    [Header("Firing Settings")]
    [SerializeField, Min(0.01f)] private float fireInterval = 0.2f;
    [SerializeField, Min(0f)] private float bulletSpeed = 10f;

    private float fireTimer;
    private bool hasLoggedMissingReference;

    private void Awake()
    {
        if (firePoint == null)
            firePoint = GetComponentInChildren<Firepoint>();

        if (firePoint == null)
        {
            Debug.LogError(
                "[AnnihilatorAutoShooter] Firepoint is missing.",
                this);
        }

        if (bulletSpawner == null)
        {
            Debug.LogError(
                "[AnnihilatorAutoShooter] BulletSpawner is not assigned.",
                this);
        }
    }

    private void Update()
    {
        if (!CanShoot())
            return;

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

        if (firePoint == null || bulletSpawner == null)
        {
            if (!hasLoggedMissingReference)
            {
                Debug.LogError(
                    "[AnnihilatorAutoShooter] Required references are missing.",
                    this);

                hasLoggedMissingReference = true;
            }

            return false;
        }

        return true;
    }

    private void OnDisable()
    {
        fireTimer = 0f;
    }
}
