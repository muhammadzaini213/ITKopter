
using UnityEngine;

public abstract class Character : MonoBehaviour
{
    [Header("Character Data")]
    [SerializeField]
    private CharacterData characterData;

    public CharacterData Data => characterData;

    public string CharacterId { get; private set; }
    public string CharacterName { get; private set; }
    public Sprite CharacterIcon { get; private set; }
    public Sprite CharacterPortrait { get; private set; }

    public int Level { get; private set; } = 1;
    public float Health { get; private set; }
    public float AttackPower { get; private set; }
    public float DefensePower { get; private set; }

    public float AttackPowerBonus { get; private set; }
    public float DefensePowerBonus { get; private set; }

    public float EffectiveAttackPower => AttackPower + AttackPowerBonus;
    public float EffectiveDefensePower => DefensePower + DefensePowerBonus;

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
        CharacterIcon = data.Icon;
        CharacterPortrait = data.CharacterPortrait;
        // Upgrade level scales HP / ATK / DEF (see CharacterData growth constants).
        Level = PlayerSystem.Instance != null
            ? PlayerSystem.Instance.GetCharacterLevel(data.CharacterId)
            : 1;

        Health = data.GetHealthAtLevel(Level);
        AttackPower = data.GetAttackAtLevel(Level);
        DefensePower = data.GetDefenseAtLevel(Level);
        ExSkillCooldown = data.ExSkillCooldown;
        MovementSpeed = data.MovementSpeed;
        BulletSpeed = data.BulletSpeed;
        CharacterType = data.CharacterType;
        CharacterRole = data.CharacterRole;

        AttackPowerBonus = 0f;
        DefensePowerBonus = 0f;

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

    public void AddAttackPowerBonus(float amount) => AttackPowerBonus += amount;

    public void AddDefensePowerBonus(float amount) => DefensePowerBonus += amount;

    public void ClearAttackPowerBonus() => AttackPowerBonus = 0f;

    public void ClearDefensePowerBonus() => DefensePowerBonus = 0f;

    public abstract void BaseAttack();

    public abstract void SpecialAttack();
}
