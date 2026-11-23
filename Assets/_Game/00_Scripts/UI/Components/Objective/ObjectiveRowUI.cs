using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One objective: description, progress bar, first reward and the claim button.</summary>
public class ObjectiveRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private TMP_Text rewardAmountText;
    [SerializeField] private Button claimButton;
    [SerializeField] private TMP_Text claimText;
    [SerializeField] private Image claimedCheck;
    [SerializeField] private CanvasGroup group;

    [SerializeField] private Color claimableColor = new Color(0.098f, 0.941f, 1f, 1f);
    [SerializeField] private Color inactiveColor = new Color(0.55f, 0.6f, 0.65f, 1f);

    private ObjectiveDefinition definition;
    private Action<ObjectiveDefinition> onClaim;

    private void Awake()
    {
        if (claimButton != null)
            claimButton.onClick.AddListener(HandleClaim);
    }

    private void OnDestroy()
    {
        if (claimButton != null)
            claimButton.onClick.RemoveListener(HandleClaim);
    }

    public void Setup(ObjectiveDefinition def, int progress, bool claimed, Action<ObjectiveDefinition> claimCallback)
    {
        definition = def;
        onClaim = claimCallback;

        bool complete = progress >= def.target;

        if (descriptionText != null)
            descriptionText.text = def.description;

        if (progressFill != null)
            progressFill.fillAmount = Mathf.Clamp01((float)progress / def.target);

        if (progressText != null)
            progressText.text = $"{progress:N0}/{def.target:N0}";

        StoryRewardEntry reward = def.rewards != null && def.rewards.Length > 0 ? def.rewards[0] : null;

        if (rewardIcon != null)
        {
            rewardIcon.sprite = reward?.reward != null ? reward.reward.Icon : null;
            rewardIcon.preserveAspect = true;
            rewardIcon.enabled = rewardIcon.sprite != null;
        }

        if (rewardAmountText != null)
        {
            string extra = def.rewards != null && def.rewards.Length > 1 ? $" +{def.rewards.Length - 1}" : string.Empty;
            rewardAmountText.text = reward != null ? $"x{reward.amount:N0}{extra}" : string.Empty;
        }

        bool canClaim = complete && !claimed;

        if (claimButton != null)
        {
            claimButton.interactable = canClaim;
            claimButton.gameObject.SetActive(!claimed);

            if (claimButton.targetGraphic != null)
                claimButton.targetGraphic.color = canClaim ? claimableColor : inactiveColor;
        }

        if (claimText != null)
            claimText.text = canClaim ? "Claim" : "Go";

        if (claimedCheck != null)
            claimedCheck.gameObject.SetActive(claimed);

        if (group != null)
            group.alpha = claimed ? 0.55f : 1f;

        // Claimable rows glow, like the tutorial highlight.
        if (canClaim)
            UIAttentionPulse.Attach(claimButton.gameObject);
        else if (claimButton != null)
            UIAttentionPulse.Detach(claimButton.gameObject);
    }

    private void HandleClaim()
    {
        if (definition != null)
            onClaim?.Invoke(definition);
    }
}
