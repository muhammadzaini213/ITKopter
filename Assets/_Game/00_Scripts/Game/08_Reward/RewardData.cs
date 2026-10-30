
using UnityEngine;

public abstract class RewardData : ScriptableObject
{
    [SerializeField] private RewardRarity rarity;
    [SerializeField] private Sprite icon;

    public RewardRarity Rarity => rarity;
    public Sprite Icon => icon;
}

public enum RewardRarity
{
    N,
    R,
    SR,
    UR
}
