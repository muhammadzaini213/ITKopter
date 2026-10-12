
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

    public int Energy => energy;
    public int MaxEnergy => MaxEnergyValue;
    public int EnergyMissing => MaxEnergy - Energy;

    public TimeSpan TimeUntilNextEnergy
    {
        get
        {
            if (energy >= MaxEnergy)
                return TimeSpan.Zero;

            long elapsedTicks =
                DateTime.UtcNow.Ticks - lastRegenUtcTicks;

            long remainingTicks =
                GetRecoveryIntervalTicks() - elapsedTicks;

            return TimeSpan.FromTicks(
                Math.Max(0, remainingTicks)
            );
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
        return true;
    }

    public void AddEnergy(int amount)
    {
        RefreshEnergy();

        if (amount <= 0)
            return;

        energy = (int)Math.Min(
            (long)energy + amount,
            MaxEnergy
        );

        if (energy >= MaxEnergy)
            lastRegenUtcTicks = DateTime.UtcNow.Ticks;

        SaveEnergy();
    }

    private void RefreshEnergy()
    {
        long now = DateTime.UtcNow.Ticks;

        if (energy >= MaxEnergy)
        {
            energy = MaxEnergy;
            lastRegenUtcTicks = now;
            return;
        }

        if (lastRegenUtcTicks <= 0 || now < lastRegenUtcTicks)
        {
            lastRegenUtcTicks = now;
            return;
        }

        long intervalTicks = GetRecoveryIntervalTicks();
        long elapsedTicks = now - lastRegenUtcTicks;
        long recovered = elapsedTicks / intervalTicks;

        if (recovered <= 0)
            return;

        energy = (int)Math.Min(
            (long)energy + recovered,
            MaxEnergy
        );

        if (energy >= MaxEnergy)
        {
            lastRegenUtcTicks = now;
        }
        else
        {
            lastRegenUtcTicks += recovered * intervalTicks;
        }

        SaveEnergy();
    }

    private void LoadEnergy()
    {
        if (!Save.Exists(SaveFileName))
        {
            energy = MaxEnergy;
            lastRegenUtcTicks = DateTime.UtcNow.Ticks;
            SaveEnergy();
            return;
        }

        EnergySaveData data = Save.From<EnergySaveData>(
            SaveFileName,
            new EnergySaveData()
        );

        if (data == null)
        {
            energy = MaxEnergy;
            lastRegenUtcTicks = DateTime.UtcNow.Ticks;
            SaveEnergy();
            return;
        }

        energy = Mathf.Clamp(data.energy, 0, MaxEnergy);
        lastRegenUtcTicks = data.lastRegenUtcTicks;

        if (lastRegenUtcTicks <= 0)
            lastRegenUtcTicks = DateTime.UtcNow.Ticks;
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