using System;
using System.Collections.Generic;

[Serializable]
public class GachaPullResult
{
    public RewardData Reward { get; }
    public bool WasPity { get; }

    /// <summary>How many of the reward this pull gives (from the entry's amount range; 1 otherwise).</summary>
    public int Amount { get; }

    /// <summary>Rarity the reward was rolled at (the banner entry's effective rarity).</summary>
    public RewardRarity Rarity { get; }

    /// <summary>True when the reward is a character the player already owned (converted to materials).</summary>
    public bool IsDuplicate { get; private set; }

    /// <summary>Materials a duplicate character was converted into.</summary>
    public IReadOnlyList<RewardAmount> ConvertedRewards => convertedRewards;

    private readonly List<RewardAmount> convertedRewards = new List<RewardAmount>();

    public GachaPullResult(RewardData reward, bool wasPity, int amount = 1, RewardRarity? rarity = null)
    {
        Reward = reward;
        WasPity = wasPity;
        Amount = Math.Max(1, amount);
        Rarity = rarity ?? (reward != null ? reward.Rarity : RewardRarity.N);
    }

    public void MarkDuplicate(IEnumerable<RewardAmount> converted)
    {
        IsDuplicate = true;
        convertedRewards.Clear();

        if (converted != null)
            convertedRewards.AddRange(converted);
    }
}

/// <summary>A reward together with how many were given.</summary>
public readonly struct RewardAmount
{
    public readonly RewardData Reward;
    public readonly int Amount;

    public RewardAmount(RewardData reward, int amount)
    {
        Reward = reward;
        Amount = amount;
    }
}
