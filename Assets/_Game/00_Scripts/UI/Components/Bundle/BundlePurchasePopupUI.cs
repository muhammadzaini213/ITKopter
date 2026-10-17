
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BundlePurchasePopupUI : MonoBehaviour
{
    [Header("Popup Root")]
    [SerializeField] private GameObject popupRoot;

    [Header("Bundle Information")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Feedback")]
    [SerializeField] private TMP_Text errorText;

    private BundleData currentBundleData;
    private BundleEntry currentBundle;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmPurchase);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);

        Close();

        if (errorText != null)
            errorText.text = string.Empty;
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(ConfirmPurchase);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(Close);
    }

    public void Open(BundleData data, BundleEntry entry)
    {
        if (data == null || entry == null)
        {
            Debug.LogWarning(
                "[BundlePurchasePopupUI] Invalid bundle selection.");
            return;
        }

        currentBundleData = data;
        currentBundle = entry;

        if (iconImage != null)
        {
            iconImage.sprite = entry.Icon;
            iconImage.enabled = entry.Icon != null;
        }

        if (nameText != null)
            nameText.text = entry.BundleName;

        if (descriptionText != null)
            descriptionText.text = entry.Description;

        if (priceText != null)
            priceText.text = entry.Price.ToString("N0");

        if (errorText != null)
            errorText.text = string.Empty;

        if (confirmButton != null)
            confirmButton.interactable = true;

        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    private void ConfirmPurchase()
    {
        if (currentBundleData == null || currentBundle == null)
        {
            ShowError("No bundle selected.");
            return;
        }

        if (BundleSystem.Instance == null)
        {
            ShowError("Bundle system is unavailable.");
            return;
        }

        if (confirmButton != null)
            confirmButton.interactable = false;

        bool success = BundleSystem.Instance.PurchaseBundle(
            currentBundleData,
            currentBundle,
            out string error);

        if (!success)
        {
            ShowError(string.IsNullOrEmpty(error)
                ? "Purchase failed."
                : error);

            if (confirmButton != null)
                confirmButton.interactable = true;

            return;
        }

        ShowGrantedRewards(currentBundle);
        Close();
    }

    private void ShowGrantedRewards(BundleEntry entry)
    {
        if (RewardSystem.Instance == null)
        {
            Debug.LogWarning(
                "[BundlePurchasePopupUI] RewardSystem is unavailable.");
            return;
        }

        List<RewardData> popupRewards = new List<RewardData>();

        if (entry.Rewards != null)
        {
            foreach (BundleReward bundleReward in entry.Rewards)
            {
                if (bundleReward == null ||
                    bundleReward.Reward == null)
                {
                    continue;
                }

                for (int i = 0; i < bundleReward.Amount; i++)
                {
                    popupRewards.Add(bundleReward.Reward);
                }
            }
        }

        if (popupRewards.Count > 0)
        {
            RewardSystem.Instance.ShowRewardPopup(popupRewards);
        }
    }

    private void ShowError(string message)
    {
        Debug.LogWarning(
            $"[BundlePurchasePopupUI] {message}");

        if (errorText != null)
            errorText.text = message;
    }

    public void Close()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);

        currentBundleData = null;
        currentBundle = null;
    }
}
