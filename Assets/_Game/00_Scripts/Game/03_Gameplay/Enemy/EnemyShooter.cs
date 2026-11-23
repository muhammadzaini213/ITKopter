
using UnityEngine;

public enum EnemyShotPattern
{
    Aimed,
    Burst,
    Spiral,
    Ring,
    Wall,
    Fan,
    Heavy,
    SineBullet,
    DoubleAimed,
    Scatter
}

[System.Serializable]
public class EnemyPatternSlot
{
    public bool enabled = true;
    public EnemyShotPattern pattern = EnemyShotPattern.Aimed;

    [Header("Timing")]
    [Tooltip("Delay before the first shot of this slot.")]
    public float startDelay = 0f;

    [Tooltip("Seconds between shots (Aimed, Spiral, Ring-gap uses Repeat Delay, Wall, Fan, Heavy, SineBullet, Scatter).")]
    public float interval = 1.5f;

    [Tooltip("Pause after a full Burst or Ring cycle (Burst, Ring).")]
    public float cooldown = 2f;

    [Tooltip("Seconds between shots inside a Burst (Burst).")]
    public float burstDelay = 0.12f;

    [Tooltip("Rings fired per Ring cycle before the cooldown (Ring).")]
    public int repeat = 2;

    [Tooltip("Seconds between rings within a cycle (Ring).")]
    public float repeatDelay = 1.2f;

    [Header("Shot")]
    public int count = 1;

    [Tooltip("Total spread in degrees between bullets (Aimed).")]
    public float spread = 0f;

    [Tooltip("Total arc in degrees (Fan, Heavy).")]
    public float arc = 60f;

    [Tooltip("Center the arc on the player instead of straight down (Fan).")]
    public bool aimed = false;

    [Tooltip("Parallel streams rotating together (Spiral).")]
    public int arms = 1;

    [Tooltip("Degrees the spiral rotates after each shot (Spiral).")]
    public float angleStep = 12f;

    public float speed = 5f;

    [Tooltip("Random speed range (Scatter).")]
    public float speedMin = 2f;
    public float speedMax = 5f;

    [Tooltip("Bullet scale (Heavy).")]
    public float scale = 1f;

    [Tooltip("Horizontal gap between bullets (Wall, SineBullet count offsets).")]
    public float spacingX = 0.8f;

    [Tooltip("Leave one random gap in the row (Wall).")]
    public bool randomGap = false;

    [Tooltip("Muzzle offset from center, mirrored both sides (DoubleAimed).")]
    public float offsetX = 1f;

    [Tooltip("Lateral oscillation size (SineBullet).")]
    public float amplitude = 0.6f;

    [Tooltip("Lateral oscillations per second (SineBullet).")]
    public float frequency = 2f;

    [Tooltip("Random angle cone in degrees around straight down (Scatter).")]
    public float cone = 120f;

    // ---- runtime state (not serialized) ----
    [System.NonSerialized] public float timer;
    [System.NonSerialized] public float angleAccum;
    [System.NonSerialized] public int cycleIndex;
    [System.NonSerialized] public bool bursting;

    // Shallow copy is enough (value fields only) — used so EnemyData assets
    // are never mutated: every spawned enemy gets its own slot instances.
    public EnemyPatternSlot Clone() => (EnemyPatternSlot)MemberwiseClone();
}

public class EnemyShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyPatternSpawner spawner;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private Health health;

    [Header("Firing")]
    [SerializeField, Tooltip("All patterns fire toward the player (base direction becomes aimed). Inherently aimed patterns are unaffected.")]
    private bool alwaysAimAtPlayer = false;

    [SerializeField, Tooltip("Skip emission while the enemy is outside the camera viewport.")]
    private bool fireOnlyOnScreen = true;

    [SerializeField, Tooltip("Seed for Scatter angles and Wall gaps. 0 = time-based.")]
    private int randomSeed = 0;

    [Header("Patterns")]
    [SerializeField] private EnemyPatternSlot[] patterns =
        new EnemyPatternSlot[0];

    [Header("Phase 2 (0 = disabled)")]
    [SerializeField, Range(0f, 1f), Tooltip("Swap to Phase 2 Patterns below this HP fraction.")]
    private float phase2Threshold = 0f;

    [SerializeField] private EnemyPatternSlot[] phase2Patterns =
        new EnemyPatternSlot[0];

    private EnemyPatternSlot[] activePatterns;
    private Camera cachedCamera;
    private bool phase2Active;
    private bool stopped;
    private System.Random rng;

    private void Awake()
    {
        if (spawner == null)
            spawner = GetComponentInChildren<EnemyPatternSpawner>(true);

        if (movement == null)
            movement = GetComponent<EnemyMovement>();

        if (health == null)
            health = GetComponent<Health>();

        rng = new System.Random(
            randomSeed != 0 ? randomSeed : GetInstanceID());

        activePatterns = patterns;
        ResetSlots(patterns);
        ResetSlots(phase2Patterns);

        cachedCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (health != null && phase2Threshold > 0f)
            health.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        if (health != null && phase2Threshold > 0f)
            health.OnHealthChanged -= HandleHealthChanged;
    }

    // Cancels every pending timer/burst (death, despawn — spec 5.4).
    public void StopShooting() => stopped = true;

    // Data-only apply from EnemyDataBinder (runs before this Awake, which
    // then derives rng/active patterns from the applied values).
    // Patterns are deep-copied: living enemies must not share slot state
    // through a common EnemyData asset.
    public void ApplyData(EnemyData data)
    {
        alwaysAimAtPlayer = data.alwaysAimAtPlayer;
        fireOnlyOnScreen = data.fireOnlyOnScreen;
        randomSeed = data.randomSeed;
        patterns = CloneSlots(data.patterns);
        phase2Threshold = data.phase2Threshold;
        phase2Patterns = CloneSlots(data.phase2Patterns);
    }

    private static EnemyPatternSlot[] CloneSlots(EnemyPatternSlot[] source)
    {
        if (source == null)
            return new EnemyPatternSlot[0];

        EnemyPatternSlot[] clone = new EnemyPatternSlot[source.Length];

        for (int i = 0; i < source.Length; i++)
            clone[i] = source[i] != null ? source[i].Clone() : null;

        return clone;
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (phase2Active || max <= 0f)
            return;

        if (current / max > phase2Threshold)
            return;

        if (phase2Patterns == null || phase2Patterns.Length == 0)
            return;

        phase2Active = true;
        activePatterns = phase2Patterns;
        ResetSlots(activePatterns);
    }

    private static void ResetSlots(EnemyPatternSlot[] slots)
    {
        if (slots == null)
            return;

        foreach (var slot in slots)
        {
            if (slot == null)
                continue;

            slot.timer = slot.startDelay;
            slot.angleAccum = 0f;
            slot.cycleIndex = 0;
            slot.bursting = false;
        }
    }

    private void Update()
    {
        if (stopped || spawner == null || !spawner.CanSpawn)
            return;

        if (activePatterns == null)
            return;

        float dt = Time.deltaTime;
        bool canEmit = IsAllowedToFire();

        foreach (var slot in activePatterns)
        {
            if (slot == null || !slot.enabled)
                continue;

            TickSlot(slot, dt, canEmit);
        }
    }

    private bool IsAllowedToFire()
    {
        if (movement != null && movement.SuppressFire)
            return false;

        if (!fireOnlyOnScreen)
            return true;

        if (cachedCamera == null)
            cachedCamera = Camera.main;

        if (cachedCamera == null)
            return true;

        Vector3 vp = cachedCamera.WorldToViewportPoint(transform.position);
        return vp.x >= 0f && vp.x <= 1f &&
               vp.y >= 0f && vp.y <= 1f;
    }

    // Blocked emission waits with timer pinned at 0 so exactly one
    // catch-up shot fires the moment the enemy becomes visible again.
    private bool TryFire(EnemyPatternSlot slot, float nextDelay)
    {
        if (slot.timer > 0f)
            return false;

        if (!IsAllowedToFire())
        {
            slot.timer = 0f;
            return false;
        }

        FirePattern(slot);
        spawner.PlayFireSFX();
        slot.timer += Mathf.Max(0.01f, nextDelay);
        return true;
    }

    private void TickSlot(EnemyPatternSlot slot, float dt, bool canEmit)
    {
        slot.timer -= dt;

        switch (slot.pattern)
        {
            case EnemyShotPattern.Burst:
                TickBurst(slot, canEmit);
                break;

            case EnemyShotPattern.Ring:
                TickRing(slot, canEmit);
                break;

            case EnemyShotPattern.Aimed:
            case EnemyShotPattern.Spiral:
            case EnemyShotPattern.Wall:
            case EnemyShotPattern.Fan:
            case EnemyShotPattern.Heavy:
            case EnemyShotPattern.SineBullet:
            case EnemyShotPattern.DoubleAimed:
            case EnemyShotPattern.Scatter:
                // canEmit is re-checked inside TryFire.
                TryFire(slot, slot.interval);
                break;
        }
    }

    // Burst: shot, burstDelay, ... count shots, then cooldown.
    // A blocked shot holds its position until the enemy is visible again.
    private void TickBurst(EnemyPatternSlot slot, bool canEmit)
    {
        if (!slot.bursting)
        {
            if (slot.timer > 0f)
                return;

            if (!canEmit)
            {
                slot.timer = 0f;
                return;
            }

            slot.bursting = true;
            slot.cycleIndex = 1;
            FirePattern(slot);
            spawner.PlayFireSFX();
            slot.timer = slot.burstDelay;
            return;
        }

        if (slot.timer > 0f)
            return;

        if (slot.cycleIndex >= Mathf.Max(1, slot.count))
        {
            slot.bursting = false;
            slot.cycleIndex = 0;
            slot.timer = slot.cooldown;
            return;
        }

        if (!canEmit)
        {
            slot.timer = 0f; // retry next tick while blocked
            return;
        }

        FirePattern(slot);
        spawner.PlayFireSFX();
        slot.cycleIndex++;
        slot.timer = slot.burstDelay;
    }

    // Ring: fire `repeat` rings spaced by repeatDelay, then cooldown.
    private void TickRing(EnemyPatternSlot slot, bool canEmit)
    {
        if (slot.timer > 0f)
            return;

        if (!canEmit)
        {
            slot.timer = 0f;
            return;
        }

        FirePattern(slot);
        spawner.PlayFireSFX();
        slot.cycleIndex++;

        if (slot.cycleIndex >= Mathf.Max(1, slot.repeat))
        {
            slot.cycleIndex = 0;
            slot.timer = slot.cooldown;
        }
        else
        {
            slot.timer = slot.repeatDelay;
        }
    }

    // ---- pattern implementations ----
    // Angle convention: 0 degrees = straight down (sprite default facing).

    // Base angle for a pattern: player-aimed when Always Aim is on, else 0.
    private float GetAimedAngle(Vector2 origin)
    {
        Vector2 dir = EnemyTarget.DirectionFrom(origin, Vector2.down);
        return Vector2.SignedAngle(Vector2.down, dir);
    }

    private float GetBaseAngle(Vector2 origin)
    {
        return alwaysAimAtPlayer ? GetAimedAngle(origin) : 0f;
    }

    private void FirePattern(EnemyPatternSlot slot)
    {
        Vector2 origin = transform.position;
        float baseAngle = GetBaseAngle(origin);

        switch (slot.pattern)
        {
            case EnemyShotPattern.Aimed:
                FireAimed(origin, slot);
                break;

            // Burst: one bullet straight down per tick — the trail forms
            // the line as earlier shots move away.
            case EnemyShotPattern.Burst:
                spawner.Spawn(origin, RotateDown(baseAngle), slot.speed);
                break;

            case EnemyShotPattern.Wall:
                FireWall(origin, slot, baseAngle);
                break;

            case EnemyShotPattern.Spiral:
                FireSpiral(origin, slot, baseAngle);
                break;

            case EnemyShotPattern.Ring:
                FireRing(origin, slot, baseAngle);
                break;

            case EnemyShotPattern.Fan:
                FireArc(origin, slot, baseAngle, slot.aimed, slot.speed, slot.scale);
                break;

            case EnemyShotPattern.Heavy:
                FireArc(origin, slot, baseAngle, slot.aimed, slot.speed, slot.scale);
                break;

            case EnemyShotPattern.SineBullet:
                FireSineBullet(origin, slot, baseAngle);
                break;

            case EnemyShotPattern.DoubleAimed:
                FireDoubleAimed(origin, slot);
                break;

            case EnemyShotPattern.Scatter:
                FireScatter(origin, slot, baseAngle);
                break;
        }
    }

    private static Vector2 RotateDown(float degrees)
    {
        return (Vector2)(Quaternion.Euler(0f, 0f, degrees) * Vector3.down);
    }

    private void FireAimed(Vector2 origin, EnemyPatternSlot slot)
    {
        Vector2 dir = EnemyTarget.DirectionFrom(origin, Vector2.down);
        float baseAngle = Vector2.SignedAngle(Vector2.down, dir);
        int count = Mathf.Max(1, slot.count);

        for (int i = 0; i < count; i++)
        {
            float offset = count == 1
                ? 0f
                : -slot.spread * 0.5f +
                  slot.spread * i / (count - 1);

            spawner.Spawn(origin, RotateDown(baseAngle + offset), slot.speed);
        }
    }

    private void FireWall(Vector2 origin, EnemyPatternSlot slot, float baseAngle)
    {
        int count = Mathf.Max(1, slot.count);
        int gap = slot.randomGap && count > 1
            ? NextRandom(0, count)
            : -1;

        Vector2 dir = RotateDown(baseAngle);
        // Row runs perpendicular to the travel direction.
        Vector2 perp = (Vector2)(Quaternion.Euler(0f, 0f, baseAngle) * Vector3.right);

        for (int i = 0; i < count; i++)
        {
            if (i == gap)
                continue;

            float offset = (i - (count - 1) * 0.5f) * slot.spacingX;

            spawner.Spawn(origin + perp * offset, dir, slot.speed);
        }
    }

    private void FireSpiral(Vector2 origin, EnemyPatternSlot slot, float baseAngle)
    {
        slot.angleAccum += slot.angleStep;
        int arms = Mathf.Max(1, slot.arms);
        float armStep = 360f / arms;

        for (int arm = 0; arm < arms; arm++)
        {
            spawner.Spawn(
                origin,
                RotateDown(baseAngle + slot.angleAccum + arm * armStep),
                slot.speed);
        }
    }

    private void FireRing(Vector2 origin, EnemyPatternSlot slot, float baseAngle)
    {
        int count = Mathf.Max(1, slot.count);
        float step = 360f / count;

        for (int i = 0; i < count; i++)
            spawner.Spawn(origin, RotateDown(baseAngle + i * step), slot.speed);
    }

    private void FireArc(
        Vector2 origin, EnemyPatternSlot slot, float baseAngle,
        bool centeredOnPlayer, float speed, float scale)
    {
        if (centeredOnPlayer)
            baseAngle = GetAimedAngle(origin);

        int count = Mathf.Max(1, slot.count);

        for (int i = 0; i < count; i++)
        {
            float offset = count == 1
                ? 0f
                : -slot.arc * 0.5f + slot.arc * i / (count - 1);

            spawner.Spawn(
                origin, RotateDown(baseAngle + offset), speed, scale);
        }
    }

    private void FireSineBullet(Vector2 origin, EnemyPatternSlot slot, float baseAngle)
    {
        int count = Mathf.Max(1, slot.count);
        Vector2 dir = RotateDown(baseAngle);
        Vector2 perp = (Vector2)(Quaternion.Euler(0f, 0f, baseAngle) * Vector3.right);

        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) * 0.5f) * slot.spacingX;

            spawner.Spawn(
                origin + perp * offset,
                dir,
                slot.speed,
                scale: 1f,
                sineAmplitude: slot.amplitude,
                sineFrequency: slot.frequency);
        }
    }

    private void FireDoubleAimed(Vector2 origin, EnemyPatternSlot slot)
    {
        Vector2 muzzleL = origin + Vector2.left * slot.offsetX;
        Vector2 muzzleR = origin + Vector2.right * slot.offsetX;

        Vector2 dirL = EnemyTarget.DirectionFrom(muzzleL, Vector2.down);
        Vector2 dirR = EnemyTarget.DirectionFrom(muzzleR, Vector2.down);

        spawner.Spawn(muzzleL, dirL, slot.speed);
        spawner.Spawn(muzzleR, dirR, slot.speed);
    }

    private void FireScatter(Vector2 origin, EnemyPatternSlot slot, float baseAngle)
    {
        int count = Mathf.Max(1, slot.count);
        float halfCone = slot.cone * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float angle = NextRange(-halfCone, halfCone);
            float speed = NextRange(slot.speedMin, slot.speedMax);

            spawner.Spawn(origin, RotateDown(baseAngle + angle), speed);
        }
    }

    // ---- single seedable randomness source (spec section 7) ----

    private int NextRandom(int minInclusive, int maxExclusive)
        => rng.Next(minInclusive, maxExclusive);

    private float NextRange(float min, float max)
        => min + (float)rng.NextDouble() * (max - min);
}
