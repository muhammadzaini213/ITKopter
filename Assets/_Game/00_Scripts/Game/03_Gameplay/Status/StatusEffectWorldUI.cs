using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class EffectIconEntry
{
    public string effectId;
    public Sprite icon;
}

public class StatusEffectWorldUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject iconPrefab;

    [Header("Layout")]
    [SerializeField, Tooltip("World-space offset above the entity.")]
    private Vector3 offset = new Vector3(0f, 2f, 0f);

    [SerializeField, Min(0f)]
    private float iconSize = 32f;

    [SerializeField, Min(0f)]
    private float iconSpacing = 8f;

    [Header("Effect Icon Mapping")]
    [SerializeField] private EffectIconEntry[] effectIcons;

    private readonly Dictionary<string, GameObject> activeIcons =
        new Dictionary<string, GameObject>();

    private readonly Dictionary<string, Sprite> iconLookup =
        new Dictionary<string, Sprite>();

    private RectTransform container;
    private Canvas worldCanvas;

    public bool IsActive => gameObject.activeInHierarchy;

    private void Awake()
    {
        BuildIconLookup();

        var canvasGo = new GameObject("StatusEffectWorldCanvas");
        worldCanvas = canvasGo.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        canvasGo.transform.SetParent(transform, false);

        var rect = canvasGo.GetComponent<RectTransform>();
        rect.sizeDelta = Vector2.one * 200f;

        var containerGo = new GameObject("IconContainer");
        container = containerGo.AddComponent<RectTransform>();
        container.SetParent(canvasGo.transform, false);
        container.anchorMin = new Vector2(0.5f, 0.5f);
        container.anchorMax = new Vector2(0.5f, 0.5f);
        container.pivot = new Vector2(0.5f, 0.5f);
        container.anchoredPosition = Vector2.zero;
        container.sizeDelta = Vector2.zero;
    }

    private void BuildIconLookup()
    {
        iconLookup.Clear();

        if (effectIcons == null)
            return;

        foreach (var entry in effectIcons)
        {
            if (entry == null || string.IsNullOrEmpty(entry.effectId))
                continue;

            if (!iconLookup.ContainsKey(entry.effectId))
                iconLookup[entry.effectId] = entry.icon;
        }
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

    private void LateUpdate()
    {
        if (worldCanvas != null)
            worldCanvas.transform.position = transform.position + offset;
    }

    private void RefreshFromExisting()
    {
        var effects = GetComponentsInChildren<StatusEffect>(true);

        foreach (var effect in effects)
        {
            if (effect == null || string.IsNullOrEmpty(effect.EffectId))
                continue;

            if (!activeIcons.ContainsKey(effect.EffectId))
                SpawnIcon(effect.EffectId);
        }
    }

    private void HandleEffectAdded(GameObject target, StatusEffect effect)
    {
        if (target != gameObject)
            return;

        if (effect == null || string.IsNullOrEmpty(effect.EffectId))
            return;

        if (!activeIcons.ContainsKey(effect.EffectId))
            SpawnIcon(effect.EffectId);
    }

    private void HandleEffectRemoved(GameObject target, StatusEffect effect)
    {
        if (target != gameObject)
            return;

        if (effect == null || string.IsNullOrEmpty(effect.EffectId))
            return;

        DestroyIcon(effect.EffectId);
    }

    private void SpawnIcon(string effectId)
    {
        if (iconPrefab == null)
        {
            Debug.LogWarning(
                "[StatusEffectWorldUI] Icon prefab is not assigned.", this);
            return;
        }

        var instance = Instantiate(iconPrefab, container);

        var image = instance.GetComponent<Image>();
        if (image != null && iconLookup.TryGetValue(effectId, out var sprite))
            image.sprite = sprite;

        var rect = instance.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = Vector2.one * iconSize;
            rect.anchoredPosition = GetPositionForIndex(activeIcons.Count);
        }

        activeIcons[effectId] = instance;

        RepositionIcons();
    }

    private void DestroyIcon(string effectId)
    {
        if (!activeIcons.TryGetValue(effectId, out var instance))
            return;

        if (instance != null)
            Destroy(instance);

        activeIcons.Remove(effectId);

        RepositionIcons();
    }

    private void RepositionIcons()
    {
        int index = 0;

        foreach (var kvp in activeIcons)
        {
            if (kvp.Value == null)
                continue;

            var rect = kvp.Value.GetComponent<RectTransform>();
            if (rect != null)
                rect.anchoredPosition = GetPositionForIndex(index);

            index++;
        }
    }

    private Vector2 GetPositionForIndex(int index)
    {
        float x = index * (iconSize + iconSpacing);
        float totalWidth = activeIcons.Count * (iconSize + iconSpacing) - iconSpacing;
        return new Vector2(x - totalWidth * 0.5f, 0f);
    }

    private void ClearAllIcons()
    {
        foreach (var kvp in activeIcons)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value.gameObject);
        }

        activeIcons.Clear();
    }
}
