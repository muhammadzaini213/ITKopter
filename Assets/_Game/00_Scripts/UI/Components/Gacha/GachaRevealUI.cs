using System;
using System.Collections;
using System.Collections.Generic;
using Slafurry.Systems.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gacha pull reveal overlay. Plays each result one by one:
///   1. borders slide in from the sides while the reward rises from below (items: no borders),
///   2. the stage slides left and the info card appears,
///   3. waits a moment, then advances on its own.
/// Tapping while animating snaps to the final layout; tapping on the final layout advances.
/// Skip ends the sequence. Duplicates show a corner popup with the converted materials.
/// Layout positions come from the original reveal storyboard (start / reveal / info states).
/// </summary>
public class GachaRevealUI : MonoBehaviour
{
    [Header("Overlay")]
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private GachaRevealTapArea tapArea;
    [SerializeField] private Button skipButton;
    [SerializeField] private TMP_Text counterText;

    [Header("Stage")]
    [SerializeField] private RectTransform characterImage;
    [SerializeField] private RectTransform itemImage;
    [SerializeField] private RectTransform borderLeft;
    [SerializeField] private RectTransform borderRight;
    [SerializeField] private Image flash;

    [Header("Info Card")]
    [SerializeField] private CanvasGroup infoCard;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text aliasText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private GameObject featuredTag;
    [SerializeField] private Image typeIcon;
    [SerializeField] private Image roleIcon;
    [Tooltip("Label under the hierarchy icon, e.g. ABSTRACT.")]
    [SerializeField] private TMP_Text typeText;
    [Tooltip("Label under the role icon, e.g. ATTACKER.")]
    [SerializeField] private TMP_Text roleText;
    [Tooltip("Hierarchy icons indexed by CharacterType (Abstract, Simple, Special, Primordial, Anomaly).")]
    [SerializeField] private Sprite[] typeSprites = new Sprite[5];
    [Tooltip("Role icons indexed by CharacterRole.")]
    [SerializeField] private Sprite[] roleSprites = new Sprite[0];

    [Header("Duplicate Popup")]
    [SerializeField] private RectTransform duplicatePopup;
    [SerializeField] private RectTransform duplicateList;
    [Tooltip("Inactive row with children Icon (Image) and Amount (TMP).")]
    [SerializeField] private GameObject duplicateRowTemplate;

    [Header("Layout (anchored positions, from the reference)")]
    [SerializeField] private float characterHiddenY = -379f;
    [SerializeField] private float characterShownY = 353.8f;
    [SerializeField] private float itemHiddenY = -260f;
    [SerializeField] private float itemShownY = 340f;
    [SerializeField] private float stageShiftX = -289f;
    [SerializeField] private Vector2 borderHiddenX = new Vector2(-772f, 825f);
    [SerializeField] private Vector2 borderShownX = new Vector2(-228f, 232f);
    [SerializeField] private float infoCardSlide = 80f;

    [Header("Timing (seconds, unscaled)")]
    [SerializeField, Min(0.05f)] private float revealDuration = 0.5f;
    [SerializeField, Min(0f)] private float revealHold = 0.45f;
    [SerializeField, Min(0.05f)] private float slideDuration = 0.35f;
    [SerializeField, Min(0f)] private float autoAdvanceDelay = 1.6f;
    [SerializeField, Min(0f)] private float duplicateExtraDelay = 0.8f;

    [Header("Border")]
    [Tooltip("Same cyan as the main menu / character detail portrait border.")]
    [SerializeField] private Color borderColor = new Color(0f, 0.98f, 1f, 1f);

    [Header("Rarity Flair")]
    [SerializeField] private Color rColor = new Color(0.55f, 0.95f, 1f, 1f);
    [SerializeField] private Color srColor = new Color(0.75f, 0.5f, 1f, 1f);
    [SerializeField] private Color urColor = new Color(1f, 0.82f, 0.25f, 1f);
    [SerializeField] private Color nColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField, Range(1f, 1.5f)] private float urPunchScale = 1.15f;

    [Header("Sound (optional)")]
    [SerializeField] private SFXReference revealSfx;
    [SerializeField] private SFXReference slideSfx;
    [SerializeField] private SFXReference urSfx;
    [SerializeField] private SFXReference duplicateSfx;

    private readonly List<GameObject> duplicateRows = new List<GameObject>();
    private float infoCardBaseX;

    private IReadOnlyList<GachaPullResult> results;
    private RewardData featuredReward;
    private Action onFinished;
    private Coroutine sequence;
    private int index;

    // Tap handling: snap the current animation, or advance from the final layout.
    private bool snapRequested;
    private bool advanceRequested;
    private bool atFinalLayout;

    public bool IsPlaying => sequence != null;

    private void Awake()
    {
        if (infoCard != null && infoCard.transform is RectTransform cardRect)
            infoCardBaseX = cardRect.anchoredPosition.x;

        if (tapArea != null)
            tapArea.OnTapped += HandleTap;

        if (skipButton != null)
            skipButton.onClick.AddListener(Skip);

        if (duplicateRowTemplate != null)
            duplicateRowTemplate.SetActive(false);

        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (tapArea != null)
            tapArea.OnTapped -= HandleTap;

        if (skipButton != null)
            skipButton.onClick.RemoveListener(Skip);
    }

    public void Play(
        IReadOnlyList<GachaPullResult> pullResults,
        RewardData featured,
        Action finishedCallback)
    {
        featuredReward = featured;

        if (pullResults == null || pullResults.Count == 0)
        {
            finishedCallback?.Invoke();
            return;
        }

        if (sequence != null)
            StopCoroutine(sequence);

        results = pullResults;
        onFinished = finishedCallback;

        if (overlayRoot != null)
            overlayRoot.SetActive(true);

        sequence = StartCoroutine(PlaySequence());
    }

    public void Skip()
    {
        if (sequence == null)
            return;

        StopCoroutine(sequence);
        Finish();
    }

    private void HandleTap()
    {
        if (sequence == null)
            return;

        if (atFinalLayout)
            advanceRequested = true;
        else
            snapRequested = true;
    }

    private IEnumerator PlaySequence()
    {
        for (index = 0; index < results.Count; index++)
        {
            GachaPullResult result = results[index];

            if (result == null || result.Reward == null)
                continue;

            yield return RevealOne(result);
        }

        Finish();
    }

    private IEnumerator RevealOne(GachaPullResult result)
    {
        snapRequested = false;
        advanceRequested = false;
        atFinalLayout = false;

        bool isCharacter = result.Reward is CharacterData;
        RectTransform subject = isCharacter ? characterImage : itemImage;
        float hiddenY = isCharacter ? characterHiddenY : itemHiddenY;
        float shownY = isCharacter ? characterShownY : itemShownY;
        RewardRarity rarity = GetDisplayRarity(result);

        PrepareStage(result, isCharacter, rarity);

        if (counterText != null)
            counterText.text = results.Count > 1 ? $"{index + 1} / {results.Count}" : string.Empty;

        PlaySfx(revealSfx);

        // Phase 1: borders close in, reward rises (Target_1 -> Target_2).
        yield return Animate(revealDuration, t =>
        {
            float e = EaseOutCubic(t);
            SetY(subject, Mathf.LerpUnclamped(hiddenY, shownY, e));
            SetX(subject, 0f);

            if (isCharacter)
            {
                float b = EaseOutBack(t);
                SetX(borderLeft, Mathf.LerpUnclamped(borderHiddenX.x, borderShownX.x, b));
                SetX(borderRight, Mathf.LerpUnclamped(borderHiddenX.y, borderShownX.y, b));
            }
        });

        if (!snapRequested)
        {
            if (rarity == RewardRarity.UR)
                yield return UrFlair(subject);

            yield return Wait(revealHold);
        }

        // Phase 2: stage slides left, info card appears (Target_2 -> Target_3).
        if (!snapRequested)
            PlaySfx(slideSfx);

        yield return Animate(slideDuration, t =>
        {
            float e = EaseOutCubic(t);
            float shift = Mathf.LerpUnclamped(0f, stageShiftX, e);
            SetX(subject, shift);

            if (isCharacter)
            {
                SetX(borderLeft, borderShownX.x + shift);
                SetX(borderRight, borderShownX.y + shift);
            }

            if (infoCard != null)
            {
                infoCard.alpha = e;
                SetLocalX(infoCard.transform, infoCardBaseX + Mathf.LerpUnclamped(infoCardSlide, 0f, e));
            }
        });

        ApplyFinalLayout(subject, isCharacter, shownY);

        if (result.IsDuplicate)
            ShowDuplicate(result);

        // Hold, then advance on its own (a tap advances immediately).
        atFinalLayout = true;
        float hold = autoAdvanceDelay + (result.IsDuplicate ? duplicateExtraDelay : 0f);
        float elapsed = 0f;

        while (elapsed < hold && !advanceRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        atFinalLayout = false;
        HideDuplicate();
    }

    private void PrepareStage(GachaPullResult result, bool isCharacter, RewardRarity rarity)
    {
        Color rarityColor = GetRarityColor(rarity);
        CharacterData character = result.Reward as CharacterData;

        if (characterImage != null)
        {
            characterImage.gameObject.SetActive(isCharacter);
            characterImage.localScale = Vector3.one;

            if (isCharacter && characterImage.TryGetComponent(out Image image))
            {
                image.sprite = character.CharacterPortrait;
                image.preserveAspect = true;
            }
        }

        if (itemImage != null)
        {
            itemImage.gameObject.SetActive(!isCharacter);
            itemImage.localScale = Vector3.one;

            if (!isCharacter && itemImage.TryGetComponent(out Image image))
            {
                image.sprite = result.Reward.Icon;
                image.preserveAspect = true;
            }
        }

        // Items don't get the portrait border.
        foreach (RectTransform border in new[] { borderLeft, borderRight })
        {
            if (border == null)
                continue;

            border.gameObject.SetActive(isCharacter);

            if (border.TryGetComponent(out Image borderImage))
                borderImage.color = borderColor;
        }

        if (isCharacter)
        {
            SetX(borderLeft, borderHiddenX.x);
            SetX(borderRight, borderHiddenX.y);
        }

        if (flash != null)
        {
            flash.color = new Color(1f, 1f, 1f, 0f);
            flash.raycastTarget = false;
        }

        // Info card.
        if (infoCard != null)
        {
            infoCard.alpha = 0f;
            SetLocalX(infoCard.transform, infoCardBaseX + infoCardSlide);
        }

        // The 9Slice frame stays its own colour; rarity shows on the border and label.

        if (nameText != null)
        {
            nameText.text = isCharacter
                ? character.CharacterName
                : $"{GetName(result.Reward)} x{result.Amount}";
        }

        if (aliasText != null)
        {
            aliasText.text = isCharacter ? character.Alias : string.Empty;
            aliasText.gameObject.SetActive(isCharacter && !string.IsNullOrWhiteSpace(character.Alias));
        }

        if (descriptionText != null)
        {
            descriptionText.text = isCharacter
                ? character.Description
                : result.Reward is ItemData item ? item.Description : string.Empty;
        }

        if (rarityText != null)
        {
            rarityText.text = rarity.ToString();
            rarityText.color = rarityColor;
        }

        if (featuredTag != null)
            featuredTag.SetActive(isCharacter && IsFeatured(character));

        SetIcon(typeIcon, isCharacter ? GetSprite(typeSprites, (int)character.CharacterType) : null);
        SetIcon(roleIcon, isCharacter ? GetSprite(roleSprites, (int)character.CharacterRole) : null);
        SetLabel(typeText, isCharacter ? character.CharacterType.ToString().ToUpperInvariant() : null);
        SetLabel(roleText, isCharacter ? character.CharacterRole.ToString().ToUpperInvariant() : null);

        HideDuplicate();
    }

    private void ApplyFinalLayout(RectTransform subject, bool isCharacter, float shownY)
    {
        SetY(subject, shownY);
        SetX(subject, stageShiftX);
        subject.localScale = Vector3.one;

        if (isCharacter)
        {
            SetX(borderLeft, borderShownX.x + stageShiftX);
            SetX(borderRight, borderShownX.y + stageShiftX);
        }

        if (infoCard != null)
        {
            infoCard.alpha = 1f;
            SetLocalX(infoCard.transform, infoCardBaseX);
        }

        if (flash != null)
            flash.color = new Color(1f, 1f, 1f, 0f);
    }

    private IEnumerator UrFlair(RectTransform subject)
    {
        PlaySfx(urSfx);

        float duration = 0.35f;

        yield return Animate(duration, t =>
        {
            if (flash != null)
                flash.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.75f, 0f, t));

            // Punch: up then settle back to 1.
            float punch = 1f + (urPunchScale - 1f) * Mathf.Sin(t * Mathf.PI);
            subject.localScale = new Vector3(punch, punch, 1f);
        });

        subject.localScale = Vector3.one;
    }

    private void ShowDuplicate(GachaPullResult result)
    {
        if (duplicatePopup == null || duplicateList == null || duplicateRowTemplate == null)
            return;

        foreach (RewardAmount converted in result.ConvertedRewards)
        {
            GameObject row = Instantiate(duplicateRowTemplate, duplicateList);
            row.SetActive(true);

            Image icon = row.transform.Find("Icon")?.GetComponent<Image>();
            TMP_Text amount = row.transform.Find("Amount")?.GetComponent<TMP_Text>();

            SetIcon(icon, converted.Reward != null ? converted.Reward.Icon : null);

            if (amount != null)
                amount.text = $"x{converted.Amount}";

            duplicateRows.Add(row);
        }

        duplicatePopup.gameObject.SetActive(true);
        PlaySfx(duplicateSfx);
        StartCoroutine(Punch(duplicatePopup, 1.15f, 0.25f));
    }

    private void HideDuplicate()
    {
        foreach (GameObject row in duplicateRows)
        {
            if (row != null)
                Destroy(row);
        }

        duplicateRows.Clear();

        if (duplicatePopup != null)
            duplicatePopup.gameObject.SetActive(false);
    }

    private void Finish()
    {
        sequence = null;
        atFinalLayout = false;
        HideDuplicate();

        if (overlayRoot != null)
            overlayRoot.SetActive(false);

        Action callback = onFinished;
        onFinished = null;
        results = null;
        callback?.Invoke();
    }

    // Runs step(0..1) over the duration; a tap jumps straight to step(1).
    private IEnumerator Animate(float duration, Action<float> step)
    {
        float elapsed = 0f;

        while (elapsed < duration && !snapRequested)
        {
            step(elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        step(1f);
    }

    private IEnumerator Wait(float seconds)
    {
        float elapsed = 0f;

        while (elapsed < seconds && !snapRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static IEnumerator Punch(Transform target, float peak, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration && target != null)
        {
            float t = elapsed / duration;
            float s = 1f + (peak - 1f) * Mathf.Sin(t * Mathf.PI);
            target.localScale = new Vector3(s, s, 1f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (target != null)
            target.localScale = Vector3.one;
    }

    private RewardRarity GetDisplayRarity(GachaPullResult result)
    {
        return result.Rarity;
    }

    private bool IsFeatured(CharacterData character)
    {
        return featuredReward != null && featuredReward == character;
    }

    private Color GetRarityColor(RewardRarity rarity)
    {
        switch (rarity)
        {
            case RewardRarity.UR: return urColor;
            case RewardRarity.SR: return srColor;
            case RewardRarity.R: return rColor;
            default: return nColor;
        }
    }

    private void PlaySfx(SFXReference sfx)
    {
        if (sfx != null)
            Audio.PlaySFX2D(sfx);
    }

    private static string GetName(RewardData reward)
    {
        switch (reward)
        {
            case CharacterData character: return character.CharacterName;
            case ItemData item: return item.ItemName;
            case CurrencyRewardData currency: return currency.CurrencyType.ToString();
            default: return reward != null ? reward.name : "?";
        }
    }

    private static Sprite GetSprite(Sprite[] sprites, int index)
    {
        return sprites != null && index >= 0 && index < sprites.Length ? sprites[index] : null;
    }

    private static void SetLabel(TMP_Text label, string text)
    {
        if (label == null)
            return;

        label.text = text ?? string.Empty;
        label.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }

    private static void SetIcon(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.preserveAspect = true;
        image.enabled = sprite != null;
    }

    private static void SetX(RectTransform rect, float x)
    {
        if (rect != null)
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
    }

    private static void SetY(RectTransform rect, float y)
    {
        if (rect != null)
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
    }

    private static void SetLocalX(Transform target, float x)
    {
        if (target is RectTransform rect)
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
    }

    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        t = Mathf.Clamp01(t);
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
