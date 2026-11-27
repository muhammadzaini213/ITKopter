using System.Collections;
using UnityEngine;

// Vio — Unrestrained Violence.
// Rage mode: Vio takes more damage, but every hit builds stacks of ATK and
// fire rate. Stacks bleed off one by one if Vio stops landing hits.
public class UnrestrainedViolenceExSkill : ExSkill
{
    [Header("Buff Settings")]
    [SerializeField, Min(0f)]
    private float duration = 6f;

    [SerializeField, Min(0f)]
    private float attackMultiplierPerStack = 0.2f;

    [SerializeField, Min(0f)]
    private float attackSpeedMultiplierPerStack = 0.2f;

    [SerializeField, Min(0f)]
    private int maxStacks = 8;

    [SerializeField, Min(0f)]
    private float damageTakenMultiplier = 1.5f;

    [Header("Hit Settings")]
    [SerializeField, Min(0f)]
    private float hitWindow = 1f;

    private int stacks;
    private bool isActive;
    private bool listening;
    private float damageTakenBonus;
    private Health ownerHealth;
    private Coroutine durationRoutine;
    private Coroutine decayRoutine;

    private VfxRing aura;
    private VfxRing spikes;
    private VfxText stackLabel;

    public int Stacks => stacks;

    protected override Color DefaultColor => new Color(1f, 0.12f, 0.2f);

    protected override string[] DefaultActivationLines => new[] { "No more holding back!", "Hit me. I'll hit harder!" };

    public override string Describe(float attack, float defense)
    {
        return $"For {Seconds(duration)} Vio rages and takes {Pct(damageTakenMultiplier)} damage from every hit.\n\n" +
               $"Each hit Vio lands adds a stack (max {Num(maxStacks)}): +{Pct(attackMultiplierPerStack)} ATK and +{Pct(attackSpeedMultiplierPerStack)} fire rate per stack.\n\n" +
               $"Stacks fall off one by one if Vio lands no hit for {Seconds(hitWindow)}.";
    }

    public override void Activate()
    {
        if (!CanUse())
            return;

        Cancel();

        stacks = 0;
        isActive = true;

        ownerHealth = Owner.GetComponent<Health>();
        ApplyDamageTakenModifier(true);
        ApplyStatBonuses();

        Projectile.OnHit += OnProjectileHit;
        listening = true;

        float radius = SkillVFX.GetVisualRadius(Owner.transform);
        aura = SkillVFX.Aura(Owner.transform, SkillColor.WithAlpha(0.6f), radius * 1.25f, 0.05f, -1f, 0.1f);
        spikes = SkillVFX.Aura(Owner.transform, SkillColor, radius * 1.45f, 0.05f, -1f, 0.04f, 8, 3, -200f);
        stackLabel = SkillVFX.Label(Owner.transform, Vector3.up * (radius + 0.5f), "RAGE", SkillColor, 3f);

        CombatFeel.Flash(SkillColor.WithAlpha(0.2f), 0.25f);

        durationRoutine = StartSkillRoutine(DurationRoutine());
    }

    private void OnProjectileHit(Character owner, Health target, float damage, bool wasKilled)
    {
        if (owner != Owner || !isActive)
            return;

        OnSuccessfulHit();
    }

    public void OnSuccessfulHit()
    {
        if (!isActive)
            return;

        bool gained = stacks < maxStacks;
        stacks = Mathf.Min(maxStacks, stacks + 1);
        ApplyStatBonuses();
        RefreshVisuals(gained);

        if (decayRoutine != null)
            StopCoroutine(decayRoutine);

        decayRoutine = StartCoroutine(DecayRoutine());
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSeconds(duration);

        durationRoutine = null;
        Cancel();
    }

    private IEnumerator DecayRoutine()
    {
        while (isActive && stacks > 0)
        {
            yield return new WaitForSeconds(hitWindow);

            if (!isActive)
                yield break;

            stacks = Mathf.Max(0, stacks - 1);
            ApplyStatBonuses();
            RefreshVisuals(false);
        }

        decayRoutine = null;
    }

    private void RefreshVisuals(bool gained)
    {
        if (Owner == null)
            return;

        float t = maxStacks > 0 ? stacks / (float)maxStacks : 0f;
        float radius = SkillVFX.GetVisualRadius(Owner.transform);

        if (spikes != null)
        {
            spikes.Radius = radius * Mathf.Lerp(1.45f, 2.1f, t);
            spikes.Width = Mathf.Lerp(0.05f, 0.13f, t);
        }

        if (aura != null)
            aura.Color = Color.Lerp(SkillColor.WithAlpha(0.6f), Color.white, t * 0.4f);

        if (stackLabel != null)
            stackLabel.SetText(stacks > 0 ? $"RAGE x{stacks}" : "RAGE", gained);

        if (gained && stacks == maxStacks)
        {
            SkillVFX.Sparks(Owner.transform.position, SkillColor, 18, 6f, 0.35f);
            CombatFeel.Shake(0.12f, 0.15f);
        }
    }

    private void ApplyStatBonuses()
    {
        ApplyAttack(Owner, this, Owner.AttackPower * attackMultiplierPerStack * stacks);
        ApplyFireInterval(Owner, this, 1f / (1f + attackSpeedMultiplierPerStack * stacks));
    }

    private void ApplyDamageTakenModifier(bool apply)
    {
        if (ownerHealth == null)
            return;

        if (apply)
        {
            damageTakenBonus = damageTakenMultiplier - 1f;
            ownerHealth.IncomingDamageMultiplier += damageTakenBonus;
        }
        else
        {
            ownerHealth.IncomingDamageMultiplier -= damageTakenBonus;
            damageTakenBonus = 0f;
        }
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
        StopListening();
        StopSkillRoutine(ref durationRoutine);

        if (decayRoutine != null)
        {
            StopCoroutine(decayRoutine);
            decayRoutine = null;
        }

        // Only undo the damage-taken penalty once (Cancel used to run twice
        // and could leave Vio permanently fragile or tanky).
        if (isActive)
            ApplyDamageTakenModifier(false);

        isActive = false;
        stacks = 0;
        ClearModifiers(Owner, this);

        if (aura != null) aura.Stop();
        if (spikes != null) spikes.Stop();
        if (stackLabel != null) stackLabel.Stop();

        aura = null;
        spikes = null;
        stackLabel = null;
    }
}
