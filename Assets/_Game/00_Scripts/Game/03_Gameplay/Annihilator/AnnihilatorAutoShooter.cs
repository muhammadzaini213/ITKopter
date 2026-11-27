using UnityEngine;

public class AnnihilatorAutoShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BulletSpawner bulletSpawner;
    [SerializeField] private Firepoint firePoint;

    [Header("Firing Settings")]
    [SerializeField, Min(0.01f)] private float fireInterval = 0.2f;
    [SerializeField, Min(0f)] private float bulletSpeed = 10f;

    [Header("Aim Sweep")]
    [SerializeField, Min(0f), Tooltip("Degrees the aim swings either side of AimAngle, so the gaps between cannons get hit too.")]
    private float sweepAmplitude = 9f;

    [SerializeField, Min(0f), Tooltip("Sweep speed (radians per second).")]
    private float sweepSpeed = 2.4f;

    private float fireTimer;
    private float sweepTime;
    private bool hasLoggedMissingReference;
    private Quaternion baseAimRotation = Quaternion.identity;
    private bool baseAimCaptured;

    /// <summary>Damage per bullet set by AnnihilatorManifestation (0 = spawner default).</summary>
    public float DamageOverride { get; set; }

    /// <summary>Hierarchy of this cannon's bullets (the player's Annihilator choice). Null = neutral.</summary>
    public CharacterType? AttackerType
    {
        get => bulletSpawner != null ? bulletSpawner.AttackerTypeOverride : null;
        set { if (bulletSpawner != null) bulletSpawner.AttackerTypeOverride = value; }
    }

    /// <summary>Fire interval multiplier (&lt; 1 = faster), from the player's level.</summary>
    public float IntervalMultiplier { get; set; } = 1f;

    /// <summary>Fan angle in degrees (+ = left), set by AnnihilatorManifestation.</summary>
    public float AimAngle { get; set; }

    /// <summary>Sweep phase offset so neighbouring cannons don't swing in sync.</summary>
    public float SweepPhase { get; set; }

    private void Awake()
    {
        if (firePoint == null)
            firePoint = GetComponentInChildren<Firepoint>();

        if (firePoint == null)
        {
            Debug.LogError(
                "[AnnihilatorAutoShooter] Firepoint is missing.",
                this);
        }
        else
        {
            baseAimRotation = firePoint.transform.localRotation;
            baseAimCaptured = true;
        }

        if (bulletSpawner == null)
        {
            Debug.LogError(
                "[AnnihilatorAutoShooter] BulletSpawner is not assigned.",
                this);
        }
    }

    private void Update()
    {
        if (!CanShoot())
            return;

        fireTimer += Time.deltaTime;
        sweepTime += Time.deltaTime;

        float interval = Mathf.Max(0.03f, fireInterval * Mathf.Max(0.1f, IntervalMultiplier));

        if (fireTimer < interval)
            return;

        fireTimer = Mathf.Min(fireTimer - interval, interval);

        float angle = AimAngle + Mathf.Sin(sweepTime * sweepSpeed + SweepPhase) * sweepAmplitude;
        firePoint.transform.localRotation = baseAimRotation * Quaternion.Euler(0f, 0f, angle);

        bulletSpawner.Fire(
            firePoint.transform,
            bulletSpeed,
            DamageOverride
        );
    }

    private bool CanShoot()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return false;

        if (firePoint == null || bulletSpawner == null)
        {
            if (!hasLoggedMissingReference)
            {
                Debug.LogError(
                    "[AnnihilatorAutoShooter] Required references are missing.",
                    this);

                hasLoggedMissingReference = true;
            }

            return false;
        }

        return true;
    }

    private void OnDisable()
    {
        fireTimer = 0f;
        sweepTime = 0f;

        if (baseAimCaptured && firePoint != null)
            firePoint.transform.localRotation = baseAimRotation;
    }
}
