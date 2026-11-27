
using UnityEngine;

// BulletSpawner variant driven by EnemyShooter: spawns bullets at explicit
// world directions instead of a fixed firepoint transform. Reuses the base
// pool, damage, targetMask and shoot SFX.
public class EnemyPatternSpawner : BulletSpawner
{
    private bool warnedNoSine;

    public bool CanSpawn => PoolReady;

    // Satisfies the abstract base: one straight shot at the firepoint.
    public override void Fire(Transform firePoint)
    {
        if (firePoint == null)
            return;

        PlayShootSFX();
        Spawn(firePoint.position, Vector2.down, DefaultProjectileSpeed);
    }

    // One bullet. direction = travel vector (Vector2.down = straight down,
    // the sprite's default facing). Returns null if the pool is not ready.
    public Projectile Spawn(
        Vector2 position,
        Vector2 direction,
        float speed,
        float scale = 1f,
        float sineAmplitude = 0f,
        float sineFrequency = 0f)
    {
        if (!PoolReady)
            return null;

        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.down;

        Quaternion rotation =
            Quaternion.FromToRotation(Vector3.up, direction.normalized);

        Projectile projectile = SpawnProjectile(position, rotation, speed);

        if (projectile == null)
            return null;

        // Projectile travels along transform.up, but enemy bullet art is
        // drawn facing DOWN, so rotating the root toward the travel
        // direction left every sprite pointing backwards (180° off).
        // flipX + flipY is a 180° turn of the art itself; set it on every
        // spawn since pooled bullets keep their renderer state.
        FaceSpritesForward(projectile);

        // Pool does not reset scale; assume prefab base scale of 1.
        if (!Mathf.Approximately(scale, 1f))
            projectile.transform.localScale = Vector3.one * scale;

        if (projectile is SineProjectile sine)
        {
            sine.SetSine(sineAmplitude, sineFrequency);
        }
        else if ((sineAmplitude > 0f || sineFrequency > 0f) && !warnedNoSine)
        {
            Debug.LogWarning(
                "[EnemyPatternSpawner] Sine params set but the assigned " +
                "bullet prefab has no SineProjectile component.",
                this);
            warnedNoSine = true;
        }

        return projectile;
    }

    private readonly System.Collections.Generic.Dictionary<Projectile, SpriteRenderer[]> spriteCache =
        new System.Collections.Generic.Dictionary<Projectile, SpriteRenderer[]>();

    private void FaceSpritesForward(Projectile projectile)
    {
        if (!spriteCache.TryGetValue(projectile, out SpriteRenderer[] sprites))
        {
            sprites = projectile.GetComponentsInChildren<SpriteRenderer>(true);
            spriteCache[projectile] = sprites;
        }

        foreach (SpriteRenderer sprite in sprites)
        {
            if (sprite == null)
                continue;

            sprite.flipX = true;
            sprite.flipY = true;
        }
    }

    // One sound per fire action (not per bullet in a wall/ring).
    public void PlayFireSFX() => PlayShootSFX();
}
