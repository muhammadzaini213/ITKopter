using UnityEngine;
public class CharacterProgression
{
    private int totalExp;
    private int affectionPoints;

    public int TotalExp => totalExp;
    public int AffectionPoints => affectionPoints;
    public int Level => CalculateLevel(totalExp);

    public void AddExp(int amount)
    {
        if (amount <= 0) return;
        totalExp += amount;
    }

    public void AddAffection(int amount)
    {
        if (amount <= 0) return;
        affectionPoints = Mathf.Max(0, affectionPoints + amount);
    }

    private int CalculateLevel(int exp)
    {
        int level = 1;
        int expToNextLevel = 100;

        while (exp >= expToNextLevel)
        {
            exp -= expToNextLevel;
            level++;
            expToNextLevel += 50;
        }

        return level;
    }
}