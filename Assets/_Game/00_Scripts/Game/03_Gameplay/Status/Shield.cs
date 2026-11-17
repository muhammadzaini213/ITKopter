using System;
using UnityEngine;

public class Shield : MonoBehaviour
{
    public float Current { get; private set; }
    public float Max { get; private set; }

    public event Action<float> OnAmountChanged;
    public event Action OnBroken;

    public void Set(float amount)
    {
        Max = amount;
        Current = amount;
        OnAmountChanged?.Invoke(Current);
    }

    public bool Absorb(ref float damage)
    {
        if (Current <= 0f || damage <= 0f)
            return false;

        float absorbed = Mathf.Min(Current, damage);
        Current -= absorbed;
        damage -= absorbed;

        OnAmountChanged?.Invoke(Current);

        if (Current <= 0f)
            OnBroken?.Invoke();

        return true;
    }

    public void Break()
    {
        Current = 0f;
        OnAmountChanged?.Invoke(0f);
        OnBroken?.Invoke();
    }
}
