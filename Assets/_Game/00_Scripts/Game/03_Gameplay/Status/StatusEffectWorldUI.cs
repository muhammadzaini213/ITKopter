using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EffectIconEntry
{
    public string effectId;
    public Sprite icon;
}

public class StatusEffectWorldUI : MonoBehaviour
{
    [Header("Container")]
    [SerializeField, Tooltip(
        "Transform that defines where the icon grid sits. " +
        "If empty, a child container is created at this object's origin.")]
    private Transform container;

    [Header("Grid Layout")]
    [SerializeField, Min(0.001f), Tooltip("Uniform scale applied to every icon's transform.")]
    private float iconScale = 0.2f;

    [SerializeField, Min(0f), Tooltip("Spacing between icons in world units.")]
    private float iconSpacing = 0.05f;

    [SerializeField, Min(1), Tooltip("Maximum icons per row before wrapping to a new row.")]
    private int maxColumns = 4;

    [SerializeField, Tooltip("If true, new rows stack upward. Otherwise they stack downward.")]
    private bool stackUpward = true;

    [Header("Rendering")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 100;

    [Header("Effect Icon Mapping")]
    [SerializeField] private EffectIconEntry[] effectIcons;

    private readonly Dictionary<string, SpriteRenderer> activeIcons =
        new Dictionary<string, SpriteRenderer>();

    // Keeps insertion order so the grid is stable and predictable.
    private readonly List<string> iconOrder = new List<string>();

    private readonly Dictionary<string, Sprite> iconLookup =
        new Dictionary<string, Sprite>();

    // Size of one grid cell in world units, derived from the largest mapped sprite.
    private float cellSize;

    public bool IsActive => gameObject.activeInHierarchy;

    private void Awake()
    {
        BuildIconLookup();

        if (container == null)
        {
            var containerGo = new GameObject("StatusEffectIconContainer");
            container = containerGo.transform;
            container.SetParent(transform, false);
        }
    }

    private void BuildIconLookup()
    {
        iconLookup.Clear();
        float largestSide = 0f;

        if (effectIcons != null)
        {
            foreach (var entry in effectIcons)
            {
                if (entry == null || string.IsNullOrEmpty(entry.effectId))
                    continue;

                if (!iconLookup.ContainsKey(entry.effectId))
                    iconLookup[entry.effectId] = entry.icon;

                if (entry.icon != null)
                {
                    Vector2 size = entry.icon.bounds.size;
                    largestSide = Mathf.Max(largestSide, size.x, size.y);
                }
            }
        }

        // Fall back to 1 world unit if no sprites are mapped.
        cellSize = (largestSide > 0f ? largestSide : 1f) * iconScale;
    }

    private void OnEnable()
    {
        if (StatusEffectSystem.Instance != null)
        {
            StatusEffectSystem.Instance.OnEffectAdded += HandleEffectAdded;
            StatusEffectSystem.Instance.OnEffectRemoved += HandleEffectRemoved;
        }

        RefreshFromExisting();
    }

    private void OnDisable()
    {
        if (StatusEffectSystem.Instance != null)
        {
            StatusEffectSystem.Instance.OnEffectAdded -= HandleEffectAdded;
            StatusEffectSystem.Instance.OnEffectRemoved -= HandleEffectRemoved;
        }

        ClearAllIcons();
    }

    private void RefreshFromExisting()
    {
        // Effects are parented as direct children of the target root.
        // Scan this object's ancestor chain so the component still works
        // when placed on a child of the entity.
        var seen = new HashSet<StatusEffect>();
        var node = transform;

        while (node != null)
        {
            for (int i = 0; i < node.childCount; i++)
            {
                var child = node.GetChild(i);
                if (child.TryGetComponent(out StatusEffect effect) && seen.Add(effect))
                {
                    if (!string.IsNullOrEmpty(effect.EffectId) &&
                        !activeIcons.ContainsKey(effect.EffectId))
                    {
                        SpawnIcon(effect.EffectId);
                    }
                }
            }

            node = node.parent;
        }
    }

    private bool IsMyTarget(GameObject target)
    {
        return target != null && transform.IsChildOf(target.transform);
    }

    private void HandleEffectAdded(GameObject target, StatusEffect effect)
    {
        if (!IsMyTarget(target))
            return;

        if (effect == null || string.IsNullOrEmpty(effect.EffectId))
            return;

        if (!activeIcons.ContainsKey(effect.EffectId))
            SpawnIcon(effect.EffectId);
    }

    private void HandleEffectRemoved(GameObject target, StatusEffect effect)
    {
        if (!IsMyTarget(target))
            return;

        if (effect == null || string.IsNullOrEmpty(effect.EffectId))
            return;

        DestroyIcon(effect.EffectId);
    }

    private void SpawnIcon(string effectId)
    {
        if (container == null)
            return;

        var go = new GameObject("StatusIcon_" + effectId);
        go.transform.SetParent(container, false);

        var sr = go.AddComponent<SpriteRenderer>();

        if (iconLookup.TryGetValue(effectId, out var sprite))
            sr.sprite = sprite;

        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;

        ApplySize(go.transform, sr);

        activeIcons[effectId] = sr;
        iconOrder.Add(effectId);

        RepositionIcons();
    }

    private void ApplySize(Transform t, SpriteRenderer sr)
    {
        t.localRotation = Quaternion.identity;
        t.localScale = new Vector3(iconScale, iconScale, 1f);
    }

    private void DestroyIcon(string effectId)
    {
        if (!activeIcons.TryGetValue(effectId, out var sr))
            return;

        if (sr != null)
            Destroy(sr.gameObject);

        activeIcons.Remove(effectId);
        iconOrder.Remove(effectId);

        RepositionIcons();
    }

    private void RepositionIcons()
    {
        int count = iconOrder.Count;

        for (int i = 0; i < count; i++)
        {
            if (!activeIcons.TryGetValue(iconOrder[i], out var sr) || sr == null)
                continue;

            sr.transform.localPosition = GetGridPosition(i, count);
        }
    }

    // Lays icons out in rows of up to maxColumns. Each row is centered horizontally
    // on the container; row 0 sits on the container's origin and later rows stack
    // upward or downward.
    private Vector3 GetGridPosition(int index, int count)
    {
        int columns = Mathf.Max(1, maxColumns);
        int row = index / columns;
        int col = index % columns;

        int itemsInRow = Mathf.Min(columns, count - row * columns);

        float step = cellSize + iconSpacing;
        float rowWidth = itemsInRow * step - iconSpacing;

        // Sprite pivots are centered, so shift by half a cell to center the row.
        float x = col * step - rowWidth * 0.5f + cellSize * 0.5f;
        float y = row * step * (stackUpward ? 1f : -1f);

        return new Vector3(x, y, 0f);
    }

    private void ClearAllIcons()
    {
        foreach (var kvp in activeIcons)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }

        activeIcons.Clear();
        iconOrder.Clear();
    }
}