
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuyConfirmationPopupUI : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;

    [Header("Item Info")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text unitPriceText;
    [SerializeField] private TMP_Text totalPriceText;

    [Header("Quantity")]
    [SerializeField] private TMP_InputField quantityInput;
    [SerializeField] private Button decreaseButton;
    [SerializeField] private Button increaseButton;

    [Header("Actions")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text errorText;

    private ShopData currentShop;
    private ShopItemEntry currentEntry;
    private int quantity = 1;

    private void Awake()
    {
        if (decreaseButton != null)
            decreaseButton.onClick.AddListener(DecreaseQuantity);

        if (increaseButton != null)
            increaseButton.onClick.AddListener(IncreaseQuantity);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmPurchase);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);

        if (quantityInput != null)
            quantityInput.onValueChanged.AddListener(OnQuantityChanged);

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (decreaseButton != null)
            decreaseButton.onClick.RemoveListener(DecreaseQuantity);

        if (increaseButton != null)
            increaseButton.onClick.RemoveListener(IncreaseQuantity);

        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(ConfirmPurchase);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(Close);

        if (quantityInput != null)
            quantityInput.onValueChanged.RemoveListener(OnQuantityChanged);
    }

    public void Open(ShopData shop, ShopItemEntry entry)
    {
        if (shop == null || entry == null)
        {
            Debug.LogWarning(
                "[BuyConfirmationPopupUI] Shop or entry is null.");
            return;
        }

        currentShop = shop;
        currentEntry = entry;
        quantity = 1;

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

        if (unitPriceText != null)
            unitPriceText.text = $"{entry.Price} Coins each";

        if (errorText != null)
            errorText.text = "";

        if (quantityInput != null)
            quantityInput.text = "1";

        UpdateQuantityUI();

        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    private void IncreaseQuantity()
    {
        SetQuantity(quantity + 1);
    }

    private void DecreaseQuantity()
    {
        SetQuantity(quantity - 1);
    }

    private void OnQuantityChanged(string value)
    {
        if (int.TryParse(value, out int parsed))
        {
            quantity = Mathf.Clamp(
                parsed,
                1,
                ShopSystem.MaxPurchaseQuantity);

            UpdateTotalPrice();
        }
        else
        {
            if (totalPriceText != null)
                totalPriceText.text = "Total: —";
        }
    }

    private void SetQuantity(int value)
    {
        quantity = Mathf.Clamp(
            value,
            1,
            ShopSystem.MaxPurchaseQuantity);

        if (quantityInput != null &&
            quantityInput.text != quantity.ToString())
        {
            quantityInput.text = quantity.ToString();
        }

        UpdateQuantityUI();
    }

    private void UpdateQuantityUI()
    {
        if (quantityInput != null)
            quantityInput.text = quantity.ToString();

        if (decreaseButton != null)
            decreaseButton.interactable = quantity > 1;

        if (increaseButton != null)
            increaseButton.interactable =
                quantity < ShopSystem.MaxPurchaseQuantity;

        UpdateTotalPrice();
    }

    private void UpdateTotalPrice()
    {
        if (totalPriceText == null || currentEntry == null)
            return;

        long total = (long)currentEntry.Price * quantity;
        totalPriceText.text = $"Total: {total} Coins";
    }

    private void ConfirmPurchase()
    {
        if (currentShop == null || currentEntry == null)
            return;

        if (quantityInput != null)
        {
            if (!int.TryParse(
                    quantityInput.text,
                    out int parsedQuantity))
            {
                ShowError("Enter a valid quantity.");
                return;
            }

            quantity = Mathf.Clamp(
                parsedQuantity,
                1,
                ShopSystem.MaxPurchaseQuantity);
        }

        if (ShopSystem.Instance == null)
        {
            ShowError("Shop system is unavailable.");
            return;
        }

        bool success = ShopSystem.Instance.BuyItem(
            currentShop,
            currentEntry,
            quantity,
            out string error);

        if (!success)
        {
            ShowError(error);
            return;
        }

        Close();
    }

    private void ShowError(string message)
    {
        if (errorText != null)
            errorText.text = message;

        Debug.LogWarning($"[BuyConfirmationPopupUI] {message}");
    }

    public void Close()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);

        currentShop = null;
        currentEntry = null;
    }
}
