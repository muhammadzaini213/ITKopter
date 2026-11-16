using System.Collections;
using UnityEngine;

public class EmergencyRegulationExSkill : ExSkill
{
    [Header("Limit Settings")]
    [SerializeField, Min(1)]
    private int maxEnemyProjectiles = 3;

    [SerializeField, Min(0f)]
    private float duration = 8f;

    [Header("Damage Reduction Settings")]
    [SerializeField, Min(0f)]
    private float damageReductionMultiplier = 0.5f;

    private ProjectileLimiter limiter;
    private int savedMaxProjectiles;
    private float savedDefenseBonus;

    public override void Activate()
    {
        if (!CanUse()) return;

        ApplyLimit();
        ApplyDamageReduction();

        StartCoroutine(EndAfterDuration());
    }

    private void ApplyLimit()
    {
        limiter = Object.FindFirstObjectByType<ProjectileLimiter>();

        if (limiter == null)
        {
            var go = new GameObject("ReguProjectileLimiter");
            limiter = go.AddComponent<ProjectileLimiter>();
        }

        savedMaxProjectiles = limiter.MaxEnemyProjectiles;
        limiter.SetMaxEnemyProjectiles(maxEnemyProjectiles);
    }

    private void ApplyDamageReduction()
    {
        float reduction = Owner.EffectiveDefensePower * damageReductionMultiplier;
        Owner.AddDefensePowerBonus(reduction);
        savedDefenseBonus = reduction;
    }

    private IEnumerator EndAfterDuration()
    {
        yield return new WaitForSeconds(duration);
        EndSkill();
    }

    private void EndSkill()
    {
        Owner.ClearDefensePowerBonus();
        savedDefenseBonus = 0f;

        if (limiter != null)
        {
            limiter.SetMaxEnemyProjectiles(savedMaxProjectiles);
            limiter = null;
        }
    }

    public override void Cancel()
    {
        EndSkill();

        StopAllCoroutines();
    }

    private void OnDisable()
    {
        Cancel();
    }
}
