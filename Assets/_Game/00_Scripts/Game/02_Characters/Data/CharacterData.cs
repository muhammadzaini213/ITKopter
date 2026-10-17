using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Scriptable Objects/CharacterData")]
public class CharacterData : RewardData
{
    [Header("Character Info")]
    [SerializeField] private string characterId;
    [SerializeField] private string characterName;

    [Header("Stats")]
    [SerializeField] private float health;
    [SerializeField] private float attackPower;
    [SerializeField] private float defensePower;
    [SerializeField] private float exSkillPower;
    [SerializeField] private float exDefensePower;
    [SerializeField] private float exSkillCooldown;
    [SerializeField] private float movementSpeed;
    [SerializeField] private float bulletSpeed;

    [Header("Character Settings")]
    [SerializeField] private CharacterType characterType;
    [SerializeField] private CharacterRole characterRole;
    [SerializeField] private GameObject characterPrefab;

    [Header("Duplicate Conversion")]
    [SerializeField] private ItemData duplicateItem;
    [SerializeField, Min(1)] private int duplicateItemAmount = 1;

    public string CharacterId => characterId;
    public string CharacterName => characterName;
    public float Health => health;
    public float AttackPower => attackPower;
    public float DefensePower => defensePower;
    public float ExSkillPower => exSkillPower;
    public float ExDefensePower => exDefensePower;
    public float ExSkillCooldown => exSkillCooldown;
    public float MovementSpeed => movementSpeed;
    public float BulletSpeed => bulletSpeed;
    public CharacterType CharacterType => characterType;
    public CharacterRole CharacterRole => characterRole;
    public GameObject CharacterPrefab => characterPrefab;

    public ItemData DuplicateItem => duplicateItem;
    public int DuplicateItemAmount => duplicateItemAmount;
}