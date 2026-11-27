using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Slafurry.Systems.Audio;

/// <summary>
/// Tap a menu character → they say a line (speaker toast with portrait).
/// Main menu follows the player's showcase character; Shop / Objective /
/// Gacha use a fixed character with scene-specific lines (CharacterQuips).
/// </summary>
public class CharacterTapDialogue : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private QuipContext context = QuipContext.MainMenu;

    [SerializeField, Tooltip("Speaker. Ignored when Use Showcase Character is on.")]
    private CharacterData character;

    [SerializeField, Tooltip("Main menu: speak as the player's showcase character.")]
    private bool useShowcaseCharacter;

    [SerializeField, TextArea(1, 2), Tooltip("Optional lines overriding the built-in ones.")]
    private string[] customLines = new string[0];

    [SerializeField, Min(0.5f)] private float lineDuration = 3f;

    [SerializeField, Min(0f), Tooltip("Taps within this window only bounce (no new line).")]
    private float cooldown = 2.2f;

    [SerializeField] private SFXReference tapSfx;

    private float lastLineTime = -100f;
    private int lastIndex = -1;
    private Coroutine bounce;

    public void Configure(QuipContext quipContext, CharacterData speaker, bool showcase)
    {
        context = quipContext;
        character = speaker;
        useShowcaseCharacter = showcase;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Talk();
    }

    public void Talk()
    {
        if (bounce != null)
            StopCoroutine(bounce);
        bounce = StartCoroutine(Bounce());

        if (tapSfx != null)
            Audio.PlaySFX2D(tapSfx);

        if (Time.unscaledTime - lastLineTime < cooldown)
            return;

        CharacterData speaker = useShowcaseCharacter && PlayerSystem.Instance != null
            ? PlayerSystem.Instance.ShowcaseCharacter
            : character;

        string[] lines = customLines != null && customLines.Length > 0
            ? customLines
            : CharacterQuips.Get(speaker != null ? speaker.CharacterId : null, context);

        if (lines == null || lines.Length == 0 ||
            StorySystem.Instance == null || StorySystem.Instance.Toasts == null)
        {
            return;
        }

        // Never the same line twice in a row.
        int index = Random.Range(0, lines.Length);
        if (lines.Length > 1 && index == lastIndex)
            index = (index + 1 + Random.Range(0, lines.Length - 1)) % lines.Length;
        lastIndex = index;
        lastLineTime = Time.unscaledTime;

        StorySystem.Instance.Toasts.Show(new StoryToastLine
        {
            speaker = speaker,
            speakerName = speaker != null ? speaker.CharacterName : string.Empty,
            text = lines[index],
            duration = lineDuration
        });
    }

    // Small squash-and-stretch so the tap feels acknowledged.
    private IEnumerator Bounce()
    {
        Transform target = transform;
        float t = 0f;
        const float duration = 0.22f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float wave = Mathf.Sin(k * Mathf.PI);
            target.localScale = new Vector3(1f + 0.03f * wave, 1f - 0.03f * wave, 1f);
            yield return null;
        }

        target.localScale = Vector3.one;
        bounce = null;
    }

    private void OnDisable()
    {
        transform.localScale = Vector3.one;
        bounce = null;
    }
}
