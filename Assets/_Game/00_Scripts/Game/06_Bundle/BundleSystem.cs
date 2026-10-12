
using System.Collections;
using System.Collections.Generic;
using Slafurry.Core.Abstract;
using UnityEngine;

public class BundleSystem : GameSystem<BundleSystem>
{
    public override IEnumerator Initialize()
    {
        yield return null;
    }

    public override void PostInitialize()
    {
    }

    public bool CanPurchaseBundle(
        BundleData bundleData,
        BundleEntry bundle,
        out string error)
    {
        error = null;

        if (bundleData == null)
        {
            error = "Bundle data is null.";
            return false;
        }

        if (bundle == null)
        {
            error = "Bundle entry is null.";
            return false;
        }

        if (bundle.Price <= 0f)
        {
            error = "Bundle price must be greater than zero.";
            return false;
        }

        if (bundle.Rewards == null || bundle.Rewards.Length == 0)
        {
            error = "Bundle has no rewards.";
            return false;
        }

        if (bundleData.Bundles == null ||
            !System.Array.Exists(
                bundleData.Bundles,
                item => item == bundle))
        {
            error = "Bundle does not belong to this bundle data.";
            return false;
        }

        if (RewardSystem.Instance == null)
        {
            error = "Reward system is unavailable.";
            return false;
        }

        List<RewardGrantRequest> requests =
            new List<RewardGrantRequest>();

        foreach (BundleReward bundleReward in bundle.Rewards)
        {
            if (bundleReward == null ||
                bundleReward.Reward == null)
            {
                error = "Bundle contains invalid reward data.";
                return false;
            }

            if (bundleReward.Amount <= 0)
            {
                error = "Reward amount must be greater than zero.";
                return false;
            }

            requests.Add(
                new RewardGrantRequest(
                    bundleReward.Reward,
                    bundleReward.Amount));
        }

        return RewardSystem.Instance.CanGrantRewards(
            requests, out error);
    }

    public bool PurchaseBundle(
        BundleData bundleData,
        BundleEntry bundle,
        out string error)
    {
        if (!CanPurchaseBundle(bundleData, bundle, out error))
        {
            return false;
        }

        // Dummy purchase: simulate successful payment.
        Debug.Log(
            $"[DUMMY TOP-UP] Simulating payment for " +
            $"{bundle.BundleName} ({bundle.Price}).");

        List<RewardGrantRequest> requests =
            new List<RewardGrantRequest>();

        foreach (BundleReward bundleReward in bundle.Rewards)
        {
            requests.Add(
                new RewardGrantRequest(
                    bundleReward.Reward,
                    bundleReward.Amount));
        }

        List<RewardGrantResult> results =
            RewardSystem.Instance.GrantRewards(requests);

        if (results == null ||
            results.Count != requests.Count)
        {
            error = "Reward granting failed.";
            Debug.LogError(error);
            return false;
        }

        foreach (RewardGrantResult result in results)
        {
            if (result == null || !result.Success)
            {
                error =
                    "One or more rewards could not be granted. " +
                    "Check RewardSystem for partial grants.";

                Debug.LogError(error);
                return false;
            }
        }

        error = null;

        Debug.Log(
            $"[DUMMY TOP-UP] Bundle '{bundle.BundleName}' " +
            "purchased successfully. Rewards granted.");

        return true;
    }

    public bool PurchaseBundle(
        BundleData bundleData,
        BundleEntry bundle)
    {
        bool success = PurchaseBundle(
            bundleData, bundle, out string error);

        if (!success)
        {
            Debug.LogWarning($"Top-up failed: {error}");
        }

        return success;
    }
}