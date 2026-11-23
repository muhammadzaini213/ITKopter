
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text amountText;

    public void Setup(RewardData reward, int amount)
    {
        if (reward == null)
        {
            Debug.LogWarning("[RewardItemUI] Reward data is null.");
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = reward.Icon;
            iconImage.preserveAspect = true;
            iconImage.enabled = reward.Icon != null;
        }

        if (nameText != null)
            nameText.text = GetDisplayName(reward);

        if (amountText != null)
            amountText.text = $"x{amount:N0}";
    }

    private static string GetDisplayName(RewardData reward)
    {
        switch (reward)
        {
            case ItemData item:
                return item.ItemName;
            case CharacterData character:
                return character.CharacterName;
            case CurrencyRewardData currency:
                return currency.CurrencyType.ToString();
            default:
                return reward.name;
        }
    }
}
