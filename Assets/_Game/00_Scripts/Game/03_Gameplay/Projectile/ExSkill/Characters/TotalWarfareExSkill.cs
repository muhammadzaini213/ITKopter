using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TotalWarfareExSkill : ExSkill
{
    [Header("Opening AOE Settings")]
    [SerializeField, Min(0f)]
    private float aoeDamageMultiplier = 1.5f;

    [SerializeField, Min(0f)]
    private float aoeRadius = 10f;

    [SerializeField]
    private LayerMask enemyMask;

    [Header("Focus Settings")]
    [SerializeField, Min(0f)]
    private int focusProjectiles = 6;

    [SerializeField, Min(0f)]
    private float focusDamageMultiplier = 1f;

    [SerializeField, Min(0f)]
    private float focusInterval = 0.3f;

    [Header("Finale Settings")]
    [SerializeField, Min(0f)]
    private float finaleDamageMultiplier = 3f;

    [SerializeField, Min(0f)]
    private float finaleRadius = 6f;

    private int successfulHits;
    private Health highestHpTarget;

    public override void Activate()
    {
        if (!CanUse()) return;

        successfulHits = 0;
        highestHpTarget = FindHighestHpEnemy();

        FireOpeningAOE();
        StartFocusBarrage();
    }

    private Health FindHighestHpEnemy()
    {
        Health best = null;
        float bestHealth = -1f;

        var enemies = Object.FindObjectsByType<Health>(FindObjectsSortMode.None);

        foreach (var health in enemies)
        {
            if (health == null || health.IsDead)
                continue;

            if (health.CurrentHealth > bestHealth)
            {
                bestHealth = health.CurrentHealth;
                best = health;
            }
        }

        return best;
    }

    private void FireOpeningAOE()
    {
        Vector2 center = Owner.transform.position;
        var hits = Physics2D.OverlapCircleAll(center, aoeRadius, enemyMask);

        float damage = Owner.EffectiveAttackPower * aoeDamageMultiplier;

        foreach (var hit in hits)
        {
            if (hit.transform == null)
                continue;

            Health health = hit.GetComponentInParent<Health>();
            if (health == null)
                continue;

            health.TakeDamage(damage);

            if (health == highestHpTarget)
                successfulHits++;
        }
    }

    private void StartFocusBarrage()
    {
        StartCoroutine(BarrageRoutine());
    }

    private IEnumerator BarrageRoutine()
    {
        for (int i = 0; i < focusProjectiles; i++)
        {
            if (highestHpTarget == null || highestHpTarget.IsDead)
                break;

            FireFocusProjectile();

            yield return new WaitForSeconds(focusInterval);
        }

        FireFinale();
    }

    private void FireFocusProjectile()
    {
        if (highestHpTarget == null)
            return;

        Vector2 direction = ((Vector2)highestHpTarget.transform.position - (Vector2)Owner.transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        var temp = new GameObject("TotalWarfareFirePoint");
        temp.transform.position = Owner.transform.position;
        temp.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        var bulletSpawner = Owner.GetComponentInChildren<BulletSpawner>(true);
        if (bulletSpawner != null)
        {
            bulletSpawner.Fire(
                temp.transform,
                Owner.BulletSpeed,
                Owner.EffectiveAttackPower * focusDamageMultiplier);
        }

        Destroy(temp);
    }

    private void FireFinale()
    {
        Vector2 center = Owner.transform.position;
        var hits = Physics2D.OverlapCircleAll(center, finaleRadius, enemyMask);

        float bonus = successfulHits * Owner.EffectiveAttackPower * finaleDamageMultiplier;

        foreach (var hit in hits)
        {
            if (hit.transform == null)
                continue;

            Health health = hit.GetComponentInParent<Health>();
            if (health == null)
                continue;

            health.TakeDamage(bonus);
        }
    }

    public void RegisterHit(Health target)
    {
        if (target == highestHpTarget)
            successfulHits++;
    }

    public override void Cancel()
    {
        StopAllCoroutines();
    }

    private void OnDisable()
    {
        Cancel();
    }
}
