using UnityEngine;

/// <summary>
/// Jagged pixel-art energy line between two points/transforms, rasterised
/// into its own point-filtered texture. With duration &lt;= 0 it stays
/// (re-jittering) until <see cref="Stop"/> or an endpoint disappears.
/// </summary>
[AddComponentMenu("")]
public class VfxBeam : MonoBehaviour
{
    private const int Segments = 8;
    private const float JitterInterval = 0.06f;
    private const int MaxTextureSize = 1024;

    private SpriteRenderer sprite;
    private Texture2D texture;
    private Sprite generated;
    private Color32[] buffer;

    private Transform from;
    private Transform to;
    private bool followsEnds;
    private Vector3 fromPosition;
    private Vector3 toPosition;

    private Color color;
    private float width;
    private float duration;
    private float jitter;
    private float elapsed;
    private float jitterTimer;
    private bool stopping;
    private float stopElapsed;
    private float stopDuration = 0.15f;
    private bool dirty = true;
    private Vector2Int lastFromPx;
    private Vector2Int lastToPx;

    private readonly Vector2Int[] offsets = new Vector2Int[Segments + 1];

    public Color Color { get => color; set => color = value; }

    public static VfxBeam Create()
    {
        var go = new GameObject("VfxBeam");
        var beam = go.AddComponent<VfxBeam>();

        beam.sprite = go.AddComponent<SpriteRenderer>();
        if (SkillVFX.Material != null)
            beam.sprite.sharedMaterial = SkillVFX.Material;
        beam.sprite.sortingLayerID = SkillVFX.SortingLayerId;
        beam.sprite.sortingOrder = SkillVFX.SortingOrder + 1;

        return beam;
    }

    public void Configure(
        Transform fromTransform, Transform toTransform,
        Vector3 fromPos, Vector3 toPos,
        Color c, float lineWidth, float time, float jitterAmount)
    {
        from = fromTransform;
        to = toTransform;
        followsEnds = fromTransform != null && toTransform != null;
        fromPosition = fromPos;
        toPosition = toPos;
        color = c;
        width = lineWidth;
        duration = time;
        jitter = jitterAmount;
        Rejitter();
        Rebuild();
        ApplyColor(1f);
    }

    public void Stop(float fadeDuration = 0.15f)
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

        if (followsEnds)
        {
            if (from == null || to == null)
            {
                Stop();
            }
            else
            {
                fromPosition = from.position;
                toPosition = to.position;
                sprite.enabled = from.gameObject.activeInHierarchy && to.gameObject.activeInHierarchy;
            }
        }

        jitterTimer -= dt;
        if (jitterTimer <= 0f)
            Rejitter();

        float alpha = 1f;

        if (duration > 0f)
        {
            alpha = 1f - Mathf.Clamp01(elapsed / duration);

            if (elapsed >= duration)
            {
                Destroy(gameObject);
                return;
            }
        }

        if (stopping)
        {
            stopElapsed += dt;
            alpha *= 1f - Mathf.Clamp01(stopElapsed / stopDuration);

            if (stopElapsed >= stopDuration)
            {
                Destroy(gameObject);
                return;
            }
        }

        Rebuild();
        ApplyColor(alpha);
    }

    private void ApplyColor(float alpha)
    {
        if (sprite != null)
            sprite.color = color.WithAlpha(PixelArt.StepAlpha(color.a * alpha));
    }

    private void Rejitter()
    {
        jitterTimer = JitterInterval;
        int amp = Mathf.Max(0, PixelArt.ToPixels(jitter));

        for (int i = 0; i <= Segments; i++)
        {
            bool isEnd = i == 0 || i == Segments;
            offsets[i] = isEnd || amp == 0
                ? Vector2Int.zero
                : new Vector2Int(Random.Range(-amp, amp + 1), Random.Range(-amp, amp + 1));
        }

        dirty = true;
    }

    private void Rebuild()
    {
        var a = new Vector2Int(PixelArt.ToPixels(fromPosition.x), PixelArt.ToPixels(fromPosition.y));
        var b = new Vector2Int(PixelArt.ToPixels(toPosition.x), PixelArt.ToPixels(toPosition.y));

        if (!dirty && a == lastFromPx && b == lastToPx)
            return;

        dirty = false;
        lastFromPx = a;
        lastToPx = b;

        int thickness = Mathf.Max(1, PixelArt.ToPixels(width));

        // Polyline points in absolute pixel coordinates.
        var pts = new Vector2Int[Segments + 1];
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;

        for (int i = 0; i <= Segments; i++)
        {
            float t = i / (float)Segments;
            var p = new Vector2Int(
                Mathf.RoundToInt(Mathf.Lerp(a.x, b.x, t)) + offsets[i].x,
                Mathf.RoundToInt(Mathf.Lerp(a.y, b.y, t)) + offsets[i].y);
            pts[i] = p;
            minX = Mathf.Min(minX, p.x); minY = Mathf.Min(minY, p.y);
            maxX = Mathf.Max(maxX, p.x); maxY = Mathf.Max(maxY, p.y);
        }

        int pad = thickness + 1;
        minX -= pad; minY -= pad; maxX += pad; maxY += pad;

        int w = Mathf.Clamp(maxX - minX + 1, 1, MaxTextureSize);
        int h = Mathf.Clamp(maxY - minY + 1, 1, MaxTextureSize);

        if (texture == null || texture.width != w || texture.height != h)
        {
            if (generated != null) Destroy(generated);
            if (texture != null) Destroy(texture);

            texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "PixelBeam"
            };
            buffer = new Color32[w * h];
            generated = Sprite.Create(texture, new Rect(0, 0, w, h), Vector2.zero,
                PixelArt.PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.sprite = generated;
        }
        else
        {
            System.Array.Clear(buffer, 0, buffer.Length);
        }

        for (int i = 0; i < Segments; i++)
        {
            PixelArt.DrawLine(buffer, w, h,
                pts[i].x - minX, pts[i].y - minY,
                pts[i + 1].x - minX, pts[i + 1].y - minY,
                thickness);
        }

        texture.SetPixels32(buffer);
        texture.Apply(false);

        // Pivot is bottom-left: place the texture's origin on the grid.
        transform.position = new Vector3(
            (minX - 0.5f) * PixelArt.PixelSize,
            (minY - 0.5f) * PixelArt.PixelSize,
            0f);
    }

    private void OnDestroy()
    {
        if (generated != null) Destroy(generated);
        if (texture != null) Destroy(texture);
    }
}
