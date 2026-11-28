
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

    private readonly List<Button> runtimeTabs = new();

    private void RegisterBundleButtons()
    {
        if (bundleSlots == null)
            return;

        BuildMissingTabs();

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
        HighlightTab(index);

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

        CenterContent();
    }

    // ---- category tabs (built at runtime when a slot has no button) --------

    private static readonly Color TabSelected = new Color(0.75f, 1f, 1f, 1f);
    private static readonly Color TabIdle = new Color(0.45f, 0.6f, 0.65f, 1f);

    private void BuildMissingTabs()
    {
        int missing = 0;
        foreach (BundleSlot slot in bundleSlots)
            if (slot != null && slot.bundleData != null && slot.selectButton == null)
                missing++;

        if (missing == 0)
            return;

        var font = RuntimeUIKit.FindFont();
        Sprite frame = RuntimeUIKit.FindFrameSprite();
        const float width = 200f, gap = 16f;
        float start = -(missing - 1) * (width + gap) * 0.5f;
        int n = 0;

        for (int i = 0; i < bundleSlots.Length; i++)
        {
            BundleSlot slot = bundleSlots[i];
            if (slot == null || slot.bundleData == null || slot.selectButton != null)
                continue;

            string label = string.IsNullOrWhiteSpace(slot.bundleData.BundleCategory)
                ? slot.bundleData.name
                : slot.bundleData.BundleCategory;

            // Above the panel, centred, like a row of tabs on its top edge.
            Button tab = RuntimeUIKit.NewButton($"Tab_{label}", transform, font, frame, label.ToUpperInvariant(), 22f,
                Vector2.zero, new Vector2(width, 56f), 2f);
            var rt = (RectTransform)tab.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(start + n * (width + gap), 4f);

            slot.selectButton = tab;
            runtimeTabs.Add(tab);
            n++;
        }
    }

    private void HighlightTab(int index)
    {
        for (int i = 0; i < bundleSlots.Length; i++)
        {
            Button button = bundleSlots[i]?.selectButton;
            if (button == null || !runtimeTabs.Contains(button))
                continue;

            button.targetGraphic.color = i == index ? TabSelected : TabIdle;
            var label = button.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (label != null)
                label.color = i == index ? RuntimeUIKit.Cyan : new Color(0.7f, 0.8f, 0.85f);
        }
    }

    // A few cards sit in the middle of the panel; more than fit scroll from the left.
    private void CenterContent()
    {
        if (!(contentRoot is RectTransform content))
            return;

        content.anchorMin = new Vector2(0.5f, content.anchorMin.y);
        content.anchorMax = new Vector2(0.5f, content.anchorMax.y);
        content.pivot = new Vector2(0.5f, content.pivot.y);
        content.anchoredPosition = new Vector2(0f, content.anchoredPosition.y);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        var scroll = content.GetComponentInParent<ScrollRect>();
        if (scroll != null)
            scroll.horizontalNormalizedPosition = 0f;
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
