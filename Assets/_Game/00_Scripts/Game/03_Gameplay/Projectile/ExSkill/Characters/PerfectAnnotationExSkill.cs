using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Noz — Perfect Annotation.
// Annotates every enemy on screen. While the annotations last, Noz's allies
// hit harder, and each annotated enemy the party kills heals the allies.
public class PerfectAnnotationExSkill : ExSkill
{
    [Header("Mark Settings")]
    [SerializeField, Min(0f)]
    private float annotatedDuration = 8f;

    [SerializeField]
    private string annotatedEffectId = "Annotated";

    [SerializeField, Min(0f)]
    private float teamDamageBonusMultiplier = 0.5f;

    [Header("Heal Settings")]
    [SerializeField, Min(0f)]
    private float healOnMarkedKillMultiplier = 0.25f;

    private static readonly Color HealColor = new Color(0.4f, 1f, 0.5f);

    private readonly HashSet<Health> annotatedEnemies = new HashSet<Health>();
    private readonly Dictionary<Health, VfxRing> markers = new Dictionary<Health, VfxRing>();
    private Coroutine durationRoutine;
    private bool listening;
    private VfxRing aura;

    protected override Color DefaultColor => new Color(1f, 0.95f, 0.55f);

    protected override string[] DefaultActivationLines => new[] { "Annotating every weakness.", "Marked and noted. Take them out!" };

    protected override bool PersistsWhenBenched => true;

    public override string Describe(float attack, float defense)
    {
        return $"Annotates every enemy on screen for {Seconds(annotatedDuration)}.\n\n" +
               $"While active, Noz's allies gain {AtkPct(teamDamageBonusMultiplier, attack)} bonus attack (based on Noz's ATK).\n\n" +
               $"Each annotated enemy the team defeats heals Noz's allies for {AtkPct(healOnMarkedKillMultiplier, attack)}.\n\n" +
               "Keeps working while Noz is benched.";
    }

    public override void Activate()
    {
        if (!CanUse())
            return;

        Cancel();

        MarkAllEnemies();
        ApplyTeamDamageBuff();

        // Enemies have no Character component, so the old Character-based
        // kill event never fired — use the Health-based one.
        CombatEventBus.OnEnemyKilled += HandleEnemyKilled;
        listening = true;

        float radius = SkillVFX.GetVisualRadius(Owner.transform);
        aura = SkillVFX.Aura(TeamAnchor, SkillColor.WithAlpha(0.6f), radius * 1.4f, 0.04f, annotatedDuration, 0.04f, 4, 1, 45f);

        durationRoutine = StartSkillRoutine(DurationRoutine());
    }

    private void MarkAllEnemies()
    {
        Vector3 origin = Owner.transform.position;
        Color color = SkillColor;
        int count = 0;

        // Enemies only — the old scan also matched the party's own Health.
        foreach (var health in SkillTargets.AliveEnemies())
        {
            SkillTargets.AddStatus(health, annotatedEffectId, annotatedDuration);
            annotatedEnemies.Add(health);

            markers[health] = SkillVFX.Marker(health, color, annotatedDuration, 4, 1, 0f);
            SkillVFX.Beam(origin, health.transform.position, color.WithAlpha(0.8f), 0.04f, 0.25f, 0.02f);
            SkillVFX.Sparks(health.transform.position, color, 6, 3f, 0.25f, 0.08f);
            count++;
        }

        SkillVFX.Ring(origin, color, 0.5f, 12f, 0.6f, 0.08f, 4, 1, 0f);

        if (count > 0)
            SkillVFX.Text(origin + Vector3.up * 1.6f, $"{count} ANNOTATED", color, 3.5f, 1f);
    }

    private void ApplyTeamDamageBuff()
    {
        float bonus = Owner.EffectiveAttackPower * teamDamageBonusMultiplier;

        foreach (var member in SkillTargets.Party)
        {
            if (member == null || member == Owner)
                continue;

            ApplyAttack(member, this, bonus);
        }
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSeconds(annotatedDuration);

        durationRoutine = null;
        ClearAnnotations();
    }

    private void HandleEnemyKilled(Character killer, Health enemy)
    {
        if (!SkillTargets.IsPartyMember(killer) || enemy == null)
            return;

        if (!annotatedEnemies.Remove(enemy))
            return;

        if (markers.TryGetValue(enemy, out var marker))
        {
            if (marker != null) marker.Stop();
            markers.Remove(enemy);
        }

        // Heal line from the annotated kill back to the party.
        SkillVFX.Beam(enemy.transform.position, TeamAnchor.position, HealColor, 0.06f, 0.3f, 0.1f);
        SkillVFX.Sparks(TeamAnchor.position, HealColor, 10, 3f, 0.4f);

        HealParty();
    }

    private void HealParty()
    {
        if (Owner == null)
            return;

        float heal = Owner.EffectiveAttackPower * healOnMarkedKillMultiplier;

        foreach (var member in SkillTargets.Party)
        {
            if (member == null || member == Owner)
                continue;

            Health health = member.GetComponent<Health>();
            if (health != null)
                health.Heal(heal);
        }
    }

    private void ClearAnnotations()
    {
        if (listening)
        {
            CombatEventBus.OnEnemyKilled -= HandleEnemyKilled;
            listening = false;
        }

        // Only remove this skill's buff (the old version wiped every
        // character's attack bonus, including other skills').
        ClearPartyModifiers(this);

        foreach (var health in annotatedEnemies)
            SkillTargets.RemoveStatus(health, annotatedEffectId);

        foreach (var marker in markers.Values)
            if (marker != null) marker.Stop();

        annotatedEnemies.Clear();
        markers.Clear();

        if (aura != null) aura.Stop();
        aura = null;
    }

    public override void Cancel()
    {
        StopSkillRoutine(ref durationRoutine);
        ClearAnnotations();
    }
}
