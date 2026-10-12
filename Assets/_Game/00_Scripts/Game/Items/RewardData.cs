
using UnityEngine;

public abstract class RewardData : ScriptableObject
{
    [SerializeField] private RewardRarity rarity;

    public RewardRarity Rarity => rarity;
}

public enum RewardRarity
{
    R,
    SR,
    SSR
}