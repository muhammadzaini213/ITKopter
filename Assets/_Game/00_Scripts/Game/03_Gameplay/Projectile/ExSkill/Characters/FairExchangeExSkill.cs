using System;
using System.Collections;
using UnityEngine;

public class FairExchangeExSkill : ExSkill
{
    [Header("Defense Settings")]
    [SerializeField, Min(0f)]
    private float defenseShieldMultiplier = 4f;

    [SerializeField, Min(0f)]
    private float defenseDuration = 8f;

    [SerializeField, Min(0f)]
    private float offensePenaltyMultiplier = 0.8f;

    [Header("Attack Settings")]
    [SerializeField, Min(0f)]
    private float attackDamageMultiplier = 3f;

    [SerializeField, Min(0f)]
    private float attackSpeedMultiplier = 2f;

    [SerializeField, Min(0f)]
    private float attackDuration = 8f;

    [SerializeField, Min(0f)]
    private float defensePenaltyMultiplier = 0.6f;

    private TradyExchangeChoiceUI choiceUI;
    private Coroutine durationRoutine;

    private bool choiceMade;

    public override void Activate()
    {
        if (!CanUse()) return;

        choiceUI = UnityEngine.Object.FindFirstObjectByType<TradyExchangeChoiceUI>();

        if (choiceUI == null) return;

        choiceMade = false;

        choiceUI.Show(
            Owner,
            Mathf.Max(defenseDuration, attackDuration),
            OnDefenseChosen,
            OnAttackChosen);
    }

    private void OnDefenseChosen()
    {
        choiceMade = true;

        float shield = Owner.EffectiveDefensePower * defenseShieldMultiplier;

        var shieldGo = new GameObject("FairExchangeDefenseShield");
        shieldGo.transform.position = Owner.transform.position;

        var shieldComp = shieldGo.AddComponent<Shield>();
        shieldComp.Set(Mathf.Max(1f, shield));
        Owner.GetComponent<Health>().AssignShield(shieldComp);

        Owner.AddAttackPowerBonus(-Owner.EffectiveAttackPower * offensePenaltyMultiplier);

        StartDefenseTimer(shieldComp);
    }

    private void StartDefenseTimer(Shield shieldComp)
    {
        if (durationRoutine != null)
            StopCoroutine(durationRoutine);

        durationRoutine = StartCoroutine(DefenseRoutine(shieldComp));
    }

    private IEnumerator DefenseRoutine(Shield shieldComp)
    {
        yield return new WaitForSeconds(defenseDuration);

        if (shieldComp != null)
            shieldComp.Break();

        Owner.GetComponent<Health>().RemoveShield();
        Owner.ClearAttackPowerBonus();
    }

    private void OnAttackChosen()
    {
        choiceMade = true;

        Owner.AddAttackPowerBonus(Owner.EffectiveAttackPower * attackDamageMultiplier);

        var shooter = Owner.GetComponentInChildren<PlayerAutoShooter>(true);
        if (shooter != null)
            shooter.FireIntervalMultiplier = 1f / attackSpeedMultiplier;

        Owner.AddDefensePowerBonus(-Owner.EffectiveDefensePower * (1f - defensePenaltyMultiplier));

        if (durationRoutine != null)
            StopCoroutine(durationRoutine);

        durationRoutine = StartCoroutine(AttackRoutine(shooter));
    }

    private IEnumerator AttackRoutine(PlayerAutoShooter shooter)
    {
        yield return new WaitForSeconds(attackDuration);

        Owner.ClearAttackPowerBonus();
        Owner.ClearDefensePowerBonus();

        if (shooter != null)
            shooter.FireIntervalMultiplier = 1f;
    }

    public override void Cancel()
    {
        if (durationRoutine != null)
        {
            StopCoroutine(durationRoutine);
            durationRoutine = null;
        }

        if (!choiceMade)
            return;

        Owner.ClearAttackPowerBonus();
        Owner.ClearDefensePowerBonus();

        var health = Owner.GetComponent<Health>();
        if (health != null)
            health.RemoveShield();

        var shooter = Owner.GetComponentInChildren<PlayerAutoShooter>(true);
        if (shooter != null)
            shooter.FireIntervalMultiplier = 1f;
    }

    private void OnDisable()
    {
        Cancel();
    }
}
