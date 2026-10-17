
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Slafurry.Core.Abstract;

public class InventorySystem : GameSystem<InventorySystem>
{
    [Header("Item Database")]
    [SerializeField] private List<ItemData> itemDatabase =
        new List<ItemData>();

    private readonly Dictionary<string, ItemData> itemsById =
        new Dictionary<string, ItemData>();

    private readonly Dictionary<string, int> inventory =
        new Dictionary<string, int>();

    public bool IsDatabaseReady { get; private set; }

    public override IEnumerator Initialize()
    {
        IsDatabaseReady = false;

        BuildItemDatabase();

        IsDatabaseReady = true;

        Debug.Log(
            $"[InventorySystem] Item database ready. " +
            $"Registered items: {itemsById.Count}");

        yield return null;
    }

    public override void PostInitialize()
    {
    }

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();
    }

    private void BuildItemDatabase()
    {
        itemsById.Clear();

        foreach (ItemData item in itemDatabase)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
            {
                Debug.LogWarning(
                    "[InventorySystem] An item or item ID is missing.");
                continue;
            }

            if (itemsById.ContainsKey(item.ItemId))
            {
                Debug.LogError(
                    $"[InventorySystem] Duplicate item ID: {item.ItemId}");
                continue;
            }

            itemsById.Add(item.ItemId, item);
        }
    }

    public bool CanAddItem(ItemData item, int amount)
    {
        if (!IsDatabaseReady || item == null || amount <= 0)
            return false;

        if (string.IsNullOrWhiteSpace(item.ItemId))
            return false;

        if (!itemsById.TryGetValue(item.ItemId, out ItemData registeredItem))
            return false;

        return registeredItem == item;
    }

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

        inventory.TryGetValue(itemId, out int currentAmount);

        if (currentAmount > int.MaxValue - amount)
        {
            Debug.LogWarning(
                $"[InventorySystem] Item amount overflow: {itemId}");
            return false;
        }

        inventory[itemId] = currentAmount + amount;
        return true;
    }

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (!IsDatabaseReady ||
            item == null ||
            amount <= 0 ||
            string.IsNullOrWhiteSpace(item.ItemId))
        {
            return false;
        }

        if (!itemsById.TryGetValue(item.ItemId, out ItemData registeredItem) ||
            registeredItem != item)
        {
            return false;
        }

        if (!inventory.TryGetValue(item.ItemId, out int currentAmount))
            return false;

        if (currentAmount < amount)
            return false;

        int remaining = currentAmount - amount;

        if (remaining == 0)
            inventory.Remove(item.ItemId);
        else
            inventory[item.ItemId] = remaining;

        return true;
    }

    public int GetItemAmount(ItemData item)
    {
        if (!IsDatabaseReady || item == null)
            return 0;

        if (!itemsById.TryGetValue(item.ItemId, out ItemData registeredItem) ||
            registeredItem != item)
        {
            return 0;
        }

        return GetItemAmount(item.ItemId);
    }

    public int GetItemAmount(string itemId)
    {
        if (!IsDatabaseReady || string.IsNullOrWhiteSpace(itemId))
            return 0;

        return inventory.TryGetValue(itemId, out int amount)
            ? amount
            : 0;
    }

    public bool TryGetItem(string itemId, out ItemData item)
    {
        item = null;

        if (!IsDatabaseReady || string.IsNullOrWhiteSpace(itemId))
            return false;

        return itemsById.TryGetValue(itemId, out item);
    }

    public void LoadFromSaveData(PlayerSaveData saveData)
    {
        if (!IsDatabaseReady)
        {
            Debug.LogError(
                "[InventorySystem] Cannot load inventory before " +
                "the item database is ready.");
            return;
        }

        inventory.Clear();

        if (saveData == null || saveData.inventoryItems == null)
            return;

        foreach (InventoryItemSaveData savedItem in saveData.inventoryItems)
        {
            if (savedItem == null ||
                string.IsNullOrWhiteSpace(savedItem.itemId) ||
                savedItem.amount <= 0)
            {
                continue;
            }

            if (!itemsById.ContainsKey(savedItem.itemId))
            {
                Debug.LogWarning(
                    $"[InventorySystem] Saved item is not registered: " +
                    $"{savedItem.itemId}");
                continue;
            }

            inventory.TryGetValue(savedItem.itemId, out int currentAmount);

            if (currentAmount > int.MaxValue - savedItem.amount)
            {
                Debug.LogWarning(
                    $"[InventorySystem] Saved item amount overflow: " +
                    $"{savedItem.itemId}");
                continue;
            }

            inventory[savedItem.itemId] = currentAmount + savedItem.amount;
        }
    }

    public void WriteToSaveData(PlayerSaveData saveData)
    {
        if (!IsDatabaseReady)
        {
            Debug.LogError(
                "[InventorySystem] Cannot save inventory before " +
                "the item database is ready.");
            return;
        }

        if (saveData == null)
        {
            Debug.LogWarning(
                "[InventorySystem] Cannot save inventory: save data is null.");
            return;
        }

        if (saveData.inventoryItems == null)
            saveData.inventoryItems = new List<InventoryItemSaveData>();
        else
            saveData.inventoryItems.Clear();

        foreach (KeyValuePair<string, int> entry in inventory)
        {
            saveData.inventoryItems.Add(
                new InventoryItemSaveData(entry.Key, entry.Value));
        }
    }
}
