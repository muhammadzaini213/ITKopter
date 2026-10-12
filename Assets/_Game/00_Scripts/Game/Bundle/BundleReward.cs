
using System;
using UnityEngine;

[Serializable]
public class BundleReward
{
    [SerializeField] private RewardData reward;
    [SerializeField] private int amount = 1;

    public RewardData Reward => reward;
    public int Amount => amount;
}