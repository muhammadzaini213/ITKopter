
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExSkillController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterSwitcher characterSwitcher;
    [SerializeField] private GameplayInitializer gameplayInitializer;
    [SerializeField] private Button exSkillButton;

    [Header("EX Skill UI")]
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private TextMeshProUGUI skillNameText;

    [Header("Cooldown Settings")]
    [SerializeField, Min(0f)] private float cooldownOverride = 0f;
    [SerializeField] private bool useCharacterCooldown = true;

    [Header("Hierarchy Settings")]
    [SerializeField] private string exSkillChildName = "ExSkill";

    [Header("Cooldown Debug")]
    [SerializeField]
    private List<CharacterCooldownDebug> cooldownDebug =
        new List<CharacterCooldownDebug>();

    private class CharacterCooldown
    {
        public float Duration;
        public float Remaining;
    }

    private readonly Dictionary<Character, CharacterCooldown> cooldowns =
        new Dictionary<Character, CharacterCooldown>();

    public float CooldownRemaining =>
        GetCooldownRemaining(GetActiveCharacter());

    public bool IsCooldownActive => CooldownRemaining > 0f;

    private void Awake()
    {
        if (characterSwitcher == null)
        {
            characterSwitcher =
                Object.FindFirstObjectByType<CharacterSwitcher>();
        }

        if (gameplayInitializer == null)
        {
            gameplayInitializer =
                Object.FindFirstObjectByType<GameplayInitializer>();
        }

        if (characterSwitcher == null)
        {
            Debug.LogError(
                "[ExSkillController] CharacterSwitcher was not found.",
                this
            );
        }

        if (gameplayInitializer == null)
        {
            Debug.LogError(
                "[ExSkillController] GameplayInitializer was not found.",
                this
            );
        }

        if (exSkillButton == null)
        {
            Debug.LogError(
                "[ExSkillController] EX Skill Button is not assigned.",
                this
            );
        }

        if (cooldownSlider != null)
        {
            cooldownSlider.minValue = 0f;
            cooldownSlider.maxValue = 1f;
            cooldownSlider.value = 1f;
        }
    }

    private void OnEnable()
    {
        Debug.Log("[ExSkillController] Controller enabled.", this);

        if (exSkillButton != null)
        {
            exSkillButton.onClick.AddListener(UseExSkill);
            Debug.Log("[ExSkillController] Button listener registered.", this);
        }
        else
        {
            Debug.LogError("[ExSkillController] Button reference is missing.", this);
        }
    }

    private void OnDisable()
    {
        if (exSkillButton != null)
            exSkillButton.onClick.RemoveListener(UseExSkill);
    }

    private void Start()
    {
        InitializeCharacterCooldowns();
        RefreshCooldownDebug();
        RefreshSkillUI();
        RefreshButton();
    }

    private void Update()
    {
        UpdateCooldowns();
        RefreshCooldownDebug();
        RefreshSkillUI();
        RefreshButton();
    }

    private void InitializeCharacterCooldowns()
    {
        cooldowns.Clear();
        cooldownDebug.Clear();

        if (gameplayInitializer == null)
            return;

        IReadOnlyList<Character> characters =
            gameplayInitializer.InitializedCharacters;

        if (characters == null)
        {
            Debug.LogWarning(
                "[ExSkillController] Initialized character list is null.",
                this
            );
            return;
        }

        foreach (Character character in characters)
        {
            if (character == null)
                continue;

            float duration = GetCooldown(character);

            cooldowns[character] = new CharacterCooldown
            {
                Duration = duration,
                Remaining = 0f
            };

            cooldownDebug.Add(
                new CharacterCooldownDebug(character, duration)
            );
        }

        Debug.Log(
            $"[ExSkillController] Initialized cooldowns for {cooldowns.Count} character(s).",
            this
        );
    }

    private void UpdateCooldowns()
    {
        float deltaTime = Time.deltaTime;

        foreach (CharacterCooldown state in cooldowns.Values)
        {
            if (state.Remaining <= 0f)
                continue;

            state.Remaining = Mathf.Max(
                0f,
                state.Remaining - deltaTime
            );
        }
    }


    public void UseExSkill()
    {
        Debug.Log("[ExSkillController] UseExSkill() called.", this);

        Character activeCharacter = GetActiveCharacter();

        if (activeCharacter == null)
        {
            Debug.LogWarning("[ExSkillController] No active character.", this);
            return;
        }

        if (characterSwitcher == null)
        {
            Debug.LogError("[ExSkillController] CharacterSwitcher is missing.", this);
            return;
        }

        if (characterSwitcher.IsSwitching)
        {
            Debug.LogWarning("[ExSkillController] Cannot use EX Skill while switching.", this);
            return;
        }

        float remaining = GetCooldownRemaining(activeCharacter);

        if (remaining > 0f)
        {
            Debug.LogWarning(
                $"[ExSkillController] Cooldown is still active: {remaining:0.00}s.",
                this
            );
            return;
        }

        Health health = activeCharacter.GetComponent<Health>();

        if (health != null && health.IsDead)
        {
            Debug.LogWarning("[ExSkillController] Active character is dead.", this);
            return;
        }

        Transform exSkillTransform = GetExSkillTransform(activeCharacter);

        if (exSkillTransform == null)
        {
            Debug.LogError(
                $"[ExSkillController] Child '{exSkillChildName}' not found under '{activeCharacter.name}'.",
                activeCharacter
            );
            return;
        }

        ExSkill exSkill =
            exSkillTransform.GetComponentInChildren<ExSkill>(true);

        if (exSkill == null)
        {
            Debug.LogError(
                "[ExSkillController] No ExSkill component found.",
                exSkillTransform
            );
            return;
        }

        // Ensure the skill has its owning character before checking CanUse.
        exSkill.Initialize(activeCharacter);

        Debug.Log(
            $"[ExSkillController] Found skill '{exSkill.SkillName}'. " +
            $"CanUse: {exSkill.CanUse()}, " +
            $"Skill object active: {exSkill.gameObject.activeInHierarchy}",
            exSkill
        );

        if (!exSkill.CanUse())
        {
            Debug.LogWarning(
                "[ExSkillController] CanUse returned false. Check the skill references.",
                exSkill
            );
            return;
        }

        exSkillTransform.gameObject.SetActive(true);
        exSkill.Activate();

        float duration = GetCooldown(activeCharacter);

        if (!cooldowns.TryGetValue(activeCharacter, out CharacterCooldown state))
        {
            state = new CharacterCooldown();
            cooldowns.Add(activeCharacter, state);

            cooldownDebug.Add(
                new CharacterCooldownDebug(activeCharacter, duration)
            );
        }

        state.Duration = duration;
        state.Remaining = duration;

        RefreshCooldownDebug();
        RefreshSkillUI();
        RefreshButton();

        Debug.Log(
            $"[ExSkillController] Activated '{exSkill.SkillName}' for '{activeCharacter.name}'.",
            activeCharacter
        );
    }


    private float GetCooldown(Character character)
    {
        if (character == null)
            return 0f;

        return useCharacterCooldown
            ? Mathf.Max(0f, character.ExSkillCooldown)
            : Mathf.Max(0f, cooldownOverride);
    }

    private float GetCooldownRemaining(Character character)
    {
        if (character == null)
            return 0f;

        return cooldowns.TryGetValue(
            character,
            out CharacterCooldown state)
            ? state.Remaining
            : 0f;
    }

    private Character GetActiveCharacter()
    {
        return characterSwitcher != null
            ? characterSwitcher.ActiveCharacterComponent
            : null;
    }

    private Transform GetExSkillTransform(Character character)
    {
        if (character == null)
            return null;

        return character.transform.Find(exSkillChildName);
    }

    private ExSkill GetExSkill(Character character)
    {
        Transform exSkillTransform = GetExSkillTransform(character);

        if (exSkillTransform == null)
            return null;

        return exSkillTransform.GetComponentInChildren<ExSkill>(true);
    }

    private void RefreshCooldownDebug()
    {
        foreach (KeyValuePair<Character, CharacterCooldown> entry in cooldowns)
        {
            Character character = entry.Key;

            if (character == null)
                continue;

            CharacterCooldown state = entry.Value;

            CharacterCooldownDebug debugEntry =
                cooldownDebug.Find(
                    item => item.Character == character
                );

            if (debugEntry == null)
            {
                debugEntry = new CharacterCooldownDebug(
                    character,
                    state.Duration
                );

                cooldownDebug.Add(debugEntry);
            }

            debugEntry.SetCooldown(
                state.Duration,
                state.Remaining
            );
        }
    }

    private void RefreshSkillUI()
    {
        Character activeCharacter = GetActiveCharacter();
        ExSkill exSkill = GetExSkill(activeCharacter);

        if (skillNameText != null)
        {
            skillNameText.text = exSkill != null
                ? exSkill.SkillName
                : "No EX Skill";
        }

        if (cooldownSlider == null)
            return;

        float remaining = GetCooldownRemaining(activeCharacter);
        float duration = 0f;

        if (activeCharacter != null &&
            cooldowns.TryGetValue(
                activeCharacter,
                out CharacterCooldown state))
        {
            duration = state.Duration;
        }

        cooldownSlider.value = duration > 0f
            ? Mathf.Clamp01(1f - remaining / duration)
            : 1f;
    }


    private void RefreshButton()
    {
        if (exSkillButton == null)
            return;

        Character activeCharacter = GetActiveCharacter();

        if (activeCharacter == null || characterSwitcher == null)
        {
            exSkillButton.interactable = false;
            return;
        }

        bool canUse =
            !characterSwitcher.IsSwitching &&
            GetCooldownRemaining(activeCharacter) <= 0f;

        Health health = activeCharacter.GetComponent<Health>();

        if (health != null && health.IsDead)
            canUse = false;

        Transform exSkillTransform =
            activeCharacter.transform.Find(exSkillChildName);

        // The EX Skill child may be inactive before first use.
        // Search inactive children when checking for the component.
        ExSkill exSkill = exSkillTransform != null
            ? exSkillTransform.GetComponentInChildren<ExSkill>(true)
            : null;

        if (exSkill == null)
            canUse = false;

        exSkillButton.interactable = canUse;
    }

}
