using System.Collections;
using UnityEngine;

// Warlock — Total Warfare.
// Opening shockwave, then a focused barrage at the toughest enemy on screen
// (marked with a crosshair). Every barrage hit that lands on the target
// charges the finale blast.
public class TotalWarfareExSkill : ExSkill
{
    [Header("Opening AOE Settings")]
    [SerializeField, Min(0f)]
    private float aoeDamageMultiplier = 1.5f;

    [SerializeField, Min(0f)]
    private float aoeRadius = 10f;

    [SerializeField]
    private LayerMask enemyMask;

    [Header("Focus Settings")]
    [SerializeField, Min(0f)]
    private int focusProjectiles = 6;

    [SerializeField, Min(0f)]
    private float focusDamageMultiplier = 1f;

    [SerializeField, Min(0f)]
    private float focusInterval = 0.3f;

    [Header("Finale Settings")]
    [SerializeField, Min(0f)]
    private float finaleDamageMultiplier = 3f;

    [SerializeField, Min(0f)]
    private float finaleRadius = 6f;

    private int successfulHits;
    private Health highestHpTarget;
    private Coroutine barrageRoutine;
    private VfxRing crosshair;
    private VfxRing crosshairInner;
    private VfxText hitCounter;
    private bool listening;
    private BulletSpawner bulletSpawner;

    protected override Color DefaultColor => new Color(1f, 0.42f, 0.15f);

    protected override string[] DefaultActivationLines => new[] { "Total warfare. No one walks away.", "Focus fire on the big one!" };

    public override string Describe(float attack, float defense)
    {
        return $"Opens with a shockwave: {AtkPct(aoeDamageMultiplier, attack)} damage to every enemy within {Num(aoeRadius)} units.\n\n" +
               $"Then fires {Num(focusProjectiles)} focused shots at the toughest enemy, one every {Seconds(focusInterval)}, each dealing {AtkPct(focusDamageMultiplier, attack)}.\n\n" +
               $"Finale: enemies within {Num(finaleRadius)} units take {AtkPct(finaleDamageMultiplier, attack)} for <i>every</i> focused shot that hit.";
    }

    public override void Activate()
    {
        if (!CanUse())
            return;

        Cancel();

        successfulHits = 0;
        bulletSpawner = Owner.GetComponentInChildren<BulletSpawner>(true);

        FireOpeningAOE();
        SetTarget(SkillTargets.HighestHealthEnemy());

        Projectile.OnHit += OnProjectileHit;
        listening = true;

        barrageRoutine = StartSkillRoutine(BarrageRoutine());
    }

    private void FireOpeningAOE()
    {
        Vector2 center = Owner.transform.position;
        Color color = SkillColor;

        SkillVFX.Ring(center, color, 0.5f, aoeRadius, 0.5f, 0.3f);
        SkillVFX.Ring(center, Color.white.WithAlpha(0.8f), 0.3f, aoeRadius * 0.7f, 0.35f, 0.08f);
        SkillVFX.Sparks(center, color, 30, 10f, 0.5f);

        float damage = Owner.EffectiveAttackPower * aoeDamageMultiplier;

        foreach (var health in SkillTargets.EnemiesInRadius(center, aoeRadius))
        {
            SkillVFX.Sparks(health.transform.position, color, 8, 4f, 0.3f);
            CounterMechanics.Damage(Owner, health, damage);
        }
    }

    private void SetTarget(Health target)
    {
        highestHpTarget = target;

        if (crosshair != null) crosshair.Stop(0.1f);
        if (crosshairInner != null) crosshairInner.Stop(0.1f);
        if (hitCounter != null) hitCounter.Stop();

        crosshair = null;
        crosshairInner = null;
        hitCounter = null;

        if (target == null || target.IsDead)
            return;

        crosshair = SkillVFX.Marker(target, SkillColor, -1f, 4, 1, 0f);
        crosshairInner = SkillVFX.Marker(target, Color.white.WithAlpha(0.8f), -1f, 4, 1, -160f);

        if (crosshairInner != null)
            crosshairInner.Radius *= 0.6f;

        float radius = SkillVFX.GetVisualRadius(target.transform);
        hitCounter = SkillVFX.Label(target.transform, Vector3.up * (radius + 0.5f), HitText(), SkillColor, 3f);
    }

    private string HitText() => $"x{successfulHits}";

    private void OnProjectileHit(Character owner, Health target, float damage, bool wasKilled)
    {
        if (owner != Owner || target == null || target != highestHpTarget)
            return;

        successfulHits++;

        if (hitCounter != null)
            hitCounter.SetText(HitText());

        SkillVFX.Sparks(target.transform.position, SkillColor, 6, 4f, 0.25f);
    }

    private IEnumerator BarrageRoutine()
    {
        for (int i = 0; i < focusProjectiles; i++)
        {
            // Target died: retarget the next toughest enemy instead of
            // abandoning the barrage.
            if (highestHpTarget == null || highestHpTarget.IsDead)
                SetTarget(SkillTargets.HighestHealthEnemy());

            if (highestHpTarget == null)
                break;

            FireFocusProjectile();

            yield return new WaitForSeconds(focusInterval);
        }

        // Let the last shots land before the finale.
        yield return new WaitForSeconds(0.25f);

        FireFinale();

        StopListening();
        SetTarget(null);
        barrageRoutine = null;
    }

    private void FireFocusProjectile()
    {
        if (highestHpTarget == null || bulletSpawner == null)
            return;

        Vector2 origin = Owner.transform.position;
        Vector2 direction = ((Vector2)highestHpTarget.transform.position - origin).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        var temp = new GameObject("TotalWarfareFirePoint");
        temp.transform.SetPositionAndRotation(origin, Quaternion.Euler(0f, 0f, angle));

        bulletSpawner.Fire(
            temp.transform,
            Owner.BulletSpeed * 1.3f,
            Owner.EffectiveAttackPower * focusDamageMultiplier);

        Destroy(temp);

        SkillVFX.Sparks(origin + direction * 0.4f, SkillColor, 5, 5f, 0.2f);
        SkillVFX.Beam(origin, highestHpTarget.transform.position, SkillColor.WithAlpha(0.35f), 0.03f, 0.1f, 0.02f);
        CombatFeel.Shake(0.07f, 0.08f);
    }

    private void FireFinale()
    {
        if (Owner == null)
            return;

        Vector2 center = Owner.transform.position;
        float bonus = successfulHits * Owner.EffectiveAttackPower * finaleDamageMultiplier;

        SkillVFX.Explosion(center, SkillColor, finaleRadius, withFeel: false);
        SkillVFX.Text(center + Vector2.up * 1.5f, $"FINALE x{successfulHits}", SkillColor, 4.5f, 1f);

        if (bonus > 0f)
        {
            foreach (var health in SkillTargets.EnemiesInRadius(center, finaleRadius))
            {
                SkillVFX.Explosion(health.transform.position, SkillColor, 1.2f, withFeel: false);
                CounterMechanics.Damage(Owner, health, bonus);
            }
        }

        CombatFeel.Flash(SkillColor.WithAlpha(0.25f), 0.2f);
        CombatFeel.HitStop(Mathf.Lerp(0.08f, 0.18f, Mathf.Clamp01(successfulHits / 6f)));
        CombatFeel.Shake(Mathf.Lerp(0.25f, 0.5f, Mathf.Clamp01(successfulHits / 6f)), 0.4f);
    }

    // Kept for external callers.
    public void RegisterHit(Health target)
    {
        if (target == highestHpTarget)
            successfulHits++;
    }

    private void StopListening()
    {
        if (!listening)
            return;

        Projectile.OnHit -= OnProjectileHit;
        listening = false;
    }

    public override void Cancel()
    {
        StopSkillRoutine(ref barrageRoutine);
        StopListening();
        SetTarget(null);
    }
}
