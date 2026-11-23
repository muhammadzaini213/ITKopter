
using System.Collections;
using UnityEngine;

// Lifetime exit + death/despawn cleanup (spec section 5):
// - after `lifetime` seconds alive -> BeginExit -> fly up -> destroy
// - HitAndRun's own exit reaches the same destroy path via OnLeftScreen
// - death -> stop all shooting (corpses stay until the wave collection clears)
public class EnemyLifecycle : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField, Tooltip("Infinite — never exits on lifetime; only removed when the wave clears (death) or its own exit (HitAndRun).")]
    private bool infiniteLifetime = false;

    [SerializeField, Min(0f), Tooltip("Exit after this many seconds alive. 0 = never. Ignored when Infinite is on.")]
    private float lifetime = 20f;

    [SerializeField, Min(0f), Tooltip("Fly-up speed for the lifetime exit.")]
    private float exitSpeed = 4f;

    [Header("Death Effect (helicopter fall)")]
    [SerializeField, Tooltip("Spin + shrink the corpse before destroying. 0 = destroy instantly.")]
    private float deathSpinDuration = 1f;

    [SerializeField, Min(0f), Tooltip("Spin speed in deg/s, random direction. Harsh = 600+.")]
    private float deathSpinSpeed = 600f;

    [SerializeField, Tooltip("Shrink the corpse to zero scale during the spin.")]
    private bool deathShrink = true;

    [SerializeField, Tooltip("Spin accelerates up to 3x over the duration (crash feel).")]
    private bool deathSpinAccelerate = true;

    [SerializeField, Tooltip("Stop the corpse from eating bullets while it falls.")]
    private bool disableCollidersOnDeath = true;

    [Header("References")]
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyShooter shooter;
    [SerializeField] private Health health;

    private float timer;
    private bool exiting;
    private bool finished;
    private bool deathEffectStarted;

    // Data-only apply from EnemyDataBinder (runs before this Awake, which
    // reads infiniteLifetime for SuppressOwnExit).
    public void ApplyData(EnemyData data)
    {
        infiniteLifetime = data.infiniteLifetime;
        lifetime = data.lifetime;
        exitSpeed = data.lifetimeExitSpeed;
    }

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<EnemyMovement>();

        if (shooter == null)
            shooter = GetComponent<EnemyShooter>();

        if (health == null)
            health = GetComponent<Health>();

        // Death VFX plays after the spin+shrink effect, not on hit.
        if (health != null)
            health.DeferDeathVFX = true;

        if (infiniteLifetime && movement != null)
            movement.SuppressOwnExit = true;
    }

    private void OnEnable()
    {
        if (movement != null)
            movement.OnLeftScreen += HandleLeftScreen;

        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (movement != null)
            movement.OnLeftScreen -= HandleLeftScreen;

        if (health != null)
            health.OnDeath -= HandleDeath;
    }

    private void Update()
    {
        if (finished || exiting || infiniteLifetime || lifetime <= 0f)
            return;

        timer += Time.deltaTime;

        if (timer < lifetime)
            return;

        exiting = true;

        if (movement != null)
        {
            movement.BeginExit(exitSpeed);
        }
        else
        {
            // No movement component to fly out — despawn directly.
            Cleanup(destroy: true);
        }
    }

    private void HandleLeftScreen()
    {
        Cleanup(destroy: true);
    }

    private bool deathReported;

    private void HandleDeath()
    {
        if (!deathReported)
        {
            deathReported = true;
            Objectives.Report(ObjectiveType.DefeatEnemies, 1);

            if (GetComponent<EnemyBoss>() != null)
                Objectives.Report(ObjectiveType.DefeatBosses, 1);
        }

        // Stop everything — the corpse no longer moves or shoots.
        if (shooter != null)
            shooter.StopShooting();

        if (movement != null)
            movement.StopMoving();

        if (deathEffectStarted)
            return;

        deathEffectStarted = true;

        if (disableCollidersOnDeath)
        {
            Collider2D[] colliders =
                GetComponentsInChildren<Collider2D>();

            foreach (var col in colliders)
                col.enabled = false;
        }

        // SquashBlink animates scale on damage — let the death shrink own it.
        SquashBlink blink = GetComponent<SquashBlink>();

        if (blink != null)
            blink.enabled = false;

        if (deathSpinDuration <= 0f)
        {
            SpawnDeathVFXAndDestroy();
            return;
        }

        StartCoroutine(DeathSpinRoutine());
    }

    private void SpawnDeathVFXAndDestroy()
    {
        if (health != null)
            health.SpawnDeathVFX();

        Destroy(gameObject);
    }

    // Harsh spin (random direction, optional acceleration) + shrink to
    // zero scale, then destroy. Wave flow unaffected: Health.IsDead is
    // already true, so EnemyCollection counts this enemy as cleared.
    private IEnumerator DeathSpinRoutine()
    {
        Vector3 startScale = transform.localScale;
        float direction = Random.value < 0.5f ? -1f : 1f;
        float time = 0f;

        while (time < deathSpinDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / deathSpinDuration);

            float speed = deathSpinSpeed * direction;

            if (deathSpinAccelerate)
                speed *= 1f + t * 2f; // up to 3x at the end

            transform.Rotate(0f, 0f, speed * Time.deltaTime);

            if (deathShrink)
                transform.localScale = startScale * (1f - t);

            yield return null;
        }

        SpawnDeathVFXAndDestroy();
    }

    private void Cleanup(bool destroy)
    {
        if (finished)
            return;

        finished = true;

        if (shooter != null)
            shooter.StopShooting();

        if (movement != null)
            movement.StopMoving();

        if (destroy)
            Destroy(gameObject);
    }
}
