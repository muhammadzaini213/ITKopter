
using System;
using System.Collections;
using UnityEngine;
using Slafurry.Core.Abstract;
using Slafurry.Systems.Save;

[Serializable]
public class CurrencySaveData
{
    public int coins;
    public int gems;
}

public class CurrencySystem : GameSystem<CurrencySystem>
{
    private const string SaveFileName = "currency";

    private int coins;
    private int gems;

    public int Coins => coins;
    public int Gems => gems;

    public event Action<int> OnCoinsChanged;
    public event Action<int> OnGemsChanged;

    public override IEnumerator Initialize()
    {
        LoadCurrency();
        yield return null;
    }

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();
    }

    public override void PostInitialize()
    {
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0)
            return;

        int previous = coins;
        coins = (int)Math.Min((long)coins + amount, int.MaxValue);

        if (coins == previous)
            return;

        SaveCurrency();
        OnCoinsChanged?.Invoke(coins);
    }

    public void AddGems(int amount)
    {
        if (amount <= 0)
            return;

        int previous = gems;
        gems = (int)Math.Min((long)gems + amount, int.MaxValue);

        if (gems == previous)
            return;

        SaveCurrency();
        OnGemsChanged?.Invoke(gems);
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0 || amount > coins)
            return false;

        coins -= amount;
        SaveCurrency();
        OnCoinsChanged?.Invoke(coins);

        return true;
    }

    public bool SpendGems(int amount)
    {
        if (amount <= 0 || amount > gems)
            return false;

        gems -= amount;
        SaveCurrency();
        OnGemsChanged?.Invoke(gems);

        return true;
    }

    private void LoadCurrency()
    {
        CurrencySaveData data = Save.From(
            SaveFileName,
            new CurrencySaveData()
        );

        if (data == null)
            data = new CurrencySaveData();

        coins = Mathf.Max(0, data.coins);
        gems = Mathf.Max(0, data.gems);

        OnCoinsChanged?.Invoke(coins);
        OnGemsChanged?.Invoke(gems);

        Debug.Log($"[CurrencySystem] Loaded: Coins={coins}, Gems={gems}");
    }

    private void SaveCurrency()
    {
        CurrencySaveData data = new CurrencySaveData
        {
            coins = coins,
            gems = gems
        };

        Save.To(data, SaveFileName);
    }
}
