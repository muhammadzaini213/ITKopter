using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Plays a story dialogue segment in the story scene: speaker portraits and a
/// typewriter dialogue box, centered narration, rule cards, chapter title cards,
/// black screens, flashes and shakes. Tap / Space / Enter advances; Skip ends
/// the segment.
/// </summary>
public class StoryDialogueUI : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private CanvasGroup blackOverlay;
    [SerializeField] private Image flashImage;
    [SerializeField] private RectTransform shakeTarget;

    [Header("Portraits")]
    [SerializeField] private Image leftPortrait;
    [SerializeField] private Image rightPortrait;
    [SerializeField] private Color activePortraitColor = Color.white;
    [SerializeField] private Color inactivePortraitColor = new Color(0.35f, 0.38f, 0.45f, 1f);

    [Header("Portrait Border (same as the character detail menu)")]
    [SerializeField] private Sprite portraitBorderSprite;
    [SerializeField] private Color portraitBorderColor = new Color(0f, 0.98f, 1f, 1f);
    [Tooltip("Border height relative to its width (detail menu: 512 / 467).")]
    [SerializeField, Min(0.1f)] private float portraitBorderAspect = 512f / 467f;

    private Image leftBorder;
    private Image rightBorder;

    [Header("Dialogue Box")]
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private GameObject namePlate;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject nextIndicator;

    [Header("Narration")]
    [SerializeField] private CanvasGroup narrationGroup;
    [SerializeField] private TMP_Text narrationText;

    [Header("Rule Card")]
    [SerializeField] private CanvasGroup ruleGroup;
    [SerializeField] private TMP_Text ruleLabelText;
    [SerializeField] private TMP_Text ruleHeadingText;
    [SerializeField] private TMP_Text ruleBodyText;

    [Header("Title Card")]
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private TMP_Text titleLabelText;
    [SerializeField] private TMP_Text titleHeadingText;
    [SerializeField, Min(0.5f)] private float titleCardSeconds = 2.4f;

    [Header("Input")]
    [SerializeField] private Button tapArea;
    [SerializeField] private Button skipButton;

    [Header("Timing (unscaled)")]
    [SerializeField, Min(5f)] private float charactersPerSecond = 55f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.25f;

    private StoryLine[] lines;
    private Action onFinished;
    private Coroutine playRoutine;
    private bool advanceRequested;
    private bool skipRequested;
    private CharacterData leftSpeaker;
    private CharacterData rightSpeaker;
    private Vector2 shakeBase;

    public bool IsPlaying => playRoutine != null;

    private void Awake()
    {
        if (tapArea != null)
            tapArea.onClick.AddListener(RequestAdvance);

        if (skipButton != null)
            skipButton.onClick.AddListener(RequestSkip);

        if (shakeTarget != null)
            shakeBase = shakeTarget.anchoredPosition;

        HideAllPanels();
        SetPortrait(leftPortrait, null, false);
        SetPortrait(rightPortrait, null, false);
        SetGroup(blackOverlay, 1f);

        if (flashImage != null)
            flashImage.color = new Color(1f, 1f, 1f, 0f);
    }

    private void OnDestroy()
    {
        if (tapArea != null)
            tapArea.onClick.RemoveListener(RequestAdvance);

        if (skipButton != null)
            skipButton.onClick.RemoveListener(RequestSkip);
    }

    private void Update()
    {
        if (!IsPlaying || Keyboard.current == null)
            return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame ||
            Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            RequestAdvance();
        }
    }

    /// <summary>Plays the lines; <paramref name="titleStory"/> shows its chapter title card first.</summary>
    public void Play(StoryData story, StoryLine[] segmentLines, StoryData titleStory, Action finished)
    {
        if (playRoutine != null)
            StopCoroutine(playRoutine);

        lines = segmentLines ?? new StoryLine[0];
        onFinished = finished;
        advanceRequested = false;
        skipRequested = false;

        if (backgroundImage != null)
        {
            Sprite bg = story != null ? story.Background : null;
            backgroundImage.sprite = bg;
            backgroundImage.color = bg != null ? new Color(0.55f, 0.55f, 0.6f, 1f) : Color.black;
        }

        playRoutine = StartCoroutine(PlayRoutine(titleStory));
    }

    private void RequestAdvance()
    {
        advanceRequested = true;
    }

    private void RequestSkip()
    {
        skipRequested = true;
        advanceRequested = true;
    }

    private IEnumerator PlayRoutine(StoryData titleStory)
    {
        if (titleStory != null && !string.IsNullOrWhiteSpace(titleStory.StoryName))
        {
            SetGroup(blackOverlay, 1f);
            yield return ShowTitle(titleStory.ChapterLabel, titleStory.StoryName, titleCardSeconds);
        }

        for (int i = 0; i < lines.Length && !skipRequested; i++)
        {
            StoryLine line = lines[i];

            if (line == null)
                continue;

            yield return ShowLine(line);
        }

        HideAllPanels();
        playRoutine = null;

        Action callback = onFinished;
        onFinished = null;
        callback?.Invoke();
    }

    private IEnumerator ShowTitle(string label, string heading, float autoSeconds)
    {
        HideAllPanels();

        if (titleLabelText != null)
            titleLabelText.text = label ?? string.Empty;

        if (titleHeadingText != null)
            titleHeadingText.text = heading ?? string.Empty;

        yield return Fade(titleGroup, 0f, 1f);

        advanceRequested = false;
        float time = 0f;

        while (time < autoSeconds && !advanceRequested)
        {
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        advanceRequested = false;
        yield return Fade(titleGroup, 1f, 0f);
    }

    private IEnumerator ShowLine(StoryLine line)
    {
        // Scene change: fade out, swap the background; the fade-in below brings it back.
        if (line.background != null && backgroundImage != null && backgroundImage.sprite != line.background)
        {
            if (blackOverlay != null && blackOverlay.alpha < 1f)
                yield return Fade(blackOverlay, blackOverlay.alpha, 1f);

            backgroundImage.sprite = line.background;
            backgroundImage.color = new Color(0.55f, 0.55f, 0.6f, 1f);
        }

        // Background: black screen lines fade the scene out, others fade it back in.
        float black = line.blackScreen || line.style == StoryLineStyle.Title ? 1f : 0f;

        if (blackOverlay != null && !Mathf.Approximately(blackOverlay.alpha, black))
            yield return Fade(blackOverlay, blackOverlay.alpha, black);

        if (line.style == StoryLineStyle.Title)
        {
            yield return ShowTitle(line.label, line.heading, float.MaxValue);
            yield break;
        }

        HideAllPanels();

        string text = Format(line.text);

        switch (line.style)
        {
            case StoryLineStyle.Narration:
                DimPortraits();
                SetHidden(narrationText, text);
                SetGroup(narrationGroup, 1f);
                PlayEffect(line.effect);
                yield return Type(narrationText);
                break;

            case StoryLineStyle.Rule:
                SetPortrait(leftPortrait, null, false);
                SetPortrait(rightPortrait, null, false);
                leftSpeaker = rightSpeaker = null;

                ruleLabelText.text = line.label ?? string.Empty;
                ruleHeadingText.text = Format(line.heading);
                SetHidden(ruleBodyText, text);
                yield return Fade(ruleGroup, 0f, 1f);
                PlayEffect(line.effect);
                yield return Type(ruleBodyText);
                break;

            default:
                ShowSpeaker(line);
                dialogueBox.SetActive(true);

                string speakerName = Format(line.DisplayName);
                namePlate.SetActive(!string.IsNullOrWhiteSpace(speakerName));
                nameText.text = speakerName;
                SetHidden(dialogueText, text);

                PlayEffect(line.effect);

                if (line.effect == StoryLineEffect.None && IsShouting(text))
                    StartCoroutine(Shake(0.25f, 6f));

                yield return Type(dialogueText);
                break;
        }

        yield return WaitForAdvance();
    }

    // New text starts hidden: the previous line ended fully revealed, so
    // assigning text used to flash the whole line before typing restarted.
    private static void SetHidden(TMP_Text target, string text)
    {
        if (target == null)
            return;

        target.maxVisibleCharacters = 0;
        target.text = text;
    }

    private IEnumerator Type(TMP_Text target)
    {
        if (target == null)
            yield break;

        target.ForceMeshUpdate();
        int total = target.textInfo.characterCount;

        advanceRequested = false;
        target.maxVisibleCharacters = 0;

        float visible = 0f;

        while (visible < total && !advanceRequested)
        {
            visible += charactersPerSecond * Time.unscaledDeltaTime;
            target.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(visible));
            yield return null;
        }

        // A tap while typing reveals the whole line and doesn't skip it.
        target.maxVisibleCharacters = int.MaxValue;
        advanceRequested = false;
    }

    private IEnumerator WaitForAdvance()
    {
        if (nextIndicator != null)
            nextIndicator.SetActive(true);

        advanceRequested = false;

        while (!advanceRequested)
            yield return null;

        advanceRequested = false;

        if (nextIndicator != null)
            nextIndicator.SetActive(false);
    }

    private void ShowSpeaker(StoryLine line)
    {
        // Named speakers without a playable unit (Fajar, the King of All
        // Concepts) resolve by name to their portrait data.
        CharacterData speaker = StorySystem.Instance != null
            ? StorySystem.Instance.ResolveSpeaker(line.speaker, line.speakerName)
            : line.speaker;
        Sprite portrait = speaker != null ? speaker.CharacterPortrait : null;

        if (portrait == null)
        {
            // Off-screen speaker (Council, ...): keep whoever is on stage, dimmed.
            DimPortraits();
            return;
        }

        bool right = line.rightSide;

        // A character already on stage speaks from their current side.
        if (speaker == leftSpeaker)
            right = false;
        else if (speaker == rightSpeaker)
            right = true;

        if (right)
            rightSpeaker = speaker;
        else
            leftSpeaker = speaker;

        SetPortrait(leftPortrait, leftSpeaker, !right);
        SetPortrait(rightPortrait, rightSpeaker, right);
    }

    private void DimPortraits()
    {
        SetPortrait(leftPortrait, leftSpeaker, false);
        SetPortrait(rightPortrait, rightSpeaker, false);
    }

    private void SetPortrait(Image image, CharacterData character, bool active)
    {
        if (image == null)
            return;

        Sprite sprite = character != null ? character.CharacterPortrait : null;
        image.sprite = sprite;
        image.preserveAspect = true;
        image.enabled = sprite != null;
        image.color = active ? activePortraitColor : inactivePortraitColor;

        Image border = GetBorder(image);

        if (border != null)
        {
            border.gameObject.SetActive(sprite != null);

            // Dim with the portrait so the speaking side stands out.
            border.color = active
                ? portraitBorderColor
                : portraitBorderColor * inactivePortraitColor;
        }
    }

    // Detail-menu style bracket border over each portrait (drawn on top,
    // like 04_CharBorder over 03_CharPortrait), created on first use as the
    // portrait's next sibling so it isn't hidden by the portrait's toggling.
    private Image GetBorder(Image portrait)
    {
        if (portraitBorderSprite == null || portrait == null)
            return null;

        bool left = portrait == leftPortrait;
        Image existing = left ? leftBorder : rightBorder;

        if (existing != null)
            return existing;

        var go = new GameObject(portrait.name + "Border", typeof(RectTransform));
        go.transform.SetParent(portrait.transform.parent, false);
        go.transform.SetSiblingIndex(portrait.transform.GetSiblingIndex() + 1);

        var border = go.AddComponent<Image>();
        border.sprite = portraitBorderSprite;
        border.raycastTarget = false;
        border.preserveAspect = false;

        var src = portrait.rectTransform;
        var rect = border.rectTransform;
        rect.anchorMin = src.anchorMin;
        rect.anchorMax = src.anchorMax;
        rect.pivot = new Vector2(0.5f, 0f);

        // Bottom-aligned like the detail menu: full width, shorter than the art.
        float width = src.rect.width;
        rect.sizeDelta = new Vector2(width, width * portraitBorderAspect);
        rect.anchoredPosition = src.anchoredPosition +
                                new Vector2((0.5f - src.pivot.x) * width, -src.pivot.y * src.rect.height);

        if (left) leftBorder = border;
        else rightBorder = border;

        return border;
    }

    private void HideAllPanels()
    {
        if (dialogueBox != null)
            dialogueBox.SetActive(false);

        if (nextIndicator != null)
            nextIndicator.SetActive(false);

        SetGroup(narrationGroup, 0f);
        SetGroup(ruleGroup, 0f);
        SetGroup(titleGroup, 0f);
    }

    private void PlayEffect(StoryLineEffect effect)
    {
        switch (effect)
        {
            case StoryLineEffect.Flash:
                StartCoroutine(Flash());
                break;
            case StoryLineEffect.Shake:
                StartCoroutine(Shake(0.4f, 14f));
                break;
        }
    }

    private IEnumerator Flash()
    {
        if (flashImage == null)
            yield break;

        const float duration = 0.6f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            flashImage.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01(time / duration));
            yield return null;
        }

        flashImage.color = new Color(1f, 1f, 1f, 0f);
    }

    private IEnumerator Shake(float duration, float strength)
    {
        if (shakeTarget == null)
            yield break;

        float time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float falloff = 1f - Mathf.Clamp01(time / duration);
            shakeTarget.anchoredPosition = shakeBase + UnityEngine.Random.insideUnitCircle * strength * falloff;
            yield return null;
        }

        shakeTarget.anchoredPosition = shakeBase;
    }

    private IEnumerator Fade(CanvasGroup group, float from, float to)
    {
        if (group == null)
            yield break;

        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            SetGroup(group, Mathf.Lerp(from, to, time / fadeDuration));
            yield return null;
        }

        SetGroup(group, to);
    }

    private static void SetGroup(CanvasGroup group, float alpha)
    {
        if (group == null)
            return;

        group.alpha = alpha;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    // Mostly upper-case lines (Ei-Pi, Cashino, Warlock) give the screen a little kick.
    private static bool IsShouting(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length < 8)
            return false;

        int letters = 0, upper = 0;

        foreach (char c in text)
        {
            if (!char.IsLetter(c))
                continue;

            letters++;

            if (char.IsUpper(c))
                upper++;
        }

        return letters >= 6 && upper >= letters * 0.85f;
    }

    private static string Format(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (text.Contains("{player}"))
        {
            string name = PlayerSystem.Instance != null ? PlayerSystem.Instance.PlayerName : "Commander";
            text = text.Replace("{player}", name);
        }

        return text;
    }
}
