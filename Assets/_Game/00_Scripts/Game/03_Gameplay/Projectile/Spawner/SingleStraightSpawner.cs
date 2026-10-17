
using UnityEngine;

public class SingleStraightSpawner : BulletSpawner
{
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

        SpawnProjectile(
            firePoint.position,
            firePoint.rotation,
            speedOverride
        );
    }
}
