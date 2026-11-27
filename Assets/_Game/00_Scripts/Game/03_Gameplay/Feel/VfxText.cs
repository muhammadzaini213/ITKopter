using TMPro;
using UnityEngine;

/// <summary>
/// World-space TextMeshPro callout ("JACKPOT!", skill names) or a
/// persistent label that follows a target (stack counters).
/// </summary>
[AddComponentMenu("")]
public class VfxText : MonoBehaviour
{
    private TextMeshPro text;
    private Transform follow;
    private Vector3 offset;
    private bool followAssigned;

    private bool persistent;
    private float duration;
    private float rise;
    private float elapsed;
    private Color color;
    private Vector3 startPosition;

    private bool stopping;
    private float stopElapsed;
    private const float StopDuration = 0.2f;

    private float punch;

    public static VfxText Create(
        string value, Color color, float size,
        Vector3 position, Transform follow, Vector3 offset)
    {
        var go = new GameObject("VfxText");
        go.transform.position = position;

        var label = go.AddComponent<VfxText>();
        label.follow = follow;
        label.followAssigned = follow != null;
        label.offset = offset;
        label.color = color;
        label.startPosition = position;

        label.text = go.AddComponent<TextMeshPro>();

        if (SkillVFX.Font != null)
            label.text.font = SkillVFX.Font;

        label.text.text = value;
        label.text.fontSize = size;
        label.text.alignment = TextAlignmentOptions.Center;
        label.text.color = color;
        label.text.rectTransform.sizeDelta = new Vector2(24f, 4f);
        label.text.outlineWidth = 0.2f;
        label.text.outlineColor = new Color32(0, 0, 0, 220);

        var renderer = label.text.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerID = SkillVFX.SortingLayerId;
            renderer.sortingOrder = SkillVFX.SortingOrder + 10;
        }

        return label;
    }

    public void ConfigureCallout(float time, float riseHeight)
    {
        persistent = false;
        duration = Mathf.Max(0.1f, time);
        rise = riseHeight;
        transform.localScale = Vector3.zero;
    }

    public void ConfigurePersistent()
    {
        persistent = true;
        transform.localScale = Vector3.one;
    }

    public void SetText(string value, bool doPunch = true)
    {
        if (text == null || text.text == value)
            return;

        text.text = value;

        if (doPunch)
            punch = 1f;
    }

    public void SetColor(Color c)
    {
        color = c;
    }

    public void Stop()
    {
        if (this == null || stopping)
            return;

        stopping = true;
        stopElapsed = 0f;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        elapsed += dt;

        if (persistent)
        {
            if (followAssigned && follow == null && !stopping)
                Stop();

            if (follow != null)
            {
                transform.position = follow.position + offset;

                if (text != null)
                    text.enabled = follow.gameObject.activeInHierarchy;
            }

            float alpha = 1f;

            if (stopping)
            {
                stopElapsed += dt;
                alpha = 1f - Mathf.Clamp01(stopElapsed / StopDuration);

                if (stopElapsed >= StopDuration)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            punch = Mathf.MoveTowards(punch, 0f, dt * 6f);
            transform.localScale = Vector3.one * (1f + punch * 0.35f);

            if (text != null)
                text.color = color.WithAlpha(color.a * alpha);

            return;
        }

        float t = Mathf.Clamp01(elapsed / duration);

        // Pop: overshoot to 1.25 in the first 15%, settle to 1.
        float scale = t < 0.15f
            ? Mathf.Lerp(0f, 1.25f, t / 0.15f)
            : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((t - 0.15f) / 0.15f));

        transform.localScale = Vector3.one * scale;

        Vector3 anchor = follow != null ? follow.position + offset : startPosition;
        transform.position = anchor + Vector3.up * (rise * (1f - (1f - t) * (1f - t)));

        float fade = t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f;

        if (text != null)
            text.color = color.WithAlpha(color.a * fade);

        if (t >= 1f)
            Destroy(gameObject);
    }
}
