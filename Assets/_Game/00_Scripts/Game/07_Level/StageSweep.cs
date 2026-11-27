using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sweep: instantly re-clear a stage the player has already cleared. Costs
/// the stage's energy per sweep and grants the same as a normal clear: the
/// level's rewards, its clear EXP and the "clear stages" objective progress.
/// </summary>
public static class StageSweep
{
    /// <summary>Most sweeps in one go.</summary>
    public const int MaxPerSweep = 10;

    public static bool IsSweepable(LevelData level)
    {
        PlayerSystem player = PlayerSystem.Instance;

        return level != null && player != null && player.IsPlayerDataLoaded &&
               !string.IsNullOrEmpty(level.LevelId) && player.IsLevelCompleted(level.LevelId);
    }

    /// <summary>How many sweeps the player can afford right now (0..MaxPerSweep).</summary>
    public static int MaxAffordable(LevelData level)
    {
        if (!IsSweepable(level))
            return 0;

        int cost = Mathf.Max(0, level.EnergyCost);
        EnergySystem energy = EnergySystem.Instance;

        if (cost == 0)
            return MaxPerSweep;

        if (energy == null)
            return 0;

        return Mathf.Clamp(energy.Energy / cost, 0, MaxPerSweep);
    }

    /// <summary>Runs <paramref name="count"/> sweeps. Returns false (and changes nothing) when it can't.</summary>
    public static bool Sweep(LevelData level, int count, out string error)
    {
        error = null;

        if (!IsSweepable(level))
        {
            error = "Clear this stage first.";
            return false;
        }

        if (count <= 0 || count > MaxPerSweep)
        {
            error = "Invalid sweep count.";
            return false;
        }

        int totalCost = Mathf.Max(0, level.EnergyCost) * count;
        EnergySystem energy = EnergySystem.Instance;

        if (totalCost > 0 && (energy == null || energy.Energy < totalCost))
        {
            error = "Not enough energy.";
            return false;
        }

        if (totalCost > 0 && !energy.SpendEnergy(totalCost))
        {
            error = "Failed to spend energy.";
            return false;
        }

        PlayerSystem.Instance.GrantSweepExp(level, count);
        Objectives.Report(ObjectiveType.ClearStages, count);
        GrantRewards(level, count);
        return true;
    }

    private static void GrantRewards(LevelData level, int count)
    {
        RewardSystem rewardSystem = RewardSystem.Instance;

        if (rewardSystem == null)
        {
            Debug.LogError("[StageSweep] RewardSystem is unavailable; sweep rewards not granted.");
            return;
        }

        var requests = new List<RewardGrantRequest>();

        if (level.RewardEntries != null)
        {
            foreach (StoryRewardEntry entry in level.RewardEntries)
            {
                if (entry != null && entry.reward != null)
                    requests.Add(new RewardGrantRequest(entry.reward, Mathf.Max(1, entry.amount) * count));
            }
        }

        if (level.LevelRewards != null)
        {
            foreach (RewardData reward in level.LevelRewards)
            {
                if (reward != null)
                    requests.Add(new RewardGrantRequest(reward, count));
            }
        }

        var granted = new List<RewardData>();
        var amounts = new List<int>();

        if (requests.Count > 0)
        {
            foreach (RewardGrantResult result in rewardSystem.GrantRewards(requests))
            {
                if (result == null || !result.Success)
                {
                    Debug.LogWarning($"[StageSweep] Reward failed: {result?.Message}");
                    continue;
                }

                granted.Add(result.Reward);
                amounts.Add(result.GrantedAmount);
            }
        }

        // The popup also animates the EXP bar, even with no item rewards.
        rewardSystem.ShowRewardPopup(granted, amounts);
    }
}
