using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CharacterSwitcher : MonoBehaviour
{
    [Header("Character Parent")]
    [SerializeField] private Transform characterParent;
    [SerializeField] private BoxCollider2D parentCollider;

    [Header("Characters - Exactly 3")]
    [SerializeField] private GameObject[] characters = new GameObject[3];

    [Header("Switch Buttons - Exactly 3")]
    [SerializeField] private Button[] switchButtons = new Button[3];

    [Header("Arena Markers (direct children of Character Parent)")]
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

    [Header("Events")]
    [SerializeField] private UnityEvent<int> onActiveCharacterChanged;
    [SerializeField] private UnityEvent<float> onCooldownChanged;
    [SerializeField] private UnityEvent<bool> onSwitchingWindowChanged;

    private readonly BoxCollider2D[] childColliders = new BoxCollider2D[3];
    private readonly UnityAction[] buttonCallbacks = new UnityAction[3];

    private int activeCharacterIndex;
    private float cooldownTimer;
    private bool isSwitching;
    private bool isInitialized;

    public int ActiveCharacterIndex => activeCharacterIndex;
    public GameObject ActiveCharacter =>
        isInitialized ? characters[activeCharacterIndex] : null;
    public bool IsSwitching => isSwitching;
    public float CooldownRemaining => cooldownTimer;

    public bool CanSwitch =>
        isInitialized && !isSwitching && cooldownTimer <= 0f;

    private void Awake()
    {
        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        for (int i = 0; i < characters.Length; i++)
        {
            childColliders[i] =
                characters[i].GetComponent<BoxCollider2D>();

            if (childColliders[i] == null)
            {
                Debug.LogError(
                    $"[CharacterSwitcher] {characters[i].name} needs a BoxCollider2D.",
                    characters[i]
                );

                enabled = false;
                return;
            }

            // Rigidbody2D di child membuat karakter tidak ikut parent.
            // Hanya parent yang boleh punya Rigidbody2D.
            Rigidbody2D childBody = characters[i].GetComponent<Rigidbody2D>();
            if (childBody != null)
            {
                Debug.LogWarning(
                    $"[CharacterSwitcher] {characters[i].name} punya Rigidbody2D sendiri, " +
                    "sehingga tidak ikut parent. Rigidbody2D dihapus otomatis. " +
                    "Hapus juga secara manual dari prefab/scene.",
                    characters[i]
                );
                Destroy(childBody);
            }
        }

        activeCharacterIndex = 0;

        for (int i = 0; i < characters.Length; i++)
            characters[i].SetActive(i == activeCharacterIndex);

        RegisterButtonCallbacks();

        isInitialized = true;
        onActiveCharacterChanged?.Invoke(activeCharacterIndex);
        onCooldownChanged?.Invoke(0f);

        RefreshButtons();
    }

    private void Update()
    {
        if (cooldownTimer <= 0f)
            return;

        cooldownTimer = Mathf.Max(
            0f,
            cooldownTimer - Time.deltaTime
        );

        onCooldownChanged?.Invoke(cooldownTimer);
        RefreshButtons();
    }

    private void RegisterButtonCallbacks()
    {
        for (int i = 0; i < switchButtons.Length; i++)
        {
            int index = i;
            buttonCallbacks[i] = () => SwitchTo(index);
            switchButtons[i].onClick.AddListener(buttonCallbacks[i]);
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

        StartCoroutine(SwitchRoutine(targetIndex));
    }

    private IEnumerator SwitchRoutine(int targetIndex)
    {
        isSwitching = true;

        onSwitchingWindowChanged?.Invoke(true);
        RefreshButtons();

        int fromIndex = activeCharacterIndex;

        // Marker satu parent dengan karakter, jadi pakai localPosition langsung.
        Vector3 frontLocal = frontPosition.localPosition;
        Vector3 leftLocal = leftPosition.localPosition;
        Vector3 rightLocal = rightPosition.localPosition;

        int leftIndex = (targetIndex + 1) % 3;
        int rightIndex = (targetIndex + 2) % 3;

        Vector3[] formationTargets = new Vector3[3];
        formationTargets[targetIndex] = frontLocal;
        formationTargets[leftIndex] = leftLocal;
        formationTargets[rightIndex] = rightLocal;

        for (int i = 0; i < 3; i++)
            formationTargets[i] = ClampChildLocalPosition(i, formationTargets[i]);

        // ---------- FASE 1: karakter aktif MUNDUR ----------
        Vector3 retreatPos = ClampChildLocalPosition(
            fromIndex,
            characters[fromIndex].transform.localPosition
                + Vector3.down * retreatDistance
        );

        Vector3[] retreatTargets = new Vector3[3];
        for (int i = 0; i < 3; i++)
            retreatTargets[i] = characters[i].transform.localPosition;
        retreatTargets[fromIndex] = retreatPos;

        yield return AnimateAllCharacters(retreatTargets, retreatDuration);

        // ---------- FASE 2: MEKAR dari satu titik ke 3 marker ----------
        for (int i = 0; i < 3; i++)
        {
            characters[i].transform.localPosition =
                ClampChildLocalPosition(i, retreatPos);
            characters[i].SetActive(true);
        }

        yield return AnimateAllCharacters(formationTargets, spreadDuration);

        // ---------- Switching window (parent tetap bisa bergerak) ----------
        yield return new WaitForSeconds(switchingWindowDuration);

        // ---------- FASE 3: kiri & kanan MENYATU ke karakter depan ----------
        Vector3 mainPos = formationTargets[targetIndex];

        Vector3[] mergeTargets = new Vector3[3];
        for (int i = 0; i < 3; i++)
            mergeTargets[i] = mainPos;

        yield return AnimateAllCharacters(mergeTargets, mergeDuration);

        for (int i = 0; i < 3; i++)
        {
            if (i != targetIndex)
                characters[i].SetActive(false);
        }

        characters[targetIndex].transform.localPosition =
            ClampChildLocalPosition(targetIndex, frontLocal);

        activeCharacterIndex = targetIndex;
        cooldownTimer = switchCooldown;
        isSwitching = false;

        onActiveCharacterChanged?.Invoke(activeCharacterIndex);
        onCooldownChanged?.Invoke(cooldownTimer);
        onSwitchingWindowChanged?.Invoke(false);

        RefreshButtons();
    }

    private IEnumerator AnimateAllCharacters(Vector3[] targets, float duration)
    {
        Vector3[] starts = new Vector3[3];

        for (int i = 0; i < 3; i++)
            starts[i] = characters[i].transform.localPosition;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / duration)
            );

            for (int i = 0; i < 3; i++)
            {
                Vector3 desired = Vector3.Lerp(starts[i], targets[i], t);

                characters[i].transform.localPosition =
                    ClampChildLocalPosition(i, desired);
            }

            yield return null;
        }

        for (int i = 0; i < 3; i++)
        {
            characters[i].transform.localPosition =
                ClampChildLocalPosition(i, targets[i]);
        }
    }

    private Vector3 ClampChildLocalPosition(
        int characterIndex,
        Vector3 desiredLocalPosition)
    {
        BoxCollider2D childCollider = childColliders[characterIndex];
        Transform childTransform = characters[characterIndex].transform;

        // parentCollider.size/offset sudah dalam local space parent, dan
        // localScale child juga relatif ke parent. Tidak perlu dibagi lossyScale.
        // Asumsi: tidak ada rotasi Z pada parent maupun child.
        Vector2 parentSize = parentCollider.size;
        Vector2 parentOffset = parentCollider.offset;

        Vector2 childScale = new Vector2(
            Mathf.Abs(childTransform.localScale.x),
            Mathf.Abs(childTransform.localScale.y)
        );

        Vector2 halfChild = new Vector2(
            childCollider.size.x * childScale.x * 0.5f,
            childCollider.size.y * childScale.y * 0.5f
        );

        Vector2 childOffset = new Vector2(
            childCollider.offset.x * childScale.x,
            childCollider.offset.y * childScale.y
        );

        float minX = parentOffset.x - parentSize.x * 0.5f
            + halfChild.x + boundaryPadding;

        float maxX = parentOffset.x + parentSize.x * 0.5f
            - halfChild.x - boundaryPadding;

        float minY = parentOffset.y - parentSize.y * 0.5f
            + halfChild.y + boundaryPadding;

        float maxY = parentOffset.y + parentSize.y * 0.5f
            - halfChild.y - boundaryPadding;

        desiredLocalPosition.x = Mathf.Clamp(
            desiredLocalPosition.x,
            minX - childOffset.x,
            maxX - childOffset.x
        );

        desiredLocalPosition.y = Mathf.Clamp(
            desiredLocalPosition.y,
            minY - childOffset.y,
            maxY - childOffset.y
        );

        return desiredLocalPosition;
    }

    private void RefreshButtons()
    {
        if (!isInitialized)
            return;

        for (int i = 0; i < switchButtons.Length; i++)
        {
            switchButtons[i].interactable =
                CanSwitch && i != activeCharacterIndex;
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < switchButtons.Length; i++)
        {
            if (switchButtons[i] == null || buttonCallbacks[i] == null)
                continue;

            switchButtons[i].onClick.RemoveListener(buttonCallbacks[i]);
        }
    }

    private bool ValidateSetup()
    {
        if (characterParent == null || parentCollider == null)
        {
            Debug.LogError(
                "[CharacterSwitcher] Assign Character Parent and Parent Collider.",
                this
            );
            return false;
        }

        if (characters == null || characters.Length != 3 ||
            switchButtons == null || switchButtons.Length != 3)
        {
            Debug.LogError(
                "[CharacterSwitcher] Exactly three characters and three buttons are required.",
                this
            );
            return false;
        }

        if (frontPosition == null ||
            leftPosition == null ||
            rightPosition == null)
        {
            Debug.LogError(
                "[CharacterSwitcher] Assign all three arena markers.",
                this
            );
            return false;
        }

        if (frontPosition.parent != characterParent ||
            leftPosition.parent != characterParent ||
            rightPosition.parent != characterParent)
        {
            Debug.LogError(
                "[CharacterSwitcher] Arena markers must be direct children of Character Parent.",
                this
            );
            return false;
        }

        if (parentCollider.transform != characterParent)
        {
            Debug.LogError(
                "[CharacterSwitcher] Parent Collider must belong to Character Parent.",
                this
            );
            return false;
        }

        for (int i = 0; i < 3; i++)
        {
            if (characters[i] == null || switchButtons[i] == null)
            {
                Debug.LogError(
                    "[CharacterSwitcher] A character or button reference is missing.",
                    this
                );
                return false;
            }

            if (characters[i].transform.parent != characterParent)
            {
                Debug.LogError(
                    $"[CharacterSwitcher] {characters[i].name} must be a direct child of Character Parent.",
                    characters[i]
                );
                return false;
            }

            for (int j = i + 1; j < 3; j++)
            {
                if (characters[i] == characters[j] ||
                    switchButtons[i] == switchButtons[j])
                {
                    Debug.LogError(
                        "[CharacterSwitcher] Character and button references must be unique.",
                        this
                    );
                    return false;
                }
            }
        }

        return true;
    }
}