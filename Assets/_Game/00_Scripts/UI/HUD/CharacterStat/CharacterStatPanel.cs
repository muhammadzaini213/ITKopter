
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterStatPanel : MonoBehaviour
{
    [Header("Character Info")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Image characterIcon;

    [Header("Character Classification UI")]
    [SerializeField] private Image roleImage;
    [SerializeField] private Image hierarchyImage;

    [Header("Role Sprites")]
    [SerializeField] private RoleSpriteMapping[] roleSprites;

    [Header("Hierarchy / Type Sprites")]
    [SerializeField] private TypeSpriteMapping[] typeSprites;

    [Header("Health UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("EX Skill Cooldown UI")]
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private TextMeshProUGUI cooldownText;

    [Serializable]
    private struct RoleSpriteMapping
    {
        public CharacterRole role;
        public Sprite sprite;
    }

    [Serializable]
    private struct TypeSpriteMapping
    {
        public CharacterType type;
        public Sprite sprite;
    }

    private Character character;
    private Health health;
    private ExSkillController exSkillController;

    private void Awake()
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
        }

        if (cooldownSlider != null)
        {
            cooldownSlider.minValue = 0f;
            cooldownSlider.maxValue = 1f;
        }
    }

    public void Initialize(
        Character targetCharacter,
        ExSkillController skillController)
    {
        UnsubscribeHealthEvents();

        character = targetCharacter;
        health = null;
        exSkillController = skillController;

        if (character == null)
        {
            Debug.LogError(
                "[CharacterStatPanel] Character reference is missing.",
                this);
            return;
        }

        health = character.GetComponent<Health>();

        if (health == null)
        {
            Debug.LogError(
                $"[CharacterStatPanel] Health component is missing on '{character.name}'.",
                character);
            return;
        }

        if (characterNameText != null)
            characterNameText.text = character.CharacterName;

        if (characterIcon != null)
        {
            characterIcon.sprite = character.CharacterIcon;
            characterIcon.enabled = character.CharacterIcon != null;
        }

        UpdateRoleImage(character.CharacterRole);
        UpdateHierarchyImage(character.CharacterType);

        health.OnHealthChanged += HandleHealthChanged;
        health.OnDeath += HandleDeath;

        RefreshHealthUI();
        RefreshCooldownUI();
    }

    private void UpdateRoleImage(CharacterRole role)
    {
        Sprite sprite = null;

        if (roleSprites != null)
        {
            foreach (RoleSpriteMapping mapping in roleSprites)
            {
                if (mapping.role == role)
                {
                    sprite = mapping.sprite;
                    break;
                }
            }
        }

        SetImage(roleImage, sprite);
    }

    private void UpdateHierarchyImage(CharacterType type)
    {
        Sprite sprite = null;

        if (typeSprites != null)
        {
            foreach (TypeSpriteMapping mapping in typeSprites)
            {
                if (mapping.type == type)
                {
                    sprite = mapping.sprite;
                    break;
                }
            }
        }

        SetImage(hierarchyImage, sprite);
    }

    private void SetImage(Image target, Sprite sprite)
    {
        if (target == null)
            return;

        target.sprite = sprite;
        target.enabled = sprite != null;
    }

    private void Update()
    {
        if (character == null || health == null)
            return;

        RefreshCooldownUI();
    }

    private void HandleHealthChanged(float current, float max)
    {
        RefreshHealthUI();
    }

    private void HandleDeath()
    {
        RefreshHealthUI();
    }

    private void RefreshHealthUI()
    {
        if (health == null)
            return;

        float current = health.CurrentHealth;
        float max = health.MaxHealth;

        if (healthSlider != null)
        {
            healthSlider.value = max > 0f
                ? Mathf.Clamp01(current / max)
                : 0f;
        }

        if (healthText != null)
            healthText.text = $"{current:0} / {max:0}";
    }

    private void RefreshCooldownUI()
    {
        if (cooldownSlider == null && cooldownText == null)
            return;

        bool isActiveCharacter =
            exSkillController != null &&
            exSkillController.CooldownCharacter == character;

        if (exSkillController == null || !isActiveCharacter)
        {
            if (cooldownSlider != null)
                cooldownSlider.value = 1f;

            if (cooldownText != null)
                cooldownText.text = "READY";

            return;
        }

        float remaining = exSkillController.CooldownRemaining;
        float duration = exSkillController.CooldownDuration;

        if (cooldownSlider != null)
        {
            cooldownSlider.value = duration > 0f
                ? Mathf.Clamp01(1f - remaining / duration)
                : 1f;
        }

        if (cooldownText != null)
        {
            cooldownText.text = remaining > 0f
                ? $"{remaining:0.0}s"
                : "READY";
        }
    }

    private void UnsubscribeHealthEvents()
    {
        if (health == null)
            return;

        health.OnHealthChanged -= HandleHealthChanged;
        health.OnDeath -= HandleDeath;
    }

    private void OnDestroy()
    {
        UnsubscribeHealthEvents();
    }
}
