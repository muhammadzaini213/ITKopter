
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    [SerializeField] private string levelName;
    [SerializeField] private LevelPartData[] levelParts;
    [SerializeField] private RewardData[] levelRewards;
    [SerializeField] private int energyCost;

    public string LevelName => levelName;
    public LevelPartData[] LevelParts => levelParts;
    public RewardData[] LevelRewards => levelRewards;
    public int EnergyCost => energyCost;
}