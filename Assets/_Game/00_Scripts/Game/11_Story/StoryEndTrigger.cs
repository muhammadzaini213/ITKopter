using System.Collections.Generic;
using UnityEngine;

public class StoryEndTrigger : MonoBehaviour
{
    public static void Complete(StoryData story)
    {
        if (story == null)
        {
            Debug.LogWarning("[StoryEndTrigger] Story is null.");
            return;
        }

        GrantStoryRewards(story);
        MarkStoryCompleted(story);
    }

    public void Trigger()
    {
        Complete(StorySession.Selected);
    }

    private static void GrantStoryRewards(StoryData story)
    {
        RewardSystem rewardSystem = RewardSystem.Instance;

        if (rewardSystem == null)
        {
            Debug.LogError(
                "[StoryEndTrigger] RewardSystem is unavailable. " +
                "Story rewards could not be granted.");
            return;
        }

        RewardData[] rewards = story.StoryRewards;

        if (rewards == null || rewards.Length == 0)
            return;

        List<RewardGrantRequest> requests = new();

        foreach (RewardData reward in rewards)
        {
            if (reward == null)
            {
                Debug.LogWarning(
                    "[StoryEndTrigger] A story reward is null.");
                continue;
            }

            requests.Add(new RewardGrantRequest(reward, 1));
        }

        if (requests.Count == 0)
            return;

        List<RewardGrantResult> results =
            rewardSystem.GrantRewards(requests);

        List<RewardData> grantedRewards = new();
        bool allGranted = true;

        foreach (RewardGrantResult result in results)
        {
            if (result == null || !result.Success)
            {
                allGranted = false;

                Debug.LogWarning(
                    $"[StoryEndTrigger] Failed to grant reward: " +
                    $"{result?.Message}");
                continue;
            }

            Debug.Log(
                $"[StoryEndTrigger] Granted reward: " +
                $"{result.Reward.name} x{result.GrantedAmount}.");

            grantedRewards.Add(result.Reward);
        }

        if (grantedRewards.Count > 0)
            rewardSystem.ShowRewardPopup(grantedRewards);

        if (!allGranted)
        {
            Debug.LogWarning(
                "[StoryEndTrigger] Some story rewards could not be granted.");
        }
    }

    private static void MarkStoryCompleted(StoryData story)
    {
        PlayerSystem player = PlayerSystem.Instance;

        if (player == null || !player.IsPlayerDataLoaded)
        {
            Debug.LogWarning(
                "[StoryEndTrigger] Player data is not ready; " +
                "story completion not saved.");
            return;
        }

        if (string.IsNullOrWhiteSpace(story.StoryId))
        {
            Debug.LogWarning(
                "[StoryEndTrigger] Story has no id; completion not saved.");
            return;
        }

        player.CompleteLevel(story.StoryId);
    }
}
