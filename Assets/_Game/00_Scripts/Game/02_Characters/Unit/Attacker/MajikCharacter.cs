using UnityEngine;

public class MajikCharacter : Character
{
    [Header("Debug Settings")]
    [SerializeField] private bool logStatsOnStart = true;

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        if (logStatsOnStart)
            LogCharacterStats();
    }

    public override void BaseAttack()
    {
        Debug.Log(
            $"[{CharacterName}] performed Base Attack. " +
            $"Attack Power: {AttackPower}",
            this
        );
    }

    public override void SpecialAttack()
    {
        Debug.Log(
            $"[{CharacterName}] used EX Skill. " +
            $"EX Skill Power: {AttackPower}",
            this
        );
    }

    [ContextMenu("Debug/Log Character Stats")]
    public void LogCharacterStats()
    {
        Debug.Log(
            $"===== Character Debug =====\n" +
            $"ID: {CharacterId}\n" +
            $"Name: {CharacterName}\n" +
            $"Type: {CharacterType}\n" +
            $"Role: {CharacterRole}\n" +
            $"Health: {Health}\n" +
            $"Attack Power: {AttackPower}\n" +
            $"Defense Power: {DefensePower}\n" +
            $"EX Skill Cooldown: {ExSkillCooldown}\n" +
            $"Movement Speed: {MovementSpeed}\n" +
            $"Bullet Speed: {BulletSpeed}\n" +
            $"===========================",
            this
        );
    }
}
