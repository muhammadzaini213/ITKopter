
using System;
using System.Collections;
using System.Collections.Generic;
using Slafurry.Core.Abstract;
using Slafurry.Systems.Save;
using UnityEngine;

[Serializable]
public class ShopDailyCount
{
    public string key;
    public int count;
}

[Serializable]
public class ShopSaveData
{
    public string date;
    public List<ShopDailyCount> conversionCounts = new();
}

public class ShopSystem : GameSystem<ShopSystem>
{
    public const int MaxPurchaseQuantity = 999;
    private const string SaveFileName = "shop";

    // Conversions done today, keyed by ConversionOffer.Key. Reset when the local date changes.
    private readonly Dictionary<string, int> conversionCounts = new();
    private string countsDate;

    /// <summary>Raised after a successful conversion so shop UIs can refresh.</summary>
    public event Action OnConversionCompleted;

    public override IEnumerator Initialize()
    {
        LoadDailyCounts();
        yield return null;
    }

    public override void PostInitialize()
    {
    }

    public bool CanBuyItem(
        ShopData shop,
        ShopItemEntry entry,
        int quantity,
        out string error)
    {
        error = null;

        if (shop == null)
        {
            error = "Shop data is null.";
            return false;
        }

        if (entry == null)
        {
            error = "Shop item entry is null.";
            return false;
        }

        if (shop.ItemsForSale == null ||
            !System.Array.Exists(
                shop.ItemsForSale,
                item => item == entry))
        {
            error = "Item does not belong to this shop.";
            return false;
        }

        if (entry.Price <= 0)
        {
            error = "Item price must be greater than zero.";
            return false;
        }

        if (entry.RewardData == null)
        {
            error = "Item reward data is null.";
            return false;
        }

        if (quantity <= 0 || quantity > MaxPurchaseQuantity)
        {
            error = "Purchase quantity is outside the allowed range.";
            return false;
        }

        long totalCost = (long)entry.Price * quantity;

        if (totalCost > int.MaxValue)
        {
            error = "Total purchase price is too high.";
            return false;
        }

        if ((long)GetUnitsPerPurchase(entry) * quantity > int.MaxValue)
        {
            error = "Purchase amount is too high.";
            return false;
        }

        if (CurrencySystem.Instance == null)
        {
            error = "Currency system is unavailable.";
            return false;
        }

        if (CurrencySystem.Instance.Coins < totalCost)
        {
            error = "Not enough Coins.";
            return false;
        }

        if (RewardSystem.Instance == null)
        {
            error = "Reward system is unavailable.";
            return false;
        }

        List<RewardGrantRequest> requests =
            new List<RewardGrantRequest>
            {
                new RewardGrantRequest(
                    entry.RewardData,
                    GetUnitsPerPurchase(entry) * quantity)
            };

        if (!RewardSystem.Instance.CanGrantRewards(
                requests, out error))
        {
            return false;
        }

        return true;
    }

    public bool BuyItem(
        ShopData shop,
        ShopItemEntry entry,
        int quantity,
        out string error)
    {
        if (!CanBuyItem(shop, entry, quantity, out error))
            return false;

        int totalCost = (int)((long)entry.Price * quantity);

        if (!CurrencySystem.Instance.SpendCoins(totalCost))
        {
            error = "Failed to spend Coins.";
            return false;
        }

        // Each purchase gives the entry's bundle size (e.g. "Simple Dust x5" = 5 per buy).
        int grantedAmount = GetUnitsPerPurchase(entry) * quantity;

        List<RewardGrantResult> results =
            RewardSystem.Instance.GrantReward(
                entry.RewardData,
                grantedAmount);

        if (results == null ||
            results.Count == 0 ||
            results[0] == null ||
            !results[0].Success)
        {
            CurrencySystem.Instance.AddCoins(totalCost);

            error = "Failed to grant reward. Coins refunded.";
            Debug.LogError($"[ShopSystem] {error}");
            return false;
        }

        // Repeat the reward entry so the popup displays the granted amount.
        List<RewardData> popupRewards =
            new List<RewardData>(grantedAmount);

        for (int i = 0; i < grantedAmount; i++)
            popupRewards.Add(entry.RewardData);

        RewardSystem.Instance.ShowRewardPopup(popupRewards);

        error = null;

        Debug.Log(
            $"Purchased {quantity}x {entry.EntryName} ({grantedAmount} granted) " +
            $"from {shop.ShopName} for {totalCost} Coins.");

        Objectives.Report(ObjectiveType.ShopPurchases, quantity);
        return true;
    }

    /// <summary>Units granted per single purchase of an entry (its bundle size, at least 1).</summary>
    public static int GetUnitsPerPurchase(ShopItemEntry entry)
    {
        return entry != null ? Mathf.Max(1, entry.ItemCount) : 1;
    }

    public bool BuyItem(
        ShopData shop,
        ShopItemEntry entry,
        out string error)
    {
        return BuyItem(shop, entry, 1, out error);
    }

    public bool BuyItem(ShopData shop, ShopItemEntry entry)
    {
        bool success = BuyItem(shop, entry, 1, out string error);

        if (!success)
            Debug.LogWarning($"[ShopSystem] Purchase failed: {error}");

        return success;
    }

    // ============================================
    // CONVERSION
    // ============================================

    /// <summary>Every offer the conversion shop shows: each rule applied to each matching character.</summary>
    public List<ConversionOffer> GetConversionOffers(ShopData shop)
    {
        List<ConversionOffer> offers = new List<ConversionOffer>();
        PlayerSystem player = PlayerSystem.Instance;

        if (shop == null || !shop.IsConversionShop || player == null ||
            player.CharacterDatabase == null || player.CharacterDatabase.Characters == null)
        {
            return offers;
        }

        foreach (ConversionRule rule in shop.ConversionRules)
        {
            if (rule == null)
                continue;

            foreach (CharacterData character in player.CharacterDatabase.Characters)
            {
                if (character == null || character.Rarity != rule.Rarity)
                    continue;

                // Unlock offers only make sense for characters not owned yet.
                if (rule.Kind == ConversionKind.EngramToUnlock &&
                    player.IsCharacterOwned(character))
                {
                    continue;
                }

                ConversionOffer offer = new ConversionOffer(rule, character);

                if (offer.IsValid)
                    offers.Add(offer);
            }
        }

        return offers;
    }

    /// <summary>Conversions still allowed today for this offer (int.MaxValue when unlimited).</summary>
    public int GetRemainingToday(ConversionOffer offer)
    {
        if (offer == null)
            return 0;

        if (offer.DailyLimit <= 0)
            return int.MaxValue;

        RefreshDayIfNeeded();
        conversionCounts.TryGetValue(offer.Key, out int used);
        return Mathf.Max(0, offer.DailyLimit - used);
    }

    /// <summary>Largest quantity the player can convert right now (materials and daily limit).</summary>
    public int GetMaxConvertQuantity(ConversionOffer offer)
    {
        if (offer == null || InventorySystem.Instance == null)
            return 0;

        int affordable = InventorySystem.Instance.GetItemAmount(offer.InputItem) / offer.InputAmount;
        int max = Mathf.Min(affordable, GetRemainingToday(offer), MaxPurchaseQuantity);

        // A character can only be unlocked once.
        if (offer.Rule.Kind == ConversionKind.EngramToUnlock)
            max = Mathf.Min(max, 1);

        return Mathf.Max(0, max);
    }

    public bool CanConvert(ConversionOffer offer, int quantity, out string error)
    {
        error = null;

        if (offer == null || !offer.IsValid)
        {
            error = "Invalid conversion.";
            return false;
        }

        if (InventorySystem.Instance == null || !InventorySystem.Instance.IsDatabaseReady ||
            PlayerSystem.Instance == null)
        {
            error = "Inventory is unavailable.";
            return false;
        }

        if (quantity <= 0 || quantity > MaxPurchaseQuantity)
        {
            error = "Quantity is outside the allowed range.";
            return false;
        }

        if (quantity > GetRemainingToday(offer))
        {
            error = "Daily limit reached.";
            return false;
        }

        if (offer.Rule.Kind == ConversionKind.EngramToUnlock)
        {
            if (quantity != 1)
            {
                error = "A character can only be unlocked once.";
                return false;
            }

            if (PlayerSystem.Instance.IsCharacterOwned(offer.Character))
            {
                error = $"{offer.Character.CharacterName} is already owned.";
                return false;
            }
        }

        long needed = (long)offer.InputAmount * quantity;

        if (InventorySystem.Instance.GetItemAmount(offer.InputItem) < needed)
        {
            error = $"Not enough {offer.InputItem.ItemName}.";
            return false;
        }

        if (offer.Output is ItemData outputItem &&
            !InventorySystem.Instance.CanAddItem(outputItem, offer.OutputAmount * quantity))
        {
            error = "Cannot add the converted item.";
            return false;
        }

        return true;
    }

    public bool Convert(ConversionOffer offer, int quantity, out string error)
    {
        if (!CanConvert(offer, quantity, out error))
            return false;

        InventorySystem inventory = InventorySystem.Instance;
        PlayerSystem player = PlayerSystem.Instance;
        int inputTotal = offer.InputAmount * quantity;

        if (!inventory.RemoveItem(offer.InputItem, inputTotal))
        {
            error = $"Not enough {offer.InputItem.ItemName}.";
            return false;
        }

        bool granted;
        int outputTotal = offer.OutputAmount * quantity;

        if (offer.Output is CharacterData character)
            granted = player.AddNewOwnedCharacter(character);
        else
            granted = inventory.AddItem((ItemData)offer.Output, outputTotal);

        if (!granted)
        {
            inventory.AddItem(offer.InputItem, inputTotal);
            error = "Conversion failed. Materials refunded.";
            return false;
        }

        RefreshDayIfNeeded();
        conversionCounts.TryGetValue(offer.Key, out int used);
        conversionCounts[offer.Key] = used + quantity;
        SaveDailyCounts();

        // Inventory does not persist by itself.
        player.SaveProgress();

        if (RewardSystem.Instance != null)
        {
            int shown = offer.Output is CharacterData ? 1 : outputTotal;
            List<RewardData> popupRewards = new List<RewardData>(shown);

            for (int i = 0; i < shown; i++)
                popupRewards.Add(offer.Output);

            RewardSystem.Instance.ShowRewardPopup(popupRewards);
        }

        Debug.Log($"[ShopSystem] Converted {inputTotal}x {offer.InputItem.ItemName} into {offer.OutputName} x{quantity}.");

        error = null;
        OnConversionCompleted?.Invoke();
        return true;
    }

    private void RefreshDayIfNeeded()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        if (countsDate == today)
            return;

        countsDate = today;
        conversionCounts.Clear();
    }

    private void LoadDailyCounts()
    {
        ShopSaveData data = Save.From(SaveFileName, new ShopSaveData());
        conversionCounts.Clear();
        countsDate = data?.date;

        if (data?.conversionCounts != null)
        {
            foreach (ShopDailyCount entry in data.conversionCounts)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.key) && entry.count > 0)
                    conversionCounts[entry.key] = entry.count;
            }
        }

        // Drops yesterday's counts.
        RefreshDayIfNeeded();
    }

    private void SaveDailyCounts()
    {
        ShopSaveData data = new ShopSaveData { date = countsDate };

        foreach (KeyValuePair<string, int> pair in conversionCounts)
            data.conversionCounts.Add(new ShopDailyCount { key = pair.Key, count = pair.Value });

        Save.To(data, SaveFileName);
    }
}
