/// <summary>One row of the gacha rates popup.</summary>
public readonly struct GachaRateEntry
{
    public readonly RewardData Reward;
    public readonly RewardRarity Rarity;
    /// <summary>Chance per pull, in percent (0-100).</summary>
    public readonly float Percent;
    public readonly bool IsFeatured;
    /// <summary>Amount given when this entry is pulled (inclusive range).</summary>
    public readonly int MinAmount;
    public readonly int MaxAmount;

    public GachaRateEntry(RewardData reward, RewardRarity rarity, float percent, bool isFeatured, int minAmount = 1, int maxAmount = 1)
    {
        Reward = reward;
        Rarity = rarity;
        Percent = percent;
        IsFeatured = isFeatured;
        MinAmount = minAmount;
        MaxAmount = maxAmount;
    }
}
