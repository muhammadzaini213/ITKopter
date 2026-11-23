using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Asks the player to confirm a gacha pull (banner, pull count, gem cost) before it happens.
/// </summary>
public class GachaConfirmationPopupUI : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;

    [Header("Info")]
    [SerializeField] private Image featuredIcon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text balanceText;

    [Header("Actions")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    [Header("Colors")]
    [SerializeField] private Color enoughColor = Color.white;
    [SerializeField] private Color notEnoughColor = new Color(1f, 0.35f, 0.35f, 1f);

    private Action onConfirm;

    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(HandleConfirm);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Close);

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(HandleConfirm);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(Close);
    }

    public void Open(
        GachaData gacha,
        int pullCount,
        int cost,
        int currentGems,
        Action confirmCallback)
    {
        if (gacha == null)
            return;

        onConfirm = confirmCallback;

        bool canAfford = currentGems >= cost;

        if (featuredIcon != null)
        {
            Sprite icon = gacha.FeaturedReward != null ? gacha.FeaturedReward.Icon : null;
            featuredIcon.sprite = icon;
            featuredIcon.preserveAspect = true;
            featuredIcon.enabled = icon != null;
        }

        if (titleText != null)
            titleText.text = gacha.GachaName;

        if (messageText != null)
            messageText.text = $"Pull {pullCount}x on this banner?";

        if (costText != null)
        {
            costText.text = $"Cost: {cost} Gems";
            costText.color = canAfford ? enoughColor : notEnoughColor;
        }

        if (balanceText != null)
        {
            balanceText.text = canAfford
                ? $"You have {currentGems} Gems ({currentGems - cost} after)"
                : $"You have {currentGems} Gems (need {cost - currentGems} more)";
            balanceText.color = canAfford ? enoughColor : notEnoughColor;
        }

        if (confirmButton != null)
            confirmButton.interactable = canAfford;

        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    public void Close()
    {
        onConfirm = null;

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void HandleConfirm()
    {
        // Close first so the reward popup shown by the pull isn't covered.
        Action callback = onConfirm;
        Close();
        callback?.Invoke();
    }
}
