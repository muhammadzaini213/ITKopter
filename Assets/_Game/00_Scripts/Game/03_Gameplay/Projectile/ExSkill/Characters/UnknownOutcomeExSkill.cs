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
    private Health holderHealth;
    private Coroutine durationRoutine;
    private VfxRing zone;
    private VfxRing glyph;
    private float afterimageTimer;
    private float scanTimer;

    public bool IsActive => stealthShield != null && stealthShield.Current > 0f;

    protected override Color DefaultColor => new Color(0.62f, 0.5f, 1f);

    protected override string[] DefaultActivationLines => new[] { "You can't hit what you can't see.", "Who, me? I was never here." };

    // The cloak passes to whoever is switched in.
    protected override bool PersistsWhenBenched => true;

    private Transform HolderTransform => FieldHolder != null ? FieldHolder.transform : Owner != null ? Owner.transform : null;

    public override string Describe(float attack, float defense)
    {
        return $"For {Seconds(duration)} Anon fades out: gains a shield worth {DefPct(shieldMultiplier, defense)} and erases every enemy bullet that comes within {Num(untouchableRadius)} units.\n\n" +
               "Ends early if the shield breaks. Carries over to whoever Anon switches to.";
    }

    public override void Activate()
    {
        if (!CanUse())
            return;

        Cancel();

        BeginFollowField();
        CreateShield();
        BuildZone(FieldHolder.transform);

        durationRoutine = StartSkillRoutine(DurationRoutine());
    }

    private void CreateShield()
    {
        var shieldGo = new GameObject("UnknownOutcomeShield");
        shieldGo.transform.SetParent(FieldHolder.transform, false);

        stealthShield = shieldGo.AddComponent<Shield>();
        stealthShield.Set(Mathf.Max(1f, Owner.EffectiveDefensePower * shieldMultiplier));
        stealthShield.OnBroken += OnShieldBroken;

        holderHealth = FieldHolder.GetComponent<Health>();
        if (holderHealth != null)
            holderHealth.AssignShield(stealthShield);
    }

    private void BuildZone(Transform holder)
    {
        if (zone != null) zone.Stop();
        if (glyph != null) glyph.Stop();

        zone = SkillVFX.Aura(holder, SkillColor.WithAlpha(0.55f), untouchableRadius, 0.05f, -1f, 0.04f, 0, 1, 0f);
        glyph = SkillVFX.Aura(holder, SkillColor, untouchableRadius * 0.92f, 0.04f, -1f, 0.02f, 5, 2, 60f);
    }

    protected override void OnFieldHolderChanged(Character previous, Character next)
    {
        if (stealthShield == null)
            return;

        holderHealth = MoveShield(stealthShield, holderHealth, next);
        BuildZone(next.transform);
    }

    // Runs on the CombatFeel runner: Anon's own Update stops once benched.
    private IEnumerator DurationRoutine()
    {
        float elapsed = 0f;

        while (elapsed < duration && stealthShield != null)
        {
            elapsed += Time.deltaTime;
            Tick(Time.deltaTime);
            yield return null;
        }

        durationRoutine = null;
        EndSkill(false);
    }

    private void OnShieldBroken()
    {
        EndSkill(true);
    }

    private void Tick(float dt)
    {
        Transform holder = HolderTransform;
        if (stealthShield == null || holder == null)
            return;

        afterimageTimer -= dt;
        if (afterimageTimer <= 0f)
        {
            afterimageTimer = afterimageInterval;
            SkillVFX.Ghost(holder, SkillColor.WithAlpha(0.45f), 0.35f, 1.05f);
        }

        // Scanning every projectile each frame is expensive; 30 Hz is plenty.
        scanTimer -= dt;
        if (scanTimer <= 0f)
        {
            scanTimer = 1f / 30f;
            DestroyIncomingProjectiles(holder.position);
        }
    }

    private void DestroyIncomingProjectiles(Vector2 center)
    {
        // Projectiles have no colliders (they BoxCast), so OverlapCircle can't see them.
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

            if (holderHealth != null && holderHealth.Shield == stealthShield)
                holderHealth.RemoveShield();

            Destroy(stealthShield.gameObject);
            stealthShield = null;

            Transform holder = HolderTransform;
            if (holder != null)
            {
                SkillVFX.Ring(holder.position, SkillColor, untouchableRadius, untouchableRadius * 0.2f, 0.25f, 0.06f);

                if (broken)
                {
                    SkillVFX.Sparks(holder.position, SkillColor, 16, 5f, 0.4f);
                    CombatFeel.Shake(0.15f, 0.2f);
                }
            }
        }

        if (zone != null) zone.Stop();
        if (glyph != null) glyph.Stop();
        zone = null;
        glyph = null;
        holderHealth = null;
        EndFollowField();
    }

    public override void Cancel()
    {
        EndSkill(false);
    }
}
