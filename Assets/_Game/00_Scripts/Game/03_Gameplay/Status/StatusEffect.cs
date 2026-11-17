using System;
using UnityEngine;

public class StatusEffect : MonoBehaviour
{
    public string EffectId { get; private set; }
    public float Duration { get; private set; }
    public int MaxStacks { get; private set; } = 1;
    public bool RefreshOnReapply { get; private set; } = true;

    public int CurrentStacks { get; private set; }
    public float TimeRemaining { get; private set; }
    public bool IsActive => gameObject.activeInHierarchy && TimeRemaining > 0f;

    public event Action<StatusEffect> OnExpired;

    public void Configure(string effectId, float duration, int stacks = 1, bool refreshOnReapply = true)
    {
        EffectId = effectId;
        Duration = duration;
        MaxStacks = Mathf.Max(1, stacks);
        RefreshOnReapply = refreshOnReapply;
        CurrentStacks = Mathf.Max(1, stacks);
        TimeRemaining = duration;
    }

    public void Apply(int stacks = 1)
    {
        CurrentStacks = Mathf.Min(MaxStacks, CurrentStacks + stacks);
        TimeRemaining = Duration;
    }

    public void Refresh() => TimeRemaining = Duration;

    public void Tick(float deltaTime)
    {
        if (!IsActive) return;

        TimeRemaining -= deltaTime;

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            OnExpired?.Invoke(this);
        }
    }

    public void ExpireNow()
    {
        TimeRemaining = 0f;
        OnExpired?.Invoke(this);
    }
}
