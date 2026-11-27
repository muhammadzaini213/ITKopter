
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

        SetBusy(true);

        BundleEntry bundle = currentBundle;

        BundleSystem.Instance.BeginPurchase(currentBundleData, bundle, (state, message) =>
        {
            switch (state)
            {
                case BundleSystem.PurchaseState.CreatingOrder:
                case BundleSystem.PurchaseState.WaitingForPayment:
                    ShowStatus(message);
                    break;

                case BundleSystem.PurchaseState.Granted:
                    SetBusy(false);
                    BundleSystem.Instance.ShowGrantedPopup(bundle);
                    Close();
                    break;

                case BundleSystem.PurchaseState.PendingPayment:
                    SetBusy(false);
                    ShowStatus(message);
                    break;

                case BundleSystem.PurchaseState.Cancelled:
                    SetBusy(false);
                    ShowStatus(message);
                    break;

                default:
                    SetBusy(false);
                    ShowError(string.IsNullOrEmpty(message) ? "Purchase failed." : message);
                    break;
            }
        });
    }

    private void SetBusy(bool busy)
    {
        if (confirmButton != null)
            confirmButton.interactable = !busy;

        // Closing mid-payment is fine (the order is resumed in the background),
        // but keep it explicit while the order is being created.
        if (cancelButton != null)
            cancelButton.interactable = true;
    }

    private void ShowStatus(string message)
    {
        if (errorText != null)
        {
            errorText.color = new Color(0.55f, 0.95f, 1f);
            errorText.text = message ?? string.Empty;
        }
    }

    private void ShowError(string message)
    {
        Debug.LogWarning(
            $"[BundlePurchasePopupUI] {message}");

        if (errorText != null)
        {
            errorText.color = new Color(1f, 0.45f, 0.45f);
            errorText.text = message;
        }
    }

    public void Close()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);

        currentBundleData = null;
        currentBundle = null;
    }
}
