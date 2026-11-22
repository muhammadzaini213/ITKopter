
using System;
using UnityEngine;

public enum EnemyMovementType
{
    Strafe,
    Dive,
    Hold,
    HitAndRun,
    Descend,
    SineWave,
    MoveToPoint,
    Zigzag,
    Blink,
    Orbit
}

public enum EnemyFacingMode
{
    None,
    RotateToPlayer,
    RotateToMove
}

public class EnemyMovement : MonoBehaviour
{
    [Header("Type")]
    [SerializeField] private EnemyMovementType movementType = EnemyMovementType.Hold;

    [Header("Common (Strafe / Dive / Hold / HitAndRun / Blink)")]
    [SerializeField, Tooltip("Absolute world Y where the enemy holds position.")]
    private float holdY = 4f;

    [Header("Strafe")]
    [SerializeField, Min(0f)] private float speed = 1.5f;
    [SerializeField, Min(0f)] private float rangeX = 3f;

    [Header("Dive")]
    [SerializeField, Min(0f)] private float idleTime = 2f;
    [SerializeField, Min(0f)] private float diveSpeed = 9f;
    [SerializeField, Min(0f)] private float retreatSpeed = 3f;

    [Header("Hit And Run")]
    [SerializeField, Min(0f)] private float enterSpeed = 4f;
    [SerializeField, Min(0f)] private float stayTime = 3f;
    [SerializeField, Min(0f)] private float exitSpeed = 4f;

    [Header("Descend / SineWave / Zigzag")]
    [SerializeField, Min(0f), Tooltip("Straight-down speed (Descend).")]
    private float descendSpeed = 0.8f;

    [SerializeField, Min(0f), Tooltip("Straight-down speed (SineWave, Zigzag).")]
    private float moveDownSpeed = 0.6f;

    [Header("SineWave")]
    [SerializeField, Min(0f)] private float amplitudeX = 2.5f;
    [SerializeField, Min(0f)] private float frequency = 0.5f;

    [Header("Move To Point")]
    [SerializeField] private Vector2 targetPoint = new Vector2(0f, 3f);
    [SerializeField, Min(0f)] private float moveToSpeed = 1f;

    [Header("Zigzag")]
    [SerializeField, Min(0f)] private float lateralSpeed = 4f;
    [SerializeField, Min(0.01f)] private float switchInterval = 0.6f;

    [Header("Blink")]
    [SerializeField, Min(0f)] private float blinkInterval = 3f;
    [SerializeField, Min(0f)] private float fadeTime = 0.2f;
    [SerializeField] private bool mirrorX = true;

    [Header("Orbit")]
    [SerializeField] private Vector2 center = new Vector2(0f, 3f);
    [SerializeField, Min(0f)] private float radius = 2.5f;
    [SerializeField] private float angularSpeed = 40f;

    [Header("Sprite Spin (optional, e.g. spiked star)")]
    [SerializeField] private Transform spinTarget;
    [SerializeField] private float spinSpeed; // deg/s

    [Header("Facing (rotates the sprite transform only, not this object)")]
    [SerializeField, Tooltip("None = keep default facing. Ignored while Sprite Spin is set.")]
    private EnemyFacingMode facingMode = EnemyFacingMode.None;

    [SerializeField, Tooltip("Transform that rotates. Auto = first SpriteRenderer found on a CHILD (assign explicitly for multi-sprite prefabs).")]
    private Transform facingTarget;

    [SerializeField, Min(0f), Tooltip("Turn speed toward the target angle (deg/s).")]
    private float facingSpeed = 360f;

    public event Action OnLeftScreen;

    public bool HasEntered { get; private set; }

    // Set by EnemyLifecycle when infiniteLifetime is on: HitAndRun's own
    // stay-exit is suppressed so the enemy only leaves via death.
    public bool SuppressOwnExit { get; set; }

    // True while blinked out — the shooter pauses emission during this.
    public bool SuppressFire { get; private set; }

    // ---- runtime state ----
    private Vector3 spawnPosition;
    private int strafeDir = 1;

    // Dive
    private enum DivePhase { ToHold, Idle, Dashing, Retreating }
    private DivePhase divePhase = DivePhase.ToHold;
    private float diveTimer;
    private Vector2 diveTarget;

    // HitAndRun
    private enum HnRPhase { ToHold, Staying }
    private HnRPhase hnrPhase = HnRPhase.ToHold;
    private float hnrTimer;

    // Blink
    private bool fading;
    private bool fadeOut = true;
    private float fadeTimer;
    private float blinkTimer;
    private float baseAlpha = 1f;
    private SpriteRenderer[] fadeRenderers;

    // Orbit
    private float orbitAngle;
    private bool orbitOnCircle;
    private float orbitLinearSpeed;

    // Zigzag
    private float zigzagTimer;
    private int zigzagDir = 1;

    // SineWave
    private float sineTimer;

    // Exit
    private bool exiting;
    private bool finished;
    private float exitMoveSpeed = 4f;

    // Facing
    private float currentFacingAngle;
    private Vector2 lastMoveDirection = Vector2.down;
    private Vector3 previousPosition;

    private bool stopped;

    private void Awake()
    {
        spawnPosition = transform.position;
        previousPosition = transform.position;

        ResolveFacingTarget();

        orbitLinearSpeed = Mathf.Max(0.01f, radius) *
                           angularSpeed * Mathf.Deg2Rad;

        Vector2 fromCenter = (Vector2)spawnPosition - center;
        // Down-rotation convention matches DirFromAngle so the initial
        // approach target is the nearest point on the circle.
        orbitAngle = fromCenter.sqrMagnitude > 0.0001f
            ? Vector2.SignedAngle(Vector2.down, fromCenter)
            : 0f;

        if (movementType == EnemyMovementType.Blink)
        {
            fadeRenderers = GetComponentsInChildren<SpriteRenderer>(true);

            if (fadeRenderers.Length > 0)
                baseAlpha = fadeRenderers[0].color.a;
        }

        if (movementType == EnemyMovementType.Zigzag)
            zigzagTimer = switchInterval;

        if (movementType == EnemyMovementType.Blink)
            blinkTimer = blinkInterval;
    }

    // Stops all behavior (used on death so corpses do not keep moving).
    public void StopMoving() => stopped = true;

    // Begins the generic fly-up exit. HitAndRun calls this itself after
    // its stay phase; EnemyLifecycle calls it when lifetime expires.
    public void BeginExit(float speed)
    {
        if (exiting || finished)
            return;

        exiting = true;
        exitMoveSpeed = speed > 0f ? speed : 4f;
        SuppressFire = true;
        fading = false;
        RestoreAlpha();
    }

    private void Update()
    {
        if (stopped || finished)
            return;

        Vector3 positionBefore = transform.position;

        // Sprite Spin is decorative; Facing wins when both are configured.
        if (spinTarget != null && spinSpeed != 0f &&
            facingMode == EnemyFacingMode.None)
        {
            spinTarget.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        }

        if (exiting)
        {
            ExitTick();
        }
        else
        {
            switch (movementType)
            {
                case EnemyMovementType.Strafe: TickStrafe(); break;
                case EnemyMovementType.Dive: TickDive(); break;
                case EnemyMovementType.Hold: TickDescendToHold(enterSpeed); break;
                case EnemyMovementType.HitAndRun: TickHitAndRun(); break;
                case EnemyMovementType.Descend: TickDescend(); break;
                case EnemyMovementType.SineWave: TickSineWave(); break;
                case EnemyMovementType.MoveToPoint: TickMoveToPoint(); break;
                case EnemyMovementType.Zigzag: TickZigzag(); break;
                case EnemyMovementType.Blink: TickBlink(); break;
                case EnemyMovementType.Orbit: TickOrbit(); break;
            }
        }

        UpdateMoveDirection(positionBefore);
        UpdateFacing();
    }

    // ---- facing ----

    // Prefer a SpriteRenderer on a child so the root object itself never
    // rotates (spec: rotate the sprite, not the whole object).
    private void ResolveFacingTarget()
    {
        if (facingTarget != null)
            return;

        SpriteRenderer[] renderers =
            GetComponentsInChildren<SpriteRenderer>(true);

        foreach (var r in renderers)
        {
            if (r.transform != transform)
            {
                facingTarget = r.transform;
                return;
            }
        }

        // Renderer sits on the root — rotating it is the only option.
        if (renderers.Length > 0)
            facingTarget = renderers[0].transform;
    }

    private void UpdateMoveDirection(Vector3 positionBefore)
    {
        Vector3 delta = transform.position - positionBefore;

        if (delta.sqrMagnitude > 0.0000001f)
            lastMoveDirection = delta.normalized;
    }

    private void UpdateFacing()
    {
        if (facingMode == EnemyFacingMode.None || facingTarget == null)
            return;

        Vector2 dir;

        if (facingMode == EnemyFacingMode.RotateToPlayer)
        {
            dir = EnemyTarget.DirectionFrom(
                transform.position, lastMoveDirection);
        }
        else
        {
            dir = lastMoveDirection;
        }

        float targetAngle = Vector2.SignedAngle(Vector2.down, dir);

        currentFacingAngle = Mathf.MoveTowardsAngle(
            currentFacingAngle, targetAngle, facingSpeed * Time.deltaTime);

        facingTarget.rotation = Quaternion.Euler(0f, 0f, currentFacingAngle);
    }

    // ---- helpers ----

    // Moves straight down to holdY at the given speed. speed <= 0 teleports
    // (used by Hold where only the final position matters). Returns true on arrival.
    private bool MoveDownToHold(float moveSpeed)
    {
        Vector3 p = transform.position;

        if (p.y <= holdY)
        {
            p.y = holdY;
            transform.position = p;
            return true;
        }

        if (moveSpeed <= 0f)
        {
            p.y = holdY;
            transform.position = p;
            return true;
        }

        p.y = Mathf.MoveTowards(p.y, holdY, moveSpeed * Time.deltaTime);
        transform.position = p;
        return p.y <= holdY;
    }

    private Vector2 DirFromAngle(float degrees)
    {
        return (Vector2)(Quaternion.Euler(0f, 0f, degrees) * Vector3.down);
    }

    // Shared "descend to holdY then stay" behavior (Hold).
    private void TickDescendToHold(float enterMoveSpeed)
    {
        if (MoveDownToHold(enterMoveSpeed))
            HasEntered = true;
    }

    private void ExitTick()
    {
        Vector3 p = transform.position;
        p.y += exitMoveSpeed * Time.deltaTime;
        transform.position = p;

        if (p.y >= spawnPosition.y)
        {
            finished = true;
            exiting = false;
            OnLeftScreen?.Invoke();
        }
    }

    // ---- types ----

    private void TickStrafe()
    {
        if (!HasEntered)
        {
            HasEntered = MoveDownToHold(speed);
            return;
        }

        Vector3 p = transform.position;
        float minX = spawnPosition.x - rangeX;
        float maxX = spawnPosition.x + rangeX;

        p.x += strafeDir * speed * Time.deltaTime;

        if (p.x <= minX) { p.x = minX; strafeDir = 1; }
        else if (p.x >= maxX) { p.x = maxX; strafeDir = -1; }

        transform.position = p;
    }

    private void TickDive()
    {
        Vector3 p = transform.position;

        switch (divePhase)
        {
            case DivePhase.ToHold:
                if (MoveDownToHold(speed))
                {
                    HasEntered = true;
                    divePhase = DivePhase.Idle;
                    diveTimer = 0f;
                }
                break;

            case DivePhase.Idle:
                diveTimer += Time.deltaTime;
                if (diveTimer >= idleTime)
                {
                    // Lock the player position at dive start.
                    diveTarget = EnemyTarget.Position(new Vector2(p.x, 0f));
                    divePhase = DivePhase.Dashing;
                }
                break;

            case DivePhase.Dashing:
                Vector3 dashTarget = diveTarget;
                transform.position = Vector3.MoveTowards(
                    p, dashTarget, diveSpeed * Time.deltaTime);

                // Reached (or overshot) the locked point -> retreat.
                if ((dashTarget - transform.position).sqrMagnitude <= 0.01f ||
                    (dashTarget - p).magnitude <= diveSpeed * Time.deltaTime)
                {
                    divePhase = DivePhase.Retreating;
                }
                break;

            case DivePhase.Retreating:
                Vector3 holdPoint = new Vector3(spawnPosition.x, holdY, spawnPosition.z);
                transform.position = Vector3.MoveTowards(
                    p, holdPoint, retreatSpeed * Time.deltaTime);

                if ((holdPoint - transform.position).sqrMagnitude <= 0.001f)
                {
                    divePhase = DivePhase.Idle;
                    diveTimer = 0f;
                }
                break;
        }
    }

    private void TickHitAndRun()
    {
        switch (hnrPhase)
        {
            case HnRPhase.ToHold:
                if (MoveDownToHold(enterSpeed))
                {
                    HasEntered = true;
                    hnrPhase = HnRPhase.Staying;
                    hnrTimer = 0f;
                }
                break;

            case HnRPhase.Staying:
                hnrTimer += Time.deltaTime;
                if (hnrTimer >= stayTime && !SuppressOwnExit)
                    BeginExit(exitSpeed);
                break;
        }
    }

    private void TickDescend()
    {
        Vector3 p = transform.position;
        p.y -= descendSpeed * Time.deltaTime;
        transform.position = p;

        if (!HasEntered && p.y < spawnPosition.y)
            HasEntered = true;
    }

    private void TickSineWave()
    {
        sineTimer += Time.deltaTime;

        Vector3 p = transform.position;
        p.y -= moveDownSpeed * Time.deltaTime;
        p.x = spawnPosition.x +
              Mathf.Sin(sineTimer * frequency * 2f * Mathf.PI) * amplitudeX;
        transform.position = p;

        if (!HasEntered && p.y < spawnPosition.y)
            HasEntered = true;
    }

    private void TickMoveToPoint()
    {
        Vector3 p = transform.position;
        p = Vector3.MoveTowards(
            p, (Vector3)targetPoint, moveToSpeed * Time.deltaTime);
        transform.position = p;

        if (!HasEntered &&
            ((Vector2)p - targetPoint).sqrMagnitude <= 0.001f)
        {
            HasEntered = true;
        }
    }

    private void TickZigzag()
    {
        zigzagTimer += Time.deltaTime;
        if (zigzagTimer >= switchInterval)
        {
            zigzagTimer = 0f;
            zigzagDir = -zigzagDir;
        }

        Vector3 p = transform.position;
        p.y -= moveDownSpeed * Time.deltaTime;
        p.x += zigzagDir * lateralSpeed * Time.deltaTime;
        transform.position = p;

        if (!HasEntered && p.y < spawnPosition.y)
            HasEntered = true;
    }

    private void TickBlink()
    {
        if (fading)
        {
            TickFade();
            return;
        }

        // Enter first: descend to holdY, then start blinking.
        if (!HasEntered)
        {
            HasEntered = MoveDownToHold(speed);
            return;
        }

        blinkTimer += Time.deltaTime;
        if (blinkTimer >= blinkInterval)
        {
            blinkTimer = 0f;
            StartFade();
        }
    }

    private void StartFade()
    {
        fading = true;
        fadeOut = true;
        fadeTimer = 0f;
        SuppressFire = true;
    }

    private void TickFade()
    {
        fadeTimer += Time.deltaTime;
        float t = fadeTime > 0f ? Mathf.Clamp01(fadeTimer / fadeTime) : 1f;

        if (fadeOut)
        {
            SetAlpha(Mathf.Lerp(baseAlpha, 0f, t));

            if (t >= 1f)
            {
                // Teleport while invisible.
                Vector3 p = transform.position;
                if (mirrorX)
                    p.x = -p.x;
                transform.position = p;

                fadeOut = false;
                fadeTimer = 0f;
            }
        }
        else
        {
            SetAlpha(Mathf.Lerp(0f, baseAlpha, t));

            if (t >= 1f)
            {
                fading = false;
                SuppressFire = false; // fire again right after fading back in
            }
        }
    }

    private void SetAlpha(float a)
    {
        if (fadeRenderers == null)
            return;

        for (int i = 0; i < fadeRenderers.Length; i++)
        {
            if (fadeRenderers[i] == null)
                continue;

            Color c = fadeRenderers[i].color;
            c.a = a;
            fadeRenderers[i].color = c;
        }
    }

    private void RestoreAlpha()
    {
        if (!fading)
            return;

        SetAlpha(baseAlpha);
        fading = false;
        SuppressFire = false;
    }

    private void TickOrbit()
    {
        Vector3 p = transform.position;

        if (!orbitOnCircle)
        {
            Vector2 onCircle =
                center + DirFromAngle(orbitAngle) * radius;
            p = Vector3.MoveTowards(
                p, (Vector3)onCircle, orbitLinearSpeed * Time.deltaTime);
            transform.position = p;

            if (((Vector2)p - onCircle).sqrMagnitude <= 0.001f)
            {
                orbitOnCircle = true;
                HasEntered = true;
            }

            return;
        }

        orbitAngle += angularSpeed * Time.deltaTime;
        transform.position =
            center + DirFromAngle(orbitAngle) * radius;
    }
}
