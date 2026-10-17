using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnnihilatorManifestation : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterSwitcher characterSwitcher;
    [SerializeField] private GameObject annihilatorObject;
    [SerializeField] private Button manifestationButton;
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private TextMeshProUGUI cooldownText;

    [Header("Manifestation Settings")]
    [SerializeField, Min(0.1f)]
    private float manifestationDuration = 5f;

    [SerializeField, Min(0f)]
    private float cooldownDuration = 60f;

    private GameObject[] teamCharacters;
    private bool[] originalActiveStates;

    private float cooldownTimer;
    private bool isManifesting;
    private Coroutine manifestationCoroutine;

    public bool IsManifesting => isManifesting;

    public float CooldownRemaining => cooldownTimer;

    public float CooldownDuration => cooldownDuration;

    public bool CanManifest =>
        !isManifesting &&
        cooldownTimer <= 0f &&
        annihilatorObject != null &&
        characterSwitcher != null;

    private void Awake()
    {
        if (annihilatorObject != null)
            annihilatorObject.SetActive(false);

        if (cooldownSlider != null)
        {
            cooldownSlider.minValue = 0f;
            cooldownSlider.maxValue = 1f;
            cooldownSlider.value = 1f;
        }

        if (manifestationButton != null)
            manifestationButton.onClick.AddListener(ActivateManifestation);
    }

    private void Start()
    {
        if (characterSwitcher == null)
        {
            Debug.LogError(
                "[AnnihilatorManifestation] CharacterSwitcher is not assigned.",
                this);

            enabled = false;
            return;
        }

        if (annihilatorObject == null)
        {
            Debug.LogError(
                "[AnnihilatorManifestation] Annihilator object is not assigned.",
                this);

            enabled = false;
            return;
        }

        int characterCount = characterSwitcher.CharacterCount;

        teamCharacters = new GameObject[characterCount];
        originalActiveStates = new bool[characterCount];

        for (int i = 0; i < characterCount; i++)
        {
            // CharacterSwitcher exposes its active character directly.
            // Other team members are supplied through the configured hierarchy below.
            teamCharacters[i] = null;
        }

        RefreshUI();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer = Mathf.Max(
                0f,
                cooldownTimer - Time.deltaTime);
        }

        RefreshUI();
    }

    public void ActivateManifestation()
    {
        if (!CanManifest)
            return;

        if (manifestationCoroutine != null)
            return;

        manifestationCoroutine =
            StartCoroutine(ManifestationRoutine());
    }

    private IEnumerator ManifestationRoutine()
    {
        isManifesting = true;

        // Start the long cooldown when manifestation is activated.
        cooldownTimer = cooldownDuration;

        RefreshUI();

        // Temporarily hide the currently active player character.
        GameObject activeCharacter = characterSwitcher.ActiveCharacter;

        if (activeCharacter != null)
            activeCharacter.SetActive(false);

        // Show the Annihilator and let its existing child skill objects run.
        annihilatorObject.SetActive(true);

        yield return new WaitForSeconds(manifestationDuration);

        // Hide the Annihilator after the manifestation ends.
        annihilatorObject.SetActive(false);

        // Restore the previously active player character.
        if (activeCharacter != null)
            activeCharacter.SetActive(true);

        isManifesting = false;
        manifestationCoroutine = null;

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (cooldownSlider != null)
        {
            cooldownSlider.value = cooldownDuration > 0f
                ? Mathf.Clamp01(1f - cooldownTimer / cooldownDuration)
                : 1f;
        }

        if (cooldownText != null)
        {
            if (isManifesting)
            {
                cooldownText.text = "MANIFESTING";
            }
            else if (cooldownTimer > 0f)
            {
                cooldownText.text = $"Cooldown: {Mathf.CeilToInt(cooldownTimer)}s";
            }
            else
            {
                cooldownText.text = "READY";
            }
        }

        if (manifestationButton != null)
        {
            manifestationButton.interactable = CanManifest;
        }
    }

    private void OnDestroy()
    {
        if (manifestationButton != null)
        {
            manifestationButton.onClick.RemoveListener(
                ActivateManifestation);
        }

        if (manifestationCoroutine != null)
            StopCoroutine(manifestationCoroutine);
    }
}
