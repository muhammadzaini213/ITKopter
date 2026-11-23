using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Slafurry.Systems.Audio;
using IEnumerator = global::System.Collections.IEnumerator;
using StringBuilder = global::System.Text.StringBuilder;
using BindingFlags = global::System.Reflection.BindingFlags;

[global::System.Serializable]
public struct SubtitleLine
{
    [TextArea(2, 4)]
    public string text;
    public float duration;
    [Range(0f, 5f)]
    public float shakiness;
    [Tooltip("Pause (seconds) after this line finishes, before the next line starts typing.")]
    public float delayAfter;
}

namespace Slafurry.Utils.UI
{

    /// <summary>
    /// Plays subtitles line by line with typing effect and per-line shaky text.
    /// </summary>
    public class UISubtitle : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text textLabel;

        [Header("Lines")]
        [SerializeField] private SubtitleLine[] lines;

        [Header("Typing")]
        [SerializeField] private float typeDelay = 0.04f;

        [Header("Audio")]
        [SerializeField] private string sfxCategory = "UI";
        [SerializeField] private string typeSFX = "typing";

        [Header("Shake")]
        [SerializeField] private float shakeUpdateInterval = 0.08f;
        [Tooltip("Maximum offset (in em) when shakiness is at its maximum (5). Kept small so characters stay inside the TMP bounds.")]
        [SerializeField] private float maxShakeOffsetEm = 0.12f;
        [Tooltip("Maximum value of the shakiness slider (must match the Range in SubtitleLine).")]
        [SerializeField] private float maxShakinessValue = 5f;

        [Header("Fade")]
        [SerializeField] private float fadeOutDuration = 0.3f;
        [SerializeField] private float hideDelay = 1f;

        [Header("Options")]
        [SerializeField] private bool playOnEnable = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onSubtitleStart;
        [SerializeField] private UnityEvent onLineComplete;
        [SerializeField] private UnityEvent onAllComplete;

        private Coroutine _routine;
        private Coroutine _shakeRoutine;
        private string _currentLine;
        private int _typedCount;
        private float _currentShakiness;

        private void Awake()
        {
            // Make sure glyphs that poke slightly outside the bounds (from the voffset shake)
            // still render instead of being culled by TMP.
            // The property name differs between TMP versions ("enableExtraPadding" / "extraPadding"),
            // so use reflection to avoid a compile error on any version.
            TrySetExtraPadding(textLabel);
        }

        private static void TrySetExtraPadding(TMP_Text label)
        {
            if (label == null) return;

            var type = label.GetType();
            string[] possibleNames = { "enableExtraPadding", "extraPadding" };

            foreach (var name in possibleNames)
            {
                var prop = type.GetProperty(name,
                    BindingFlags.Public | BindingFlags.Instance);

                if (prop != null && prop.CanWrite && prop.PropertyType == typeof(bool))
                {
                    prop.SetValue(label, true);
                    return;
                }
            }

            // If neither is found, that's fine: just tick "Extra Padding"
            // manually in the Inspector of the TMP component.
        }

        private void OnEnable()
        {
            if (playOnEnable)
                Play(lines);
        }

        private void OnDisable()
        {
            Stop();
        }

        public void Play(SubtitleLine[] newLines)
        {
            Stop();
            lines = newLines;
            _routine = StartCoroutine(SubtitleRoutine());
        }

        public void Stop()
        {
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            StopShake();
            StopTypeSfx();
            if (textLabel != null) textLabel.text = "";
            HidePanel();
        }

        private IEnumerator SubtitleRoutine()
        {
            if (lines == null || lines.Length == 0) yield break;

            ShowPanel();
            onSubtitleStart?.Invoke();

            for (int i = 0; i < lines.Length; i++)
            {
                yield return ShowLine(lines[i]);
                onLineComplete?.Invoke();

                // The pause between lines comes from the line's own data (delayAfter),
                // except after the last line (the last line already has its own hideDelay).
                if (i < lines.Length - 1 && lines[i].delayAfter > 0f)
                    yield return new WaitForSecondsRealtime(lines[i].delayAfter);
            }

            yield return new WaitForSecondsRealtime(hideDelay);
            HidePanel();
            _routine = null;
            onAllComplete?.Invoke();
        }

        private IEnumerator ShowLine(SubtitleLine line)
        {
            _currentLine = line.text;
            _typedCount = 0;
            _currentShakiness = line.shakiness;

            textLabel.text = "";
            textLabel.alpha = 1f;

            PlayTypeSfx();

            for (int i = 0; i < line.text.Length; i++)
            {
                _typedCount = i + 1;
                ApplyShake();
                yield return new WaitForSecondsRealtime(typeDelay);
            }

            StopTypeSfx();

            StartShake();

            float typeTime = line.text.Length * typeDelay;
            float holdTime = Mathf.Max(0f, line.duration - typeTime);
            yield return new WaitForSecondsRealtime(holdTime);

            StopShake();
            yield return FadeAlpha(1f, 0f, fadeOutDuration);
        }

        private void StartShake()
        {
            if (_currentShakiness <= 0f) return;
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ShakeRoutine());
        }

        private void StopShake()
        {
            if (_shakeRoutine != null) { StopCoroutine(_shakeRoutine); _shakeRoutine = null; }
        }

        private IEnumerator ShakeRoutine()
        {
            while (true)
            {
                ApplyShake();
                yield return new WaitForSecondsRealtime(shakeUpdateInterval);
            }
        }

        private void ApplyShake()
        {
            if (string.IsNullOrEmpty(_currentLine) || textLabel == null) return;

            char[] chars = _currentLine.ToCharArray();
            var sb = new StringBuilder(chars.Length * 12);

            // Remap shakiness (0..maxShakinessValue) to a safe maximum offset (em),
            // so high shakiness values don't push the text outside the TMP bounds.
            float normalized = maxShakinessValue > 0f
                ? Mathf.Clamp01(_currentShakiness / maxShakinessValue)
                : 0f;
            float clampedRange = normalized * maxShakeOffsetEm;

            for (int i = 0; i < _typedCount && i < chars.Length; i++)
            {
                if (clampedRange > 0f)
                {
                    float offsetY = Random.Range(-clampedRange, clampedRange);
                    sb.Append("<voffset=").Append(offsetY.ToString("F3")).Append("em>")
                      .Append(chars[i])
                      .Append("</voffset>");
                }
                else
                {
                    sb.Append(chars[i]);
                }
            }

            textLabel.text = sb.ToString();
        }

        private IEnumerator FadeAlpha(float from, float to, float fadeDur)
        {
            if (fadeDur <= 0f)
            {
                textLabel.alpha = to;
                yield break;
            }

            float t = 0f;
            while (t < fadeDur)
            {
                t += Time.unscaledDeltaTime;
                textLabel.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / fadeDur));
                yield return null;
            }

            textLabel.alpha = to;
        }

        private void ShowPanel()
        {
            if (panel != null) panel.SetActive(true);
        }

        private void HidePanel()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void PlayTypeSfx()
        {
            if (AudioSystem.Instance == null) return;
            Audio.PlaySFX2D(sfxCategory, typeSFX, loop: true);
        }

        private void StopTypeSfx()
        {
            if (AudioSystem.Instance == null) return;
            Audio.StopSFX(sfxCategory, typeSFX);
        }
    }
}