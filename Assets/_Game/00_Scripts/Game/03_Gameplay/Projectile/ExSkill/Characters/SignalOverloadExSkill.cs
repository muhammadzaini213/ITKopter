using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ei-Pi — Signal Overload.
// An electric pulse hits everything in range and "Connects" it to Ei-Pi
// with a live wire. After a short charge the network overloads: the burst
// damage is split across every still-alive connected enemy.
public class SignalOverloadExSkill : ExSkill
{
    [Header("Wave Settings")]
    [SerializeField, Min(0f)]
    private float waveDamageMultiplier = 1f;

    [SerializeField, Min(0f)]
    private float waveRadius = 8f;

    [SerializeField]
    private LayerMask enemyMask;

    [Header("Mark Settings")]
    [SerializeField, Min(0f)]
    private float connectedDuration = 6f;

    [SerializeField]
    private string connectedEffectId = "Connected";

    [Header("Delayed Burst Settings")]
    [SerializeField, Min(0f)]
    private float burstDelay = 2f;

    [SerializeField, Min(0f)]
    private float burstDamageMultiplier = 2f;

    private Coroutine burstRoutine;
    private readonly List<Health> markedEnemies = new List<Health>();
    private readonly List<VfxBeam> wires = new List<VfxBeam>();
    private readonly List<VfxRing> markers = new List<VfxRing>();

    protected override Color DefaultColor => new Color(0.3f, 0.95f, 1f);

    protected override string[] DefaultActivationLines => new[] { "Signal locked. Overloading the network!", "Everyone's connected... now burn." };

    // The overload still goes off if Ei-Pi is switched out mid-charge.
    protected override bool PersistsWhenBenched => true;

    public override string Describe(float attack, float defense)
    {
        return $"Sends an electric pulse out to {Num(waveRadius)} units. Every enemy hit takes {AtkPct(waveDamageMultiplier, attack)} damage and is <color=#4CF2FF>Connected</color> for {Seconds(connectedDuration)}.\n\n" +
               $"After {Seconds(burstDelay)} the network overloads: {AtkPct(burstDamageMultiplier, attack)} damage is split evenly across every enemy still connected.";
    }

    public override void Activate()
    {
        if (!CanUse())
            return;

        Cancel();

        FireWave();
        burstRoutine = StartSkillRoutine(BurstRoutine());
    }

    private void FireWave()
    {
        Vector2 center = Owner.transform.position;
        Color color = SkillColor;

        // Visual: fast double shockwave out to the real hit radius.
        SkillVFX.Ring(center, color, 0.3f, waveRadius, 0.4f, 0.22f);
        SkillVFX.Ring(center, Color.white.WithAlpha(0.7f), 0.2f, waveRadius * 0.85f, 0.32f, 0.06f, 12, 5, 240f);

        float damage = Owner.EffectiveAttackPower * waveDamageMultiplier;

        foreach (var health in SkillTargets.EnemiesInRadius(center, waveRadius))
        {
            if (markedEnemies.Contains(health))
                continue;

            CounterMechanics.Damage(Owner, health, damage);
            SkillTargets.AddStatus(health, connectedEffectId, connectedDuration);
            markedEnemies.Add(health);

            Vector3 pos = health.transform.position;
            SkillVFX.Sparks(pos, color, 10, 5f, 0.3f);
            SkillVFX.Beam(center, pos, Color.white, 0.12f, 0.12f, 0.25f);

            if (health.IsDead)
                continue;

            // Persistent wire + marker until the overload.
            wires.Add(SkillVFX.Beam(TeamAnchor, health.transform, color.WithAlpha(0.75f), 0.05f, -1f, 0.18f));
            markers.Add(SkillVFX.Marker(health, color, burstDelay + 0.5f, 4, 1, 180f));
        }

        if (markedEnemies.Count > 0)
            SkillVFX.Text(Owner.transform.position + Vector3.up * 1.6f, $"{markedEnemies.Count} CONNECTED", color, 3.5f, 0.9f);
    }

    private IEnumerator BurstRoutine()
    {
        // Charge-up: wires flicker brighter as the overload approaches.
        float elapsed = 0f;

        while (elapsed < burstDelay)
        {
            elapsed += Time.deltaTime;
            float t = burstDelay > 0f ? elapsed / burstDelay : 1f;

            foreach (var wire in wires)
            {
                if (wire != null)
                    wire.Color = Color.Lerp(SkillColor.WithAlpha(0.6f), Color.white, t * t);
            }

            yield return null;
        }

        // Destroyed or killed enemies just drop out of the split (this used
        // to abort the whole burst if any marked enemy had been destroyed).
        var alive = new List<Health>(markedEnemies);
        alive.RemoveAll(h => h == null || h.IsDead);

        ClearVisuals();

        if (alive.Count > 0 && Owner != null)
        {
            float damagePerTarget = Owner.EffectiveAttackPower * burstDamageMultiplier / alive.Count;
            Vector3 origin = Owner.transform.position;

            foreach (var health in alive)
            {
                Vector3 pos = health.transform.position;
                SkillVFX.Beam(origin, pos, Color.white, 0.2f, 0.18f, 0.35f);
                SkillVFX.Explosion(pos, SkillColor, 1.4f, withFeel: false);
                CounterMechanics.Damage(Owner, health, damagePerTarget);
            }

            CombatFeel.Flash(SkillColor.WithAlpha(0.2f), 0.15f);
            CombatFeel.HitStop(0.08f);
            CombatFeel.Shake(0.3f, 0.3f);
        }

        markedEnemies.Clear();
        burstRoutine = null;
    }

    private void ClearVisuals()
    {
        foreach (var wire in wires)
            if (wire != null) wire.Stop();

        foreach (var marker in markers)
            if (marker != null) marker.Stop();

        wires.Clear();
        markers.Clear();
    }

    public override void Cancel()
    {
        StopSkillRoutine(ref burstRoutine);
        ClearVisuals();
        markedEnemies.Clear();
    }
}
