
using System.Collections;
using UnityEngine;

public class BurstSpawner : BulletSpawner
{
    [Header("Burst Settings")]
    [SerializeField, Min(1)]
    private int shotsPerBurst = 3;

    [SerializeField, Min(0f)]
    private float shotInterval = 0.1f;

    private Coroutine burstCoroutine;
    private bool isBursting;

    public override void Fire(Transform firePoint)
    {
        Fire(firePoint, DefaultProjectileSpeed);
    }

    public override void Fire(
        Transform firePoint,
        float speedOverride)
    {
        if (!PoolReady || firePoint == null)
            return;

        if (isBursting)
            return;

        isBursting = true;

        burstCoroutine = StartCoroutine(
            FireBurst(firePoint, speedOverride)
        );
    }

    private IEnumerator FireBurst(
        Transform firePoint,
        float speedOverride)
    {
        int shotCount = Mathf.Max(1, shotsPerBurst);

        for (int i = 0; i < shotCount; i++)
        {
            if (firePoint == null || !PoolReady)
                break;

            SpawnProjectile(
                firePoint.position,
                firePoint.rotation,
                speedOverride
            );

            if (i < shotCount - 1)
            {
                if (shotInterval > 0f)
                    yield return new WaitForSeconds(shotInterval);
                else
                    yield return null;
            }
        }

        burstCoroutine = null;
        isBursting = false;
    }

    private void OnDisable()
    {
        // Reset the burst state when the character is switched off.
        if (burstCoroutine != null)
        {
            StopCoroutine(burstCoroutine);
            burstCoroutine = null;
        }

        isBursting = false;
    }

    protected override void OnDestroy()
    {
        if (burstCoroutine != null)
        {
            StopCoroutine(burstCoroutine);
            burstCoroutine = null;
        }

        isBursting = false;

        base.OnDestroy();
    }
}
