
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
        if (amount <= 0) return;

        coins += amount;
        SaveCurrency();
    }

    public void AddGems(int amount)
    {
        if (amount <= 0) return;

        gems += amount;
        SaveCurrency();
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0 || amount > coins) return false;

        coins -= amount;
        SaveCurrency();
        return true;
    }

    public bool SpendGems(int amount)
    {
        if (amount <= 0 || amount > gems) return false;

        gems -= amount;
        SaveCurrency();
        return true;
    }

    private void LoadCurrency()
    {
        CurrencySaveData data = Save.From(
            SaveFileName,
            new CurrencySaveData()
        );

        coins = Mathf.Max(0, data.coins);
        gems = Mathf.Max(0, data.gems);
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