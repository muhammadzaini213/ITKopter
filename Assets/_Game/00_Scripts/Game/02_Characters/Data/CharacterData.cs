using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Scriptable Objects/CharacterData")]
public class CharacterData : RewardData
{
    [Header("Character Info")]
    [SerializeField] private string characterId;
    [SerializeField] private string characterName;
    [Tooltip("Epithet shown under the name, e.g. 'Simple Concept Of Box'.")]
    [SerializeField] private string alias;
    [Tooltip("Short description shown on the gacha reveal card.")]
    [SerializeField, TextArea(2, 5)] private string description;
    [SerializeField] private Sprite characterPortrait;

    [Header("Stats")]
    [SerializeField] private float health;
    [SerializeField] private float attackPower;
    [SerializeField] private float defensePower;
    [SerializeField] private float exSkillCooldown;
    [SerializeField] private float movementSpeed;
    [SerializeField] private float bulletSpeed;

    [Header("Character Settings")]
    [SerializeField] private CharacterType characterType;
    [SerializeField] private CharacterRole characterRole;
    [SerializeField] private GameObject characterPrefab;

    [Header("Duplicate Conversion")]
    [SerializeField] private ItemData duplicateDust;
    [SerializeField] private ItemData duplicateEngram;
    [SerializeField, Min(1)] private int duplicateDustAmount = 3;
    [SerializeField, Min(1)] private int duplicateEngramAmount = 1;

    [Header("Upgrades")]
    [SerializeField] private ItemData dustMaterial;
    [SerializeField] private ItemData engramMaterial;

    [Tooltip("Dust needed = base x perLevel x current level (rounded up).")]
    [SerializeField, Min(0)] private int upgradeDustBase = 10;
    [SerializeField, Min(0f)] private float upgradeDustPerLevel = 2f;

    [Tooltip("Engrams needed = base x perLevel x current level (rounded up).")]
    [SerializeField, Min(0)] private int upgradeEngramBase = 3;
    [SerializeField, Min(0f)] private float upgradeEngramPerLevel = 1.5f;

    [Tooltip("Coins needed = base x perLevel x current level (rounded up).")]
    [SerializeField, Min(0)] private int upgradeCoinBase = 1000;
    [SerializeField, Min(0f)] private float upgradeCoinPerLevel = 3.2f;

    // Stat growth per level above 1: stat x (1 + growth x (L - 1)).
    public const float HealthGrowthPerLevel = 0.06f;
    public const float AttackGrowthPerLevel = 0.05f;
    public const float DefenseGrowthPerLevel = 0.04f;

    public string CharacterId => characterId;
    public string CharacterName => characterName;
    public string Alias => alias;
    public string Description => description;
    public Sprite CharacterPortrait => characterPortrait;
    public float Health => health;
    public float AttackPower => attackPower;
    public float DefensePower => defensePower;
    public float ExSkillCooldown => exSkillCooldown;
    public float MovementSpeed => movementSpeed;
    public float BulletSpeed => bulletSpeed;
    public CharacterType CharacterType => characterType;
    public CharacterRole CharacterRole => characterRole;
    public GameObject CharacterPrefab => characterPrefab;

    public ItemData DuplicateDust => duplicateDust;
    public ItemData DuplicateEngram => duplicateEngram;
    public int DuplicateDustAmount => duplicateDustAmount;
    public int DuplicateEngramAmount => duplicateEngramAmount;

    public ItemData DustMaterial => dustMaterial;
    public ItemData EngramMaterial => engramMaterial;

    public float GetHealthAtLevel(int level) => Scale(health, HealthGrowthPerLevel, level);
    public float GetAttackAtLevel(int level) => Scale(attackPower, AttackGrowthPerLevel, level);
    public float GetDefenseAtLevel(int level) => Scale(defensePower, DefenseGrowthPerLevel, level);

    /// <summary>Cost to upgrade from <paramref name="currentLevel"/> to the next level.</summary>
    public CharacterUpgradeCost GetUpgradeCost(int currentLevel)
    {
        int level = Mathf.Max(1, currentLevel);

        return new CharacterUpgradeCost(
            dustMaterial,
            Cost(upgradeDustBase, upgradeDustPerLevel, level),
            engramMaterial,
            Cost(upgradeEngramBase, upgradeEngramPerLevel, level),
            Cost(upgradeCoinBase, upgradeCoinPerLevel, level));
    }

    private static float Scale(float baseValue, float growth, int level)
    {
        return baseValue * (1f + growth * (Mathf.Max(1, level) - 1));
    }

    private static int Cost(int baseAmount, float perLevel, int level)
    {
        // decimal keeps inspector values exact (3.2f is 3.2000000476..., which would ceil 3200 to 3201).
        decimal value = baseAmount * (decimal)perLevel * level;

        return value >= int.MaxValue
            ? int.MaxValue
            : (int)decimal.Ceiling(value);
    }
}

public readonly struct CharacterUpgradeCost
{
    public readonly ItemData Dust;
    public readonly int DustAmount;
    public readonly ItemData Engram;
    public readonly int EngramAmount;
    public readonly int Coins;

    public CharacterUpgradeCost(ItemData dust, int dustAmount, ItemData engram, int engramAmount, int coins)
    {
        Dust = dust;
        DustAmount = dustAmount;
        Engram = engram;
        EngramAmount = engramAmount;
        Coins = coins;
    }
}