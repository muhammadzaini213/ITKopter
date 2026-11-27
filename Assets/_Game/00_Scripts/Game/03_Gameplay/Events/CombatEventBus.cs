using System;

public static class CombatEventBus
{
    /// <summary>A party member dealt real HP damage to an enemy (bullets and EX skills).</summary>
    public static event Action<Character, float> OnTeamDealtDamage;

    /// <summary>A party member landed the killing blow on an enemy.</summary>
    public static event Action<Character, Health> OnEnemyKilled;

    /// <summary>Legacy: only fires when the victim carries a Character component (enemies don't).</summary>
    public static event Action<Character, Character> OnEnemyDiedMarked;

    public static void InvokeTeamDealtDamage(Character dealer, float amount)
    {
        OnTeamDealtDamage?.Invoke(dealer, amount);
    }

    public static void InvokeEnemyKilled(Character killer, Health enemy)
    {
        OnEnemyKilled?.Invoke(killer, enemy);
    }

    public static void InvokeEnemyDiedMarked(Character killer, Character enemy)
    {
        OnEnemyDiedMarked?.Invoke(killer, enemy);
    }

    /// <summary>
    /// Shared reporting for any damage a party member applies to a target:
    /// raises team-damage and kill events for enemy targets only.
    /// </summary>
    public static void ReportDamage(Character attacker, Health target, float hpBefore, bool wasAliveBefore)
    {
        if (attacker == null || target == null)
            return;

        // Friendly targets (player characters) never count.
        if (target.GetComponent<Character>() != null)
            return;

        float dealt = hpBefore - target.CurrentHealth;

        if (dealt > 0f)
            InvokeTeamDealtDamage(attacker, dealt);

        if (wasAliveBefore && target.IsDead)
            InvokeEnemyKilled(attacker, target);
    }
}
