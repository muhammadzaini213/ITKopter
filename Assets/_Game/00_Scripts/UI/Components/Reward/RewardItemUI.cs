
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
            iconImage.enabled = reward.Icon != null;
        }

        if (nameText != null)
        {
            if (reward is ItemData item)
            {
                nameText.text = item.ItemName;
            }
            else if (reward is CharacterData character)
            {
                nameText.text = character.CharacterName;
            }
        }

        if (amountText != null)
            amountText.text = $"× {amount}";
    }
}
