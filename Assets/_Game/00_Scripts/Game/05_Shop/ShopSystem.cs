
using System.Collections;
using System.Collections.Generic;
using Slafurry.Core.Abstract;
using UnityEngine;

public class ShopSystem : GameSystem<ShopSystem>
{
    public const int MaxPurchaseQuantity = 999;

    public override IEnumerator Initialize()
    {
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
                    quantity)
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

        List<RewardGrantResult> results =
            RewardSystem.Instance.GrantReward(
                entry.RewardData,
                quantity);

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

        // Repeat the reward entry so the popup displays the purchased quantity.
        List<RewardData> popupRewards =
            new List<RewardData>(quantity);

        for (int i = 0; i < quantity; i++)
            popupRewards.Add(entry.RewardData);

        RewardSystem.Instance.ShowRewardPopup(popupRewards);

        error = null;

        Debug.Log(
            $"Purchased {quantity}x {entry.EntryName} " +
            $"from {shop.ShopName} for {totalCost} Coins.");

        return true;
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
}
