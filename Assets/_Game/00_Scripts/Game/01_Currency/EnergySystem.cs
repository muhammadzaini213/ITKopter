
using System;
using System.Collections;
using UnityEngine;
using Slafurry.Core.Abstract;
using Slafurry.Systems.Save;

[Serializable]
public class EnergySaveData
{
    public int energy;
    public long lastRegenUtcTicks;
}

public class EnergySystem : GameSystem<EnergySystem>
{
    private const string SaveFileName = "energy";
    private const int MaxEnergyValue = 120;

    [Header("Energy Settings")]
    [SerializeField, Min(1)]
    private int recoveryIntervalMinutes = 5;

    private int energy;
    private long lastRegenUtcTicks;

    public int Energy
    {
        get
        {
            RefreshEnergy();
            return energy;
        }
    }

    public int MaxEnergy => MaxEnergyValue;
    public int EnergyMissing => MaxEnergy - Energy;

    public event Action<int> OnEnergyChanged;

    public TimeSpan TimeUntilNextEnergy
    {
        get
        {
            RefreshEnergy();

            if (energy >= MaxEnergy)
                return TimeSpan.Zero;

            long elapsed = DateTime.UtcNow.Ticks - lastRegenUtcTicks;
            long remaining = GetRecoveryIntervalTicks() - elapsed;

            return TimeSpan.FromTicks(Math.Max(0, remaining));
        }
    }

    public override IEnumerator Initialize()
    {
        LoadEnergy();
        RefreshEnergy();
        yield return null;
    }

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();
    }

    public override void PostInitialize()
    {
    }

    public bool SpendEnergy(int amount)
    {
        RefreshEnergy();

        if (amount <= 0 || amount > energy)
            return false;

        bool wasFull = energy >= MaxEnergy;

        energy -= amount;

        if (wasFull)
            lastRegenUtcTicks = DateTime.UtcNow.Ticks;

        SaveEnergy();
        OnEnergyChanged?.Invoke(energy);

        return true;
    }

    public void AddEnergy(int amount)
    {
        RefreshEnergy();

        if (amount <= 0 || energy >= MaxEnergy)
            return;

        int previous = energy;

        energy = (int)Math.Min(
            (long)energy + amount,
            MaxEnergy);

        if (energy >= MaxEnergy)
            lastRegenUtcTicks = DateTime.UtcNow.Ticks;

        if (energy == previous)
            return;

        SaveEnergy();
        OnEnergyChanged?.Invoke(energy);
    }

    private void RefreshEnergy()
    {
        long now = DateTime.UtcNow.Ticks;

        if (energy >= MaxEnergy)
        {
            energy = MaxEnergy;
            return;
        }

        if (lastRegenUtcTicks <= 0 || now < lastRegenUtcTicks)
        {
            lastRegenUtcTicks = now;
            SaveEnergy();
            return;
        }

        long intervalTicks = GetRecoveryIntervalTicks();
        long elapsedTicks = now - lastRegenUtcTicks;
        long recovered = elapsedTicks / intervalTicks;

        if (recovered <= 0)
            return;

        int previous = energy;

        energy = (int)Math.Min(
            (long)energy + recovered,
            MaxEnergy);

        if (energy >= MaxEnergy)
        {
            lastRegenUtcTicks = now;
        }
        else
        {
            lastRegenUtcTicks += recovered * intervalTicks;
        }

        if (energy != previous)
        {
            SaveEnergy();
            OnEnergyChanged?.Invoke(energy);
        }
    }

    private void LoadEnergy()
    {
        if (!Save.Exists(SaveFileName))
        {
            energy = MaxEnergy;
            lastRegenUtcTicks = DateTime.UtcNow.Ticks;
            SaveEnergy();
            OnEnergyChanged?.Invoke(energy);
            return;
        }

        EnergySaveData data = Save.From(
            SaveFileName,
            new EnergySaveData());

        if (data == null)
        {
            energy = MaxEnergy;
            lastRegenUtcTicks = DateTime.UtcNow.Ticks;
            SaveEnergy();
            OnEnergyChanged?.Invoke(energy);
            return;
        }

        energy = Mathf.Clamp(data.energy, 0, MaxEnergy);
        lastRegenUtcTicks = data.lastRegenUtcTicks;

        long now = DateTime.UtcNow.Ticks;

        if (lastRegenUtcTicks <= 0 || lastRegenUtcTicks > now)
            lastRegenUtcTicks = now;

        Debug.Log($"[EnergySystem] Loaded: {energy}/{MaxEnergy}");
        OnEnergyChanged?.Invoke(energy);
    }

    private void SaveEnergy()
    {
        EnergySaveData data = new EnergySaveData
        {
            energy = energy,
            lastRegenUtcTicks = lastRegenUtcTicks
        };

        Save.To(data, SaveFileName);
    }

    private long GetRecoveryIntervalTicks()
    {
        int minutes = Mathf.Max(1, recoveryIntervalMinutes);
        return TimeSpan.FromMinutes(minutes).Ticks;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
            return;

        RefreshEnergy();
        SaveEnergy();
    }

    private void OnApplicationQuit()
    {
        RefreshEnergy();
        SaveEnergy();
    }
}
