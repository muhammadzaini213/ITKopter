
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using Slafurry.Systems.Audio;

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

    private UnityAction useExSkillAction;

    private class CharacterCooldown
    {
        public float Duration;
        public float Remaining;
    }

    public Character CooldownCharacter => GetActiveCharacter();

    public float CooldownDuration
    {
        get
        {
            Character character = GetActiveCharacter();

            return character != null &&
                   cooldowns.TryGetValue(character, out CharacterCooldown state)
                ? state.Duration
                : 0f;
        }
    }

    private readonly Dictionary<Character, CharacterCooldown> cooldowns =
        new Dictionary<Character, CharacterCooldown>();

    public float CooldownRemaining =>
        GetCooldownRemaining(GetActiveCharacter());

    public bool IsCooldownActive => CooldownRemaining > 0f;

    private void Awake()
    {
        useExSkillAction = new UnityAction(UseExSkill);

        if (characterSwitcher == null)
        {
        characterSwitcher =
            UnityEngine.Object.FindFirstObjectByType<CharacterSwitcher>();
        }

        if (gameplayInitializer == null)
        {
        gameplayInitializer =
            UnityEngine.Object.FindFirstObjectByType<GameplayInitializer>();
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
        if (exSkillButton != null)
        {
            exSkillButton.onClick.AddListener(useExSkillAction);
        }
        else
        {
            Debug.LogError("[ExSkillController] Button reference is missing.", this);
        }
    }

    private void OnDisable()
    {
        if (exSkillButton != null)
            exSkillButton.onClick.RemoveListener(useExSkillAction);
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

        if (characterSwitcher.IsSwitching || AnnihilatorManifestation.IsAnyManifesting)
            return;

        float remaining = GetCooldownRemaining(activeCharacter);

        if (remaining > 0f)
        {
            ShowNotReady(activeCharacter, remaining);
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

        PlaySkillMoment(activeCharacter, exSkill);

        if (exSkill.ActivationSfx != null)
            Audio.PlaySFX2D(exSkill.ActivationSfx);

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

    }

    [Header("Skill Moment")]
    [SerializeField, Tooltip("Slow-motion beat when an EX skill fires so the player notices it.")]
    private bool slowMoOnActivate = true;

    [SerializeField, Min(0f), Tooltip("Real-time seconds of the slow-mo beat.")]
    private float slowMoDuration = 0.75f;

    [SerializeField, Range(0.02f, 1f)]
    private float slowMoTimeScale = 0.15f;

    [SerializeField, Tooltip("Show the character's activation line as a speaker toast.")]
    private bool showSkillDialogue = true;

    [SerializeField, Min(0.5f)]
    private float skillDialogueDuration = 2.6f;

    // The "EX fired" beat: slow-mo + letterbox, callout/afterimage/burst,
    // hitstop + shake, and the character's line as a speaker toast.
    private void PlaySkillMoment(Character character, ExSkill exSkill)
    {
        if (character == null || exSkill == null)
            return;

        if (slowMoOnActivate && slowMoDuration > 0f)
            CombatFeel.SlowMo(slowMoDuration, slowMoTimeScale, letterbox: true);

        if (exSkill.PlayDefaultActivationFeel)
            SkillVFX.Activation(character, exSkill.SkillName, exSkill.SkillColor);

        if (!showSkillDialogue)
            return;

        string line = exSkill.PickActivationLine();

        if (string.IsNullOrWhiteSpace(line) ||
            StorySystem.Instance == null ||
            StorySystem.Instance.Toasts == null)
        {
            return;
        }

        var toast = new StoryToastLine
        {
            speaker = character.Data,
            speakerName = character.CharacterName,
            text = line,
            duration = skillDialogueDuration
        };

        // A skill that opens a paused popup (Trady's choice) would have the
        // toast drawn over it — show the line once the popup has closed.
        if (Slafurry.Systems.Pause.PauseSystem.Instance != null &&
            Slafurry.Systems.Pause.PauseSystem.Instance.IsPaused)
        {
            StartCoroutine(ShowToastWhenUnpaused(toast));
            return;
        }

        StorySystem.Instance.Toasts.Show(toast);
    }

    private System.Collections.IEnumerator ShowToastWhenUnpaused(StoryToastLine toast)
    {
        float waited = 0f;

        while (Slafurry.Systems.Pause.PauseSystem.Instance != null &&
               Slafurry.Systems.Pause.PauseSystem.Instance.IsPaused &&
               waited < 30f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (StorySystem.Instance != null && StorySystem.Instance.Toasts != null)
            StorySystem.Instance.Toasts.Show(toast);
    }

    private float lastNotReadyTime = -10f;

    private void ShowNotReady(Character character, float remaining)
    {
        if (character == null || Time.unscaledTime - lastNotReadyTime < 0.5f)
            return;

        lastNotReadyTime = Time.unscaledTime;

        float radius = SkillVFX.GetVisualRadius(character.transform);
        SkillVFX.Text(
            character.transform.position + Vector3.up * (radius + 0.4f),
            $"EX {Mathf.CeilToInt(remaining)}s",
            new Color(0.7f, 0.7f, 0.75f),
            3f, 0.6f, 0.4f);
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

        // Prefer a child named "ExSkill".
        Transform child = character.transform.Find(exSkillChildName);

        if (child != null)
            return child;

        // Fallback: EX Skill component placed on the same GameObject as the Character.
        if (character.GetComponent<ExSkill>() != null)
            return character.transform;

        return null;
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
            !AnnihilatorManifestation.IsAnyManifesting &&
            GetCooldownRemaining(activeCharacter) <= 0f;

        Health health = activeCharacter.GetComponent<Health>();

        if (health != null && health.IsDead)
            canUse = false;

        Transform exSkillTransform = GetExSkillTransform(activeCharacter);

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
