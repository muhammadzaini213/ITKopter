using System.Collections;
using UnityEngine;

public class UnknownOutcomeExSkill : ExSkill
{
    [Header("Shield Settings")]
    [SerializeField, Min(0f)]
    private float shieldMultiplier = 2f;

    [SerializeField, Min(0f)]
    private float duration = 8f;

    [Header("Untargetable Settings")]
    [SerializeField, Min(0f)]
    private float untouchableRadius = 2f;

    private Shield stealthShield;
    private Coroutine durationRoutine;

    public bool IsActive => stealthShield != null && stealthShield.Current > 0f;

    public override void Activate()
    {
        if (!CanUse()) return;

        CreateShield();
        StartDuration();
    }

    private void CreateShield()
    {
        var shieldGo = new GameObject("UnknownOutcomeShield");
        shieldGo.transform.position = Owner.transform.position;

        var circle = shieldGo.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = untouchableRadius;

        stealthShield = shieldGo.AddComponent<Shield>();
        float hp = Owner.EffectiveDefensePower * shieldMultiplier;
        stealthShield.Set(Mathf.Max(1f, hp));
        stealthShield.OnBroken += OnShieldBroken;

        var ownerHealth = Owner.GetComponent<Health>();
        if (ownerHealth != null)
            ownerHealth.AssignShield(stealthShield);
    }

    private void StartDuration()
    {
        if (durationRoutine != null)
            StopCoroutine(durationRoutine);

        durationRoutine = StartCoroutine(DurationRoutine());
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSeconds(duration);
        DestroyShield();
    }

    private void OnShieldBroken()
    {
        DestroyShield();
    }

    private void DestroyShield()
    {
        if (durationRoutine != null)
        {
            StopCoroutine(durationRoutine);
            durationRoutine = null;
        }

        if (stealthShield != null)
        {
            stealthShield.OnBroken -= OnShieldBroken;

            var ownerHealth = Owner.GetComponent<Health>();
            if (ownerHealth != null && ownerHealth.Shield == stealthShield)
                ownerHealth.RemoveShield();

            if (stealthShield.gameObject != null)
                Destroy(stealthShield.gameObject);

            stealthShield = null;
        }
    }

    private void Update()
    {
        if (stealthShield != null && stealthShield.gameObject != null)
            stealthShield.gameObject.transform.position = Owner.transform.position;

        DestroyIncomingProjectiles();
    }

    private void DestroyIncomingProjectiles()
    {
        // Projectiles have no colliders (they BoxCast), so OverlapCircle can't see them.
        // Scan live projectiles and clear enemy bullets inside the radius instead.
        Vector2 center = Owner.transform.position;
        float sqrRadius = untouchableRadius * untouchableRadius;

        var projectiles = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);

        foreach (var projectile in projectiles)
        {
            if (projectile == null || !projectile.IsHostileToPlayer)
                continue;

            if (((Vector2)projectile.transform.position - center).sqrMagnitude <= sqrRadius)
                projectile.ReturnToPool();
        }
    }

    public override void Cancel()
    {
        DestroyShield();
    }

    private void OnDisable()
    {
        Cancel();
    }
}
