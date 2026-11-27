using System.Collections;
using UnityEngine;

// Tren — Viral Sensation.
// Every hit Tren lands "goes viral": +ATK and +fire rate per stack at the
// cost of defense. When the trend dies Tren crashes (slower fire) for a bit.
public class ViralSensationExSkill : ExSkill
{
    [Header("Viral Settings")]
    [SerializeField, Min(0f)]
    private float duration = 10f;

    [SerializeField, Min(0f)]
    private float damageMultiplierPerStack = 0.3f;

    [SerializeField, Min(0f)]
    private float speedMultiplierPerStack = 0.3f;

    [SerializeField, Min(0f)]
    private float defenseReductionMultiplierPerStack = 0.2f;

    [SerializeField, Min(0f)]
    private int maxStacks = 10;

    [Header("Crash Settings")]
    [SerializeField, Min(0f)]
    private float crashSlowMultiplier = 0.5f;

    [SerializeField, Min(0f)]
    private float crashDuration = 4f;

    private static readonly Color CrashColor = new Color(0.55f, 0.55f, 0.6f);

    private int stacks;
    private bool viralActive;
    private bool listening;
    private Coroutine durationRoutine;
    private Coroutine crashRoutine;

    private VfxRing aura;
    private VfxRing auraInner;
    private VfxText stackLabel;

    public int Stacks => stacks;

    protected override Color DefaultColor => new Color(1f, 0.3f, 0.75f);

    protected override string[] DefaultActivationLines => new[] { "This is going viral!", "Like, share, and get wrecked!" };

    public override string Describe(float attack, float defense)
    {
        return $"For {Seconds(duration)}, every hit Tren lands adds a stack (max {Num(maxStacks)}).\n\n" +
               $"Per stack: +{Pct(damageMultiplierPerStack)} ATK and +{Pct(speedMultiplierPerStack)} fire rate, but -{Pct(defenseReductionMultiplierPerStack)} DEF.\n" +
               $"At max stacks: +{Pct(damageMultiplierPerStack * maxStacks)} ATK.\n\n" +
               $"When it ends Tren crashes: fire rate drops to {Pct(crashSlowMultiplier)} for {Seconds(crashDuration)}.";
    }

    public override void Activate()
    {
        if (!CanUse())
            return;

        Cancel();

        stacks = 0;
        viralActive = true;
        ApplyBonuses();

        Projectile.OnHit += OnProjectileHit;
        listening = true;

        float radius = SkillVFX.GetVisualRadius(Owner.transform);
        aura = SkillVFX.Aura(Owner.transform, SkillColor, radius * 1.3f, 0.06f, -1f, 0.05f, 6, 1, 90f);
        auraInner = SkillVFX.Aura(Owner.transform, SkillColor.WithAlpha(0.5f), radius * 1.1f, 0.04f, -1f, 0.05f, 0, 1, 0f);
        stackLabel = SkillVFX.Label(Owner.transform, Vector3.up * (radius + 0.5f), "VIRAL x0", SkillColor, 3f);

        durationRoutine = StartSkillRoutine(DurationRoutine());
    }

    private void OnProjectileHit(Character owner, Health target, float damage, bool wasKilled)
    {
        if (owner != Owner || !viralActive)
            return;

        AddStack();
    }

    public void AddStack()
    {
        if (!viralActive || stacks >= maxStacks)
            return;

        stacks++;
        ApplyBonuses();

        float t = maxStacks > 0 ? stacks / (float)maxStacks : 1f;
        float radius = SkillVFX.GetVisualRadius(Owner.transform);

        if (aura != null)
        {
            aura.Radius = radius * Mathf.Lerp(1.3f, 1.9f, t);
            aura.Width = Mathf.Lerp(0.06f, 0.14f, t);
        }

        if (stackLabel != null)
            stackLabel.SetText(stacks >= maxStacks ? "VIRAL MAX!" : $"VIRAL x{stacks}");

        SkillVFX.Ring(Owner.transform.position, SkillColor, radius, radius * 1.8f, 0.2f, 0.04f);

        if (stacks >= maxStacks)
        {
            SkillVFX.Sparks(Owner.transform.position, SkillColor, 20, 6f, 0.4f);
            CombatFeel.Shake(0.12f, 0.15f);
        }
    }

    private void ApplyBonuses()
    {
        ApplyAttack(Owner, this, Owner.AttackPower * damageMultiplierPerStack * stacks);
        ApplyDefense(Owner, this, -Owner.DefensePower * defenseReductionMultiplierPerStack * stacks);
        ApplyFireInterval(Owner, this, 1f / (1f + speedMultiplierPerStack * stacks));
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSeconds(duration);

        durationRoutine = null;
        EndViral();
    }

    private void EndViral()
    {
        viralActive = false;
        StopListening();

        // Crash: buffs gone, fire interval stretched.
        ClearModifiers(Owner, this);
        ApplyFireInterval(Owner, this, 1f / Mathf.Max(0.05f, crashSlowMultiplier));

        if (aura != null) aura.Color = CrashColor;
        if (auraInner != null) auraInner.Stop();
        if (stackLabel != null)
        {
            stackLabel.SetColor(CrashColor);
            stackLabel.SetText("CRASH...");
        }

        if (Owner != null)
            SkillVFX.Sparks(Owner.transform.position, CrashColor, 12, 3f, 0.4f);

        crashRoutine = StartSkillRoutine(CrashRoutine());
    }

    private IEnumerator CrashRoutine()
    {
        yield return new WaitForSeconds(crashDuration);

        crashRoutine = null;
        ClearModifiers(Owner, this);
        StopVisuals();
        stacks = 0;
    }

    private void StopListening()
    {
        if (!listening)
            return;

        Projectile.OnHit -= OnProjectileHit;
        listening = false;
    }

    private void StopVisuals()
    {
        if (aura != null) aura.Stop();
        if (auraInner != null) auraInner.Stop();
        if (stackLabel != null) stackLabel.Stop();

        aura = null;
        auraInner = null;
        stackLabel = null;
    }

    public override void Cancel()
    {
        StopListening();
        StopSkillRoutine(ref durationRoutine);
        StopSkillRoutine(ref crashRoutine);

        viralActive = false;
        ClearModifiers(Owner, this);
        StopVisuals();
        stacks = 0;
    }
}
