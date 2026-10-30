
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Slafurry.Core.Abstract;

public class InventorySystem : GameSystem<InventorySystem>
{
    [Header("Item Database")]
    [SerializeField]
    private ItemDatabase itemDatabase;

    private readonly Dictionary<string, int> inventory =
        new Dictionary<string, int>();

    public bool IsDatabaseReady =>
        itemDatabase != null;

    public override IEnumerator Initialize()
    {
        if (!IsDatabaseReady)
        {
            Debug.LogError(
                "[InventorySystem] ItemDatabase reference is missing.");

            yield break;
        }

        Debug.Log(
            "[InventorySystem] Initialized successfully.");

        yield return null;
    }

    public override void PostInitialize()
    {
    }

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();
    }

    // ============================================
    // ITEM VALIDATION
    // ============================================

    public bool CanAddItem(ItemData item, int amount)
    {
        if (!IsDatabaseReady ||
            item == null ||
            amount <= 0)
        {
            return false;
        }

        return itemDatabase.Contains(item);
    }

    // ============================================
    // ADD ITEM
    // ============================================

    public bool AddItem(ItemData item, int amount = 1)
    {
        if (!CanAddItem(item, amount))
        {
            Debug.LogWarning(
                "[InventorySystem] Cannot add an invalid, " +
                "unregistered, or not-yet-ready item.");

            return false;
        }

        string itemId = item.ItemId;

        inventory.TryGetValue(
            itemId,
            out int currentAmount);

        if (currentAmount > int.MaxValue - amount)
        {
            Debug.LogWarning(
                $"[InventorySystem] Item amount overflow: {itemId}");

            return false;
        }

        inventory[itemId] = currentAmount + amount;

        return true;
    }

    // ============================================
    // REMOVE ITEM
    // ============================================

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (!IsDatabaseReady ||
            item == null ||
            amount <= 0 ||
            !itemDatabase.Contains(item))
        {
            return false;
        }

        string itemId = item.ItemId;

        if (!inventory.TryGetValue(
            itemId,
            out int currentAmount))
        {
            return false;
        }

        if (currentAmount < amount)
            return false;

        int remaining = currentAmount - amount;

        if (remaining == 0)
            inventory.Remove(itemId);
        else
            inventory[itemId] = remaining;

        return true;
    }

    // ============================================
    // GET ITEM AMOUNT
    // ============================================

    public int GetItemAmount(ItemData item)
    {
        if (!IsDatabaseReady ||
            item == null ||
            !itemDatabase.Contains(item))
        {
            return 0;
        }

        return GetItemAmount(item.ItemId);
    }

    public int GetItemAmount(string itemId)
    {
        if (!IsDatabaseReady ||
            string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        return inventory.TryGetValue(
            itemId,
            out int amount)
            ? amount
            : 0;
    }

    // ============================================
    // GET ITEM DATA
    // ============================================

    public bool TryGetItem(string itemId, out ItemData item)
    {
        item = null;

        if (!IsDatabaseReady ||
            string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        return itemDatabase.TryGetItem(
            itemId,
            out item);
    }

    // ============================================
    // LOAD INVENTORY
    // ============================================

    public void LoadFromSaveData(PlayerSaveData saveData)
    {
        if (!IsDatabaseReady)
        {
            Debug.LogError(
                "[InventorySystem] Cannot load inventory. " +
                "ItemDatabase is missing.");

            return;
        }

        inventory.Clear();

        if (saveData == null ||
            saveData.inventoryItems == null)
        {
            return;
        }

        foreach (InventoryItemSaveData savedItem
                 in saveData.inventoryItems)
        {
            if (savedItem == null ||
                string.IsNullOrWhiteSpace(savedItem.itemId) ||
                savedItem.amount <= 0)
            {
                continue;
            }

            if (!itemDatabase.TryGetItem(
                savedItem.itemId,
                out _))
            {
                Debug.LogWarning(
                    $"[InventorySystem] Saved item is not registered: " +
                    $"{savedItem.itemId}");

                continue;
            }

            inventory.TryGetValue(
                savedItem.itemId,
                out int currentAmount);

            if (currentAmount > int.MaxValue - savedItem.amount)
            {
                Debug.LogWarning(
                    $"[InventorySystem] Saved item amount overflow: " +
                    $"{savedItem.itemId}");

                continue;
            }

            inventory[savedItem.itemId] =
                currentAmount + savedItem.amount;
        }

        Debug.Log(
            $"[InventorySystem] Inventory loaded. " +
            $"Unique items: {inventory.Count}");
    }

    // ============================================
    // SAVE INVENTORY
    // ============================================

    public void WriteToSaveData(PlayerSaveData saveData)
    {
        if (!IsDatabaseReady)
        {
            Debug.LogError(
                "[InventorySystem] Cannot save inventory. " +
                "ItemDatabase is missing.");

            return;
        }

        if (saveData == null)
        {
            Debug.LogWarning(
                "[InventorySystem] Cannot save inventory: " +
                "save data is null.");

            return;
        }

        if (saveData.inventoryItems == null)
        {
            saveData.inventoryItems =
                new List<InventoryItemSaveData>();
        }
        else
        {
            saveData.inventoryItems.Clear();
        }

        foreach (KeyValuePair<string, int> entry in inventory)
        {
            if (entry.Value <= 0)
                continue;

            saveData.inventoryItems.Add(
                new InventoryItemSaveData(
                    entry.Key,
                    entry.Value));
        }
    }

    // ============================================
    // GET INVENTORY SNAPSHOT
    // ============================================

    public List<InventoryItemSaveData> GetInventoryItems()
    {
        List<InventoryItemSaveData> result =
            new List<InventoryItemSaveData>();

        if (!IsDatabaseReady)
            return result;

        foreach (KeyValuePair<string, int> entry in inventory)
        {
            if (entry.Value <= 0)
                continue;

            result.Add(
                new InventoryItemSaveData(
                    entry.Key,
                    entry.Value));
        }

        return result;
    }
}
