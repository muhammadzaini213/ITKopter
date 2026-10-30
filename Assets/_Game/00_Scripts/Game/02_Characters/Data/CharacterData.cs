using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Scriptable Objects/CharacterData")]
public class CharacterData : RewardData
{
    [Header("Character Info")]
    [SerializeField] private string characterId;
    [SerializeField] private string characterName;
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

    public string CharacterId => characterId;
    public string CharacterName => characterName;
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
}