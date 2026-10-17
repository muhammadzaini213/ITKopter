using System;
using System.Collections.Generic;

[Serializable]
public class InventoryItemSaveData
{
    public string itemId;
    public int amount;

    public InventoryItemSaveData(string itemId, int amount)
    {
        this.itemId = itemId;
        this.amount = amount;
    }
}