using System.Collections;
using UnityEngine;

// Regu — Emergency Regulation.
// Field-wide regulation: enemy bullets on screen are capped (the excess is
// struck down) and Regu's defense rises. Keeps running while Regu is
// switched out, since the cap protects the whole party.
public class EmergencyRegulationExSkill : ExSkill
{
    [Header("Limit Settings")]
    [SerializeField, Min(1)]
    private int maxEnemyProjectiles = 3;

    [SerializeField, Min(0f)]
    private float duration = 8f;

    [Header("Damage Reduction Settings")]
    [SerializeField, Min(0f)]
    private float damageReductionMultiplier = 0.5f;

    private ProjectileLimiter limiter;
    private bool createdLimiter;
    private int savedMaxProjectiles;
    private bool savedLimiterEnabled;
    private Coroutine routine;
    private VfxRing aura;
    private VfxRing seal;

    protected override Color DefaultColor => new Color(0.55f, 1f, 0.35f);

    protected override string[] DefaultActivationLines => new[] { "Emergency regulation in effect!", "Rule one: no bullet spam." };

    protected override bool PersistsWhenBenched => true;

    public override string Describe(float attack, float defense)
    {
        return $"For {Seconds(duration)}, at most {Num(maxEnemyProjectiles)} enemy bullets may exist on screen; the rest are struck down.\n\n" +
               $"Regu also gains +{Pct(damageReductionMultiplier)} DEF.\n\n" +
               "Keeps working while Regu is benched.";
    }

    public override void Activate()
    {
        if (!CanUse())
            return;

        Cancel();

        ApplyLimit();
        ApplyDefense(Owner, this, Owner.DefensePower * damageReductionMultiplier);

        // Visual: a regulation sweep across the screen + a seal on the party.
        Vector3 pos = Owner.transform.position;
        SkillVFX.Ring(pos, SkillColor, 0.5f, 14f, 0.7f, 0.25f, 4, 1, 90f);
        SkillVFX.Ring(pos, Color.white.WithAlpha(0.6f), 0.3f, 10f, 0.5f, 0.06f);

        float radius = SkillVFX.GetVisualRadius(Owner.transform);
        aura = SkillVFX.Aura(TeamAnchor, SkillColor.WithAlpha(0.7f), radius * 1.6f, 0.05f, -1f, 0.03f, 4, 1, 40f);
        seal = SkillVFX.Aura(TeamAnchor, SkillColor.WithAlpha(0.45f), radius * 1.6f, 0.04f, -1f, 0.03f, 4, 1, -40f);

        routine = StartSkillRoutine(EndAfterDuration());
    }

    private void ApplyLimit()
    {
        limiter = Object.FindFirstObjectByType<ProjectileLimiter>(FindObjectsInactive.Include);
        createdLimiter = limiter == null;

        if (createdLimiter)
        {
            var go = new GameObject("ReguProjectileLimiter");
            limiter = go.AddComponent<ProjectileLimiter>();
        }

        savedMaxProjectiles = limiter.MaxEnemyProjectiles;
        savedLimiterEnabled = limiter.enabled;

        limiter.SetMaxEnemyProjectiles(maxEnemyProjectiles);
        limiter.CullColor = SkillColor;
        limiter.enabled = true;
        limiter.gameObject.SetActive(true);
    }

    private IEnumerator EndAfterDuration()
    {
        yield return new WaitForSeconds(duration);

        routine = null;
        EndSkill();
    }

    private void EndSkill()
    {
        ClearModifiers(Owner, this);

        if (limiter != null)
        {
            // The limiter used to stay alive at its default cap (8) forever
            // after the first cast. Remove it if we made it, else restore.
            if (createdLimiter)
            {
                Destroy(limiter.gameObject);
            }
            else
            {
                limiter.SetMaxEnemyProjectiles(savedMaxProjectiles);
                limiter.enabled = savedLimiterEnabled;
                limiter.CullColor = null;
            }

            limiter = null;
        }

        if (aura != null) aura.Stop();
        if (seal != null) seal.Stop();
        aura = null;
        seal = null;
    }

    public override void Cancel()
    {
        StopSkillRoutine(ref routine);
        EndSkill();
    }
}
