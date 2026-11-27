using System.Collections;
using UnityEngine;

// Anon — Unknown Outcome.
// Anon slips out of sight: a shield plus an "untouchable" zone that erases
// enemy bullets before they reach Anon. Trails afterimages while active.
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

    [SerializeField, Min(0.05f)]
    private float afterimageInterval = 0.12f;

    private Shield stealthShield;
    private Health ownerHealth;
    private Coroutine durationRoutine;
    private VfxRing zone;
    private VfxRing glyph;
    private float afterimageTimer;
    private float scanTimer;

    public bool IsActive => stealthShield != null && stealthShield.Current > 0f;

    protected override Color DefaultColor => new Color(0.62f, 0.5f, 1f);

    protected override string[] DefaultActivationLines => new[] { "You can't hit what you can't see.", "Who, me? I was never here." };

    public override string Describe(float attack, float defense)
    {
        return $"For {Seconds(duration)} Anon fades out: gains a shield worth {DefPct(shieldMultiplier, defense)} and erases every enemy bullet that comes within {Num(untouchableRadius)} units.\n\n" +
               "Ends early if the shield breaks.";
    }

    public override void Activate()
    {
        if (!CanUse())
            return;

        Cancel();

        CreateShield();

        zone = SkillVFX.Aura(Owner.transform, SkillColor.WithAlpha(0.55f), untouchableRadius, 0.05f, -1f, 0.04f, 0, 1, 0f);
        glyph = SkillVFX.Aura(Owner.transform, SkillColor, untouchableRadius * 0.92f, 0.04f, -1f, 0.02f, 5, 2, 60f);

        durationRoutine = StartSkillRoutine(DurationRoutine());
    }

    private void CreateShield()
    {
        var shieldGo = new GameObject("UnknownOutcomeShield");
        shieldGo.transform.SetParent(Owner.transform, false);

        stealthShield = shieldGo.AddComponent<Shield>();
        stealthShield.Set(Mathf.Max(1f, Owner.EffectiveDefensePower * shieldMultiplier));
        stealthShield.OnBroken += OnShieldBroken;

        ownerHealth = Owner.GetComponent<Health>();
        if (ownerHealth != null)
            ownerHealth.AssignShield(stealthShield);
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSeconds(duration);

        durationRoutine = null;
        EndSkill(false);
    }

    private void OnShieldBroken()
    {
        EndSkill(true);
    }

    private void Update()
    {
        if (stealthShield == null || Owner == null)
            return;

        afterimageTimer -= Time.deltaTime;
        if (afterimageTimer <= 0f)
        {
            afterimageTimer = afterimageInterval;
            SkillVFX.Ghost(Owner.transform, SkillColor.WithAlpha(0.45f), 0.35f, 1.05f);
        }

        // Scanning every projectile each frame is expensive; 30 Hz is plenty.
        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = 1f / 30f;
            DestroyIncomingProjectiles();
        }
    }

    private void DestroyIncomingProjectiles()
    {
        // Projectiles have no colliders (they BoxCast), so OverlapCircle can't see them.
        Vector2 center = Owner.transform.position;
        float sqrRadius = untouchableRadius * untouchableRadius;

        foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
        {
            if (projectile == null || !projectile.IsHostileToPlayer)
                continue;

            Vector2 pos = projectile.transform.position;

            if ((pos - center).sqrMagnitude > sqrRadius)
                continue;

            SkillVFX.Sparks(pos, SkillColor, 4, 2.5f, 0.2f, 0.08f);
            projectile.ReturnToPool();
        }
    }

    private void EndSkill(bool broken)
    {
        StopSkillRoutine(ref durationRoutine);

        if (stealthShield != null)
        {
            stealthShield.OnBroken -= OnShieldBroken;

            if (ownerHealth != null && ownerHealth.Shield == stealthShield)
                ownerHealth.RemoveShield();

            Destroy(stealthShield.gameObject);
            stealthShield = null;

            if (Owner != null)
            {
                SkillVFX.Ring(Owner.transform.position, SkillColor, untouchableRadius, untouchableRadius * 0.2f, 0.25f, 0.06f);

                if (broken)
                {
                    SkillVFX.Sparks(Owner.transform.position, SkillColor, 16, 5f, 0.4f);
                    CombatFeel.Shake(0.15f, 0.2f);
                }
            }
        }

        if (zone != null) zone.Stop();
        if (glyph != null) glyph.Stop();
        zone = null;
        glyph = null;
    }

    public override void Cancel()
    {
        EndSkill(false);
    }
}
