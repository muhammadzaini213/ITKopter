
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Switches the on-field party member (1-3 characters) with a short
/// "switching window" in which the whole party spreads out and fires.
///
/// Partial class, split by concern:
///   CharacterSwitcher.cs           fields, events, public API, Start/Update, buttons
///   CharacterSwitcher.Setup.cs     setup validation and character initialisation
///   CharacterSwitcher.Switching.cs the manual switch routine and interrupted switches
///   CharacterSwitcher.Defeat.cs    a teammate falling (forced swap) and revives
///   CharacterSwitcher.Feel.cs      switch-in feel, i-frame blink, collider fitting, position clamping
/// </summary>
public partial class CharacterSwitcher : MonoBehaviour
{
    [Header("Character Setup")]
    [SerializeField] private GameplayInitializer gameplayInitializer;
    [SerializeField] private Transform characterParent;
    [SerializeField] private BoxCollider2D parentCollider;

    [Header("Controllers")]
    [SerializeField] private CharacterBoundaryController boundaryController;
    [SerializeField] private CharacterCombatController combatController;
    [SerializeField] private CharacterSwitchAnimation switchAnimation;
    [SerializeField] private CharacterLifeController lifeController;

    [Header("Switch Buttons")]
    [Tooltip("Button index must match the character's team index.")]
    [SerializeField] private Button[] switchButtons = new Button[3];

    [Header("Arena Markers")]
    [SerializeField] private Transform frontPosition;
    [SerializeField] private Transform leftPosition;
    [SerializeField] private Transform rightPosition;

    [Header("Switch Settings")]
    [SerializeField, Min(0.01f)] private float switchCooldown = 5f;
    [SerializeField, Min(0f)] private float switchingWindowDuration = 2f;

    [Header("Switch Animation")]
    [SerializeField, Min(0f)] private float retreatDistance = 1f;
    [SerializeField, Min(0.01f)] private float retreatDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float spreadDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float mergeDuration = 0.35f;

    [Header("Feel")]
    [SerializeField, Min(0f), Tooltip("I-frames for the character forced onto the field after a teammate falls.")]
    private float forcedSwapInvulnerability = 1.2f;

    [SerializeField, Min(0f), Tooltip("I-frames for the incoming character after a manual switch.")]
    private float switchInInvulnerability = 0.35f;

    [Header("Events")]
    [SerializeField] private UnityEvent<int> onActiveCharacterChanged;
    [SerializeField] private UnityEvent<float> onCooldownChanged;
    [SerializeField] private UnityEvent<bool> onSwitchingWindowChanged;

    private GameObject[] characters;
    private BoxCollider2D[] childColliders;
    private Health[] characterHealths;
    private bool[] defeatedCharacters;
    private UnityAction[] buttonCallbacks;

    private int activeCharacterIndex;
    private float cooldownTimer;
    private bool isSwitching;
    private bool isInitialized;
    private bool isHandlingDeath;

    private Coroutine switchCoroutine;

    // The Units collider is sized for the 3-character switch formation.
    // Outside a switch only one character is on screen, so it is shrunk to
    // that character; otherwise an invisible margin kept the player away
    // from the play-area walls.
    private Vector2 formationColliderSize;
    private Vector2 formationColliderOffset;
    private bool formationColliderCaptured;

    public int ActiveCharacterIndex => activeCharacterIndex;

    private SwitchRewards rewards;

    /// <summary>Tag-in burst / perfect switch / fresh-in buff / counter tag-in.</summary>
    public SwitchRewards Rewards
    {
        get
        {
            if (rewards == null)
                rewards = GetComponent<SwitchRewards>();

            if (rewards == null)
                rewards = gameObject.AddComponent<SwitchRewards>();

            return rewards;
        }
    }

    /// <summary>Raised when a manual switch starts (the switching window opens).</summary>
    public static event Action ManualSwitchStarted;

    /// <summary>Tutorials: make switching available right now.</summary>
    public void ResetCooldown()
    {
        if (cooldownTimer <= 0f)
            return;

        cooldownTimer = 0f;
        onCooldownChanged?.Invoke(cooldownTimer);
        RefreshButtons();
    }

    /// <summary>Give back a share (0..1) of the full switch cooldown.</summary>
    public void RefundCooldown(float fraction)
    {
        if (cooldownTimer <= 0f || fraction <= 0f)
            return;

        cooldownTimer = Mathf.Max(0f, cooldownTimer - switchCooldown * Mathf.Clamp01(fraction));
        onCooldownChanged?.Invoke(cooldownTimer);
        RefreshButtons();
    }

    /// <summary>
    /// Raised after the on-field character changes (manual switch, forced
    /// swap after a defeat, revive). Team skills use it to move their
    /// effect onto whoever is now fighting. Null = nobody on the field.
    /// </summary>
    public static event Action<Character> FieldCharacterChanged;

    private void NotifyActiveCharacterChanged()
    {
        onActiveCharacterChanged?.Invoke(activeCharacterIndex);

        GameObject active = ActiveCharacter;
        FieldCharacterChanged?.Invoke(active != null ? active.GetComponent<Character>() : null);
    }

    public GameObject ActiveCharacter =>
        isInitialized &&
        characters != null &&
        defeatedCharacters != null &&
        activeCharacterIndex >= 0 &&
        activeCharacterIndex < characters.Length &&
        !defeatedCharacters[activeCharacterIndex]
            ? characters[activeCharacterIndex]
            : null;

    public Character ActiveCharacterComponent =>
        ActiveCharacter != null
            ? ActiveCharacter.GetComponent<Character>()
            : null;

    public int CharacterCount =>
        isInitialized && characters != null
            ? characters.Length
            : 0;

    public bool IsSwitching => isSwitching;

    public float CooldownRemaining => cooldownTimer;

    public float SwitchCooldownDuration => switchCooldown;

    public bool CanSwitch =>
        isInitialized &&
        !AnnihilatorManifestation.IsAnyManifesting &&
        characters != null &&
        characters.Length > 1 &&
        !isSwitching &&
        cooldownTimer <= 0f &&
        lifeController != null &&
        lifeController.HasLivingCharacterOtherThan(activeCharacterIndex);

    private void Start()
    {
        _ = Rewards;

        if (gameplayInitializer == null)
        {
            Debug.LogError(
                "[CharacterSwitcher] GameplayInitializer is not assigned.",
                this);

            enabled = false;
            return;
        }

        if (!gameplayInitializer.IsInitialized &&
            !gameplayInitializer.InitializeGameplay())
        {
            Debug.LogError(
                "[CharacterSwitcher] Gameplay initialization failed.",
                this);

            enabled = false;
            return;
        }

        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        if (boundaryController == null)
            boundaryController = GetComponent<CharacterBoundaryController>();

        if (combatController == null)
            combatController = GetComponent<CharacterCombatController>();

        if (switchAnimation == null)
            switchAnimation = GetComponent<CharacterSwitchAnimation>();

        if (lifeController == null)
            lifeController = GetComponent<CharacterLifeController>();

        if (boundaryController == null ||
            !boundaryController.Initialize(parentCollider))
        {
            Debug.LogError(
                "[CharacterSwitcher] CharacterBoundaryController is missing or invalid.",
                this);

            enabled = false;
            return;
        }

        if (combatController == null ||
            switchAnimation == null ||
            lifeController == null)
        {
            Debug.LogError(
                "[CharacterSwitcher] One or more required controllers are missing.",
                this);

            enabled = false;
            return;
        }

        if (!InitializeCharacters())
        {
            enabled = false;
            return;
        }

        lifeController.Initialize(
            characters,
            characterHealths,
            defeatedCharacters);

        lifeController.CharacterDefeated += HandleCharacterDeath;
        lifeController.CharacterRevived += HandleCharacterRevived;

        CombatFeel.Ensure();

        combatController.Initialize(
            characters,
            defeatedCharacters);

        RegisterButtonCallbacks();

        isInitialized = true;

        formationColliderSize = parentCollider.size;
        formationColliderOffset = parentCollider.offset;
        formationColliderCaptured = true;

        activeCharacterIndex =
            lifeController.FindNextLivingCharacter(
                characters.Length > 0
                    ? characters.Length - 1
                    : 0);

        if (activeCharacterIndex >= 0)
        {
            characters[activeCharacterIndex].transform.localPosition =
                ClampChildLocalPosition(
                    activeCharacterIndex,
                    frontPosition.localPosition);

            characters[activeCharacterIndex].SetActive(true);
        }

        cooldownTimer = 0f;
        isSwitching = false;

        RefreshAutoShoot();
        RefreshButtons();
        FitColliderToActiveCharacter();

        NotifyActiveCharacterChanged();
        onCooldownChanged?.Invoke(cooldownTimer);
        onSwitchingWindowChanged?.Invoke(false);
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        if (cooldownTimer > 0f)
        {
            cooldownTimer = Mathf.Max(
                0f,
                cooldownTimer - Time.deltaTime);

            onCooldownChanged?.Invoke(cooldownTimer);
            RefreshButtons();
        }
    }

    private void RegisterButtonCallbacks()
    {
        buttonCallbacks = new UnityAction[switchButtons.Length];

        for (int i = 0; i < switchButtons.Length; i++)
        {
            Button button = switchButtons[i];

            if (button == null)
                continue;

            int index = i;

            buttonCallbacks[i] = () => SwitchTo(index);
            button.onClick.AddListener(buttonCallbacks[i]);
        }
    }

    public void SwitchTo(int targetIndex)
    {
        if (!CanSwitch)
            return;

        if (targetIndex < 0 || targetIndex >= characters.Length)
            return;

        if (targetIndex == activeCharacterIndex)
            return;

        // No living active character (everyone down) — nothing to switch from.
        if (activeCharacterIndex < 0 || activeCharacterIndex >= characters.Length)
            return;

        if (!lifeController.IsAlive(targetIndex))
            return;

        GameObject outgoing = ActiveCharacter;
        Rewards.OnManualSwitchStarted(outgoing != null ? outgoing.GetComponent<Character>() : null);
        ManualSwitchStarted?.Invoke();

        switchCoroutine = StartCoroutine(
            SwitchRoutine(targetIndex));
    }

    private void RefreshAutoShoot()
    {
        if (!isInitialized || combatController == null)
            return;

        combatController.RefreshAutoShoot(
            activeCharacterIndex,
            isSwitching);
    }

    private void RefreshButtons()
    {
        if (!isInitialized || switchButtons == null)
            return;

        for (int i = 0; i < switchButtons.Length; i++)
        {
            Button button = switchButtons[i];

            if (button == null)
                continue;

            bool characterAlive =
                characters != null &&
                i < characters.Length &&
                lifeController != null &&
                lifeController.IsAlive(i);

            button.interactable =
                characterAlive &&
                CanSwitch &&
                i != activeCharacterIndex;
        }
    }

    public float GetEffectiveMovementSpeed()
    {
        if (!isInitialized || combatController == null)
            return 0f;

        return combatController.GetAverageMovementSpeed(
            activeCharacterIndex,
            isSwitching,
            characterHealths);
    }

    private void OnDestroy()
    {
        if (switchButtons != null && buttonCallbacks != null)
        {
            for (int i = 0; i < switchButtons.Length; i++)
            {
                if (switchButtons[i] == null ||
                    i >= buttonCallbacks.Length ||
                    buttonCallbacks[i] == null)
                {
                    continue;
                }

                switchButtons[i].onClick.RemoveListener(
                    buttonCallbacks[i]);
            }
        }

        if (lifeController != null)
        {
            lifeController.CharacterDefeated -= HandleCharacterDeath;
            lifeController.CharacterRevived -= HandleCharacterRevived;
        }

        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
            switchCoroutine = null;
        }
    }
}
