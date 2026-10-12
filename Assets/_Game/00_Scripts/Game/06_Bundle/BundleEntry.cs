
using System;
using UnityEngine;

[Serializable]
public class BundleEntry
{
    [SerializeField] private string bundleName;
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private float price;
    [SerializeField] private BundleReward[] rewards;

    public string BundleName => bundleName;
    public string Description => description;
    public Sprite Icon => icon;
    public float Price => price;
    public BundleReward[] Rewards => rewards;
}