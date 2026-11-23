using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Persistent overlay (lives under StorySystem in 00_Boot) for speaker toasts —
/// in-battle tutorial tips and banter, menu hints after onboarding chapters —
/// and the VICTORY / DEFEAT banner of story battles.
/// </summary>
public class StoryToastUI : MonoBehaviour
{
    [Header("Toast")]
    [SerializeField] private CanvasGroup toastGroup;
    [SerializeField] private Image speakerIcon;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.2f;

    [Header("Banner")]
    [SerializeField] private CanvasGroup bannerGroup;
    [SerializeField] private TMP_Text bannerText;
    [SerializeField] private Color victoryColor = new Color(1f, 0.82f, 0.25f, 1f);
    [SerializeField] private Color defeatColor = new Color(1f, 0.35f, 0.35f, 1f);

    private readonly Queue<(StoryToastLine line, float delay)> queue = new();
    private Coroutine toastRoutine;
    private Coroutine bannerRoutine;

    private void Awake()
    {
        SetGroup(toastGroup, 0f);
        SetGroup(bannerGroup, 0f);
    }

    /// <summary>Shows one toast now (queued behind any toast already showing).</summary>
    public void Show(StoryToastLine line)
    {
        if (line == null || string.IsNullOrWhiteSpace(line.text))
            return;

        queue.Enqueue((line, 0f));
        EnsureRunning();
    }

    /// <summary>Shows toasts one after another; each line's Time is the delay before it.</summary>
    public void PlaySequence(IEnumerable<StoryToastLine> lines)
    {
        if (lines == null)
            return;

        foreach (StoryToastLine line in lines)
        {
            if (line != null && !string.IsNullOrWhiteSpace(line.text))
                queue.Enqueue((line, line.time));
        }

        EnsureRunning();
    }

    public void HideAll()
    {
        queue.Clear();

        if (toastRoutine != null)
        {
            StopCoroutine(toastRoutine);
            toastRoutine = null;
        }

        SetGroup(toastGroup, 0f);
    }

    public void ShowBanner(string text, bool positive)
    {
        if (bannerGroup == null)
            return;

        if (bannerText != null)
        {
            bannerText.text = text;
            bannerText.color = positive ? victoryColor : defeatColor;
        }

        if (bannerRoutine != null)
            StopCoroutine(bannerRoutine);

        bannerRoutine = StartCoroutine(BannerIn());
    }

    public void HideBanner()
    {
        if (bannerRoutine != null)
        {
            StopCoroutine(bannerRoutine);
            bannerRoutine = null;
        }

        SetGroup(bannerGroup, 0f);
    }

    private void EnsureRunning()
    {
        if (toastRoutine == null && isActiveAndEnabled)
            toastRoutine = StartCoroutine(ToastLoop());
    }

    private IEnumerator ToastLoop()
    {
        while (queue.Count > 0)
        {
            (StoryToastLine line, float delay) = queue.Dequeue();

            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            Fill(line);

            yield return Fade(toastGroup, 0f, 1f);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, line.duration));
            yield return Fade(toastGroup, 1f, 0f);
        }

        toastRoutine = null;
    }

    private void Fill(StoryToastLine line)
    {
        if (speakerNameText != null)
            speakerNameText.text = line.DisplayName;

        if (messageText != null)
            messageText.text = line.text;

        if (speakerIcon != null)
        {
            Sprite icon = line.speaker != null ? line.speaker.Icon : null;
            speakerIcon.sprite = icon;
            speakerIcon.preserveAspect = true;
            speakerIcon.enabled = icon != null;
        }
    }

    private IEnumerator BannerIn()
    {
        Transform target = bannerGroup.transform;
        float time = 0f;
        const float duration = 0.35f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            float punch = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;

            SetGroup(bannerGroup, t);
            target.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, t) * punch;
            yield return null;
        }

        target.localScale = Vector3.one;
        SetGroup(bannerGroup, 1f);
        bannerRoutine = null;
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
}
