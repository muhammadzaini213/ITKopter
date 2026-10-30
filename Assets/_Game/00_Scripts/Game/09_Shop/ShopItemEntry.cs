
using System;
using UnityEngine;

[Serializable]
public class ShopItemEntry
{
    [SerializeField] private string entryName;
    [SerializeField] private RewardData rewardData;
    [SerializeField] private int itemCount;
    [SerializeField] private ItemData[] allowedItemExchange;
    [SerializeField] private int price;
    [SerializeField] private int dailyLimit;

    public string EntryName => entryName;
    public RewardData RewardData => rewardData;
    public int ItemCount => itemCount;
    public ItemData[] AllowedItemExchange => allowedItemExchange;
    public int Price => price;
    public int DailyLimit => dailyLimit;
    
}