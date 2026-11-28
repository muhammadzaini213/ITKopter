
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

    private static readonly System.Globalization.CultureInfo Indonesian = new System.Globalization.CultureInfo("id-ID");

    /// <summary>Rupiah price as shown to players, e.g. "Rp 15.000".</summary>
    public static string FormatPrice(float price) => "Rp " + price.ToString("N0", Indonesian);

    private void Awake()
    {
        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(HandlePurchaseClicked);

        // Names wrap to two lines and descriptions vary a lot in length:
        // size them to their boxes so nothing spills onto the price button.
        FitText(nameText, 16f, 28f, TextAlignmentOptions.Center);
        FitText(descriptionText, 10f, 16f, TextAlignmentOptions.Center);

        // The prefab's boxes are a single name line and a short description:
        // room for two name lines, and the full gap between art and price.
        Resize(nameText, 220f, 56f);
        Resize(descriptionText, 214f, 80f);
    }

    private static void Resize(TMP_Text text, float width, float height)
    {
        if (text != null)
            text.rectTransform.sizeDelta = new Vector2(width, height);
    }

    private static void FitText(TMP_Text text, float min, float max, TextAlignmentOptions alignment)
    {
        if (text == null)
            return;

        text.enableAutoSizing = true;
        text.fontSizeMin = min;
        text.fontSizeMax = max;
        text.alignment = alignment;
        text.overflowMode = TextOverflowModes.Ellipsis;
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
            // The prefab's placeholder is a sliced check-mark: show the art whole.
            iconImage.sprite = entry.Icon;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
            iconImage.enabled = entry.Icon != null;
        }

        if (nameText != null)
            nameText.text = entry.BundleName;

        if (descriptionText != null)
            descriptionText.text = entry.Description;

        if (priceText != null)
            priceText.text = FormatPrice(entry.Price);

        if (purchaseButton != null)
            purchaseButton.interactable = entry.Price > 0f;
    }

    private void HandlePurchaseClicked()
    {
        onPurchaseClicked?.Invoke();
    }
}
