using System;

public static class CombatEventBus
{
    public static event Action<Character, float> OnTeamDealtDamage;
    public static event Action<Character, Character> OnEnemyDiedMarked;

    public static void InvokeTeamDealtDamage(Character dealer, float amount)
    {
        OnTeamDealtDamage?.Invoke(dealer, amount);
    }

    public static void InvokeEnemyDiedMarked(Character killer, Character enemy)
    {
        OnEnemyDiedMarked?.Invoke(killer, enemy);
    }
}
