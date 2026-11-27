using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pixel-art rasteriser for procedural VFX. Shapes (rings, polygons, stars)
/// are drawn into small point-filtered textures at a fixed world pixel size
/// so effects match the game's sprites instead of looking like smooth vector
/// lines. Shapes are cached (LRU by pixel count); positions snap to the grid.
/// </summary>
public static class PixelArt
{
    /// <summary>World size of one VFX pixel (≈ one art pixel of the scaled sprites).</summary>
    public static float PixelSize = 1f / 16f;

    private const int AngleSteps = 32;
    private const int MaxCachedPixels = 4_000_000;

    private class Entry
    {
        public Sprite Sprite;
        public Texture2D Texture;
        public int Pixels;
        public int LastFrame;
    }

    private static readonly Dictionary<long, Entry> cache = new Dictionary<long, Entry>();
    private static int cachedPixels;

    public static float PixelsPerUnit => 1f / PixelSize;

    public static int ToPixels(float world) => Mathf.RoundToInt(world / PixelSize);

    public static Vector3 Snap(Vector3 position)
    {
        position.x = Mathf.Round(position.x / PixelSize) * PixelSize;
        position.y = Mathf.Round(position.y / PixelSize) * PixelSize;
        return position;
    }

    /// <summary>Quantise alpha into 4 steps (stepped pixel-style fades).</summary>
    public static float StepAlpha(float alpha)
    {
        return Mathf.Ceil(Mathf.Clamp01(alpha) * 4f) / 4f;
    }

    /// <summary>
    /// White ring / polygon / star sprite. radius and thickness in pixels;
    /// points = 0 for a circle; angle in degrees (ignored for circles).
    /// </summary>
    public static Sprite Shape(int radius, int thickness, int points, int step, float angleDegrees)
    {
        radius = Mathf.Clamp(radius, 1, 400);
        thickness = Mathf.Clamp(thickness, 1, Mathf.Max(1, radius));

        // Coarser radius steps for big shapes keep the cache small.
        if (radius > 96) radius = radius / 4 * 4;
        else if (radius > 48) radius = radius / 2 * 2;

        points = points >= 3 ? Mathf.Min(points, 24) : 0;
        step = points == 0 ? 1 : Mathf.Clamp(step, 1, 15);

        int angleIndex = 0;
        if (points > 0)
        {
            float a = Mathf.Repeat(angleDegrees, 360f);
            angleIndex = Mathf.RoundToInt(a / 360f * AngleSteps) % AngleSteps;
        }

        long key = (long)radius
                   | ((long)thickness << 10)
                   | ((long)points << 20)
                   | ((long)step << 26)
                   | ((long)angleIndex << 32);

        if (cache.TryGetValue(key, out Entry entry) && entry.Sprite != null)
        {
            entry.LastFrame = Time.frameCount;
            return entry.Sprite;
        }

        entry = Rasterise(radius, thickness, points, step, angleIndex * (360f / AngleSteps));
        cache[key] = entry;
        cachedPixels += entry.Pixels;

        if (cachedPixels > MaxCachedPixels)
            Evict();

        return entry.Sprite;
    }

    private static Entry Rasterise(int radius, int thickness, int points, int step, float angleDegrees)
    {
        int size = 2 * (radius + 1) + 1;
        int c = size / 2;
        var pixels = new Color32[size * size];
        var on = new Color32(255, 255, 255, 255);

        if (points == 0)
        {
            float outer = radius + 0.5f;
            float inner = radius - thickness + 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - c;
                    float dy = y - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    if (d <= outer && d > inner)
                        pixels[y * size + x] = on;
                }
            }
        }
        else
        {
            var vertices = new Vector2[points];
            float offset = angleDegrees * Mathf.Deg2Rad + Mathf.PI * 0.5f;

            for (int i = 0; i < points; i++)
            {
                int v = (i * step) % points;
                float a = v / (float)points * Mathf.PI * 2f + offset;
                vertices[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            }

            float half = thickness * 0.5f + 0.25f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x - c, y - c);

                    for (int i = 0; i < points; i++)
                    {
                        if (DistanceToSegment(p, vertices[i], vertices[(i + 1) % points]) <= half)
                        {
                            pixels[y * size + x] = on;
                            break;
                        }
                    }
                }
            }
        }

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "PixelShape"
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        var sprite = Sprite.Create(
            texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            PixelsPerUnit, 0, SpriteMeshType.FullRect);

        return new Entry { Sprite = sprite, Texture = texture, Pixels = size * size, LastFrame = Time.frameCount };
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * Mathf.Clamp01(t));
    }

    private static void Evict()
    {
        int frame = Time.frameCount;
        var keys = new List<long>(cache.Keys);
        keys.Sort((x, y) => cache[x].LastFrame.CompareTo(cache[y].LastFrame));

        foreach (long key in keys)
        {
            if (cachedPixels <= MaxCachedPixels * 3 / 4)
                break;

            Entry e = cache[key];

            // Never free a sprite a renderer may still be showing.
            if (e.LastFrame >= frame - 2)
                continue;

            cachedPixels -= e.Pixels;
            cache.Remove(key);

            if (e.Sprite != null) Object.Destroy(e.Sprite);
            if (e.Texture != null) Object.Destroy(e.Texture);
        }
    }

    // ---- free-form canvas (beams) ---------------------------------------

    /// <summary>Draws a thick pixel line into a Color32 buffer (Bresenham with a square brush).</summary>
    public static void DrawLine(Color32[] buffer, int width, int height, int x0, int y0, int x1, int y1, int thickness)
    {
        var on = new Color32(255, 255, 255, 255);
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        int lo = -(thickness - 1) / 2;
        int hi = thickness / 2;

        while (true)
        {
            for (int oy = lo; oy <= hi; oy++)
            {
                for (int ox = lo; ox <= hi; ox++)
                {
                    int px = x0 + ox, py = y0 + oy;
                    if (px >= 0 && py >= 0 && px < width && py < height)
                        buffer[py * width + px] = on;
                }
            }

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }
}
