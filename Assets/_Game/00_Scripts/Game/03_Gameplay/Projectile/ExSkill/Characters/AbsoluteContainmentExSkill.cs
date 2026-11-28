using System.Collections;
using UnityEngine;

// Bokusu — Absolute Containment.
// A barrier dome around Bokusu. Enemy bullets that enter the dome are
// contained (destroyed) and their damage is drained from the barrier's HP;
// the barrier also soaks direct hits. It shatters when its HP runs out.
public class AbsoluteContainmentExSkill : ExSkill
{
    [Header("Barrier Settings")]
    [SerializeField, Min(0f)]
    private float barrierDuration = 5f;

    [SerializeField, Min(0f)]
    private float barrierHitpointsMultiplier = 3f;

    [SerializeField, Min(0f), Tooltip("Extra barrier HP as a share of Bokusu's max HP (0.4 = 40%). DEF alone made the barrier break after one or two bullets.")]
    private float barrierMaxHealthShare = 0.4f;

    [SerializeField, Range(0f, 1f), Tooltip("Share of a contained bullet's damage the barrier takes (it destroys the bullet). Direct hits still cost full damage.")]
    private float containedDamageShare = 0.25f;

    [SerializeField, Min(0f)]
    private float barrierRadius = 3f;

    [SerializeField]
    private LayerMask enemyProjectileMask;

    private Shield barrierShield;
    private Health holderHealth;
    private Coroutine durationRoutine;
    private VfxRing dome;
    private VfxRing frame;
    private float scanTimer;

    public bool IsActive => barrierShield != null && barrierShield.Current > 0f;

    protected override Color DefaultColor => new Color(1f, 0.78f, 0.3f);

    protected override string[] DefaultActivationLines => new[] { "Absolute containment! Nothing gets through!", "Stay behind the barrier!" };

    // The barrier stays up around whoever is switched in.
    protected override bool PersistsWhenBenched => true;

    private Transform HolderTransform => FieldHolder != null ? FieldHolder.transform : Owner != null ? Owner.transform : null;

    public override string Describe(float attack, float defense)
    {
        return $"Raises a containment barrier (radius {Num(barrierRadius)}) for {Seconds(barrierDuration)} with HP equal to {Pct(barrierMaxHealthShare)} of Bokusu's max HP plus {DefPct(barrierHitpointsMultiplier, defense)}.\n\n" +
               $"Enemy bullets entering the barrier are destroyed and drain only {Pct(containedDamageShare)} of their damage from it. Direct hits drain full damage.\n\n" +
               "Shatters when its HP runs out.\n\n" +
               "Stays up around whoever Bokusu switches to.";
    }

    public override void Activate()
    {
        if (!CanUse())
            return;

        Cancel();

        BeginFollowField();
        CreateBarrier();
        durationRoutine = StartSkillRoutine(DurationRoutine());
    }

    private void CreateBarrier()
    {
        var shieldGo = new GameObject("AbsoluteContainmentBarrier");
        shieldGo.transform.SetParent(FieldHolder.transform, false);

        barrierShield = shieldGo.AddComponent<Shield>();
        Health bokusuHealth = Owner.GetComponent<Health>();
        float fromHealth = bokusuHealth != null ? bokusuHealth.MaxHealth * barrierMaxHealthShare : 0f;
        barrierShield.Set(Mathf.Max(1f, fromHealth + Owner.EffectiveDefensePower * barrierHitpointsMultiplier));
        barrierShield.OnBroken += OnBarrierBroken;
        barrierShield.OnAmountChanged += OnBarrierChanged;

        holderHealth = FieldHolder.GetComponent<Health>();
        if (holderHealth != null)
            holderHealth.AssignShield(barrierShield);

        BuildDome(FieldHolder.transform);

        SkillVFX.Ring(FieldHolder.transform.position, Color.white, barrierRadius * 0.3f, barrierRadius, 0.25f, 0.1f);
    }

    private void BuildDome(Transform holder)
    {
        if (dome != null) dome.Stop();
        if (frame != null) frame.Stop();

        dome = SkillVFX.Aura(holder, SkillColor, barrierRadius, 0.12f, -1f, 0.02f);
        frame = SkillVFX.Aura(holder, SkillColor.WithAlpha(0.6f), barrierRadius * 0.95f, 0.05f, -1f, 0.02f, 6, 1, 25f);

        if (barrierShield != null)
            OnBarrierChanged(barrierShield.Current);
    }

    protected override void OnFieldHolderChanged(Character previous, Character next)
    {
        if (barrierShield == null)
            return;

        holderHealth = MoveShield(barrierShield, holderHealth, next);
        BuildDome(next.transform);
    }

    // Runs on the CombatFeel runner: Bokusu's own Update stops once benched.
    private IEnumerator DurationRoutine()
    {
        float elapsed = 0f;

        while (elapsed < barrierDuration && barrierShield != null)
        {
            elapsed += Time.deltaTime;

            scanTimer -= Time.deltaTime;
            if (scanTimer <= 0f)
            {
                scanTimer = 1f / 30f;
                ContainProjectiles();
            }

            yield return null;
        }

        durationRoutine = null;
        DestroyBarrier(false);
    }

    private void OnBarrierChanged(float current)
    {
        if (barrierShield == null || barrierShield.Max <= 0f)
            return;

        float t = Mathf.Clamp01(current / barrierShield.Max);

        if (dome != null)
        {
            dome.Width = Mathf.Lerp(0.04f, 0.12f, t);
            dome.Color = Color.Lerp(new Color(1f, 0.3f, 0.2f), SkillColor, t);
        }
    }

    private void OnBarrierBroken()
    {
        DestroyBarrier(true);
    }

    private void ContainProjectiles()
    {
        Transform holder = HolderTransform;
        if (holder == null)
            return;

        Vector2 center = holder.position;
        float sqrRadius = barrierRadius * barrierRadius;

        foreach (var projectile in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
        {
            if (barrierShield == null)
                return;

            if (projectile == null || !projectile.IsHostileToPlayer)
                continue;

            Vector2 pos = projectile.transform.position;

            if ((pos - center).sqrMagnitude > sqrRadius)
                continue;

            float damage = projectile.Damage * containedDamageShare;
            projectile.ReturnToPool();

            SkillVFX.Sparks(pos, SkillColor, 5, 3f, 0.2f, 0.08f);
            SkillVFX.Ring(pos, SkillColor, 0.05f, 0.35f, 0.15f, 0.04f);

            // May break the barrier (→ DestroyBarrier via OnBroken).
            barrierShield.Absorb(ref damage);
        }
    }

    private void DestroyBarrier(bool broken)
    {
        StopSkillRoutine(ref durationRoutine);

        if (barrierShield != null)
        {
            barrierShield.OnBroken -= OnBarrierBroken;
            barrierShield.OnAmountChanged -= OnBarrierChanged;

            if (holderHealth != null && holderHealth.Shield == barrierShield)
                holderHealth.RemoveShield();

            Destroy(barrierShield.gameObject);
            barrierShield = null;

            Transform holder = HolderTransform;
            if (holder != null)
            {
                Vector3 pos = holder.position;

                if (broken)
                {
                    SkillVFX.Ring(pos, SkillColor, barrierRadius, barrierRadius * 1.6f, 0.3f, 0.08f);
                    SkillVFX.Sparks(pos, SkillColor, 30, 7f, 0.5f);
                    SkillVFX.Text(pos + Vector3.up * 1.4f, "BARRIER BROKEN", SkillColor, 3.5f, 0.9f);
                    CombatFeel.HitStop(0.08f);
                    CombatFeel.Shake(0.25f, 0.25f);
                }
                else
                {
                    SkillVFX.Ring(pos, SkillColor, barrierRadius, barrierRadius * 0.2f, 0.3f, 0.06f);
                }
            }
        }

        if (dome != null) dome.Stop();
        if (frame != null) frame.Stop();
        dome = null;
        frame = null;
        holderHealth = null;
        EndFollowField();
    }

    public override void Cancel()
    {
        DestroyBarrier(false);
    }
}
