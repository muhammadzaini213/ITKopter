using UnityEngine;

/// <summary>
/// Shared rarity colours and labels for UI (inventory slots, character detail, etc.).
/// Matches the gacha reveal's default rarity flair colours.
/// </summary>
public static class RarityStyle
{
    public static readonly Color N = new Color(0.85f, 0.85f, 0.85f, 1f);
    public static readonly Color R = new Color(0.55f, 0.95f, 1f, 1f);
    public static readonly Color SR = new Color(0.75f, 0.5f, 1f, 1f);
    public static readonly Color UR = new Color(1f, 0.82f, 0.25f, 1f);

    public static Color GetColor(RewardRarity rarity)
    {
        switch (rarity)
        {
            case RewardRarity.UR: return UR;
            case RewardRarity.SR: return SR;
            case RewardRarity.R: return R;
            default: return N;
        }
    }

    public static string GetFullName(RewardRarity rarity)
    {
        switch (rarity)
        {
            case RewardRarity.UR: return "ULTRA RARE";
            case RewardRarity.SR: return "SUPER RARE";
            case RewardRarity.R: return "RARE";
            default: return "NORMAL";
        }
    }
}
