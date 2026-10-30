
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ItemDatabase",
    menuName = "Scriptable Objects/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Header("Item Collection")]
    [SerializeField]
    private List<ItemData> items = new List<ItemData>();

    private Dictionary<string, ItemData> itemsById =
        new Dictionary<string, ItemData>();

    public IReadOnlyList<ItemData> Items => items;

    public int Count => items.Count;

    private void OnEnable()
    {
        BuildDatabase();
    }

    private void BuildDatabase()
    {
        itemsById.Clear();

        foreach (ItemData item in items)
        {
            if (item == null ||
                string.IsNullOrWhiteSpace(item.ItemId))
            {
                Debug.LogWarning(
                    "[ItemDatabase] Invalid item or missing ItemId.");
                continue;
            }

            if (itemsById.ContainsKey(item.ItemId))
            {
                Debug.LogError(
                    $"[ItemDatabase] Duplicate ItemId: {item.ItemId}");
                continue;
            }

            itemsById.Add(item.ItemId, item);
        }
    }

    public bool TryGetItem(string itemId, out ItemData item)
    {
        item = null;

        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        return itemsById.TryGetValue(itemId, out item);
    }

    public bool Contains(ItemData item)
    {
        if (item == null ||
            string.IsNullOrWhiteSpace(item.ItemId))
            return false;

        return itemsById.TryGetValue(
            item.ItemId, out ItemData registeredItem)
            && registeredItem == item;
    }
}
