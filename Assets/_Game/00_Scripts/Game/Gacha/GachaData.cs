
using UnityEngine;

[CreateAssetMenu(fileName = "GachaData", menuName = "Scriptable Objects/GachaData")]
public class GachaData : ScriptableObject
{
    [Header("Gacha Info")]
    [SerializeField] private string gachaName;

    [Header("Cost")]
    [SerializeField] private int gachaCost;
    [SerializeField] private float gachaDiscount;

    [Header("Pull Settings")]
    [SerializeField] private int singlePullCount = 1;
    [SerializeField] private int multiPullCount = 10;
    [SerializeField] private int pityThreshold = 200;

    [Header("Reward Pool")]
    [SerializeField] private RewardData[] rewardPool;

    public string GachaName => gachaName;
    public int GachaCost => gachaCost;
    public float GachaDiscount => gachaDiscount;
    public int SinglePullCount => singlePullCount;
    public int MultiPullCount => multiPullCount;
    public int PityThreshold => pityThreshold;
    public RewardData[] RewardPool => rewardPool;
}