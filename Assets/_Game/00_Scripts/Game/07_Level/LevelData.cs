
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

    [Tooltip("Rewards with amounts, granted on every clear (shown as icons on the stage list).")]
    [SerializeField] private StoryRewardEntry[] rewardEntries = new StoryRewardEntry[0];

    [Header("Speaker Toasts")]
    [Tooltip("Tips or banter shown during the battle (Time = seconds after start).")]
    [SerializeField] private StoryToastLine[] battleToasts = new StoryToastLine[0];

    [Tooltip("Shown when the first boss of the level appears (Time = delay between lines).")]
    [SerializeField] private StoryToastLine[] bossIntroToasts = new StoryToastLine[0];

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

    [SerializeField, Tooltip("Tint for the (white) minion sprites, usually the boss's colour. Alpha 0 = no tint. Bosses keep their own colours.")]
    private Color enemyTint = new Color(1f, 1f, 1f, 0f);

    public string LevelId => levelId;
    public string LevelName => levelName;
    public LevelData PreviousLevel => previousLevel;
    public LevelPartData[] LevelParts => levelParts;
    public RewardData[] LevelRewards => levelRewards;
    public StoryRewardEntry[] RewardEntries => rewardEntries;
    public StoryToastLine[] BattleToasts => battleToasts;
    public StoryToastLine[] BossIntroToasts => bossIntroToasts;
    public int EnergyCost => energyCost;
    public MusicReference LevelMusic => levelMusic;

    public int TimeLimitSeconds => timeLimitSeconds;
    public float EnemyStatMultiplier => enemyStatMultiplier;
    public Color EnemyTint => enemyTint;
}
