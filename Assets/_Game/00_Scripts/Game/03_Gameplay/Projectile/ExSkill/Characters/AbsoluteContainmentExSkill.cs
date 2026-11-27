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

    [SerializeField, Min(0f)]
    private float barrierRadius = 3f;

    [SerializeField]
    private LayerMask enemyProjectileMask;

    private Shield barrierShield;
    private Health ownerHealth;
    private Coroutine durationRoutine;
    private VfxRing dome;
    private VfxRing frame;
    private float scanTimer;

    public bool IsActive => barrierShield != null && barrierShield.Current > 0f;

    protected override Color DefaultColor => new Color(1f, 0.78f, 0.3f);

    protected override string[] DefaultActivationLines => new[] { "Absolute containment! Nothing gets through!", "Stay behind the barrier!" };

    public override string Describe(float attack, float defense)
    {
        return $"Raises a containment barrier (radius {Num(barrierRadius)}) for {Seconds(barrierDuration)} with {DefPct(barrierHitpointsMultiplier, defense)} HP.\n\n" +
               "Enemy bullets entering the barrier are destroyed and their damage is drained from it; it also absorbs hits on Bokusu. Shatters when its HP runs out.";
    }

    public override void Activate()
    {
        if (!CanUse())
            return;

        Cancel();

        CreateBarrier();
        durationRoutine = StartSkillRoutine(DurationRoutine());
    }

    private void CreateBarrier()
    {
        var shieldGo = new GameObject("AbsoluteContainmentBarrier");
        shieldGo.transform.SetParent(Owner.transform, false);

        barrierShield = shieldGo.AddComponent<Shield>();
        barrierShield.Set(Mathf.Max(1f, Owner.EffectiveDefensePower * barrierHitpointsMultiplier));
        barrierShield.OnBroken += OnBarrierBroken;
        barrierShield.OnAmountChanged += OnBarrierChanged;

        ownerHealth = Owner.GetComponent<Health>();
        if (ownerHealth != null)
            ownerHealth.AssignShield(barrierShield);

        dome = SkillVFX.Aura(Owner.transform, SkillColor, barrierRadius, 0.12f, -1f, 0.02f);
        frame = SkillVFX.Aura(Owner.transform, SkillColor.WithAlpha(0.6f), barrierRadius * 0.95f, 0.05f, -1f, 0.02f, 6, 1, 25f);

        SkillVFX.Ring(Owner.transform.position, Color.white, barrierRadius * 0.3f, barrierRadius, 0.25f, 0.1f);
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSeconds(barrierDuration);

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

    private void Update()
    {
        if (barrierShield == null || Owner == null)
            return;

        scanTimer -= Time.deltaTime;
        if (scanTimer > 0f)
            return;

        scanTimer = 1f / 30f;
        ContainProjectiles();
    }

    private void ContainProjectiles()
    {
        Vector2 center = Owner.transform.position;
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

            float damage = projectile.Damage;
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

            if (ownerHealth != null && ownerHealth.Shield == barrierShield)
                ownerHealth.RemoveShield();

            Destroy(barrierShield.gameObject);
            barrierShield = null;

            if (Owner != null)
            {
                Vector3 pos = Owner.transform.position;

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
    }

    public override void Cancel()
    {
        DestroyBarrier(false);
    }
}
