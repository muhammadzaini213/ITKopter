
using System.Collections;
using System.Collections.Generic;
using Slafurry.Core.Abstract;
using UnityEngine;

public class ShopSystem : GameSystem<ShopSystem>
{
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

        if (shop.ItemsForSale == null ||
            !System.Array.Exists(
                shop.ItemsForSale,
                item => item == entry))
        {
            error = "Item does not belong to this shop.";
            return false;
        }

        if (CurrencySystem.Instance == null)
        {
            error = "Currency system is unavailable.";
            return false;
        }

        if (CurrencySystem.Instance.Coins < entry.Price)
        {
            error = "Not enough Coins.";
            return false;
        }

        if (RewardSystem.Instance == null)
        {
            error = "Reward system is unavailable.";
            return false;
        }

        // CanGrantRewards membutuhkan list request dan parameter out.
        List<RewardGrantRequest> requests =
            new List<RewardGrantRequest>
            {
                new RewardGrantRequest(entry.RewardData, 1)
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
        out string error)
    {
        if (!CanBuyItem(shop, entry, out error))
        {
            return false;
        }

        if (!CurrencySystem.Instance.SpendCoins(entry.Price))
        {
            error = "Failed to spend Coins.";
            return false;
        }

        // GrantReward mengembalikan List<RewardGrantResult>.
        List<RewardGrantResult> results =
            RewardSystem.Instance.GrantReward(
                entry.RewardData, 1);

        if (results == null ||
            results.Count == 0 ||
            results[0] == null ||
            !results[0].Success)
        {
            // Refund jika reward gagal diberikan.
            CurrencySystem.Instance.AddCoins(entry.Price);

            error = "Failed to grant reward. Coins refunded.";
            Debug.LogError(error);
            return false;
        }

        error = null;

        Debug.Log(
            $"Purchased {entry.EntryName} from {shop.ShopName} " +
            $"for {entry.Price} Coins.");

        return true;
    }

    public bool BuyItem(ShopData shop, ShopItemEntry entry)
    {
        bool success = BuyItem(shop, entry, out string error);

        if (!success)
        {
            Debug.LogWarning($"Purchase failed: {error}");
        }

        return success;
    }
}