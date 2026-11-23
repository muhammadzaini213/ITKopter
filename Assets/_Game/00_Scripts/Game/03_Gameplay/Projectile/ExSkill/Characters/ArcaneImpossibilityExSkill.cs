using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private readonly List<Health> enchantedEnemies = new List<Health>();

    public override void Activate()
    {
        if (!CanUse()) return;

        SpawnMagicCircle();

        CombatEventBus.OnEnemyDiedMarked += OnEnemyDiedMarked;

        StartExplosionSequence();
    }

    private void OnEnemyDiedMarked(Character killer, Character enemy)
    {
        if (killer != Owner)
            return;

        if (!enchantedEnemies.Exists(h => h != null && h.GetComponent<Character>() == enemy))
            return;

        DetonateNearestEnchanted();
    }

    private void SpawnMagicCircle()
    {
        Vector2 center = Owner.transform.position;

        for (int i = 0; i < circleSpawnCount; i++)
        {
            float angle = (i / (float)circleSpawnCount) * 360f;
            Vector2 offset = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)) * circleRadius;

            var hits = Physics2D.OverlapCircleAll(center + offset, 1f, enemyMask);

            foreach (var hit in hits)
            {
                if (hit.transform == null)
                    continue;

                Health health = hit.GetComponentInParent<Health>();
                if (health == null || enchantedEnemies.Contains(health))
                    continue;

                CounterMechanics.Damage(
                    Owner, health,
                    Owner.EffectiveAttackPower * markDamageMultiplier);

                StatusEffectSystem.Instance.Add(
                    health.gameObject,
                    enchantedEffectId,
                    enchantedDuration,
                    stacks: 1,
                    refreshOnReapply: true);

                enchantedEnemies.Add(health);
            }
        }
    }

    private void StartExplosionSequence()
    {
        StartCoroutine(ExplosionRoutine());
    }

    private IEnumerator ExplosionRoutine()
    {
        yield return new WaitForSeconds(explosionDelay);

        var alive = new List<Health>(enchantedEnemies);
        alive.RemoveAll(h => h == null || h.IsDead);

        if (alive.Count == 0)
        {
            enchantedEnemies.Clear();
            yield break;
        }

        Vector2 center = Owner.transform.position;
        float damage = Owner.EffectiveAttackPower * explosionDamageMultiplier;

        foreach (var health in alive)
        {
            if ((new Vector2(health.transform.position.x, health.transform.position.y) - center).sqrMagnitude <= explosionRadius * explosionRadius)
                CounterMechanics.Damage(Owner, health, damage);
        }

        enchantedEnemies.Clear();
    }

    public void OnEnchantedEnemyKilled(Character enemy)
    {
        if (enemy == null)
            return;

        if (!enchantedEnemies.Exists(h => h != null && h.GetComponent<Character>() == enemy))
            return;

        DetonateNearestEnchanted();
    }

    private void DetonateNearestEnchanted()
    {
        Vector2 center = Owner.transform.position;
        Health nearest = null;
        float nearestDistSqr = float.MaxValue;

        foreach (var health in enchantedEnemies)
        {
            if (health == null || health.IsDead)
                continue;

            float distSqr = (new Vector2(health.transform.position.x, health.transform.position.y) - center).sqrMagnitude;
            if (distSqr < nearestDistSqr)
            {
                nearestDistSqr = distSqr;
                nearest = health;
            }
        }

        if (nearest != null)
            CounterMechanics.Damage(
                Owner, nearest,
                Owner.EffectiveAttackPower * explosionDamageMultiplier);
    }

    public override void Cancel()
    {
        CombatEventBus.OnEnemyDiedMarked -= OnEnemyDiedMarked;
        StopAllCoroutines();
        enchantedEnemies.Clear();
    }

    private void OnDisable()
    {
        Cancel();
    }
}
