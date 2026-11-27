
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CharacterSwitcher : MonoBehaviour
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

        onActiveCharacterChanged?.Invoke(activeCharacterIndex);
        onCooldownChanged?.Invoke(cooldownTimer);
        onSwitchingWindowChanged?.Invoke(false);
    }

    private bool ValidateSetup()
    {
        if (characterParent == null || parentCollider == null)
        {
            Debug.LogError(
                "[CharacterSwitcher] Assign Character Parent and Parent Collider.",
                this);

            return false;
        }

        if (parentCollider.transform != characterParent)
        {
            Debug.LogError(
                "[CharacterSwitcher] Parent Collider must belong to Character Parent.",
                this);

            return false;
        }

        if (frontPosition == null ||
            frontPosition.parent != characterParent)
        {
            Debug.LogError(
                "[CharacterSwitcher] Front Position must be a direct child of Character Parent.",
                this);

            return false;
        }

        if (switchButtons == null || switchButtons.Length < 3)
        {
            Debug.LogError(
                "[CharacterSwitcher] Assign an array with three button slots.",
                this);

            return false;
        }

        int teamSize = gameplayInitializer.InitializedCharacters.Count;

        if (teamSize < 1 || teamSize > 3)
        {
            Debug.LogError(
                "[CharacterSwitcher] Gameplay team must contain between one and three characters.",
                this);

            return false;
        }

        if (teamSize >= 2 &&
            (leftPosition == null ||
             leftPosition.parent != characterParent))
        {
            Debug.LogError(
                "[CharacterSwitcher] A valid Left Position is required for a team of at least two characters.",
                this);

            return false;
        }

        if (teamSize >= 3 &&
            (rightPosition == null ||
             rightPosition.parent != characterParent))
        {
            Debug.LogError(
                "[CharacterSwitcher] A valid Right Position is required for a team of three characters.",
                this);

            return false;
        }

        return true;
    }

    private bool InitializeCharacters()
    {
        var initializedCharacters =
            gameplayInitializer.InitializedCharacters;

        int count = initializedCharacters.Count;

        if (count < 1 || count > 3)
        {
            Debug.LogError(
                "[CharacterSwitcher] Invalid character count.",
                this);

            return false;
        }

        characters = new GameObject[count];
        childColliders = new BoxCollider2D[count];
        characterHealths = new Health[count];
        defeatedCharacters = new bool[count];

        for (int i = 0; i < count; i++)
        {
            Character character = initializedCharacters[i];

            if (character == null)
            {
                Debug.LogError(
                    $"[CharacterSwitcher] Character at index {i} is missing.",
                    this);

                return false;
            }

            GameObject characterObject = character.gameObject;

            if (characterObject.transform.parent != characterParent)
            {
                Debug.LogError(
                    $"[CharacterSwitcher] Character '{characterObject.name}' must be parented to Character Parent.",
                    characterObject);

                return false;
            }

            BoxCollider2D childCollider =
                characterObject.GetComponent<BoxCollider2D>();

            if (childCollider == null)
            {
                Debug.LogError(
                    $"[CharacterSwitcher] '{characterObject.name}' needs a BoxCollider2D on its root.",
                    characterObject);

                return false;
            }

            Rigidbody2D body =
                characterObject.GetComponent<Rigidbody2D>();

            if (body != null)
            {
                Debug.LogWarning(
                    $"[CharacterSwitcher] Removing Rigidbody2D from '{characterObject.name}' because movement is handled by the parent.",
                    characterObject);

                Destroy(body);
            }

            characters[i] = characterObject;
            childColliders[i] = childCollider;
            characterHealths[i] =
                characterObject.GetComponent<Health>();

            characterObject.transform.localPosition =
                frontPosition.localPosition;

            characterObject.transform.localRotation =
                Quaternion.identity;

            characterObject.SetActive(false);

            if (characterHealths[i] != null &&
                characterHealths[i].IsDead)
            {
                defeatedCharacters[i] = true;
            }
        }

        return true;
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

        switchCoroutine = StartCoroutine(
            SwitchRoutine(targetIndex));
    }

    private IEnumerator SwitchRoutine(int targetIndex)
    {
        isSwitching = true;
        RestoreFormationCollider();

        RefreshAutoShoot();
        RefreshButtons();

        onSwitchingWindowChanged?.Invoke(true);

        int fromIndex = activeCharacterIndex;
        int count = characters.Length;

        Vector3 frontLocal = frontPosition.localPosition;
        Vector3[] formationTargets = new Vector3[count];

        formationTargets[targetIndex] = frontLocal;

        int remainingPositionIndex = 0;

        for (int i = 0; i < count; i++)
        {
            if (i == targetIndex)
                continue;

            // Defeated members don't take a slot (a living teammate used to
            // end up on the far slot next to an empty one).
            if (!lifeController.IsAlive(i))
            {
                formationTargets[i] = characters[i].transform.localPosition;
                continue;
            }

            formationTargets[i] =
                remainingPositionIndex == 0
                    ? leftPosition.localPosition
                    : rightPosition.localPosition;

            remainingPositionIndex++;
        }

        for (int i = 0; i < count; i++)
        {
            if (!lifeController.IsAlive(i))
                continue;

            formationTargets[i] =
                ClampChildLocalPosition(i, formationTargets[i]);
        }

        Vector3 retreatPosition =
            ClampChildLocalPosition(
                fromIndex,
                characters[fromIndex].transform.localPosition +
                Vector3.down * retreatDistance);

        Vector3[] retreatTargets = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            retreatTargets[i] =
                characters[i].transform.localPosition;

            if (lifeController.IsAlive(i) && i == fromIndex)
                retreatTargets[i] = retreatPosition;
        }

        yield return switchAnimation.Animate(
            characters,
            defeatedCharacters,
            retreatTargets,
            retreatDuration,
            ClampChildLocalPosition);

        if (!IsSwitchTargetAlive(targetIndex))
        {
            FinishInterruptedSwitch();
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            if (!lifeController.IsAlive(i))
            {
                characters[i].SetActive(false);
                continue;
            }

            characters[i].transform.localPosition =
                ClampChildLocalPosition(i, retreatPosition);

            characters[i].SetActive(true);
        }

        RefreshAutoShoot();

        yield return switchAnimation.Animate(
            characters,
            defeatedCharacters,
            formationTargets,
            spreadDuration,
            ClampChildLocalPosition);

        if (!IsSwitchTargetAlive(targetIndex))
        {
            FinishInterruptedSwitch();
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < switchingWindowDuration)
        {
            if (!IsSwitchTargetAlive(targetIndex))
            {
                FinishInterruptedSwitch();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Vector3 mainPosition = formationTargets[targetIndex];
        Vector3[] mergeTargets = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            mergeTargets[i] =
                characters[i].transform.localPosition;

            if (lifeController.IsAlive(i))
                mergeTargets[i] = mainPosition;
        }

        yield return switchAnimation.Animate(
            characters,
            defeatedCharacters,
            mergeTargets,
            mergeDuration,
            ClampChildLocalPosition);

        if (!IsSwitchTargetAlive(targetIndex))
        {
            FinishInterruptedSwitch();
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            characters[i].SetActive(
                i == targetIndex &&
                lifeController.IsAlive(i));
        }

        characters[targetIndex].transform.localPosition =
            ClampChildLocalPosition(
                targetIndex,
                frontLocal);

        activeCharacterIndex = targetIndex;
        cooldownTimer = switchCooldown;
        isSwitching = false;
        switchCoroutine = null;

        RefreshAutoShoot();
        RefreshButtons();
        FitColliderToActiveCharacter();
        PlaySwitchInFeel(targetIndex, switchInInvulnerability, forced: false);

        onActiveCharacterChanged?.Invoke(activeCharacterIndex);
        onCooldownChanged?.Invoke(cooldownTimer);
        onSwitchingWindowChanged?.Invoke(false);

        Debug.Log(
            $"[CharacterSwitcher] Switch completed. Active index: {activeCharacterIndex}, Active character: {ActiveCharacter?.name}",
            this);
    }

    private bool IsSwitchTargetAlive(int targetIndex)
    {
        return lifeController != null &&
               lifeController.IsAlive(targetIndex);
    }

    private void HandleCharacterDeath(int characterIndex)
    {
        if (!isInitialized || isHandlingDeath)
            return;

        isHandlingDeath = true;

        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
            switchCoroutine = null;
        }

        bool wasSwitching = isSwitching;
        isSwitching = false;

        // A defeat mid-switch used to leave the spread-out teammates active
        // (visible and hittable but not shooting). Settle the formation.
        if (wasSwitching && characterIndex != activeCharacterIndex &&
            lifeController.IsAlive(activeCharacterIndex))
        {
            for (int i = 0; i < characters.Length; i++)
                characters[i].SetActive(i == activeCharacterIndex);

            characters[activeCharacterIndex].transform.localPosition =
                ClampChildLocalPosition(activeCharacterIndex, frontPosition.localPosition);
        }

        bool forcedSwap = false;

        if (characterIndex == activeCharacterIndex ||
            !lifeController.IsAlive(activeCharacterIndex))
        {
            int nextIndex =
                lifeController.FindNextLivingCharacter(
                    activeCharacterIndex);

            if (nextIndex >= 0)
            {
                activeCharacterIndex = nextIndex;

                characters[nextIndex].transform.localPosition =
                    ClampChildLocalPosition(
                        nextIndex,
                        frontPosition.localPosition);

                for (int i = 0; i < characters.Length; i++)
                    characters[i].SetActive(i == nextIndex);

                cooldownTimer = 0f;
                forcedSwap = true;
            }
            else
            {
                activeCharacterIndex = -1;

                for (int i = 0; i < characters.Length; i++)
                    characters[i].SetActive(false);
            }
        }

        for (int i = 0; i < characters.Length; i++)
        {
            if (!lifeController.IsAlive(i))
                characters[i].SetActive(false);
        }

        RefreshAutoShoot();
        RefreshButtons();

        onActiveCharacterChanged?.Invoke(activeCharacterIndex);
        onCooldownChanged?.Invoke(cooldownTimer);
        onSwitchingWindowChanged?.Invoke(false);

        // Losing a teammate should land hard.
        CombatFeel.HitStop(0.2f, 0.02f);
        CombatFeel.Shake(0.45f, 0.45f);
        CombatFeel.Flash(new Color(1f, 0.1f, 0.1f, 0.35f), 0.35f);

        if (forcedSwap)
            PlaySwitchInFeel(activeCharacterIndex, forcedSwapInvulnerability, forced: true);

        FitColliderToActiveCharacter();
        isHandlingDeath = false;
    }

    private void HandleCharacterRevived(int characterIndex)
    {
        if (!isInitialized)
            return;

        // Revived members stay benched until switched in.
        if (characterIndex != activeCharacterIndex && !isSwitching &&
            characterIndex >= 0 && characterIndex < characters.Length)
        {
            characters[characterIndex].SetActive(false);
        }

        RefreshAutoShoot();
        RefreshButtons();
    }

    private void PlaySwitchInFeel(int index, float invulnerability, bool forced)
    {
        if (index < 0 || index >= characters.Length || characters[index] == null)
            return;

        GameObject character = characters[index];
        Vector3 pos = character.transform.position;
        float radius = SkillVFX.GetVisualRadius(character.transform);
        Color color = forced ? new Color(1f, 0.45f, 0.35f) : new Color(0.098f, 0.941f, 1f);

        SkillVFX.Ghost(character.transform, color.WithAlpha(0.7f), 0.3f, 1.5f);
        SkillVFX.Ring(pos, color, radius * 0.5f, radius * 2.6f, 0.3f, 0.08f);
        SkillVFX.Sparks(pos, color, 10, 4f, 0.3f);

        if (!forced)
            CombatFeel.Shake(0.08f, 0.12f);

        if (invulnerability > 0f && characterHealths[index] != null)
        {
            characterHealths[index].SetInvulnerable(invulnerability);
            StartCoroutine(InvulnerabilityBlink(index, invulnerability));
        }
    }

    private IEnumerator InvulnerabilityBlink(int index, float duration)
    {
        GameObject character = characters[index];
        SpriteRenderer[] sprites = character.GetComponentsInChildren<SpriteRenderer>(true);
        float elapsed = 0f;
        bool dim = false;

        while (elapsed < duration && character != null && character.activeInHierarchy)
        {
            dim = !dim;

            foreach (var sr in sprites)
            {
                if (sr == null) continue;
                Color c = sr.color;
                c.a = dim ? 0.35f : 1f;
                sr.color = c;
            }

            yield return new WaitForSeconds(0.07f);
            elapsed += 0.07f;
        }

        foreach (var sr in sprites)
        {
            if (sr == null) continue;
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
    }

    /// <summary>Shrinks the movement collider to the active character (no switch running).</summary>
    private void FitColliderToActiveCharacter()
    {
        if (!formationColliderCaptured || parentCollider == null || isSwitching)
            return;

        if (activeCharacterIndex < 0 || activeCharacterIndex >= characters.Length)
            return;

        BoxCollider2D child = childColliders[activeCharacterIndex];
        Transform childTransform = characters[activeCharacterIndex].transform;

        if (child == null)
            return;

        Vector2 scale = new Vector2(Mathf.Abs(childTransform.localScale.x), Mathf.Abs(childTransform.localScale.y));
        Vector2 size = Vector2.Scale(child.size, scale) + Vector2.one * 0.06f;
        Vector2 center = (Vector2)childTransform.localPosition + Vector2.Scale(child.offset, scale);

        parentCollider.size = size;
        parentCollider.offset = center;
    }

    /// <summary>Back to the full formation size while characters spread out.</summary>
    private void RestoreFormationCollider()
    {
        if (!formationColliderCaptured || parentCollider == null)
            return;

        parentCollider.size = formationColliderSize;
        parentCollider.offset = formationColliderOffset;
    }

    private Vector3 ClampChildLocalPosition(
        int characterIndex,
        Vector3 desiredLocalPosition)
    {
        if (boundaryController == null ||
            characterIndex < 0 ||
            characterIndex >= characters.Length)
        {
            return desiredLocalPosition;
        }

        return boundaryController.ClampChildLocalPosition(
            childColliders[characterIndex],
            characters[characterIndex].transform,
            desiredLocalPosition);
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

    private void FinishInterruptedSwitch()
    {
        switchCoroutine = null;
        isSwitching = false;

        int livingIndex = -1;

        if (lifeController != null &&
            lifeController.IsAlive(activeCharacterIndex))
        {
            livingIndex = activeCharacterIndex;
        }
        else if (lifeController != null)
        {
            livingIndex =
                lifeController.FindNextLivingCharacter(
                    activeCharacterIndex);
        }

        activeCharacterIndex = livingIndex;

        for (int i = 0; i < characters.Length; i++)
        {
            bool shouldBeActive =
                i == livingIndex &&
                lifeController.IsAlive(i);

            characters[i].SetActive(shouldBeActive);

            if (shouldBeActive)
            {
                characters[i].transform.localPosition =
                    ClampChildLocalPosition(
                        i,
                        frontPosition.localPosition);
            }
        }

        RefreshAutoShoot();
        RefreshButtons();

        onActiveCharacterChanged?.Invoke(activeCharacterIndex);
        onCooldownChanged?.Invoke(cooldownTimer);
        onSwitchingWindowChanged?.Invoke(false);
        FitColliderToActiveCharacter();
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
