using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private int stacks;
    private Coroutine durationRoutine;

    public int Stacks => stacks;

    public override void Activate()
    {
        if (!CanUse()) return;

        stacks = 0;
        ApplyBonuses();

        Projectile.OnHit += OnProjectileHit;

        if (durationRoutine != null)
            StopCoroutine(durationRoutine);

        durationRoutine = StartCoroutine(DurationRoutine());
    }

    private void OnProjectileHit(Character owner, Health target, float damage, bool wasKilled)
    {
        if (owner != Owner)
            return;

        AddStack();
    }

    public void AddStack()
    {
        if (stacks >= maxStacks)
            return;

        stacks++;
        ApplyBonuses();
    }

    private void ApplyBonuses()
    {
        Owner.ClearAttackPowerBonus();
        Owner.ClearDefensePowerBonus();

        float attackBonus = Owner.AttackPower * damageMultiplierPerStack * stacks;
        Owner.AddAttackPowerBonus(attackBonus);

        float defensePenalty = Owner.DefensePower * defenseReductionMultiplierPerStack * stacks;
        Owner.AddDefensePowerBonus(-defensePenalty);

        var shooter = Owner.GetComponentInChildren<PlayerAutoShooter>(true);
        if (shooter != null)
            shooter.FireIntervalMultiplier = 1f / (1f + speedMultiplierPerStack * stacks);
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSeconds(duration);

        EndViral();
    }

    private void EndViral()
    {
        Owner.ClearAttackPowerBonus();
        Owner.ClearDefensePowerBonus();

        var shooter = Owner.GetComponentInChildren<PlayerAutoShooter>(true);
        if (shooter != null)
            shooter.FireIntervalMultiplier = 1f;

        // Crash: slow attack speed.
        if (shooter != null)
            shooter.FireIntervalMultiplier = 1f / crashSlowMultiplier;

        if (durationRoutine != null)
        {
            StopCoroutine(durationRoutine);
            durationRoutine = null;
        }

        StartCoroutine(CrashRoutine());
    }

    private IEnumerator CrashRoutine()
    {
        yield return new WaitForSeconds(crashDuration);

        var shooter = Owner.GetComponentInChildren<PlayerAutoShooter>(true);
        if (shooter != null)
            shooter.FireIntervalMultiplier = 1f;

        stacks = 0;
    }

    public override void Cancel()
    {
        Projectile.OnHit -= OnProjectileHit;

        if (durationRoutine != null)
        {
            StopCoroutine(durationRoutine);
            durationRoutine = null;
        }

        Owner.ClearAttackPowerBonus();
        Owner.ClearDefensePowerBonus();

        var shooter = Owner.GetComponentInChildren<PlayerAutoShooter>(true);
        if (shooter != null)
            shooter.FireIntervalMultiplier = 1f;

        stacks = 0;
    }

    private void OnDisable()
    {
        Cancel();
    }
}
