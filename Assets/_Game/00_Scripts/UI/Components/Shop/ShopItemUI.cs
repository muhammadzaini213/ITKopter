
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;

    private Action onBuyClicked;

    private void Awake()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(HandleBuyClicked);
    }

    private void OnDestroy()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(HandleBuyClicked);
    }

    public void Setup(ShopItemEntry entry, Action buyCallback)
    {
        if (entry == null)
        {
            Debug.LogWarning("[ShopItemUI] Shop entry is null.");
            return;
        }

        onBuyClicked = buyCallback;

        if (iconImage != null)
        {
            Sprite icon = entry.RewardData != null
                ? entry.RewardData.Icon
                : null;

            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (nameText != null)
            nameText.text = entry.EntryName;

        if (priceText != null)
            priceText.text = $"{entry.Price} Coins";
    }

    private void HandleBuyClicked()
    {
        onBuyClicked?.Invoke();
    }
}
