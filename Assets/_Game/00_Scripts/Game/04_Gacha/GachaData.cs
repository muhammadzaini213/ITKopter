
using UnityEngine;

[CreateAssetMenu(fileName = "GachaData", menuName = "Scriptable Objects/GachaData")]
public class GachaData : ScriptableObject
{
    [Header("Gacha Info")]
    [SerializeField] private string gachaId;
    [SerializeField] private string gachaName;
    [Tooltip("Shown under the banner name on the Gacha screen.")]
    [SerializeField, TextArea(2, 6)] private string description;

    [Header("Cost")]
    [SerializeField, Min(1)] private int gachaCost = 160;
    [SerializeField, Range(0f, 100f)] private float gachaDiscount;

    [Header("Pull Settings")]
    [SerializeField, Min(1)] private int singlePullCount = 1;
    [SerializeField, Min(1)] private int multiPullCount = 10;
    [SerializeField, Min(1)] private int pityThreshold = 200;
    [SerializeField] private RewardRarity pityRarity = RewardRarity.UR;

    [Header("Rarity Rates (%)")]
    [SerializeField, Min(0f)] private float nRate = 50f;
    [SerializeField, Min(0f)] private float rRate = 30f;
    [SerializeField, Min(0f)] private float srRate = 18f;
    [SerializeField, Min(0f)] private float urRate = 2f;

    [Header("Reward Pool")]
    [SerializeField] private RewardData featuredReward;
    [SerializeField] private GachaEntry[] gachaEntries;

    public string GachaId => gachaId;
    public string GachaName => gachaName;
    public string Description => description;
    public int GachaCost => gachaCost;
    public float GachaDiscount => gachaDiscount;
    public int SinglePullCount => singlePullCount;
    public int MultiPullCount => multiPullCount;
    public int PityThreshold => pityThreshold;
    public RewardRarity PityRarity => pityRarity;

    public float NRate => nRate;
    public float RRate => rRate;
    public float SRRate => srRate;
    public float URRate => urRate;

    public RewardData FeaturedReward => featuredReward;
    public GachaEntry[] GachaEntries => gachaEntries;
}

[System.Serializable]
public class GachaEntry
{
    public RewardData RewardData;
    [UnityEngine.Tooltip("Give a random amount between Low and High (inclusive) instead of 1.")]
    public bool UseInterval;
    public int LowInterval = 1;
    public int HighInterval = 1;

    /// <summary>Amount range this entry grants (1-1 when intervals are off).</summary>
    public int MinAmount => UseInterval ? UnityEngine.Mathf.Max(1, LowInterval) : 1;
    public int MaxAmount => UseInterval ? UnityEngine.Mathf.Max(MinAmount, HighInterval) : 1;
    public bool OverrideRarity;
    public RewardRarity OverrideRarityValue;
}