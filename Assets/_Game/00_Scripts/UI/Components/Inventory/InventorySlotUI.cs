
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemAmountText;

    public void Setup(ItemData item, int amount)
    {
        if (item == null)
        {
            Clear();
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = item.Icon;
            itemIcon.enabled = item.Icon != null;
        }

        if (itemNameText != null)
            itemNameText.text = item.ItemName;

        if (itemAmountText != null)
            itemAmountText.text = amount.ToString();
    }

    public void Clear()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (itemNameText != null)
            itemNameText.text = string.Empty;

        if (itemAmountText != null)
            itemAmountText.text = string.Empty;
    }
}
