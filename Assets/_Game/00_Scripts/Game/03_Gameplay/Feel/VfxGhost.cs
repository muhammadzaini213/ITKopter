using UnityEngine;

/// <summary>Afterimage: a tinted copy of a sprite that swells and fades.</summary>
[AddComponentMenu("")]
public class VfxGhost : MonoBehaviour
{
    private SpriteRenderer sprite;
    private Color tint;
    private float duration;
    private float scaleTo;
    private float elapsed;
    private Vector3 baseScale;

    public static VfxGhost Create(SpriteRenderer source, Color tint, float duration, float scaleTo)
    {
        var go = new GameObject("VfxGhost");
        go.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        go.transform.localScale = source.transform.lossyScale;

        var ghost = go.AddComponent<VfxGhost>();
        ghost.sprite = go.AddComponent<SpriteRenderer>();
        ghost.sprite.sprite = source.sprite;
        ghost.sprite.flipX = source.flipX;
        ghost.sprite.flipY = source.flipY;
        ghost.sprite.sharedMaterial = SkillVFX.Material != null ? SkillVFX.Material : source.sharedMaterial;
        ghost.sprite.sortingLayerID = source.sortingLayerID;
        ghost.sprite.sortingOrder = source.sortingOrder - 1;
        ghost.sprite.color = tint;

        ghost.tint = tint;
        ghost.duration = Mathf.Max(0.05f, duration);
        ghost.scaleTo = scaleTo;
        ghost.baseScale = go.transform.localScale;

        return ghost;
    }

    private void LateUpdate()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        // Grow in chunky steps rather than smoothly.
        float grow = Mathf.Lerp(1f, scaleTo, 1f - (1f - t) * (1f - t));
        transform.localScale = baseScale * (Mathf.Round(grow * 8f) / 8f);
        sprite.color = tint.WithAlpha(PixelArt.StepAlpha(tint.a * (1f - t)));

        if (t >= 1f)
            Destroy(gameObject);
    }
}
