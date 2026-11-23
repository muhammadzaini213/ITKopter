
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Scriptable Objects/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    [SerializeField] private string levelId;
    [SerializeField] private string levelName;

    [Tooltip("Leave empty if this level is unlocked from the beginning.")]
    [SerializeField] private LevelData previousLevel;

    [SerializeField] private LevelPartData[] levelParts;
    [SerializeField] private RewardData[] levelRewards;

    [Header("Energy")]
    [SerializeField, Min(0)] private int energyCost;

    [Header("Time Limit")]
    [Tooltip("Maximum time allowed to complete this level, in seconds.")]
    [SerializeField, Min(1)] private int timeLimitSeconds = 180;

    [Header("Music")]
    [SerializeField] private MusicReference levelMusic;

    [Header("Enemy Scaling")]
    [SerializeField, Min(0.01f),
     Tooltip("Scales enemy combat stats in this level: Max Health, Defense and Projectile Damage. 1 = unchanged, 2 = double. Movement/pattern timing untouched.")]
    private float enemyStatMultiplier = 1f;

    public string LevelId => levelId;
    public string LevelName => levelName;
    public LevelData PreviousLevel => previousLevel;
    public LevelPartData[] LevelParts => levelParts;
    public RewardData[] LevelRewards => levelRewards;
    public int EnergyCost => energyCost;
    public MusicReference LevelMusic => levelMusic;

    public int TimeLimitSeconds => timeLimitSeconds;
    public float EnemyStatMultiplier => enemyStatMultiplier;
}
