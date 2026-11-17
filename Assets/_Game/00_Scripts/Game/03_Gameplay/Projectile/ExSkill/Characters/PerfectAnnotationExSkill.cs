using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private readonly Dictionary<Character, float> annotatedEnemies =
        new Dictionary<Character, float>();

    private Coroutine durationRoutine;

    public override void Activate()
    {
        if (!CanUse()) return;

        MarkAllEnemies();
        ApplyTeamDamageBuff();

        CombatEventBus.OnEnemyDiedMarked += HandleEnemyDied;

        StartDuration();
    }

    private void MarkAllEnemies()
    {
        var enemies = UnityEngine.Object.FindObjectsByType<Health>(FindObjectsSortMode.None);

        foreach (var health in enemies)
        {
            if (health == null || health.IsDead)
                continue;

            Character enemy = health.GetComponent<Character>();
            if (enemy == null)
                continue;

            StatusEffectSystem.Instance.Add(
                health.gameObject,
                annotatedEffectId,
                annotatedDuration,
                stacks: 1,
                refreshOnReapply: true);

            annotatedEnemies[enemy] = annotatedDuration;
        }
    }

    private void ApplyTeamDamageBuff()
    {
        foreach (var character in GameObject.FindObjectsByType<Character>(FindObjectsSortMode.None))
        {
            if (character == Owner)
                continue;

            character.AddAttackPowerBonus(Owner.EffectiveAttackPower * teamDamageBonusMultiplier);
        }
    }

    private void StartDuration()
    {
        if (durationRoutine != null)
            StopCoroutine(durationRoutine);

        durationRoutine = StartCoroutine(DurationRoutine());
    }

    private IEnumerator DurationRoutine()
    {
        yield return new WaitForSeconds(annotatedDuration);

        ClearAnnotations();
    }

    public void HandleEnemyDied(Character killer, Character enemy)
    {
        if (killer == null || killer != Owner && !IsPartyMember(killer))
            return;

        if (!annotatedEnemies.ContainsKey(enemy))
            return;

        HealParty();
    }

    private void HealParty()
    {
        float heal = Owner.EffectiveAttackPower * healOnMarkedKillMultiplier;

        foreach (var character in GameObject.FindObjectsByType<Character>(FindObjectsSortMode.None))
        {
            if (character == null || character == Owner)
                continue;

            Health health = character.GetComponent<Health>();
            health?.Heal(heal);
        }
    }

    private bool IsPartyMember(Character character)
    {
        foreach (var c in GameObject.FindObjectsByType<Character>(FindObjectsSortMode.None))
        {
            if (c == character)
                return true;
        }

        return false;
    }

    private void ClearAnnotations()
    {
        foreach (var character in UnityEngine.Object.FindObjectsByType<Character>(FindObjectsSortMode.None))
        {
            if (character == null)
                continue;

            character.ClearAttackPowerBonus();
        }

        foreach (var kvp in annotatedEnemies)
        {
            if (kvp.Key == null)
                continue;

            Health health = kvp.Key.GetComponent<Health>();
            StatusEffectSystem.Instance.Remove(health?.gameObject, annotatedEffectId);
        }

        annotatedEnemies.Clear();
    }

    public override void Cancel()
    {
        CombatEventBus.OnEnemyDiedMarked -= HandleEnemyDied;

        if (durationRoutine != null)
        {
            StopCoroutine(durationRoutine);
            durationRoutine = null;
        }

        ClearAnnotations();
    }

    private void OnDisable()
    {
        Cancel();
    }
}
