
using System;
using UnityEngine;

[Serializable]
public class BundleEntry
{
    [SerializeField, Tooltip("Stable store ID sent to the payment server (e.g. gems_300). Never rename after release.")]
    private string productId;
    [SerializeField] private string bundleName;
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private float price;
    [SerializeField] private BundleReward[] rewards;

    public string ProductId => productId;
    public string BundleName => bundleName;
    public string Description => description;
    public Sprite Icon => icon;
    public float Price => price;
    public BundleReward[] Rewards => rewards;
}