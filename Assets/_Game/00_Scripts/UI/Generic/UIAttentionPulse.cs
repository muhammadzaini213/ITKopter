using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tutorial highlight: a pulsing cyan glow drawn over a UI element (a copy of its own
/// Image). Uses a child overlay instead of scaling, so it never fights UIHoverScale.
/// </summary>
public class UIAttentionPulse : MonoBehaviour
{
    [SerializeField] private Color glowColor = new Color(0.098f, 0.941f, 1f, 1f);
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.45f;
    [SerializeField, Min(0.1f)] private float period = 1.1f;

    private Image glow;

    /// <summary>Adds (or returns) a pulse on the target.</summary>
    public static UIAttentionPulse Attach(GameObject target)
    {
        if (target == null)
            return null;

        UIAttentionPulse pulse = target.GetComponent<UIAttentionPulse>();
        return pulse != null ? pulse : target.AddComponent<UIAttentionPulse>();
    }

    public static void Detach(GameObject target)
    {
        if (target != null && target.TryGetComponent(out UIAttentionPulse pulse))
            Destroy(pulse);
    }

    private void OnEnable()
    {
        if (glow != null)
            return;

        Image source = GetComponent<Image>();
        var go = new GameObject("AttentionGlow", typeof(RectTransform), typeof(Image));
        go.layer = gameObject.layer;
        go.transform.SetParent(transform, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        glow = go.GetComponent<Image>();
        glow.raycastTarget = false;

        if (source != null)
        {
            glow.sprite = source.sprite;
            glow.type = source.type;
            glow.preserveAspect = source.preserveAspect;
            glow.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
        }
    }

    private void Update()
    {
        if (glow == null)
            return;

        float t = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / period) + 1f) * 0.5f;
        Color c = glowColor;
        c.a = Mathf.Lerp(0.05f, maxAlpha, t);
        glow.color = c;
    }

    private void OnDisable()
    {
        if (glow != null)
            Destroy(glow.gameObject);

        glow = null;
    }
}
