using System;
using UnityEngine;

/// <summary>
/// Per-character progress: upgrade level (raised with materials, no EXP) and affection.
/// </summary>
[Serializable]
public class CharacterProgression
{
    private const int BaseAffectionRequired = 100;
    private const double AffectionMultiplier = 2.0;

    [SerializeField, Min(1)] private int level = 1;
    [SerializeField, Min(0)] private int affectionPoints;

    public int Level => Mathf.Max(1, level);
    public int AffectionPoints => affectionPoints;

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

    public CharacterProgression(int level, int affection)
    {
        this.level = Mathf.Max(1, level);
        affectionPoints = Mathf.Max(0, affection);
    }

    public void LevelUp()
    {
        if (Level < int.MaxValue)
            level = Level + 1;
    }

    public void AddAffection(int amount)
    {
        if (amount <= 0)
            return;

        affectionPoints = (int)Math.Min(
            (long)affectionPoints + amount,
            int.MaxValue);
    }

    public void SetProgress(int level, int affection)
    {
        this.level = Mathf.Max(1, level);
        affectionPoints = Mathf.Max(0, affection);
    }

    private static int CalculateLevel(
        int totalPoints,
        int baseRequired,
        double multiplier)
    {
        int result = 1;
        long remaining = totalPoints;
        long required = baseRequired;

        while (remaining >= required && result < int.MaxValue)
        {
            remaining -= required;
            result++;

            required = GetNextRequired(required, multiplier);
        }

        return result;
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
        int targetLevel,
        int baseRequired,
        double multiplier)
    {
        long required = baseRequired;

        for (int currentLevel = 1;
             currentLevel < targetLevel;
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
