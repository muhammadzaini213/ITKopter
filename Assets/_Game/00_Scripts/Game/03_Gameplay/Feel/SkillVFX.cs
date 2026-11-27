using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Procedural, asset-free combat visuals (rings, auras, markers, beams,
/// sparks, callout text, afterimages). Everything is built at runtime from
/// LineRenderers / ParticleSystems / TextMeshPro with the always-included
/// Sprites/Default shader, so EX skills read clearly without art or scene
/// setup. Objects clean themselves up.
/// </summary>
public static class SkillVFX
{
    public const string SortingLayerName = "Bullet";
    public const int SortingOrder = 60;

    private static Material material;
    private static TMP_FontAsset font;
    private static int fontSceneHandle = -1;

    public static Material Material
    {
        get
        {
            if (material != null)
                return material;

            Shader shader = Shader.Find("Sprites/Default");

            if (shader != null)
            {
                material = new Material(shader) { name = "SkillVFX (Runtime)" };
            }
            else
            {
                // Last resort: borrow whatever sprites already render with.
                var sr = Object.FindFirstObjectByType<SpriteRenderer>();
                if (sr != null)
                    material = sr.sharedMaterial;
            }

            return material;
        }
    }

    public static int SortingLayerId
    {
        get
        {
            int id = SortingLayer.NameToID(SortingLayerName);
            return SortingLayer.IsValid(id) ? id : 0;
        }
    }

    /// <summary>The game's pixel font if any UI in the scene uses it, else TMP's default.</summary>
    public static TMP_FontAsset Font
    {
        get
        {
            int handle = SceneManager.GetActiveScene().handle;

            if (font != null && fontSceneHandle == handle)
                return font;

            fontSceneHandle = handle;
            font = null;

            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (text != null && text.font != null &&
                    text.font.name.IndexOf("Abaddon", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    font = text.font;
                    break;
                }
            }

            if (font == null)
                font = TMP_Settings.defaultFontAsset;

            return font;
        }
    }

    public static Color WithAlpha(this Color c, float a)
    {
        c.a = a;
        return c;
    }

    // =================================================================
    // Rings / auras / markers
    // =================================================================

    /// <summary>One-shot ring expanding from <paramref name="fromRadius"/> to <paramref name="toRadius"/>.</summary>
    public static VfxRing Ring(
        Vector3 position, Color color,
        float fromRadius, float toRadius,
        float duration, float width,
        int points = 0, int step = 1, float rotationSpeed = 0f)
    {
        VfxRing ring = VfxRing.Create("VfxRing", position, null);
        ring.ConfigureBurst(color, fromRadius, toRadius, duration, width);
        ring.SetShape(points, step, rotationSpeed);
        return ring;
    }

    /// <summary>
    /// Persistent pulsing ring that follows <paramref name="follow"/>.
    /// duration &lt;= 0 = until <see cref="VfxRing.Stop"/> or the target is gone.
    /// </summary>
    public static VfxRing Aura(
        Transform follow, Color color, float radius, float width,
        float duration = -1f, float pulse = 0.06f,
        int points = 0, int step = 1, float rotationSpeed = 0f)
    {
        if (follow == null)
            return null;

        VfxRing ring = VfxRing.Create("VfxAura", follow.position, follow);
        ring.ConfigureAura(color, radius, width, duration, pulse);
        ring.SetShape(points, step, rotationSpeed);
        return ring;
    }

    /// <summary>Rotating marker sized to the target sprite; ends when the target dies.</summary>
    public static VfxRing Marker(
        Health target, Color color, float duration,
        int points = 4, int step = 1, float rotationSpeed = 90f)
    {
        if (target == null)
            return null;

        float radius = Mathf.Clamp(GetVisualRadius(target.transform) * 1.15f, 0.35f, 3f);

        VfxRing ring = Aura(target.transform, color, radius, 0.07f, duration, 0.08f, points, step, rotationSpeed);

        if (ring != null)
            ring.StopWhenDead(target);

        return ring;
    }

    public static float GetVisualRadius(Transform root)
    {
        if (root == null)
            return 0.5f;

        var renderers = root.GetComponentsInChildren<SpriteRenderer>();
        bool any = false;
        Bounds bounds = new Bounds(root.position, Vector3.zero);

        foreach (var sr in renderers)
        {
            if (sr == null || sr.sprite == null || !sr.enabled)
                continue;

            if (!any)
            {
                bounds = sr.bounds;
                any = true;
            }
            else
            {
                bounds.Encapsulate(sr.bounds);
            }
        }

        return any ? Mathf.Max(bounds.extents.x, bounds.extents.y) : 0.5f;
    }

    // =================================================================
    // Beams
    // =================================================================

    public static VfxBeam Beam(
        Transform from, Transform to, Color color,
        float width, float duration, float jitter = 0.15f)
    {
        if (from == null || to == null)
            return null;

        VfxBeam beam = VfxBeam.Create();
        beam.Configure(from, to, from.position, to.position, color, width, duration, jitter);
        return beam;
    }

    public static VfxBeam Beam(
        Vector3 from, Vector3 to, Color color,
        float width, float duration, float jitter = 0.15f)
    {
        VfxBeam beam = VfxBeam.Create();
        beam.Configure(null, null, from, to, color, width, duration, jitter);
        return beam;
    }

    // =================================================================
    // Text
    // =================================================================

    /// <summary>Pop-in callout that floats up and fades.</summary>
    public static VfxText Text(
        Vector3 position, string text, Color color,
        float size = 5f, float duration = 0.9f, float rise = 0.8f)
    {
        VfxText label = VfxText.Create(text, color, size, position, null, Vector3.zero);
        label.ConfigureCallout(duration, rise);
        return label;
    }

    /// <summary>Persistent label following a transform (stack counters, etc.).</summary>
    public static VfxText Label(
        Transform follow, Vector3 offset, string text, Color color, float size = 3.5f)
    {
        if (follow == null)
            return null;

        VfxText label = VfxText.Create(text, color, size, follow.position + offset, follow, offset);
        label.ConfigurePersistent();
        return label;
    }

    // =================================================================
    // Particles
    // =================================================================

    public static ParticleSystem Sparks(
        Vector3 position, Color color,
        int count = 14, float speed = 5f, float lifetime = 0.4f, float size = 0.12f)
    {
        if (Material == null || count <= 0)
            return null;

        var go = new GameObject("VfxSparks");
        go.transform.position = position;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 0.1f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.5f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.35f, speed);
        // Pixel squares: 1–3 VFX pixels, never sub-pixel.
        float px = PixelArt.PixelSize;
        float maxSize = Mathf.Max(px, Mathf.Round(size / px) * px);
        main.startSize = new ParticleSystem.MinMaxCurve(px, maxSize);
        main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.6f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.Max(count, 1);
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.dampen = 0.15f;
        limit.limit = speed * 0.2f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient { mode = GradientMode.Fixed };   // stepped fade
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0.5f, 0.8f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;


        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = Material;
        renderer.sortingLayerID = SortingLayerId;
        renderer.sortingOrder = SortingOrder + 2;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.World;

        ps.Play();
        return ps;
    }

    // =================================================================
    // Afterimage
    // =================================================================

    /// <summary>Tinted copy of every sprite under <paramref name="root"/> that swells and fades.</summary>
    public static void Ghost(Transform root, Color tint, float duration = 0.35f, float scaleTo = 1.5f)
    {
        if (root == null)
            return;

        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>())
        {
            if (sr == null || sr.sprite == null || !sr.enabled || !sr.gameObject.activeInHierarchy)
                continue;

            VfxGhost.Create(sr, tint, duration, scaleTo);
        }
    }

    // =================================================================
    // Composite presets
    // =================================================================

    /// <summary>Shared "EX skill fired" moment: callout, afterimage, burst ring, sparks.</summary>
    public static void Activation(Character owner, string skillName, Color color)
    {
        if (owner == null)
            return;

        Vector3 pos = owner.transform.position;
        float radius = GetVisualRadius(owner.transform);

        Ghost(owner.transform, color.WithAlpha(0.8f), 0.4f, 1.8f);
        Ring(pos, color, radius * 0.6f, radius * 3.2f, 0.45f, 0.14f);
        Ring(pos, Color.white.WithAlpha(0.8f), radius * 0.4f, radius * 2.2f, 0.3f, 0.06f);
        Sparks(pos, color, 22, 7f, 0.45f);

        if (!string.IsNullOrEmpty(skillName))
            Text(pos + Vector3.up * (radius + 0.6f), skillName.ToUpperInvariant(), color, 4.5f, 1.1f, 0.9f);

        CombatFeel.Flash(color.WithAlpha(0.16f), 0.2f);
        CombatFeel.HitStop(0.08f);
        CombatFeel.Shake(0.18f, 0.22f);
    }

    /// <summary>Explosion at a point: ring + sparks (+ optional feel).</summary>
    public static void Explosion(Vector3 position, Color color, float radius, bool withFeel = true)
    {
        Ring(position, color, radius * 0.15f, radius, 0.35f, Mathf.Clamp(radius * 0.08f, 0.06f, 0.3f));
        Ring(position, Color.white.WithAlpha(0.9f), radius * 0.1f, radius * 0.6f, 0.2f, 0.05f);
        Sparks(position, color, Mathf.Clamp(Mathf.RoundToInt(radius * 8f), 8, 40), 4f + radius * 1.5f, 0.45f);

        if (withFeel)
            CombatFeel.MediumImpact();
    }
}
