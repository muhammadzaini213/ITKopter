
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BundleItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button purchaseButton;

    private Action onPurchaseClicked;

    private void Awake()
    {
        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(HandlePurchaseClicked);
    }

    private void OnDestroy()
    {
        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(HandlePurchaseClicked);
    }

    public void Setup(BundleEntry entry, Action purchaseCallback)
    {
        if (entry == null)
        {
            Debug.LogWarning("[BundleItemUI] Bundle entry is null.");
            return;
        }

        onPurchaseClicked = purchaseCallback;

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

        if (purchaseButton != null)
            purchaseButton.interactable = entry.Price > 0f;
    }

    private void HandlePurchaseClicked()
    {
        onPurchaseClicked?.Invoke();
    }
}
