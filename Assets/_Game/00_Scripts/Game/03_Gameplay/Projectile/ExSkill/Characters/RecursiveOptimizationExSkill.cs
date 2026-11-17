using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private Coroutine durationRoutine;

    public int Stacks => stacks;

    public override void Activate()
    {
        if (!CanUse()) return;

        stacks = 0;
        ApplyBonuses();

        CombatEventBus.OnTeamDealtDamage += OnTeamDealtDamage;

        if (durationRoutine != null)
            StopCoroutine(durationRoutine);

        durationRoutine = StartCoroutine(DurationRoutine());
    }

    private void OnTeamDealtDamage(Character dealer, float amount)
    {
        if (dealer == null)
            return;

        if (!IsPartyMember(dealer))
            return;

        stacks = Mathf.Min(maxStacks, stacks + 1);
        ApplyBonuses();
    }

    private bool IsPartyMember(Character character)
    {
        foreach (var c in UnityEngine.Object.FindObjectsByType<Character>(FindObjectsSortMode.None))
        {
            if (c == character)
                return true;
        }

        return false;
    }

    private void ApplyBonuses()
    {
        foreach (var character in UnityEngine.Object.FindObjectsByType<Character>(FindObjectsSortMode.None))
        {
            if (character == null)
                continue;

            character.ClearAttackPowerBonus();

            float attackBonus = character.AttackPower * damageMultiplierPerStack * stacks;
            character.AddAttackPowerBonus(attackBonus);

            var shooter = character.GetComponentInChildren<PlayerAutoShooter>(true);
            if (shooter != null)
                shooter.FireIntervalMultiplier = 1f / (1f + speedMultiplierPerStack * stacks);
        }
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSeconds(duration);

        EndOptimization();
    }

    private void EndOptimization()
    {
        CombatEventBus.OnTeamDealtDamage -= OnTeamDealtDamage;

        foreach (var character in UnityEngine.Object.FindObjectsByType<Character>(FindObjectsSortMode.None))
        {
            if (character == null)
                continue;

            character.ClearAttackPowerBonus();

            var shooter = character.GetComponentInChildren<PlayerAutoShooter>(true);
            if (shooter != null)
                shooter.FireIntervalMultiplier = 1f;
        }

        stacks = 0;
    }

    public override void Cancel()
    {
        if (durationRoutine != null)
        {
            StopCoroutine(durationRoutine);
            durationRoutine = null;
        }

        CombatEventBus.OnTeamDealtDamage -= OnTeamDealtDamage;
        EndOptimization();
    }

    private void OnDisable()
    {
        Cancel();
    }
}
