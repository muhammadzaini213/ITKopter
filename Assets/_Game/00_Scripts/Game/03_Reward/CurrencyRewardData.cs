
using UnityEngine;

public enum CurrencyType
{
    Coins,
    Gems
}

[CreateAssetMenu(
    fileName = "CurrencyRewardData",
    menuName = "Scriptable Objects/Currency Reward")]
public class CurrencyRewardData : RewardData
{
    [SerializeField] private CurrencyType currencyType;
    [SerializeField] private int amount;

    public CurrencyType CurrencyType => currencyType;
    public int Amount => amount;
}