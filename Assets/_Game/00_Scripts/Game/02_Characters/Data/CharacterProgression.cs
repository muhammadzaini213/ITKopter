
using System;
using UnityEngine;

[Serializable]
public class CharacterProgression
{
    private const int BaseExpRequired = 100;
    private const double ExpMultiplier = 1.5;

    private const int BaseAffectionRequired = 100;
    private const double AffectionMultiplier = 2.0;

    [SerializeField, Min(0)] private int totalExp;
    [SerializeField, Min(0)] private int affectionPoints;

    public int TotalExp => totalExp;
    public int AffectionPoints => affectionPoints;

    public int Level => CalculateLevel(
        totalExp, BaseExpRequired, ExpMultiplier);

    public int CurrentLevelExp => GetCurrentLevelPoints(
        totalExp, BaseExpRequired, ExpMultiplier);

    public int RequiredLevelExp => GetRequiredForLevel(
        Level, BaseExpRequired, ExpMultiplier);

    public int AffectionLevel => CalculateLevel(
        affectionPoints, BaseAffectionRequired, AffectionMultiplier);

    public int CurrentAffection => GetCurrentLevelPoints(
        affectionPoints, BaseAffectionRequired, AffectionMultiplier);

    public int RequiredAffection =>
        GetRequiredForLevel(
            AffectionLevel,
            BaseAffectionRequired,
            AffectionMultiplier);

    public CharacterProgression()
    {
    }

    public CharacterProgression(int exp, int affection)
    {
        totalExp = Mathf.Max(0, exp);
        affectionPoints = Mathf.Max(0, affection);
    }

    public void AddExp(int amount)
    {
        if (amount <= 0)
            return;

        totalExp = (int)Math.Min(
            (long)totalExp + amount,
            int.MaxValue);
    }

    public void AddAffection(int amount)
    {
        if (amount <= 0)
            return;

        affectionPoints = (int)Math.Min(
            (long)affectionPoints + amount,
            int.MaxValue);
    }

    public void SetProgress(int exp, int affection)
    {
        totalExp = Mathf.Max(0, exp);
        affectionPoints = Mathf.Max(0, affection);
    }

    private static int CalculateLevel(
        int totalPoints,
        int baseRequired,
        double multiplier)
    {
        int level = 1;
        long remaining = totalPoints;
        long required = baseRequired;

        while (remaining >= required && level < int.MaxValue)
        {
            remaining -= required;
            level++;

            required = GetNextRequired(required, multiplier);
        }

        return level;
    }

    private static int GetCurrentLevelPoints(
        int totalPoints,
        int baseRequired,
        double multiplier)
    {
        long remaining = totalPoints;
        long required = baseRequired;

        while (remaining >= required)
        {
            remaining -= required;
            required = GetNextRequired(required, multiplier);
        }

        return (int)remaining;
    }

    private static int GetRequiredForLevel(
        int level,
        int baseRequired,
        double multiplier)
    {
        long required = baseRequired;

        for (int currentLevel = 1;
             currentLevel < level;
             currentLevel++)
        {
            required = GetNextRequired(required, multiplier);
        }

        return (int)required;
    }

    private static long GetNextRequired(
        long currentRequired,
        double multiplier)
    {
        double next = Math.Ceiling(currentRequired * multiplier);

        if (next >= int.MaxValue)
            return int.MaxValue;

        return Math.Max(1L, (long)next);
    }
}
