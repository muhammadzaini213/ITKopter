
using UnityEngine;

// Historically fired a multi-shot burst. Now fires exactly one projectile
// per Fire() call — fire rate comes from the shooter's interval (data).
// Name kept so existing prefabs keep working.
public class BurstSpawner : BulletSpawner
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

        PlayShootSFX();

        SpawnProjectile(
            firePoint.position,
            firePoint.rotation,
            speedOverride
        );
    }
}
