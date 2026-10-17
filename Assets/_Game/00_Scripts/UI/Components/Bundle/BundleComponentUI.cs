
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BundleComponentUI : MonoBehaviour
{
    [Serializable]
    public class BundleSlot
    {
        public BundleData bundleData;
        public Button selectButton;
    }

    [Header("Bundle Categories")]
    [SerializeField] private BundleSlot[] bundleSlots;

    [Header("Bundle List UI")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private BundleItemUI itemPrefab;

    [Header("Purchase Confirmation")]
    [SerializeField] private BundlePurchasePopupUI purchasePopup;

    private readonly List<BundleItemUI> spawnedItems = new();
    private readonly List<UnityAction> buttonCallbacks = new();

    private BundleData currentBundleData;
    private int currentBundleIndex = -1;

    private void Start()
    {
        if (BundleSystem.Instance == null)
        {
            Debug.LogError(
                "[BundleUI] BundleSystem is unavailable.");
            return;
        }

        if (contentRoot == null ||
            itemPrefab == null ||
            purchasePopup == null)
        {
            Debug.LogError(
                "[BundleUI] Required UI references are missing.");
            return;
        }

        RegisterBundleButtons();
        SelectInitialBundleCategory();
    }

    private void RegisterBundleButtons()
    {
        if (bundleSlots == null)
            return;

        for (int i = 0; i < bundleSlots.Length; i++)
        {
            if (bundleSlots[i] == null ||
                bundleSlots[i].selectButton == null)
            {
                buttonCallbacks.Add(null);
                continue;
            }

            int index = i;
            UnityAction callback = () => SelectBundleCategory(index);

            buttonCallbacks.Add(callback);
            bundleSlots[i].selectButton.onClick.AddListener(callback);
        }
    }

    private void SelectInitialBundleCategory()
    {
        if (bundleSlots == null)
            return;

        for (int i = 0; i < bundleSlots.Length; i++)
        {
            if (bundleSlots[i] != null &&
                bundleSlots[i].bundleData != null)
            {
                SelectBundleCategory(i);
                return;
            }
        }

        Debug.LogWarning(
            "[BundleUI] No valid BundleData is assigned.");
    }

    public void SelectBundleCategory(int index)
    {
        if (bundleSlots == null ||
            index < 0 ||
            index >= bundleSlots.Length)
        {
            Debug.LogWarning(
                $"[BundleUI] Invalid category index: {index}");
            return;
        }

        if (bundleSlots[index] == null ||
            bundleSlots[index].bundleData == null)
        {
            Debug.LogWarning(
                $"[BundleUI] BundleData is missing at index {index}.");
            return;
        }

        currentBundleIndex = index;
        currentBundleData = bundleSlots[index].bundleData;

        Debug.Log(
            $"[BundleUI] Selected category: " +
            $"{currentBundleData.BundleCategory}");

        RefreshBundleItems();
    }

    private void RefreshBundleItems()
    {
        ClearBundleItems();

        if (currentBundleData == null ||
            currentBundleData.Bundles == null)
        {
            return;
        }

        foreach (BundleEntry entry in currentBundleData.Bundles)
        {
            if (entry == null)
                continue;

            BundleItemUI item = Instantiate(
                itemPrefab,
                contentRoot);

            BundleData selectedData = currentBundleData;
            BundleEntry selectedEntry = entry;

            item.Setup(
                selectedEntry,
                () => OpenPurchasePopup(
                    selectedData,
                    selectedEntry));

            spawnedItems.Add(item);
        }
    }

    private void OpenPurchasePopup(
        BundleData data,
        BundleEntry entry)
    {
        if (purchasePopup == null)
        {
            Debug.LogWarning(
                "[BundleUI] Purchase popup is unavailable.");
            return;
        }

        purchasePopup.Open(data, entry);
    }

    private void ClearBundleItems()
    {
        foreach (BundleItemUI item in spawnedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }

        spawnedItems.Clear();
    }

    private void OnDestroy()
    {
        if (bundleSlots == null)
            return;

        for (int i = 0; i < bundleSlots.Length; i++)
        {
            if (bundleSlots[i] == null ||
                bundleSlots[i].selectButton == null ||
                i >= buttonCallbacks.Count)
            {
                continue;
            }

            UnityAction callback = buttonCallbacks[i];

            if (callback != null)
            {
                bundleSlots[i].selectButton.onClick
                    .RemoveListener(callback);
            }
        }
    }
}
