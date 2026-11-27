using System.Collections;
using UnityEngine;

// EyaY — Recursive Optimization.
// Party-wide self-improving loop: every hit any party member lands adds a
// stack of ATK and fire rate for the whole party (up to the cap).
public class RecursiveOptimizationExSkill : ExSkill
{
    [Header("Optimization Settings")]
    [SerializeField, Min(0f)]
    private float duration = 10f;

    [SerializeField, Min(0f)]
    private float damageMultiplierPerStack = 0.2f;

    [SerializeField, Min(0f)]
    private float speedMultiplierPerStack = 0.2f;

    [SerializeField, Min(0f)]
    private int maxStacks = 10;

    private int stacks;
    private bool listening;
    private Coroutine durationRoutine;
    private VfxRing core;
    private VfxRing loop;
    private VfxText label;

    public int Stacks => stacks;

    protected override Color DefaultColor => new Color(0.35f, 0.62f, 1f);

    protected override string[] DefaultActivationLines => new[] { "Recursive optimization, running.", "Every hit makes us better." };

    protected override bool PersistsWhenBenched => true;

    public override string Describe(float attack, float defense)
    {
        return $"For {Seconds(duration)}, every hit any team member lands adds a stack (max {Num(maxStacks)}) for the <i>whole team</i>.\n\n" +
               $"Per stack: +{Pct(damageMultiplierPerStack)} ATK and +{Pct(speedMultiplierPerStack)} fire rate.\n" +
               $"At max stacks: +{Pct(damageMultiplierPerStack * maxStacks)} ATK.\n\n" +
               "Keeps working while EyaY is benched.";
    }

    public override void Activate()
    {
        if (!CanUse())
            return;

        Cancel();

        stacks = 0;
        ApplyBonuses();

        // Was never raised before — bullets and EX skills now report it.
        CombatEventBus.OnTeamDealtDamage += OnTeamDealtDamage;
        listening = true;

        float radius = SkillVFX.GetVisualRadius(Owner.transform);
        core = SkillVFX.Aura(TeamAnchor, SkillColor, radius * 1.3f, 0.05f, -1f, 0.04f, 6, 1, 60f);
        loop = SkillVFX.Aura(TeamAnchor, SkillColor.WithAlpha(0.5f), radius * 1.5f, 0.04f, -1f, 0.04f, 6, 1, -90f);
        label = SkillVFX.Label(TeamAnchor, Vector3.up * (radius + 0.5f), "OPT v0", SkillColor, 3f);

        durationRoutine = StartSkillRoutine(DurationRoutine());
    }

    private void OnTeamDealtDamage(Character dealer, float amount)
    {
        if (stacks >= maxStacks || !SkillTargets.IsPartyMember(dealer))
            return;

        stacks++;
        ApplyBonuses();

        float t = maxStacks > 0 ? stacks / (float)maxStacks : 1f;
        float radius = Owner != null ? SkillVFX.GetVisualRadius(Owner.transform) : 0.5f;

        if (core != null) core.Width = Mathf.Lerp(0.05f, 0.12f, t);
        if (loop != null) loop.Radius = radius * Mathf.Lerp(1.5f, 2.2f, t);
        if (label != null) label.SetText(stacks >= maxStacks ? "OPT MAX" : $"OPT v{stacks}");

        SkillVFX.Ring(TeamAnchor.position, SkillColor, radius, radius * 2f, 0.2f, 0.035f, 6, 1, 0f);

        if (stacks >= maxStacks)
        {
            SkillVFX.Sparks(TeamAnchor.position, SkillColor, 20, 6f, 0.4f);
            CombatFeel.Shake(0.12f, 0.15f);
        }
    }

    private void ApplyBonuses()
    {
        foreach (var member in SkillTargets.Party)
        {
            if (member == null)
                continue;

            ApplyAttack(member, this, member.AttackPower * damageMultiplierPerStack * stacks);
            ApplyFireInterval(member, this, 1f / (1f + speedMultiplierPerStack * stacks));
        }
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSeconds(duration);

        durationRoutine = null;
        EndOptimization();
    }

    private void EndOptimization()
    {
        if (listening)
        {
            CombatEventBus.OnTeamDealtDamage -= OnTeamDealtDamage;
            listening = false;
        }

        ClearPartyModifiers(this);
        stacks = 0;

        if (core != null) core.Stop();
        if (loop != null) loop.Stop();
        if (label != null) label.Stop();

        core = null;
        loop = null;
        label = null;
    }

    public override void Cancel()
    {
        StopSkillRoutine(ref durationRoutine);
        EndOptimization();
    }
}
