
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

    [Header("References")]
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyShooter shooter;
    [SerializeField] private Health health;

    private float timer;
    private bool exiting;
    private bool finished;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<EnemyMovement>();

        if (shooter == null)
            shooter = GetComponent<EnemyShooter>();

        if (health == null)
            health = GetComponent<Health>();

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

    private void HandleDeath()
    {
        // Stop everything; the corpse stays until EnemyCollection clears
        // and the wave container is destroyed (existing wave flow).
        if (shooter != null)
            shooter.StopShooting();

        if (movement != null)
            movement.StopMoving();
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
