
using UnityEngine;

public class PlayerAutoShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BulletSpawner bulletSpawner;

    [Header("Firing Settings")]
    [SerializeField, Min(0.01f)] private float fireInterval = 0.2f;

    private Transform firePoint;
    private CharacterSwitcher characterSwitcher;
    private Character character;
    private Health characterHealth;
    private float fireTimer;
    private bool hasLoggedMissingReference;

    /// <summary>
    /// Extra multiplier on the fire interval (&lt; 1 = faster). EX skills use
    /// Character.SetFireIntervalModifier instead; both stack.
    /// </summary>
    public float FireIntervalMultiplier { get; set; } = 1f;

    public float EffectiveFireInterval =>
        fireInterval *
        Mathf.Max(0.05f, FireIntervalMultiplier) *
        (character != null ? character.FireIntervalMultiplier : 1f);

    private void Awake()
    {
        character = GetComponentInParent<Character>();
        characterHealth = character != null ? character.GetComponent<Health>() : null;

        Firepoint point = GetComponentInChildren<Firepoint>();
        firePoint = point != null ? point.transform : null;
        if (character == null)
        {
            Debug.LogError(
                "[PlayerAutoShooter] Character component is missing.",
                this);
        }

        if (firePoint == null)
        {
            Debug.LogError(
                "[PlayerAutoShooter] Fire Point is not assigned.",
                this);
        }

        if (bulletSpawner == null)
        {
            Debug.LogError(
                "[PlayerAutoShooter] Bullet Spawner is not assigned.",
                this);
        }

        characterSwitcher = Object.FindFirstObjectByType<CharacterSwitcher>();

        if (characterSwitcher == null)
        {
            Debug.LogError(
                "[PlayerAutoShooter] CharacterSwitcher is not found in the scene.",
                this);
        }

        if (bulletSpawner != null)
            bulletSpawner.OwnerCharacter = character;
    }

    private void Update()
    {
        if (!CanShoot())
        {
            fireTimer = 0f;
            return;
        }

        fireTimer += Time.deltaTime;

        // Interval multiplier: 0.5 = twice the fire rate. (It used to divide,
        // which inverted every EX skill's haste/slow.)
        float effectiveInterval = Mathf.Max(0.02f, EffectiveFireInterval);

        if (fireTimer < effectiveInterval)
            return;

        // Subtract the interval actually used; clamp so a long hitch can't
        // queue a burst of shots.
        fireTimer = Mathf.Min(fireTimer - effectiveInterval, effectiveInterval);

        bulletSpawner.Fire(
            firePoint,
            character.BulletSpeed,
            character.EffectiveAttackPower
        );
    }

    private bool CanShoot()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            return false;

        if (character == null || firePoint == null || bulletSpawner == null)
            return false;

        if (characterSwitcher == null)
        {
            if (!hasLoggedMissingReference)
            {
                Debug.LogError(
                    "[PlayerAutoShooter] CharacterSwitcher is not assigned.",
                    this);

                hasLoggedMissingReference = true;
            }

            return false;
        }

        // Health sits on the character root, not on this child.
        if (characterHealth != null && characterHealth.IsDead)
            return false;

        // During the switching window, every active character can shoot.
        if (characterSwitcher.IsSwitching)
            return true;

        // Outside the switching window, only the selected character can shoot.
        if (characterSwitcher.ActiveCharacter != character.gameObject)
            return false;


        return true;
    }

    private void OnDisable()
    {
        fireTimer = 0f;
    }
}
