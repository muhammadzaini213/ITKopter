
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

    [Header("Local Boundary")]
    [SerializeField, Min(0f)] private float boundaryPadding = 0.02f;

    [Header("Auto Shoot")]
    [Tooltip("Name of the AutoShoot component class on each character's child.")]
    [SerializeField] private string autoShootComponentName = "AutoShoot";

    [Header("Events")]
    [SerializeField] private UnityEvent<int> onActiveCharacterChanged;
    [SerializeField] private UnityEvent<float> onCooldownChanged;
    [SerializeField] private UnityEvent<bool> onSwitchingWindowChanged;

    private GameObject[] characters;
    private BoxCollider2D[] childColliders;
    private Health[] characterHealths;
    private bool[] defeatedCharacters;
    private Action[] deathCallbacks;
    private UnityAction[] buttonCallbacks;

    private int activeCharacterIndex;
    private float cooldownTimer;
    private bool isSwitching;
    private bool isInitialized;
    private Coroutine switchCoroutine;

    public int ActiveCharacterIndex => activeCharacterIndex;

    public GameObject ActiveCharacter =>
        isInitialized &&
        characters != null &&
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
        isInitialized ? characters.Length : 0;

    public bool IsSwitching => isSwitching;

    public float CooldownRemaining => cooldownTimer;

    public float SwitchCooldownDuration => switchCooldown;

    public bool CanSwitch =>
        isInitialized &&
        characters != null &&
        characters.Length > 1 &&
        !isSwitching &&
        cooldownTimer <= 0f &&
        HasLivingCharacterOtherThanActive();

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

        if (!InitializeFromGameplayInitializer())
        {
            enabled = false;
            return;
        }

        RegisterButtonCallbacks();

        onActiveCharacterChanged?.Invoke(activeCharacterIndex);
        onCooldownChanged?.Invoke(cooldownTimer);
        onSwitchingWindowChanged?.Invoke(false);

        RefreshAutoShoot();
        RefreshButtons();
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

        if (frontPosition == null)
        {
            Debug.LogError(
                "[CharacterSwitcher] Front Position is not assigned.",
                this);

            return false;
        }

        if (frontPosition.parent != characterParent)
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
            (leftPosition == null || leftPosition.parent != characterParent))
        {
            Debug.LogError(
                "[CharacterSwitcher] A valid Left Position is required for a team of at least two characters.",
                this);

            return false;
        }

        if (teamSize >= 3 &&
            (rightPosition == null || rightPosition.parent != characterParent))
        {
            Debug.LogError(
                "[CharacterSwitcher] A valid Right Position is required for a team of three characters.",
                this);

            return false;
        }

        return true;
    }

    private bool InitializeFromGameplayInitializer()
    {
        var initializedCharacters = gameplayInitializer.InitializedCharacters;
        int count = initializedCharacters.Count;

        if (count < 1 || count > 3)
        {
            Debug.LogError(
                "[CharacterSwitcher] Invalid character count from GameplayInitializer.",
                this);

            return false;
        }

        characters = new GameObject[count];
        childColliders = new BoxCollider2D[count];
        characterHealths = new Health[count];
        defeatedCharacters = new bool[count];
        deathCallbacks = new Action[count];
        buttonCallbacks = new UnityAction[switchButtons.Length];

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

            Rigidbody2D childBody =
                characterObject.GetComponent<Rigidbody2D>();

            if (childBody != null)
            {
                Debug.LogWarning(
                    $"[CharacterSwitcher] Removing Rigidbody2D from '{characterObject.name}' because movement is handled by the parent.",
                    characterObject);

                Destroy(childBody);
            }

            characters[i] = characterObject;
            childColliders[i] = childCollider;

            Health health = characterObject.GetComponent<Health>();
            characterHealths[i] = health;

            if (health != null)
            {
                int index = i;

                deathCallbacks[i] = () => HandleCharacterDeath(index);
                health.OnDeath += deathCallbacks[i];

                if (health.IsDead)
                {
                    defeatedCharacters[i] = true;
                }
            }

            characterObject.transform.localPosition =
                frontPosition.localPosition;

            characterObject.transform.localRotation = Quaternion.identity;
            characterObject.SetActive(false);
        }

        activeCharacterIndex = FindFirstLivingCharacter();

        if (activeCharacterIndex < 0)
        {
            Debug.LogWarning(
                "[CharacterSwitcher] No living characters are available.",
                this);

            isInitialized = true;
            RefreshButtons();
            return true;
        }

        characters[activeCharacterIndex].SetActive(true);

        cooldownTimer = 0f;
        isSwitching = false;
        isInitialized = true;

        RefreshAutoShoot();

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

        // Detect deaths even if an event was missed.
        for (int i = 0; i < characters.Length; i++)
        {
            if (defeatedCharacters[i])
                continue;

            Health health = characterHealths[i];

            if (health != null && health.IsDead)
                HandleCharacterDeath(i);
        }
    }

    private void RegisterButtonCallbacks()
    {
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

        if (defeatedCharacters[targetIndex])
            return;

        if (targetIndex == activeCharacterIndex)
            return;

        Health targetHealth = characterHealths[targetIndex];

        if (targetHealth != null && targetHealth.IsDead)
        {
            HandleCharacterDeath(targetIndex);
            return;
        }

        switchCoroutine = StartCoroutine(SwitchRoutine(targetIndex));
    }

    private IEnumerator SwitchRoutine(int targetIndex)
    {
        isSwitching = true;

        RefreshAutoShoot();

        onSwitchingWindowChanged?.Invoke(true);
        RefreshButtons();

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

            formationTargets[i] = remainingPositionIndex == 0
                ? leftPosition.localPosition
                : rightPosition.localPosition;

            remainingPositionIndex++;
        }

        for (int i = 0; i < count; i++)
        {
            if (defeatedCharacters[i])
                continue;

            formationTargets[i] =
                ClampChildLocalPosition(i, formationTargets[i]);
        }

        Vector3 retreatPos = ClampChildLocalPosition(
            fromIndex,
            characters[fromIndex].transform.localPosition +
            Vector3.down * retreatDistance);

        Vector3[] retreatTargets = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            retreatTargets[i] = characters[i].transform.localPosition;

            if (defeatedCharacters[i])
                continue;

            if (i == fromIndex)
                retreatTargets[i] = retreatPos;
        }

        yield return AnimateLivingCharacters(
            retreatTargets,
            retreatDuration);

        if (!IsSwitchTargetAlive(targetIndex))
        {
            FinishInterruptedSwitch();
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            if (defeatedCharacters[i])
            {
                characters[i].SetActive(false);
                continue;
            }

            characters[i].transform.localPosition =
                ClampChildLocalPosition(i, retreatPos);

            characters[i].SetActive(true);
        }

        RefreshAutoShoot();

        yield return AnimateLivingCharacters(
            formationTargets,
            spreadDuration);

        if (!IsSwitchTargetAlive(targetIndex))
        {
            FinishInterruptedSwitch();
            yield break;
        }

        if (switchingWindowDuration > 0f)
        {
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
        }

        if (!IsSwitchTargetAlive(targetIndex))
        {
            FinishInterruptedSwitch();
            yield break;
        }

        Vector3 mainPos = formationTargets[targetIndex];
        Vector3[] mergeTargets = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            mergeTargets[i] = characters[i].transform.localPosition;

            if (!defeatedCharacters[i])
                mergeTargets[i] = mainPos;
        }

        yield return AnimateLivingCharacters(
            mergeTargets,
            mergeDuration);

        if (!IsSwitchTargetAlive(targetIndex))
        {
            FinishInterruptedSwitch();
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            if (defeatedCharacters[i] || i != targetIndex)
            {
                characters[i].SetActive(false);
            }
        }

        characters[targetIndex].transform.localPosition =
            ClampChildLocalPosition(targetIndex, frontLocal);

        characters[targetIndex].SetActive(true);

        activeCharacterIndex = targetIndex;
        cooldownTimer = switchCooldown;
        isSwitching = false;
        switchCoroutine = null;

        RefreshAutoShoot();

        onActiveCharacterChanged?.Invoke(activeCharacterIndex);
        onCooldownChanged?.Invoke(cooldownTimer);
        onSwitchingWindowChanged?.Invoke(false);

        RefreshButtons();

        Debug.Log(
            $"[CharacterSwitcher] Switch completed. Active index: {activeCharacterIndex}, Active character: {ActiveCharacter?.name}",
            this);
    }

    private IEnumerator AnimateLivingCharacters(
        Vector3[] targets,
        float duration)
    {
        int count = characters.Length;
        Vector3[] starts = new Vector3[count];

        for (int i = 0; i < count; i++)
            starts[i] = characters[i].transform.localPosition;

        if (duration <= 0f)
        {
            for (int i = 0; i < count; i++)
            {
                if (defeatedCharacters[i])
                {
                    characters[i].SetActive(false);
                    continue;
                }

                characters[i].transform.localPosition =
                    ClampChildLocalPosition(i, targets[i]);
            }

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / duration));

            for (int i = 0; i < count; i++)
            {
                if (defeatedCharacters[i])
                {
                    characters[i].SetActive(false);
                    continue;
                }

                Vector3 desired = Vector3.Lerp(
                    starts[i],
                    targets[i],
                    t);

                characters[i].transform.localPosition =
                    ClampChildLocalPosition(i, desired);
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            if (defeatedCharacters[i])
            {
                characters[i].SetActive(false);
                continue;
            }

            characters[i].transform.localPosition =
                ClampChildLocalPosition(i, targets[i]);
        }
    }

    private bool IsSwitchTargetAlive(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= characters.Length)
            return false;

        if (defeatedCharacters[targetIndex])
            return false;

        Health health = characterHealths[targetIndex];

        if (health != null && health.IsDead)
        {
            HandleCharacterDeath(targetIndex);
            return false;
        }

        return true;
    }

    private void HandleCharacterDeath(int characterIndex)
    {
        if (!isInitialized ||
            characters == null ||
            characterIndex < 0 ||
            characterIndex >= characters.Length ||
            defeatedCharacters[characterIndex])
        {
            return;
        }

        defeatedCharacters[characterIndex] = true;

        if (characters[characterIndex] != null)
            characters[characterIndex].SetActive(false);

        bool activeCharacterDied =
            characterIndex == activeCharacterIndex;

        // Cancel any running transition so it cannot reactivate a defeated character.
        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
            switchCoroutine = null;
        }

        isSwitching = false;

        if (activeCharacterDied)
        {
            int nextIndex = FindFirstLivingCharacter();

            if (nextIndex >= 0)
            {
                activeCharacterIndex = nextIndex;

                characters[nextIndex].transform.localPosition =
                    ClampChildLocalPosition(
                        nextIndex,
                        frontPosition.localPosition);

                characters[nextIndex].SetActive(true);

                // Forced switch is immediate and does not wait for cooldown.
                cooldownTimer = 0f;
            }
            else
            {
                // No living characters remain.
                for (int i = 0; i < characters.Length; i++)
                    characters[i].SetActive(false);
            }
        }

        // Enforce the defeated state immediately.
        for (int i = 0; i < characters.Length; i++)
        {
            if (defeatedCharacters[i])
                characters[i].SetActive(false);
        }

        RefreshAutoShoot();
        RefreshButtons();

        onActiveCharacterChanged?.Invoke(activeCharacterIndex);
        onCooldownChanged?.Invoke(cooldownTimer);
        onSwitchingWindowChanged?.Invoke(false);

        Debug.Log(
            $"[CharacterSwitcher] Character at index {characterIndex} was defeated.",
            this);
    }

    private int FindFirstLivingCharacter()
    {
        if (characters == null)
            return -1;

        // Prefer the next character after the current active index.
        for (int offset = 1; offset <= characters.Length; offset++)
        {
            int index = (activeCharacterIndex + offset) % characters.Length;

            if (defeatedCharacters[index])
                continue;

            Health health = characterHealths[index];

            if (health != null && health.IsDead)
            {
                defeatedCharacters[index] = true;
                characters[index].SetActive(false);
                continue;
            }

            return index;
        }

        return -1;
    }

    private bool HasLivingCharacterOtherThanActive()
    {
        if (characters == null || defeatedCharacters == null)
            return false;

        for (int i = 0; i < characters.Length; i++)
        {
            if (i == activeCharacterIndex || defeatedCharacters[i])
                continue;

            Health health = characterHealths[i];

            if (health == null || !health.IsDead)
                return true;
        }

        return false;
    }

    private Vector3 ClampChildLocalPosition(
        int characterIndex,
        Vector3 desiredLocalPosition)
    {
        BoxCollider2D childCollider = childColliders[characterIndex];
        Transform childTransform = characters[characterIndex].transform;

        Vector2 parentSize = parentCollider.size;
        Vector2 parentOffset = parentCollider.offset;

        Vector2 childScale = new Vector2(
            Mathf.Abs(childTransform.localScale.x),
            Mathf.Abs(childTransform.localScale.y));

        Vector2 halfChild = new Vector2(
            childCollider.size.x * childScale.x * 0.5f,
            childCollider.size.y * childScale.y * 0.5f);

        Vector2 childOffset = new Vector2(
            childCollider.offset.x * childScale.x,
            childCollider.offset.y * childScale.y);

        float minX = parentOffset.x -
                     parentSize.x * 0.5f +
                     halfChild.x + boundaryPadding;

        float maxX = parentOffset.x +
                     parentSize.x * 0.5f -
                     halfChild.x - boundaryPadding;

        float minY = parentOffset.y -
                     parentSize.y * 0.5f +
                     halfChild.y + boundaryPadding;

        float maxY = parentOffset.y +
                     parentSize.y * 0.5f -
                     halfChild.y - boundaryPadding;

        if (minX > maxX)
        {
            desiredLocalPosition.x = parentOffset.x;
        }
        else
        {
            desiredLocalPosition.x = Mathf.Clamp(
                desiredLocalPosition.x,
                minX - childOffset.x,
                maxX - childOffset.x);
        }

        if (minY > maxY)
        {
            desiredLocalPosition.y = parentOffset.y;
        }
        else
        {
            desiredLocalPosition.y = Mathf.Clamp(
                desiredLocalPosition.y,
                minY - childOffset.y,
                maxY - childOffset.y);
        }

        return desiredLocalPosition;
    }

    private void SetAutoShoot(GameObject character, bool isEnabled)
    {
        if (character == null)
            return;

        MonoBehaviour[] components =
            character.GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour component in components)
        {
            if (component == null)
                continue;

            if (component.GetType().Name != autoShootComponentName)
                continue;

            component.enabled = isEnabled;
        }
    }

    private void RefreshAutoShoot()
    {
        if (!isInitialized || characters == null)
            return;

        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i] == null)
                continue;

            bool shouldShoot =
                !defeatedCharacters[i] &&
                (isSwitching || i == activeCharacterIndex);

            SetAutoShoot(characters[i], shouldShoot);
        }
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

            bool characterExists =
                characters != null && i < characters.Length;

            bool characterAlive =
                characterExists && !defeatedCharacters[i];

            button.interactable =
                characterAlive &&
                CanSwitch &&
                i != activeCharacterIndex;
        }
    }

    private void FinishInterruptedSwitch()
    {
        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
            switchCoroutine = null;
        }

        isSwitching = false;

        int livingIndex = FindLivingCharacterForRecovery();

        for (int i = 0; i < characters.Length; i++)
        {
            if (defeatedCharacters[i])
            {
                characters[i].SetActive(false);
                continue;
            }

            characters[i].SetActive(i == livingIndex);

            if (i == livingIndex)
            {
                characters[i].transform.localPosition =
                    ClampChildLocalPosition(
                        i,
                        frontPosition.localPosition);
            }
        }

        if (livingIndex >= 0)
            activeCharacterIndex = livingIndex;

        RefreshAutoShoot();
        RefreshButtons();

        onActiveCharacterChanged?.Invoke(activeCharacterIndex);
        onCooldownChanged?.Invoke(cooldownTimer);
        onSwitchingWindowChanged?.Invoke(false);
    }

    private int FindLivingCharacterForRecovery()
    {
        if (activeCharacterIndex >= 0 &&
            activeCharacterIndex < characters.Length &&
            !defeatedCharacters[activeCharacterIndex])
        {
            Health activeHealth = characterHealths[activeCharacterIndex];

            if (activeHealth == null || !activeHealth.IsDead)
                return activeCharacterIndex;
        }

        return FindFirstLivingCharacter();
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

        if (characterHealths != null && deathCallbacks != null)
        {
            for (int i = 0; i < characterHealths.Length; i++)
            {
                if (characterHealths[i] != null &&
                    deathCallbacks[i] != null)
                {
                    characterHealths[i].OnDeath -= deathCallbacks[i];
                }
            }
        }

        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
            switchCoroutine = null;
        }
    }


    public float GetEffectiveMovementSpeed()
    {
        if (!isInitialized)
            return 0f;

        if (!isSwitching)
        {
            Character activeCharacter = ActiveCharacterComponent;

            if (activeCharacter == null)
                return 0f;

            Health activeHealth =
                ActiveCharacter.GetComponent<Health>();

            if (activeHealth != null && activeHealth.IsDead)
                return 0f;

            return activeCharacter.MovementSpeed;
        }

        float totalSpeed = 0f;
        int validCharacterCount = 0;

        for (int i = 0; i < characters.Length; i++)
        {
            if (defeatedCharacters[i])
                continue;

            Character character =
                characters[i].GetComponent<Character>();

            Health health =
                characterHealths[i];

            if (character == null)
                continue;

            if (health != null && health.IsDead)
                continue;

            totalSpeed += character.MovementSpeed;
            validCharacterCount++;
        }

        if (validCharacterCount == 0)
            return 0f;

        return totalSpeed / validCharacterCount;
    }

}
