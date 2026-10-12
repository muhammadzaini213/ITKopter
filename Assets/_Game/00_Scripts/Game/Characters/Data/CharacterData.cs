using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Scriptable Objects/CharacterData")]
public class CharacterData : RewardData
{
    [SerializeField] private string characterName;
    [SerializeField] private float health;
    [SerializeField] private float attackPower;
    [SerializeField] private float defensePower;
    [SerializeField] private float specialAttackPower;
    [SerializeField] private float specialDefensePower;
    [SerializeField] private float specialSkillCooldown;
    [SerializeField] private float movementSpeed;
    [SerializeField] private float bulletSpeed;
    [SerializeField] private CharacterType characterType;
    [SerializeField] private CharacterRole characterRole;
    [SerializeField] private GameObject characterPrefab;

    public string CharacterName => characterName;
    public float Health => health;
    public float AttackPower => attackPower;
    public float DefensePower => defensePower;
    public float SpecialAttackPower => specialAttackPower;
    public float SpecialDefensePower => specialDefensePower;
    public float SpecialSkillCooldown => specialSkillCooldown;
    public float MovementSpeed => movementSpeed;
    public float BulletSpeed => bulletSpeed;
    public CharacterType CharacterType => characterType;
    public CharacterRole CharacterRole => characterRole;
    public GameObject CharacterPrefab => characterPrefab;
}
