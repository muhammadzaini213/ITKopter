
using UnityEngine;

public class CircularSpawner : BulletSpawner
{
    [Header("Circular Settings")]
    [SerializeField, Min(1)]
    private int projectileCount = 12;

    [SerializeField]
    private float angleOffset = 0f;

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
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = angleOffset + angleStep * i;

            Quaternion rotation =
                firePoint.rotation *
                Quaternion.Euler(0f, 0f, -angle);

            SpawnProjectile(
                firePoint.position,
                rotation,
                speedOverride
            );
        }
    }
}
