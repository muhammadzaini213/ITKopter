using System.Collections;
using UnityEngine;

public class AllOrNothingExSkill : ExSkill
{
    [Header("Jackpot Settings")]
    [SerializeField, Min(0f)]
    private float cooldownReductionMultiplier = 0.5f;

    [Header("Lucky Seven Settings")]
    [SerializeField, Min(0f)]
    private float luckyDamageMultiplier = 2f;

    [SerializeField, Min(0f)]
    private float luckySpeedMultiplier = 2f;

    [Header("Bad Luck Settings")]
    [SerializeField, Min(0f)]
    private float badLuckCooldownPenalty = 3f;

    [Header("Duration")]
    [SerializeField, Min(0f)]
    private float effectDuration = 8f;

    private readonly System.Collections.Generic.List<PlayerAutoShooter> modifiedShooters =
        new System.Collections.Generic.List<PlayerAutoShooter>();

    private Coroutine restoreRoutine;

    public override void Activate()
    {
        if (!CanUse()) return;

        int roll = Random.Range(0, 3);
        ActivateEffect(roll);

        if (restoreRoutine != null)
            StopCoroutine(restoreRoutine);

        restoreRoutine = StartCoroutine(RestoreAfterDuration());
    }

    private void ActivateEffect(int effectIndex)
    {
        switch (effectIndex)
        {
            case 0:
                ApplyJackpot();
                break;
            case 1:
                ApplyLuckySeven();
                break;
            case 2:
                ApplyBadLuck();
                break;
        }
    }

    private void ApplyJackpot()
    {
        var lifeController = Object.FindFirstObjectByType<CharacterLifeController>();

        if (lifeController != null)
        {
            int count = lifeController.CharacterCount;

            for (int i = 0; i < count; i++)
            {
                if (lifeController.CanRevive(i))
                {
                    lifeController.Revive(i);
                    break;
                }
            }
        }

        foreach (var character in Object.FindObjectsByType<Character>(FindObjectsSortMode.None))
        {
            if (character == null)
                continue;

            var shooter = character.GetComponentInChildren<PlayerAutoShooter>(true);
            if (shooter == null || modifiedShooters.Contains(shooter))
                continue;

            modifiedShooters.Add(shooter);
            shooter.FireIntervalMultiplier = cooldownReductionMultiplier;
        }
    }

    private void ApplyLuckySeven()
    {
        foreach (var character in Object.FindObjectsByType<Character>(FindObjectsSortMode.None))
        {
            if (character == null)
                continue;

            character.AddAttackPowerBonus(character.AttackPower * luckyDamageMultiplier);

            var shooter = character.GetComponentInChildren<PlayerAutoShooter>(true);
            if (shooter == null || modifiedShooters.Contains(shooter))
                continue;

            modifiedShooters.Add(shooter);
            shooter.FireIntervalMultiplier = 1f / luckySpeedMultiplier;
        }
    }

    private void ApplyBadLuck()
    {
        foreach (var character in Object.FindObjectsByType<Character>(FindObjectsSortMode.None))
        {
            if (character == null)
                continue;

            var shooter = character.GetComponentInChildren<PlayerAutoShooter>(true);
            if (shooter == null || modifiedShooters.Contains(shooter))
                continue;

            modifiedShooters.Add(shooter);
            shooter.FireIntervalMultiplier *= badLuckCooldownPenalty;
        }
    }

    private IEnumerator RestoreAfterDuration()
    {
        yield return new WaitForSeconds(effectDuration);

        foreach (var character in Object.FindObjectsByType<Character>(FindObjectsSortMode.None))
        {
            if (character == null)
                continue;

            character.ClearAttackPowerBonus();

            var shooter = character.GetComponentInChildren<PlayerAutoShooter>(true);
            if (shooter != null)
                shooter.FireIntervalMultiplier = 1f;
        }

        modifiedShooters.Clear();
        restoreRoutine = null;
    }

    public override void Cancel()
    {
        if (restoreRoutine != null)
        {
            StopCoroutine(restoreRoutine);
            restoreRoutine = null;
        }

        foreach (var character in Object.FindObjectsByType<Character>(FindObjectsSortMode.None))
        {
            if (character == null)
                continue;

            character.ClearAttackPowerBonus();

            var shooter = character.GetComponentInChildren<PlayerAutoShooter>(true);
            if (shooter != null)
                shooter.FireIntervalMultiplier = 1f;
        }

        modifiedShooters.Clear();
    }

    private void OnDisable()
    {
        Cancel();
    }
}
