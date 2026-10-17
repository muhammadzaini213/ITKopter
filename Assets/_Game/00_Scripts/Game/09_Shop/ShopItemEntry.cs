
using System;
using UnityEngine;

[Serializable]
public class ShopItemEntry
{
    [SerializeField] private string entryName;
    [SerializeField] private RewardData rewardData;
    [SerializeField] private int price;

    public string EntryName => entryName;
    public RewardData RewardData => rewardData;
    public int Price => price;
}
