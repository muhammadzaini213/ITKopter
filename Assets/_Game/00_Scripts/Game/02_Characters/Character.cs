
using UnityEngine;

public abstract class Character : MonoBehaviour
{
    [Header("Character Data")]
    [SerializeField]
    private CharacterData characterData;

    public CharacterData Data => characterData;

    public string CharacterId { get; private set; }
    public string CharacterName { get; private set; }

    public float Health { get; private set; }
    public float AttackPower { get; private set; }
    public float DefensePower { get; private set; }

    public float ExSkillPower { get; private set; }
    public float ExDefensePower { get; private set; }
    public float ExSkillCooldown { get; private set; }

    public float MovementSpeed { get; private set; }
    public float BulletSpeed { get; private set; }

    public CharacterType CharacterType { get; private set; }
    public CharacterRole CharacterRole { get; private set; }

    protected virtual void Awake()
    {
        if (characterData != null)
            Initialize(characterData);
    }

    public void Initialize(CharacterData data)
    {
        if (data == null)
        {
            Debug.LogError("[Character] Cannot initialize with null CharacterData.", this);
            return;
        }

        characterData = data;

        CharacterId = data.CharacterId;
        CharacterName = data.CharacterName;
        Health = data.Health;
        AttackPower = data.AttackPower;
        DefensePower = data.DefensePower;
        ExSkillPower = data.ExSkillPower;
        ExDefensePower = data.ExDefensePower;
        ExSkillCooldown = data.ExSkillCooldown;
        MovementSpeed = data.MovementSpeed;
        BulletSpeed = data.BulletSpeed;
        CharacterType = data.CharacterType;
        CharacterRole = data.CharacterRole;

        Health healthComponent = GetComponent<Health>();

        if (healthComponent != null)
            healthComponent.SetMaxHealth(Health);
        else
            Debug.LogWarning("[Character] Health component is missing.", this);

        ExSkill exSkill = GetComponent<ExSkill>();

        if (exSkill != null)
        {
            exSkill.Initialize(this);
        }
    }

    public abstract void BaseAttack();

    public abstract void SpecialAttack();
}
