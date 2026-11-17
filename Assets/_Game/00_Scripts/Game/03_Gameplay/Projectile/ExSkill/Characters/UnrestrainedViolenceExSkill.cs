using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private float damageTakenBonus;
    private Coroutine durationRoutine;
    private Coroutine decayRoutine;

    public int Stacks => stacks;

    public override void Activate()
    {
        if (!CanUse()) return;

        stacks = 0;
        ApplyDamageTakenModifier(true);
        ApplyStatBonuses();

        Projectile.OnHit += OnProjectileHit;

        if (durationRoutine != null)
            StopCoroutine(durationRoutine);

        durationRoutine = StartCoroutine(DurationRoutine());
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

        stacks = Mathf.Min(maxStacks, stacks + 1);
        ApplyStatBonuses();

        if (decayRoutine != null)
            StopCoroutine(decayRoutine);

        decayRoutine = StartCoroutine(DecayRoutine());
    }

    private bool isActive;

    private IEnumerator DurationRoutine()
    {
        isActive = true;

        yield return new WaitForSeconds(duration);

        isActive = false;
        EndSkill();
    }

    private IEnumerator DecayRoutine()
    {
        yield return new WaitForSeconds(hitWindow);

        if (!isActive)
            yield break;

        stacks = Mathf.Max(0, stacks - 1);
        ApplyStatBonuses();

        if (stacks > 0)
        {
            decayRoutine = StartCoroutine(DecayRoutine());
        }
    }

    private void ApplyStatBonuses()
    {
        Owner.ClearAttackPowerBonus();

        float attackBonus = Owner.AttackPower * attackMultiplierPerStack * stacks;
        Owner.AddAttackPowerBonus(attackBonus);

        var shooter = Owner.GetComponentInChildren<PlayerAutoShooter>(true);
        if (shooter != null)
            shooter.FireIntervalMultiplier = 1f / (1f + attackSpeedMultiplierPerStack * stacks);
    }

    private void ApplyDamageTakenModifier(bool apply)
    {
        if (apply)
        {
            damageTakenBonus = damageTakenMultiplier - 1f;
            Owner.GetComponent<Health>().IncomingDamageMultiplier += damageTakenBonus;
        }
        else
        {
            Owner.GetComponent<Health>().IncomingDamageMultiplier -= damageTakenBonus;
            damageTakenBonus = 0f;
        }
    }

    private void EndSkill()
    {
        ApplyDamageTakenModifier(false);
        Owner.ClearAttackPowerBonus();

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

        if (decayRoutine != null)
        {
            StopCoroutine(decayRoutine);
            decayRoutine = null;
        }

        isActive = false;
        EndSkill();
    }

    private void OnDisable()
    {
        Cancel();
    }
}
