
using System.Collections.Generic;
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

        legacyAttackBonus = 0f;
        legacyDefenseBonus = 0f;
        attackModifiers.Clear();
        defenseModifiers.Clear();
        fireIntervalModifiers.Clear();

        healthComponent = GetComponent<Health>();

        if (healthComponent != null)
            healthComponent.SetMaxHealth(Health);
        else
            Debug.LogWarning("[Character] Health component is missing.", this);

        RecalculateBonuses();

        // The EX skill usually lives on an (inactive) "ExSkill" child.
        ExSkill exSkill = GetComponentInChildren<ExSkill>(true);

        if (exSkill != null)
        {
            exSkill.Initialize(this);
        }
    }

    // ---- Stat modifiers ---------------------------------------------
    // Keyed by source (usually the EX skill instance) so overlapping buffs
    // from different skills never wipe each other out. Defense feeds the
    // Health component as percentage armor.

    private Health healthComponent;
    private float legacyAttackBonus;
    private float legacyDefenseBonus;
    private readonly Dictionary<object, float> attackModifiers = new Dictionary<object, float>();
    private readonly Dictionary<object, float> defenseModifiers = new Dictionary<object, float>();
    private readonly Dictionary<object, float> fireIntervalModifiers = new Dictionary<object, float>();

    /// <summary>Product of all fire-interval modifiers (&lt; 1 = shoots faster).</summary>
    public float FireIntervalMultiplier { get; private set; } = 1f;

    public void SetAttackModifier(object source, float flatAmount)
    {
        if (source == null) return;
        attackModifiers[source] = flatAmount;
        RecalculateBonuses();
    }

    public void SetDefenseModifier(object source, float flatAmount)
    {
        if (source == null) return;
        defenseModifiers[source] = flatAmount;
        RecalculateBonuses();
    }

    /// <summary>Multiplies the auto-fire interval (0.5 = twice as fast, 2 = half speed).</summary>
    public void SetFireIntervalModifier(object source, float intervalMultiplier)
    {
        if (source == null) return;
        fireIntervalModifiers[source] = Mathf.Max(0.05f, intervalMultiplier);
        RecalculateBonuses();
    }

    public void RemoveModifiers(object source)
    {
        if (source == null) return;

        bool changed = attackModifiers.Remove(source);
        changed |= defenseModifiers.Remove(source);
        changed |= fireIntervalModifiers.Remove(source);

        if (changed)
            RecalculateBonuses();
    }

    public bool HasModifiers(object source) =>
        source != null &&
        (attackModifiers.ContainsKey(source) ||
         defenseModifiers.ContainsKey(source) ||
         fireIntervalModifiers.ContainsKey(source));

    private void RecalculateBonuses()
    {
        float attack = legacyAttackBonus;
        foreach (float value in attackModifiers.Values)
            attack += value;

        float defense = legacyDefenseBonus;
        foreach (float value in defenseModifiers.Values)
            defense += value;

        float interval = 1f;
        foreach (float value in fireIntervalModifiers.Values)
            interval *= value;

        // Never let debuffs push attack below 10% of base.
        AttackPowerBonus = Mathf.Max(attack, -AttackPower * 0.9f);
        DefensePowerBonus = Mathf.Max(defense, -DefensePower);
        FireIntervalMultiplier = Mathf.Clamp(interval, 0.1f, 10f);

        if (healthComponent == null)
            healthComponent = GetComponent<Health>();

        if (healthComponent != null)
            healthComponent.SetArmor(EffectiveDefensePower);
    }

    // Legacy unkeyed API (kept for older callers) — prefer the keyed one.
    public void AddAttackPowerBonus(float amount) { legacyAttackBonus += amount; RecalculateBonuses(); }

    public void AddDefensePowerBonus(float amount) { legacyDefenseBonus += amount; RecalculateBonuses(); }

    public void ClearAttackPowerBonus() { legacyAttackBonus = 0f; RecalculateBonuses(); }

    public void ClearDefensePowerBonus() { legacyDefenseBonus = 0f; RecalculateBonuses(); }

    public abstract void BaseAttack();

    public abstract void SpecialAttack();
}
