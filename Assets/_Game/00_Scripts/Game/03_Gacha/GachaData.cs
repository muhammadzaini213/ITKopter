
using UnityEngine;

[CreateAssetMenu(fileName = "GachaData", menuName = "Scriptable Objects/GachaData")]
public class GachaData : ScriptableObject
{
    [Header("Gacha Info")]
    [SerializeField] private string gachaId;
    [SerializeField] private string gachaName;

    [Header("Cost")]
    [SerializeField, Min(1)] private int gachaCost = 160;
    [SerializeField, Range(0f, 100f)] private float gachaDiscount;

    [Header("Pull Settings")]
    [SerializeField, Min(1)] private int singlePullCount = 1;
    [SerializeField, Min(1)] private int multiPullCount = 10;
    [SerializeField, Min(1)] private int pityThreshold = 200;
    [SerializeField] private RewardRarity pityRarity = RewardRarity.SSR;

    [Header("Rarity Rates (%)")]
    [SerializeField, Min(0f)] private float rRate = 80f;
    [SerializeField, Min(0f)] private float srRate = 18f;
    [SerializeField, Min(0f)] private float ssrRate = 2f;

    [Header("Reward Pool")]
    [SerializeField] private RewardData[] rewardPool;

    public string GachaId => gachaId;
    public string GachaName => gachaName;
    public int GachaCost => gachaCost;
    public float GachaDiscount => gachaDiscount;
    public int SinglePullCount => singlePullCount;
    public int MultiPullCount => multiPullCount;
    public int PityThreshold => pityThreshold;
    public RewardRarity PityRarity => pityRarity;

    public float RRate => rRate;
    public float SRRate => srRate;
    public float SSRRate => ssrRate;

    public RewardData[] RewardPool => rewardPool;
}