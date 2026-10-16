
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ShopComponentUI : MonoBehaviour
{
    [Serializable]
    public class ShopSlot
    {
        public ShopData shopData;
        public Button selectButton;
    }

    [Header("Shop Slots")]
    [SerializeField] private ShopSlot[] shops;

    [Header("Shop Items UI")]
    [SerializeField] private Transform itemContentRoot;
    [SerializeField] private ShopItemUI itemPrefab;

    [Header("Purchase Confirmation")]
    [SerializeField] private BuyConfirmationPopupUI confirmationPopup;

    private readonly List<ShopItemUI> spawnedItems = new();
    private readonly List<UnityAction> buttonCallbacks = new();

    private ShopSystem shopSystem;
    private ShopData currentShop;
    private int currentShopIndex = -1;

    private void Start()
    {
        shopSystem = ShopSystem.Instance;

        if (shopSystem == null)
        {
            Debug.LogError(
                "[ShopUI] ShopSystem is not available!");

            return;
        }

        if (itemContentRoot == null ||
            itemPrefab == null ||
            confirmationPopup == null)
        {
            Debug.LogError(
                "[ShopUI] Required UI references are missing!");

            return;
        }

        RegisterShopButtons();
        SelectInitialShop();
    }

    private void RegisterShopButtons()
    {
        if (shops == null)
            return;

        for (int i = 0; i < shops.Length; i++)
        {
            int index = i;

            if (shops[i] == null ||
                shops[i].selectButton == null)
            {
                buttonCallbacks.Add(null);
                continue;
            }

            UnityAction callback = () => SelectShop(index);

            buttonCallbacks.Add(callback);
            shops[i].selectButton.onClick.AddListener(callback);
        }
    }

    private void SelectInitialShop()
    {
        if (shops == null || shops.Length == 0)
        {
            Debug.LogError(
                "[ShopUI] No shops are configured!");

            return;
        }

        for (int i = 0; i < shops.Length; i++)
        {
            if (shops[i] != null &&
                shops[i].shopData != null)
            {
                SelectShop(i);
                return;
            }
        }

        Debug.LogError(
            "[ShopUI] No valid ShopData is assigned!");
    }

    public void SelectShop(int index)
    {
        if (shops == null ||
            index < 0 ||
            index >= shops.Length)
        {
            Debug.LogWarning(
                $"[ShopUI] Invalid shop index: {index}");

            return;
        }

        if (shops[index] == null ||
            shops[index].shopData == null)
        {
            Debug.LogWarning(
                $"[ShopUI] ShopData is missing for slot {index + 1}!");

            return;
        }

        currentShopIndex = index;
        currentShop = shops[index].shopData;

        Debug.Log(
            $"[ShopUI] Selected shop: {currentShop.ShopName}");

        RefreshShopItems();
    }

    private void RefreshShopItems()
    {
        ClearShopItems();

        if (currentShop == null ||
            currentShop.ItemsForSale == null)
        {
            return;
        }

        foreach (ShopItemEntry entry in currentShop.ItemsForSale)
        {
            if (entry == null)
                continue;

            ShopItemUI item = Instantiate(
                itemPrefab,
                itemContentRoot);

            ShopData selectedShop = currentShop;
            ShopItemEntry selectedEntry = entry;

            item.Setup(
                selectedEntry,
                () => OpenPurchaseConfirmation(
                    selectedShop,
                    selectedEntry));

            spawnedItems.Add(item);
        }
    }

    private void OpenPurchaseConfirmation(
        ShopData shop,
        ShopItemEntry entry)
    {
        if (confirmationPopup == null)
        {
            Debug.LogWarning(
                "[ShopUI] Purchase confirmation popup is unavailable.");

            return;
        }

        confirmationPopup.Open(shop, entry);
    }

    private void ClearShopItems()
    {
        foreach (ShopItemUI item in spawnedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }

        spawnedItems.Clear();
    }

    private void OnDestroy()
    {
        if (shops == null)
            return;

        for (int i = 0; i < shops.Length; i++)
        {
            if (shops[i] == null ||
                shops[i].selectButton == null ||
                i >= buttonCallbacks.Count)
            {
                continue;
            }

            UnityAction callback = buttonCallbacks[i];

            if (callback != null)
            {
                shops[i].selectButton.onClick.RemoveListener(callback);
            }
        }
    }
}
