using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// The Annihilator: a giant ally that rises from the bottom of the screen
// and fires every cannon for a few seconds. Its power grows with the
// PLAYER's level (PlayerSystem EXP from clearing stages and upgrading
// characters): more damage per bullet, faster fire, a longer manifestation.
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

    [Header("Power (scales with player level)")]
    [SerializeField, Min(0f), Tooltip("Damage per bullet at player level 1.")]
    private float baseBulletDamage = 40f;

    [SerializeField, Min(0f), Tooltip("Extra damage per player level above 1 (0.12 = +12% per level).")]
    private float damagePerLevel = 0.12f;

    [SerializeField, Min(0f), Tooltip("Fire-rate gain per player level (0.03 = 3% faster per level).")]
    private float fireRatePerLevel = 0.03f;

    [SerializeField, Range(0.1f, 1f), Tooltip("Fastest the fire interval can get (multiplier).")]
    private float minIntervalMultiplier = 0.45f;

    [SerializeField, Min(0f), Tooltip("Extra manifestation seconds per player level.")]
    private float durationPerLevel = 0.2f;

    [SerializeField, Min(0f)]
    private float maxBonusDuration = 5f;

    [Header("Presentation")]
    [SerializeField, Min(0f), Tooltip("World units the Annihilator rises from below on entry.")]
    private float riseDistance = 4f;

    [SerializeField, Min(0.05f)]
    private float riseDuration = 0.45f;

    [SerializeField]
    private Color manifestColor = new Color(1f, 0.25f, 0.35f);

    [Header("Fajar's Lines")]
    [SerializeField, TextArea(1, 2), Tooltip("What Fajar (the King of All Concepts) says on manifesting. Empty = built-in lines.")]
    private string[] summonLines = new string[0];

    private static readonly string[] DefaultSummonLines =
    {
        "Annihilator, answer your king!",
        "Every concept bows to me. Now, annihilate!",
        "Enough. I'll handle this myself.",
        "By the authority of the King of All Concepts!",
        "Stand back, everyone. This one's mine."
    };

    private float cooldownTimer;
    private bool isManifesting;
    private Coroutine manifestationCoroutine;
    private Vector3 restLocalPosition;
    private bool restCaptured;
    private VfxRing aura;
    private VfxText label;

    /// <summary>True while any Annihilator is on the field (switching / EX skills lock).</summary>
    public static bool IsAnyManifesting { get; private set; }

    public bool IsManifesting => isManifesting;

    public float CooldownRemaining => cooldownTimer;

    public float CooldownDuration => cooldownDuration;

    public int PlayerLevel =>
        PlayerSystem.Instance != null ? Mathf.Max(1, PlayerSystem.Instance.PlayerLevel) : 1;

    public float DamageMultiplier => 1f + damagePerLevel * (PlayerLevel - 1);

    public float BulletDamage => baseBulletDamage * DamageMultiplier;

    public float IntervalMultiplier =>
        Mathf.Max(minIntervalMultiplier, 1f / (1f + fireRatePerLevel * (PlayerLevel - 1)));

    public float EffectiveDuration =>
        manifestationDuration + Mathf.Min(maxBonusDuration, durationPerLevel * (PlayerLevel - 1));

    public bool CanManifest =>
        !isManifesting &&
        cooldownTimer <= 0f &&
        annihilatorObject != null &&
        characterSwitcher != null &&
        !characterSwitcher.IsSwitching &&
        characterSwitcher.ActiveCharacter != null;

    private void Awake()
    {
        if (annihilatorObject != null)
        {
            restLocalPosition = annihilatorObject.transform.localPosition;
            restCaptured = true;
            annihilatorObject.SetActive(false);
        }

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
            characterSwitcher = FindFirstObjectByType<CharacterSwitcher>();

        if (characterSwitcher == null || annihilatorObject == null)
        {
            Debug.LogError(
                "[AnnihilatorManifestation] CharacterSwitcher or Annihilator object is not assigned.",
                this);

            enabled = false;
            return;
        }

        RefreshUI();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer = Mathf.Max(0f, cooldownTimer - Time.deltaTime);

        RefreshUI();
    }

    public void ActivateManifestation()
    {
        if (!CanManifest || manifestationCoroutine != null)
            return;

        manifestationCoroutine = StartCoroutine(ManifestationRoutine());
    }

    private void ApplyPower()
    {
        float damage = BulletDamage;
        float interval = IntervalMultiplier;

        foreach (var shooter in annihilatorObject.GetComponentsInChildren<AnnihilatorAutoShooter>(true))
        {
            shooter.DamageOverride = damage;
            shooter.IntervalMultiplier = interval;
        }
    }

    private IEnumerator ManifestationRoutine()
    {
        isManifesting = true;
        IsAnyManifesting = true;
        cooldownTimer = cooldownDuration;

        int level = PlayerLevel;
        float duration = EffectiveDuration;

        ApplyPower();
        RefreshUI();

        // Hide the party while the Annihilator fights.
        GameObject hiddenCharacter = characterSwitcher.ActiveCharacter;
        Vector3 partyPosition = hiddenCharacter != null ? hiddenCharacter.transform.position : Vector3.zero;

        // The character parent follows the player's movement.
        Transform anchor = hiddenCharacter != null && hiddenCharacter.transform.parent != null
            ? hiddenCharacter.transform.parent
            : characterSwitcher.transform;

        if (hiddenCharacter != null)
        {
            SkillVFX.Ghost(hiddenCharacter.transform, manifestColor, 0.5f, 2f);
            hiddenCharacter.SetActive(false);
        }

        // Entry: rise from below in a slow-mo cut-in.
        Transform body = annihilatorObject.transform;
        Vector3 rest = restCaptured ? restLocalPosition : body.localPosition;
        body.localPosition = rest + Vector3.down * riseDistance;
        annihilatorObject.SetActive(true);

        SayFajarLine();

        CombatFeel.SlowMo(0.9f, 0.25f, letterbox: true);
        CombatFeel.Flash(manifestColor.WithAlpha(0.45f), 0.4f);
        CombatFeel.Shake(0.45f, 0.6f);
        SkillVFX.Ring(partyPosition, manifestColor, 0.5f, 9f, 0.6f, 0.3f);
        SkillVFX.Ring(partyPosition, Color.white.WithAlpha(0.8f), 0.3f, 6f, 0.45f, 0.1f, 8, 3, 90f);
        SkillVFX.Sparks(partyPosition, manifestColor, 40, 10f, 0.7f);
        SkillVFX.Text(partyPosition + Vector3.up * 2f, $"ANNIHILATOR  Lv{level}", manifestColor, 6f, 1.6f, 1f);
        SkillVFX.Text(partyPosition + Vector3.up * 1.2f, $"POWER x{DamageMultiplier:0.0#}", Color.white, 3.5f, 1.6f, 0.8f);

        yield return MoveBody(body, rest, riseDuration);

        aura = SkillVFX.Aura(anchor, manifestColor.WithAlpha(0.6f), 1.6f, 0.12f, -1f, 0.08f, 6, 1, 40f);
        label = SkillVFX.Label(anchor, Vector3.up * 2f, "", manifestColor, 3.5f);

        // Active phase: steady rumble + countdown.
        float elapsed = 0f;
        float rumbleTimer = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rumbleTimer -= Time.deltaTime;

            if (rumbleTimer <= 0f)
            {
                rumbleTimer = 0.25f;
                CombatFeel.Shake(0.05f, 0.25f);
            }

            if (label != null)
                label.SetText($"{Mathf.CeilToInt(duration - elapsed)}", false);

            yield return null;
        }

        // Exit: sink back down.
        if (aura != null) aura.Stop();
        if (label != null) label.Stop();
        aura = null;
        label = null;

        CombatFeel.Shake(0.25f, 0.3f);
        yield return MoveBody(body, rest + Vector3.down * riseDistance, riseDuration);

        EndManifestation();
    }

    private void SayFajarLine()
    {
        StorySystem story = StorySystem.Instance;
        if (story == null || story.Toasts == null)
            return;

        CharacterData king = story.PlayerAvatar;
        string[] lines = summonLines != null && summonLines.Length > 0 ? summonLines : DefaultSummonLines;

        story.Toasts.Show(new StoryToastLine
        {
            speaker = king,
            speakerName = king != null ? king.CharacterName : "Fajar",
            text = lines[Random.Range(0, lines.Length)],
            duration = 3f
        });
    }

    private static IEnumerator MoveBody(Transform body, Vector3 target, float time)
    {
        Vector3 start = body.localPosition;
        float t = 0f;

        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            body.localPosition = Vector3.Lerp(start, target, 1f - (1f - k) * (1f - k));
            yield return null;
        }

        body.localPosition = target;
    }

    private void EndManifestation()
    {
        if (annihilatorObject != null)
        {
            annihilatorObject.SetActive(false);

            if (restCaptured)
                annihilatorObject.transform.localPosition = restLocalPosition;
        }

        // Bring back whoever is the active character NOW (not a stale
        // reference), with a short grace window.
        if (characterSwitcher != null)
        {
            GameObject active = characterSwitcher.ActiveCharacter;

            if (active != null)
            {
                active.SetActive(true);

                var health = active.GetComponent<Health>();
                if (health != null)
                    health.SetInvulnerable(0.8f);

                SkillVFX.Ghost(active.transform, manifestColor, 0.4f, 1.6f);
                SkillVFX.Ring(active.transform.position, manifestColor, 0.3f, 2.5f, 0.3f, 0.1f);
            }
        }

        isManifesting = false;
        IsAnyManifesting = false;
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
                cooldownText.text = "MANIFESTING";
            else if (cooldownTimer > 0f)
                cooldownText.text = $"Cooldown: {Mathf.CeilToInt(cooldownTimer)}s";
            else
                cooldownText.text = $"READY  Lv{PlayerLevel}";
        }

        if (manifestationButton != null)
            manifestationButton.interactable = CanManifest;
    }

    private void OnDestroy()
    {
        if (manifestationButton != null)
            manifestationButton.onClick.RemoveListener(ActivateManifestation);

        if (manifestationCoroutine != null)
            StopCoroutine(manifestationCoroutine);

        if (isManifesting)
            IsAnyManifesting = false;
    }
}
