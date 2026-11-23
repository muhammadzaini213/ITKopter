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

    [Header("Conversion (optional)")]
    [Tooltip("Icon of the material paid, shown inside the button for conversions.")]
    [SerializeField] private Image costIcon;
    [Tooltip("Daily limit, e.g. 'Today 3/5'.")]
    [SerializeField] private TMP_Text limitText;
    [SerializeField] private Color enoughColor = Color.white;
    [SerializeField] private Color notEnoughColor = new Color(1f, 0.35f, 0.35f, 1f);

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

        SetIcon(iconImage, entry.RewardData != null ? entry.RewardData.Icon : null);

        if (nameText != null)
            nameText.text = entry.EntryName;

        if (priceText != null)
        {
            priceText.text = $"{entry.Price} Coins";
            priceText.color = enoughColor;
        }

        SetIcon(costIcon, null);
        LayoutPriceForIcon(false);

        if (limitText != null)
            limitText.gameObject.SetActive(false);

        if (buyButton != null)
            buyButton.interactable = true;
    }

    public void SetupConversion(
        ConversionOffer offer,
        int ownedInput,
        int remainingToday,
        Action convertCallback)
    {
        if (offer == null)
        {
            Debug.LogWarning("[ShopItemUI] Conversion offer is null.");
            return;
        }

        onBuyClicked = convertCallback;

        SetIcon(iconImage, offer.Output != null ? offer.Output.Icon : null);

        if (nameText != null)
            nameText.text = offer.OutputName;

        bool canAfford = ownedInput >= offer.InputAmount;

        SetIcon(costIcon, offer.InputItem != null ? offer.InputItem.Icon : null);
        LayoutPriceForIcon(costIcon != null && costIcon.enabled);

        if (priceText != null)
        {
            // With an icon the button only needs the amount; otherwise spell out the item.
            priceText.text = costIcon != null && costIcon.enabled
                ? $"x{offer.InputAmount}"
                : offer.InputName;
            priceText.color = canAfford ? enoughColor : notEnoughColor;
        }

        if (limitText != null)
        {
            bool limited = offer.DailyLimit > 0;
            limitText.gameObject.SetActive(limited);

            if (limited)
            {
                limitText.text = $"Today {remainingToday}/{offer.DailyLimit}";
                limitText.color = remainingToday > 0 ? enoughColor : notEnoughColor;
            }
        }

        if (buyButton != null)
            buyButton.interactable = canAfford && remainingToday > 0;
    }

    // With a cost icon (left of centre) the amount sits in the right half of the button.
    private void LayoutPriceForIcon(bool hasIcon)
    {
        if (priceText == null)
            return;

        RectTransform rect = priceText.rectTransform;
        rect.anchorMin = new Vector2(hasIcon ? 0.5f : 0f, rect.anchorMin.y);
        rect.offsetMin = new Vector2(hasIcon ? 2f : 0f, rect.offsetMin.y);
        priceText.alignment = hasIcon
            ? TextAlignmentOptions.MidlineLeft
            : TextAlignmentOptions.Center;
    }

    private static void SetIcon(Image image, Sprite sprite)
    {
        if (image == null)
            return;

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private void HandleBuyClicked()
    {
        onBuyClicked?.Invoke();
    }
}
