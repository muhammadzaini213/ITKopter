
using System.Collections;
using UnityEngine;

public class BurstExSkill : ExSkill
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private BulletSpawner bulletSpawner;
    [SerializeField] private PlayerAutoShooter normalAutoShooter;
    private CharacterSwitcher characterSwitcher;

    [Header("Triple Shot Settings")]
    [SerializeField, Min(0f)] private float spacing = 0.3f;

    [Header("Duration Settings")]
    [SerializeField, Min(0.01f)] private float duration = 3f;

    private Coroutine durationRoutine;
    private bool isActive;
    private bool previousNormalShooterState;

    public bool IsActive => isActive;

    private void Awake()
    {
        if (normalAutoShooter == null && Owner != null)
        {
            Transform normalTransform = Owner.transform.Find("Normal");

            if (normalTransform != null)
            {
                normalAutoShooter =
                    normalTransform.GetComponentInChildren<PlayerAutoShooter>(true);
            }
        }

        if (characterSwitcher == null)
        {
            characterSwitcher =
                Object.FindFirstObjectByType<CharacterSwitcher>();
        }
    }


    public override bool CanUse()
    {
        bool baseCanUse = base.CanUse();

        if (!baseCanUse)
        {
            Debug.LogWarning(
                "[BurstExSkill] Base CanUse failed. Is Owner initialized and active?",
                this
            );
            return false;
        }

        if (firePoint == null)
            Debug.LogError("[BurstExSkill] Fire Point is missing.", this);

        if (bulletSpawner == null)
            Debug.LogError("[BurstExSkill] Bullet Spawner is missing.", this);

        if (normalAutoShooter == null)
            Debug.LogError("[BurstExSkill] Normal Auto Shooter is missing.", this);

        return firePoint != null
            && bulletSpawner != null
            && normalAutoShooter != null
            && !isActive;
    }
    public override void Activate()
    {
        if (!CanUse())
        {
            Debug.LogWarning(
                "[BurstExSkill] Skill cannot be activated. Check its references and state.",
                this
            );
            return;
        }

        isActive = true;

        previousNormalShooterState = normalAutoShooter.enabled;
        normalAutoShooter.enabled = false;

        FireTripleShot();

        durationRoutine = StartCoroutine(DurationRoutine());

        Debug.Log(
            $"[BurstExSkill] EX Skill activated for {duration:0.##} seconds.",
            this
        );
    }

    private void FireTripleShot()
    {
        Vector3 right = firePoint.right;

        FireAt(firePoint.position - right * spacing);
        FireAt(firePoint.position);
        FireAt(firePoint.position + right * spacing);
    }

    private void FireAt(Vector3 position)
    {
        GameObject temporaryPoint =
            new GameObject("TemporaryExSkillFirePoint");

        Transform pointTransform = temporaryPoint.transform;

        pointTransform.SetPositionAndRotation(
            position,
            firePoint.rotation
        );

        bulletSpawner.Fire(pointTransform, Owner.BulletSpeed);

        Destroy(temporaryPoint);
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSeconds(duration);

        FinishSkill();
    }

    private void Update()
    {
        if (!isActive || characterSwitcher == null)
            return;

        // Cancel this EX Skill if character switching begins
        // or a different character becomes active.
        if (characterSwitcher.IsSwitching ||
            characterSwitcher.ActiveCharacterComponent != Owner)
        {
            Cancel();
        }
    }

    public override void Cancel()
    {
        if (!isActive)
            return;

        FinishSkill();

        Debug.Log(
            "[BurstExSkill] EX Skill cancelled.",
            this
        );
    }

    private void FinishSkill()
    {
        if (durationRoutine != null)
        {
            StopCoroutine(durationRoutine);
            durationRoutine = null;
        }

        isActive = false;

        if (normalAutoShooter != null)
        {
            normalAutoShooter.enabled = previousNormalShooterState;
        }

        // Disable this EX Skill object after finishing.
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (durationRoutine != null)
        {
            StopCoroutine(durationRoutine);
            durationRoutine = null;
        }

        if (isActive)
        {
            isActive = false;

            if (normalAutoShooter != null)
            {
                normalAutoShooter.enabled = previousNormalShooterState;
            }
        }
    }
}
