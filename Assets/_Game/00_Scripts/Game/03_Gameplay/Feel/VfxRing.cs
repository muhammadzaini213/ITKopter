using UnityEngine;

/// <summary>
/// Pixel-art ring / polygon / star (rasterised by <see cref="PixelArt"/>). Two modes: a one-shot burst that
/// expands and fades, or a persistent aura that follows a target and pulses
/// until stopped. Created through <see cref="SkillVFX"/>.
/// </summary>
[AddComponentMenu("")]
public class VfxRing : MonoBehaviour
{
    private SpriteRenderer sprite;
    private Transform follow;
    private Health stopWhenDead;
    private bool followAssigned;

    private bool persistent;
    private bool stopping;
    private float stopDuration = 0.2f;
    private float stopElapsed;

    private Color color;
    private float startRadius;
    private float endRadius;
    private float duration;
    private float elapsed;
    private float width;
    private float pulse;

    private int points;      // 0 = circle
    private int step = 1;    // star polygon step (points/step)
    private float rotationSpeed;
    private float angle;

    public float Radius { get; set; }
    public Color Color { get => color; set => color = value; }
    public float Width { get => width; set => width = value; }
    public bool IsAlive => this != null && !stopping;

    public static VfxRing Create(string name, Vector3 position, Transform follow)
    {
        var go = new GameObject(name);
        go.transform.position = position;

        var ring = go.AddComponent<VfxRing>();
        ring.follow = follow;
        ring.followAssigned = follow != null;

        ring.sprite = go.AddComponent<SpriteRenderer>();
        if (SkillVFX.Material != null)
            ring.sprite.sharedMaterial = SkillVFX.Material;
        ring.sprite.sortingLayerID = SkillVFX.SortingLayerId;
        ring.sprite.sortingOrder = SkillVFX.SortingOrder;

        return ring;
    }

    public void ConfigureBurst(Color c, float fromRadius, float toRadius, float time, float lineWidth)
    {
        persistent = false;
        color = c;
        startRadius = fromRadius;
        endRadius = toRadius;
        duration = Mathf.Max(0.01f, time);
        width = lineWidth;
        Radius = fromRadius;
        Redraw(fromRadius, c, lineWidth);
    }

    public void ConfigureAura(Color c, float radius, float lineWidth, float time, float pulseAmount)
    {
        persistent = true;
        color = c;
        Radius = radius;
        width = lineWidth;
        duration = time;
        pulse = pulseAmount;
        Redraw(radius, c, lineWidth);
    }

    public void SetShape(int pointCount, int starStep, float rotationDegreesPerSecond)
    {
        points = pointCount >= 3 ? pointCount : 0;
        step = Mathf.Max(1, starStep);
        rotationSpeed = rotationDegreesPerSecond;
        angle = Random.Range(0f, 360f);
    }

    public void StopWhenDead(Health health) => stopWhenDead = health;

    /// <summary>Fade out and destroy.</summary>
    public void Stop(float fadeDuration = 0.2f)
    {
        if (this == null || stopping)
            return;

        stopping = true;
        stopDuration = Mathf.Max(0.01f, fadeDuration);
        stopElapsed = 0f;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        elapsed += dt;
        angle += rotationSpeed * dt;

        if (follow != null)
        {
            transform.position = PixelArt.Snap(follow.position);

            // Hidden while the target is benched / disabled.
            if (sprite != null)
                sprite.enabled = follow.gameObject.activeInHierarchy;
        }
        else if (persistent && !stopping && followAssigned)
        {
            // Target destroyed.
            Stop(0.15f);
        }

        if (stopWhenDead != null && stopWhenDead.IsDead && !stopping)
            Stop(0.15f);

        if (follow == null)
            transform.position = PixelArt.Snap(transform.position);

        if (persistent)
        {
            if (!stopping && duration > 0f && elapsed >= duration)
                Stop(0.25f);

            float alpha = 1f;

            if (stopping)
            {
                stopElapsed += dt;
                alpha = 1f - Mathf.Clamp01(stopElapsed / stopDuration);

                if (stopElapsed >= stopDuration)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            // Fade in over the first 0.12 s.
            alpha *= Mathf.Clamp01(elapsed / 0.12f);

            float pulseFactor = 1f + Mathf.Sin(elapsed * 6f) * pulse;
            Redraw(Radius * pulseFactor, color.WithAlpha(color.a * alpha), width);
            return;
        }

        float t = Mathf.Clamp01(elapsed / duration);
        float eased = 1f - (1f - t) * (1f - t);
        float radius = Mathf.Lerp(startRadius, endRadius, eased);
        float fade = 1f - t;

        if (stopping)
        {
            stopElapsed += dt;
            fade *= 1f - Mathf.Clamp01(stopElapsed / stopDuration);
        }

        Redraw(radius, color.WithAlpha(color.a * fade), Mathf.Lerp(width, width * 0.3f, t));

        if (t >= 1f || (stopping && stopElapsed >= stopDuration))
            Destroy(gameObject);
    }

    private void Redraw(float radius, Color c, float lineWidth)
    {
        if (sprite == null)
            return;

        int radiusPx = PixelArt.ToPixels(radius);
        Color stepped = c.WithAlpha(PixelArt.StepAlpha(c.a));

        if (radiusPx < 1 || stepped.a <= 0f)
        {
            sprite.color = Color.clear;
            return;
        }

        int thicknessPx = Mathf.Max(1, PixelArt.ToPixels(lineWidth));
        sprite.sprite = PixelArt.Shape(radiusPx, thicknessPx, points, step, angle);
        sprite.color = stepped;
    }
}
