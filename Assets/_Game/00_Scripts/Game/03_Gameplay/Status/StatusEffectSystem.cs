using System;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectSystem : MonoBehaviour
{
    public static StatusEffectSystem Instance { get; private set; }

    public event Action<GameObject, StatusEffect> OnEffectAdded;
    public event Action<GameObject, StatusEffect> OnEffectRemoved;

    private readonly Dictionary<GameObject, Dictionary<string, StatusEffect>> effectsByTarget =
        new Dictionary<GameObject, Dictionary<string, StatusEffect>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public StatusEffect Add(GameObject target, string effectId, float duration, int stacks = 1,
        bool refreshOnReapply = true)
    {
        if (target == null)
            return null;

        if (!effectsByTarget.TryGetValue(target, out var map))
        {
            map = new Dictionary<string, StatusEffect>();
            effectsByTarget[target] = map;
        }

        if (map.TryGetValue(effectId, out var existing))
        {
            existing.Apply(stacks);
            if (refreshOnReapply)
                existing.Refresh();
            return existing;
        }

        var go = new GameObject($"Status:{effectId}");
        go.transform.SetParent(target.transform, false);

        var effect = go.AddComponent<StatusEffect>();
        effect.Configure(effectId, duration, stacks, refreshOnReapply);
        effect.OnExpired += fx => Remove(target, effectId);

        map[effectId] = effect;
        OnEffectAdded?.Invoke(target, effect);
        return effect;
    }

    public StatusEffect Get(GameObject target, string effectId)
    {
        if (target == null || !effectsByTarget.TryGetValue(target, out var map))
            return null;

        map.TryGetValue(effectId, out var effect);
        return effect;
    }

    public int GetStacks(GameObject target, string effectId)
    {
        var effect = Get(target, effectId);
        return effect != null ? effect.CurrentStacks : 0;
    }

    public void Remove(GameObject target, string effectId)
    {
        if (target == null || !effectsByTarget.TryGetValue(target, out var map))
            return;

        if (map.Remove(effectId, out var effect))
        {
            effect.OnExpired -= null;
            Destroy(effect.gameObject);
            OnEffectRemoved?.Invoke(target, effect);
        }

        if (map.Count == 0)
            effectsByTarget.Remove(target);
    }

    public void ClearTarget(GameObject target)
    {
        if (target == null || !effectsByTarget.TryGetValue(target, out var map))
            return;

        foreach (var kvp in map)
        {
            kvp.Value.OnExpired -= null;
            Destroy(kvp.Value.gameObject);
            OnEffectRemoved?.Invoke(target, kvp.Value);
        }

        map.Clear();
        effectsByTarget.Remove(target);
    }

    private void Update()
    {
        var targets = new List<GameObject>(effectsByTarget.Keys);

        foreach (var target in targets)
        {
            if (target == null)
                continue;

            if (!effectsByTarget.TryGetValue(target, out var map))
                continue;

            var ids = new List<string>(map.Keys);
            foreach (var id in ids)
            {
                if (!map.TryGetValue(id, out var effect))
                    continue;

                effect.Tick(Time.deltaTime);
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
