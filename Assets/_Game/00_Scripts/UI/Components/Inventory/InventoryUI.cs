
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Inventory References")]
    [SerializeField] private Transform content;
    [SerializeField] private InventorySlotUI slotPrefab;

    private InventorySystem inventorySystem;
    private readonly List<InventorySlotUI> activeSlots =
        new List<InventorySlotUI>();

    private void Start()
    {
        if (inventorySystem == null)
            inventorySystem = InventorySystem.Instance;

        if (inventorySystem == null)
        {
            Debug.LogError(
                "[InventoryUI] InventorySystem was not found.",
                this);
            return;
        }

        if (content == null || slotPrefab == null)
        {
            Debug.LogError(
                "[InventoryUI] Content or slot prefab is missing.",
                this);
            return;
        }

        RefreshInventory();
    }

    public void RefreshInventory()
    {
        if (inventorySystem == null ||
            !inventorySystem.IsDatabaseReady ||
            content == null ||
            slotPrefab == null)
        {
            return;
        }

        ClearSlots();

        List<InventoryItemSaveData> savedItems =
            inventorySystem.GetInventoryItems();

        foreach (InventoryItemSaveData savedItem in savedItems)
        {
            if (savedItem == null || savedItem.amount <= 0)
                continue;

            if (!inventorySystem.TryGetItem(
                savedItem.itemId,
                out ItemData item))
            {
                continue;
            }

            InventorySlotUI slot = Instantiate(
                slotPrefab,
                content);

            slot.Setup(item, savedItem.amount);
            activeSlots.Add(slot);
        }
    }

    private void ClearSlots()
    {
        for (int i = 0; i < activeSlots.Count; i++)
        {
            if (activeSlots[i] != null)
                Destroy(activeSlots[i].gameObject);
        }

        activeSlots.Clear();
    }
}
