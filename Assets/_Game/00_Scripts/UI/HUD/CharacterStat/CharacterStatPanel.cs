
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterStatPanel : MonoBehaviour
{
    [Header("Character Info")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private Image characterIcon;

    [Header("Health UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("EX Skill Cooldown UI")]
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private TextMeshProUGUI cooldownText;

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

        health.OnHealthChanged += HandleHealthChanged;
        health.OnDeath += HandleDeath;

        if (characterIcon != null)
        {
            characterIcon.sprite = character.CharacterIcon;
            characterIcon.enabled = character.CharacterIcon != null;
        }

        RefreshHealthUI();
        RefreshCooldownUI();
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
        {
            healthText.text = $"{current:0} / {max:0}";
        }
    }

    private void RefreshCooldownUI()
    {
        if (cooldownSlider == null && cooldownText == null)
            return;

        // Cooldown is tracked for the active character by ExSkillController.
        // Only display this character's cooldown when it is active.
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
