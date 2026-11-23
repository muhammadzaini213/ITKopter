using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public override void Activate()
    {
        if (!CanUse())
        {
            Debug.LogWarning("[SignalOverloadExSkill] Cannot activate.", this);
            return;
        }

        FireWave();
        StartBurstSequence();
    }

    private void FireWave()
    {
        Vector2 center = Owner.transform.position;
        var hits = Physics2D.OverlapCircleAll(center, waveRadius, enemyMask);

        Debug.Log($"[SignalOverloadExSkill] Wave hit {hits.Length} enemy(ies).", this);

        float damage = Owner.EffectiveAttackPower * waveDamageMultiplier;

        foreach (var hit in hits)
        {
            if (hit.transform == null)
                continue;

            Health health = hit.GetComponentInParent<Health>();
            if (health == null || markedEnemies.Contains(health))
                continue;

            CounterMechanics.Damage(Owner, health, damage);

            StatusEffectSystem.Instance.Add(
                health.gameObject,
                connectedEffectId,
                connectedDuration,
                stacks: 1,
                refreshOnReapply: true);

            markedEnemies.Add(health);
        }
    }

    private void StartBurstSequence()
    {
        if (burstRoutine != null)
            StopCoroutine(burstRoutine);

        burstRoutine = StartCoroutine(BurstRoutine());
    }

    private IEnumerator BurstRoutine()
    {
        yield return new WaitForSeconds(burstDelay);

        if (!markedEnemies.Contains(null))
        {
            var alive = new List<Health>(markedEnemies);
            alive.RemoveAll(h => h == null || h.IsDead);

            int targetCount = alive.Count;

            if (targetCount == 0)
                yield break;

            float totalDamage = Owner.EffectiveAttackPower * burstDamageMultiplier;
            float damagePerTarget = totalDamage / targetCount;

            Debug.Log($"[SignalOverloadExSkill] Burst: {targetCount} target(s), {damagePerTarget:F1} dmg each.", this);

            foreach (var health in alive)
            {
                CounterMechanics.Damage(Owner, health, damagePerTarget);
            }
        }

        markedEnemies.Clear();
        burstRoutine = null;
    }

    public override void Cancel()
    {
        if (burstRoutine != null)
        {
            StopCoroutine(burstRoutine);
            burstRoutine = null;
        }

        markedEnemies.Clear();
    }

    private void OnDisable()
    {
        Cancel();
    }
}
