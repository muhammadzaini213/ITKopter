using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Majik — Arcane Impossibility.
// A magic circle of runes appears around Majik; enemies touching a rune
// take damage and become Enchanted. Killing an Enchanted enemy chain-
// detonates the nearest other one; after the delay every Enchanted enemy
// still in range explodes.
public class ArcaneImpossibilityExSkill : ExSkill
{
    [Header("Circle Settings")]
    [SerializeField, Min(0f)]
    private float circleRadius = 5f;

    [SerializeField, Min(0f)]
    private float circleSpawnCount = 8;

    [SerializeField, Min(0f)]
    private float markDamageMultiplier = 1f;

    [SerializeField, Min(0f)]
    private float enchantedDuration = 6f;

    [SerializeField]
    private string enchantedEffectId = "Enchanted";

    [Header("Delayed Explosion Settings")]
    [SerializeField, Min(0f)]
    private float explosionDelay = 2f;

    [SerializeField, Min(0f)]
    private float explosionDamageMultiplier = 2f;

    [SerializeField, Min(0f)]
    private float explosionRadius = 6f;

    [SerializeField]
    private LayerMask enemyMask;

    [SerializeField, Min(0.1f), Tooltip("Hit radius of each rune on the circle.")]
    private float runeRadius = 1.2f;

    private readonly List<Health> enchantedEnemies = new List<Health>();
    private readonly Dictionary<Health, VfxRing> markers = new Dictionary<Health, VfxRing>();
    private Coroutine explosionRoutine;
    private VfxRing circleOuter;
    private VfxRing circleStar;
    private bool listening;

    protected override Color DefaultColor => new Color(0.72f, 0.38f, 1f);

    protected override string[] DefaultActivationLines => new[] { "Watch closely, this one's impossible.", "Abracadabra... and boom." };

    protected override bool PersistsWhenBenched => true;

    public override string Describe(float attack, float defense)
    {
        return $"Draws a magic circle (radius {Num(circleRadius)}) with {Num(circleSpawnCount)} runes. Enemies touching a rune take {AtkPct(markDamageMultiplier, attack)} damage and become <color=#B85CFF>Enchanted</color> for {Seconds(enchantedDuration)}.\n\n" +
               $"Killing an Enchanted enemy detonates the nearest other one for {AtkPct(explosionDamageMultiplier, attack)}.\n\n" +
               $"After {Seconds(explosionDelay)}, every Enchanted enemy within {Num(explosionRadius)} units explodes for {AtkPct(explosionDamageMultiplier, attack)}.";
    }

    public override void Activate()
    {
        if (!CanUse())
            return;

        Cancel();

        SpawnMagicCircle();

        CombatEventBus.OnEnemyKilled += OnEnemyKilled;
        listening = true;

        explosionRoutine = StartSkillRoutine(ExplosionRoutine());
    }

    private void SpawnMagicCircle()
    {
        Vector2 center = Owner.transform.position;
        Color color = SkillColor;
        int count = Mathf.Max(1, Mathf.RoundToInt(circleSpawnCount));

        // The circle itself: outer ring + rotating 8-point star, lingering
        // for the charge so the player can read the explosion timing.
        circleOuter = SkillVFX.Ring(center, color, circleRadius * 0.4f, circleRadius, explosionDelay + 0.3f, 0.12f);
        circleStar = SkillVFX.Ring(center, color.WithAlpha(0.7f), circleRadius * 0.9f, circleRadius * 0.95f, explosionDelay + 0.3f, 0.06f, 8, 3, 45f);

        float damage = Owner.EffectiveAttackPower * markDamageMultiplier;

        for (int i = 0; i < count; i++)
        {
            float angle = i / (float)count * Mathf.PI * 2f;
            Vector2 rune = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * circleRadius;

            SkillVFX.Ring(rune, color, 0.1f, runeRadius, 0.35f, 0.08f, 4, 1, 180f);
            SkillVFX.Sparks(rune, color, 6, 3f, 0.3f);

            foreach (var health in SkillTargets.EnemiesInRadius(rune, runeRadius))
            {
                if (enchantedEnemies.Contains(health))
                    continue;

                CounterMechanics.Damage(Owner, health, damage);
                SkillTargets.AddStatus(health, enchantedEffectId, enchantedDuration);
                enchantedEnemies.Add(health);

                if (!health.IsDead)
                    markers[health] = SkillVFX.Marker(health, color, enchantedDuration, 5, 2, 120f);
            }
        }
    }

    private void OnEnemyKilled(Character killer, Health enemy)
    {
        if (!SkillTargets.IsPartyMember(killer) || enemy == null)
            return;

        if (!enchantedEnemies.Contains(enemy))
            return;

        enchantedEnemies.Remove(enemy);
        DropMarker(enemy);

        // Chain reaction from the dead enemy to the nearest Enchanted one.
        Health next = SkillTargets.NearestEnemy(enemy.transform.position, enchantedEnemies);

        if (next == null || Owner == null)
            return;

        SkillVFX.Beam(enemy.transform.position, next.transform.position, SkillColor, 0.12f, 0.2f, 0.3f);
        SkillVFX.Explosion(next.transform.position, SkillColor, 1.6f, withFeel: false);
        CombatFeel.LightImpact();

        CounterMechanics.Damage(Owner, next, Owner.EffectiveAttackPower * explosionDamageMultiplier);
    }

    private IEnumerator ExplosionRoutine()
    {
        yield return new WaitForSeconds(explosionDelay);

        StopListening();

        var alive = new List<Health>(enchantedEnemies);
        alive.RemoveAll(h => h == null || h.IsDead);

        if (Owner != null && alive.Count > 0)
        {
            Vector2 center = TeamAnchor.position;
            float damage = Owner.EffectiveAttackPower * explosionDamageMultiplier;
            float sqr = explosionRadius * explosionRadius;
            bool any = false;

            foreach (var health in alive)
            {
                if (((Vector2)health.transform.position - center).sqrMagnitude > sqr)
                    continue;

                SkillVFX.Explosion(health.transform.position, SkillColor, 1.8f, withFeel: false);
                CounterMechanics.Damage(Owner, health, damage);
                any = true;
            }

            if (any)
            {
                SkillVFX.Ring(center, SkillColor, 0.5f, explosionRadius, 0.4f, 0.18f);
                CombatFeel.Flash(SkillColor.WithAlpha(0.22f), 0.18f);
                CombatFeel.HeavyImpact();
            }
        }

        ClearMarkers();
        enchantedEnemies.Clear();
        explosionRoutine = null;
    }

    private void DropMarker(Health enemy)
    {
        if (markers.TryGetValue(enemy, out var ring))
        {
            if (ring != null) ring.Stop();
            markers.Remove(enemy);
        }
    }

    private void ClearMarkers()
    {
        foreach (var ring in markers.Values)
            if (ring != null) ring.Stop();

        markers.Clear();
    }

    private void StopListening()
    {
        if (!listening)
            return;

        CombatEventBus.OnEnemyKilled -= OnEnemyKilled;
        listening = false;
    }

    public override void Cancel()
    {
        StopListening();
        StopSkillRoutine(ref explosionRoutine);
        ClearMarkers();

        if (circleOuter != null) circleOuter.Stop();
        if (circleStar != null) circleStar.Stop();

        enchantedEnemies.Clear();
    }
}
