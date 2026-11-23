using UnityEngine;

// Central per-enemy-type definition: identity, stats, movement, shooting
// patterns and lifecycle. Applied to an enemy prefab at spawn by
// EnemyDataBinder — one asset per enemy type, reusable across prefabs
// and waves. Scene/object references (firepoints, sprite transforms,
// spawner) stay on the prefab.
[CreateAssetMenu(fileName = "EnemyData", menuName = "Slafurry/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Same enum as characters (ABSTRACT / SIMPLE / SPECIAL / PRIMORDIAL / ANOMALY).")]
    public CharacterType characterType = CharacterType.SIMPLE;

    [Header("Health")]
    [Min(1f)] public float maxHealth = 100f;
    [Min(0f)] public float defensePower = 0f;

    [Header("Weapon")]
    [Min(0f)] public float projectileDamage = 10f;
    [Min(0.01f)] public float projectileSpeed = 15f;

    [Header("Movement")]
    public EnemyMovementType movementType = EnemyMovementType.Hold;

    [Min(0f), Tooltip("Absolute world Y where the enemy holds position.")]
    public float holdY = 4f;

    [Min(0f), Tooltip("Strafe speed (also the descend speed for Dive/Blink/Hold).")]
    public float speed = 1.5f;

    [Min(0f), Tooltip("Strafe: half-width of the patrol range around spawn X.")]
    public float rangeX = 3f;

    [Min(0f), Tooltip("Dive: pause before dashing at the player.")]
    public float idleTime = 2f;
    [Min(0f)] public float diveSpeed = 9f;
    [Min(0f)] public float retreatSpeed = 3f;

    [Min(0f), Tooltip("HitAndRun / Hold: descend-to-hold speed.")]
    public float enterSpeed = 4f;
    [Min(0f), Tooltip("HitAndRun: seconds held before flying out.")]
    public float stayTime = 3f;
    [Min(0f), Tooltip("HitAndRun: fly-out speed (its own exit).")]
    public float hitAndRunExitSpeed = 4f;

    [Min(0f), Tooltip("Descend: straight-down speed.")]
    public float descendSpeed = 0.8f;
    [Min(0f), Tooltip("SineWave / Zigzag: straight-down speed.")]
    public float moveDownSpeed = 0.6f;

    [Min(0f), Tooltip("SineWave: lateral swing size.")]
    public float amplitudeX = 2.5f;
    [Min(0f), Tooltip("SineWave: oscillations per second.")]
    public float frequency = 0.5f;

    [Tooltip("MoveToPoint: world position to reach.")]
    public Vector2 targetPoint = new Vector2(0f, 3f);
    [Min(0f)] public float moveToSpeed = 1f;

    [Min(0f), Tooltip("Zigzag: lateral speed.")]
    public float lateralSpeed = 4f;
    [Min(0.01f), Tooltip("Zigzag: seconds per direction switch.")]
    public float switchInterval = 0.6f;

    [Min(0f), Tooltip("Blink: seconds between blinks.")]
    public float blinkInterval = 3f;
    [Min(0f), Tooltip("Blink: fade-out/in duration.")]
    public float fadeTime = 0.2f;
    [Tooltip("Blink: teleport to mirrored X while invisible.")]
    public bool mirrorX = true;

    [Tooltip("Orbit: world center of the circle.")]
    public Vector2 center = new Vector2(0f, 3f);
    [Min(0f)] public float radius = 2.5f;
    [Tooltip("Orbit: degrees per second (negative = other way).")]
    public float angularSpeed = 40f;

    [Tooltip("Sprite Spin: degrees per second (0 = off). Target transform stays on the prefab.")]
    public float spinSpeed = 0f;

    [Header("Facing (sprite rotation)")]
    public EnemyFacingMode facingMode = EnemyFacingMode.None;
    [Min(0f), Tooltip("Turn rate in deg/s. Target transform stays on the prefab.")]
    public float facingSpeed = 360f;

    [Header("Shooting")]
    [Tooltip("Base direction of every pattern becomes player-aimed.")]
    public bool alwaysAimAtPlayer = false;

    [Tooltip("Skip emission while outside the camera viewport.")]
    public bool fireOnlyOnScreen = true;

    [Tooltip("Seed for Scatter angles and Wall gaps. 0 = time-based.")]
    public int randomSeed = 0;

    [Tooltip("Base patterns (deep-copied per spawned enemy).")]
    public EnemyPatternSlot[] patterns = new EnemyPatternSlot[0];

    [Range(0f, 1f), Tooltip("Swap to Phase 2 Patterns below this HP fraction. 0 = disabled.")]
    public float phase2Threshold = 0f;

    public EnemyPatternSlot[] phase2Patterns = new EnemyPatternSlot[0];

    [Header("Lifecycle")]
    [Tooltip("Never exits on lifetime; only removed on death (or HitAndRun's own exit, which infinite also suppresses).")]
    public bool infiniteLifetime = false;

    [Min(0f), Tooltip("Exit after this many seconds alive. 0 = never. Ignored when Infinite is on.")]
    public float lifetime = 20f;

    [Min(0f), Tooltip("Fly-up speed for the lifetime exit.")]
    public float lifetimeExitSpeed = 4f;
}
