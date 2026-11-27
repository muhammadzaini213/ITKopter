using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Slafurry.Systems.Audio;

/// <summary>
/// The player-profile EXP bar (same slider as the main menu's profile
/// button), shown inside the reward popup after a level clear. Fills from
/// the old EXP to the new one, rolling over on each player level-up.
/// Runs on unscaled time so it animates while the game is paused.
/// </summary>
public class ExpGainBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI gainText;

    [Header("Animation")]
    [SerializeField, Min(0.1f)] private float fillDuration = 1.2f;
    [SerializeField, Min(0f)] private float startDelay = 0.25f;
    [SerializeField] private SFXReference levelUpSfx;

    private static readonly Color LevelUpColor = new Color(1f, 0.85f, 0.25f);

    private Coroutine routine;
    private Color levelTextColor = Color.white;

    private void Awake()
    {
        if (levelText != null)
            levelTextColor = levelText.color;

        if (expSlider != null)
        {
            expSlider.minValue = 0f;
            expSlider.interactable = false;
            expSlider.wholeNumbers = false;
        }
    }

    /// <summary>Shows and animates the pending level-clear EXP gain, or hides itself.</summary>
    public bool PlayPendingGain()
    {
        PlayerSystem player = PlayerSystem.Instance;

        if (player == null || !player.TryConsumeExpGain(out PlayerSystem.ExpGain gain) || gain.Amount <= 0)
        {
            gameObject.SetActive(false);
            return false;
        }

        gameObject.SetActive(true);

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(Animate(gain.ExpBefore, gain.Amount));
        return true;
    }

    private IEnumerator Animate(long expBefore, int amount)
    {
        long expAfter = expBefore + amount;

        PlayerSystem.GetLevelProgress(expBefore, out int shownLevel, out _, out _);
        Apply(expBefore);

        if (gainText != null)
            gainText.text = $"+{amount} EXP";

        float wait = 0f;
        while (wait < startDelay)
        {
            wait += Time.unscaledDeltaTime;
            yield return null;
        }

        float t = 0f;

        while (t < fillDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fillDuration);
            float eased = 1f - (1f - k) * (1f - k);
            long total = expBefore + (long)Mathf.Round(amount * eased);

            int level = Apply(total);

            if (level > shownLevel)
            {
                shownLevel = level;
                StartCoroutine(LevelUpPunch());
            }

            yield return null;
        }

        Apply(expAfter);
        routine = null;
    }

    // Returns the level shown for this total EXP.
    private int Apply(long totalExp)
    {
        PlayerSystem.GetLevelProgress(totalExp, out int level, out int current, out int required);

        if (expSlider != null)
        {
            expSlider.maxValue = Mathf.Max(1, required);
            expSlider.value = current;
        }

        if (levelText != null)
            levelText.text = $"Level {level}";

        if (expText != null)
            expText.text = $"EXP: {current} / {required}";

        return level;
    }

    private IEnumerator LevelUpPunch()
    {
        if (levelUpSfx != null)
            Audio.PlaySFX2D(levelUpSfx);

        if (gainText != null)
            gainText.text = "LEVEL UP!";

        if (levelText == null)
            yield break;

        Transform target = levelText.transform;
        float t = 0f;

        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            float k = t / 0.35f;
            float scale = k < 0.4f ? Mathf.Lerp(1f, 1.4f, k / 0.4f) : Mathf.Lerp(1.4f, 1f, (k - 0.4f) / 0.6f);
            target.localScale = Vector3.one * scale;
            levelText.color = Color.Lerp(LevelUpColor, levelTextColor, k);
            yield return null;
        }

        target.localScale = Vector3.one;
        levelText.color = levelTextColor;
    }

    private void OnDisable()
    {
        if (levelText != null)
        {
            levelText.transform.localScale = Vector3.one;
            levelText.color = levelTextColor;
        }

        routine = null;
    }
}
