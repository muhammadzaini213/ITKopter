
using UnityEngine;

public class ShotgunSpawner : BulletSpawner
{
    [Header("Shotgun Settings")]
    [SerializeField, Min(1)]
    private int projectileCount = 5;

    [SerializeField, Range(0f, 360f)]
    private float spreadAngle = 45f;

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

        int count = Mathf.Max(1, projectileCount);

        if (count == 1)
        {
            SpawnProjectile(
                firePoint.position,
                firePoint.rotation,
                speedOverride
            );
            return;
        }

        float startAngle = -spreadAngle * 0.5f;
        float angleStep = spreadAngle / (count - 1);

        for (int i = 0; i < count; i++)
        {
            float angleOffset = startAngle + angleStep * i;

            Quaternion rotation =
                firePoint.rotation *
                Quaternion.Euler(0f, 0f, -angleOffset);

            SpawnProjectile(
                firePoint.position,
                rotation,
                speedOverride
            );
        }
    }
}
