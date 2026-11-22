
using UnityEngine;
using Slafurry.Systems.Audio;

// Hit feedback helper for Health: squash + white blink + hit sound,
// triggered by Health.OnDamaged. Add next to Health on the entity root.
public class SquashBlink : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health health;
    [SerializeField, Tooltip("Transform that gets squashed. Defaults to this object.")]
    private Transform visualTarget;
    [SerializeField, Tooltip("Renderers that blink. Defaults to all children.")]
    private SpriteRenderer[] renderers;

    [Header("Squash")]
    [SerializeField, Min(0f)]
    private float squashDuration = 0.15f;

    [SerializeField, Tooltip("Peak localScale multiplier (x, y) at hit start.")]
    private Vector2 squashScale = new Vector2(1.25f, 0.75f);

    [Header("Blink")]
    [SerializeField] private Color flashColor = Color.white;

    [SerializeField, Min(0f)]
    private float blinkDuration = 0.1f;

    [Header("Audio")]
    [SerializeField] private SFXReference hitSfx;

    private Vector3 baseScale;
    private Color[] baseColors;
    private float timer;
    private bool playing;

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (visualTarget == null)
            visualTarget = transform;

        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<SpriteRenderer>(true);

        baseScale = visualTarget.localScale;

        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                baseColors[i] = renderers[i].color;
        }
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDamaged -= HandleDamaged;

        Restore();
    }

    private void HandleDamaged()
    {
        if (hitSfx != null)
            Audio.PlaySFX2D(hitSfx);

        timer = 0f;
        playing = true;
    }

    private void Update()
    {
        if (!playing)
            return;

        timer += Time.deltaTime;

        // Squash: starts at peak, eases back to base scale.
        if (squashDuration > 0f && visualTarget != null)
        {
            float t = Mathf.Clamp01(timer / squashDuration);
            var peak = new Vector3(
                baseScale.x * squashScale.x,
                baseScale.y * squashScale.y,
                baseScale.z);
            visualTarget.localScale = Vector3.Lerp(peak, baseScale, t);
        }

        // Blink: starts at flash color, fades back to original colors.
        if (blinkDuration > 0f)
        {
            float t = Mathf.Clamp01(timer / blinkDuration);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].color = Color.Lerp(flashColor, baseColors[i], t);
            }
        }

        if (timer >= Mathf.Max(squashDuration, blinkDuration))
            Restore();
    }

    private void Restore()
    {
        if (!playing)
            return;

        playing = false;
        timer = 0f;

        if (visualTarget != null)
            visualTarget.localScale = baseScale;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].color = baseColors[i];
        }
    }
}
