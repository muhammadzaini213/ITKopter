using System;
using UnityEngine;

/// <summary>A coin-priced item in a regular shop. Conversions use <see cref="ConversionRule"/>.</summary>
[Serializable]
public class ShopItemEntry
{
    [SerializeField] private string entryName;
    [SerializeField] private RewardData rewardData;
    [SerializeField] private int itemCount;
    [SerializeField] private int price;
    [SerializeField] private int dailyLimit;

    public string EntryName => entryName;
    public RewardData RewardData => rewardData;
    public int ItemCount => itemCount;
    public int Price => price;
    public int DailyLimit => dailyLimit;
}
