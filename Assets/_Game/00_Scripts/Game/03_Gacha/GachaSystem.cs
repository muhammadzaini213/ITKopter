using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Slafurry.Core.Abstract;
using Slafurry.Systems.Save;

public class GachaSystem : GameSystem<GachaSystem>
{
    private const string SaveFileName = "gacha";

    private GachaSaveData saveData = new GachaSaveData();

    public override IEnumerator Initialize()
    {
        LoadGachaData();
        yield return null;
    }

    public override void PostInitialize()
    {
    }

    public int GetPityCount(string gachaId)
    {
        if (string.IsNullOrWhiteSpace(gachaId))
            return 0;

        BannerPityData banner = FindBannerPity(gachaId);
        return banner != null ? banner.pityCount : 0;
    }

    public bool PullSingle(
        GachaData gacha,
        out GachaPullResult[] results)
    {
        return Pull(gacha, 1, out results);
    }

    public bool PullMulti(
        GachaData gacha,
        out GachaPullResult[] results)
    {
        if (gacha == null)
        {
            results = new GachaPullResult[0];
            return false;
        }

        return Pull(gacha, gacha.MultiPullCount, out results);
    }

    public bool Pull(
        GachaData gacha,
        int pullCount,
        out GachaPullResult[] results)
    {
        results = new GachaPullResult[0];

        if (!ValidateBanner(gacha, pullCount, out string error))
        {
            Debug.LogWarning($"Gacha failed: {error}");
            return false;
        }

        int cost = CalculateCost(gacha, pullCount);

        if (CurrencySystem.Instance.Gems < cost)
        {
            Debug.LogWarning("Not enough Gems.");
            return false;
        }

        // Roll memakai pity sementara.
        // Data pity yang tersimpan belum diubah.
        int currentPity = GetPityCount(gacha.GachaId);

        List<GachaPullResult> rolledResults =
            new List<GachaPullResult>(pullCount);

        List<RewardGrantRequest> grantRequests =
            new List<RewardGrantRequest>(pullCount);

        for (int i = 0; i < pullCount; i++)
        {
            GachaPullResult rolled = RollReward(
                gacha,
                ref currentPity
            );

            if (rolled == null || rolled.Reward == null)
            {
                Debug.LogError("Gacha produced an invalid reward.");
                return false;
            }

            rolledResults.Add(rolled);
            grantRequests.Add(
                new RewardGrantRequest(rolled.Reward, 1)
            );
        }

        if (!RewardSystem.Instance.CanGrantRewards(
                grantRequests,
                out error))
        {
            Debug.LogWarning($"Gacha batch rejected: {error}");
            return false;
        }

        if (!CurrencySystem.Instance.SpendGems(cost))
            return false;

        RewardGrantResult[] grantResults =
            RewardSystem.Instance.GrantRewards(grantRequests).ToArray();

        if (!AllRewardsGranted(grantResults))
        {
            Debug.LogError(
                "Gacha reward commit failed. " +
                "Currency refund/rollback is not implemented."
            );

            return false;
        }

        SetPityCount(gacha.GachaId, currentPity);
        SaveGachaData();

        results = rolledResults.ToArray();
        return true;
    }

    private bool ValidateBanner(
        GachaData gacha,
        int pullCount,
        out string error)
    {
        error = null;

        if (gacha == null)
        {
            error = "GachaData is null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(gacha.GachaId))
        {
            error = "Gacha ID is empty.";
            return false;
        }

        if (pullCount <= 0)
        {
            error = "Pull count must be greater than zero.";
            return false;
        }

        if (gacha.PityThreshold <= 0)
        {
            error = "Pity threshold must be greater than zero.";
            return false;
        }

        if (gacha.RewardPool == null ||
            gacha.RewardPool.Length == 0)
        {
            error = "Reward pool is empty.";
            return false;
        }

        if (gacha.RRate < 0f ||
            gacha.SRRate < 0f ||
            gacha.SSRRate < 0f)
        {
            error = "Rarity rates cannot be negative.";
            return false;
        }

        float totalRate =
            gacha.RRate + gacha.SRRate + gacha.SSRRate;

        if (totalRate <= 0f ||
            !Mathf.Approximately(totalRate, 100f))
        {
            error = "Rarity rates must add up to 100%.";
            return false;
        }

        foreach (RewardData reward in gacha.RewardPool)
        {
            if (reward == null)
            {
                error = "Reward pool contains a null entry.";
                return false;
            }
        }

        return true;
    }

    private int CalculateCost(GachaData gacha, int pullCount)
    {
        // Discount 10 means 10%.
        float discountMultiplier =
            1f - Mathf.Clamp(gacha.GachaDiscount, 0f, 100f) / 100f;

        long rawCost =
            (long)gacha.GachaCost * pullCount;

        long discountedCost =
            Mathf.RoundToInt(rawCost * discountMultiplier);

        if (discountedCost > int.MaxValue)
            return int.MaxValue;

        return Mathf.Max(0, (int)discountedCost);
    }

    private GachaPullResult RollReward(
        GachaData gacha,
        ref int pityCount)
    {
        pityCount++;

        // Hard pity: jamin rarity pity saat threshold tercapai.
        if (pityCount >= gacha.PityThreshold)
        {
            RewardData pityReward = PickRewardByRarity(
                gacha,
                gacha.PityRarity
            );

            if (pityReward == null)
            {
                Debug.LogError(
                    $"No reward found for pity rarity {gacha.PityRarity}."
                );

                return null;
            }

            pityCount = 0;
            return new GachaPullResult(pityReward, true);
        }

        RewardRarity rarity = RollRarity(gacha);

        RewardData reward = PickRewardByRarity(gacha, rarity);

        // Kalau rarity hasil roll tidak punya reward di pool,
        // fallback ke rarity tertinggi yang tersedia di pool.
        if (reward == null)
            reward = PickFallbackReward(gacha);

        if (reward == null)
            return null;

        return new GachaPullResult(reward, false);
    }

    private RewardRarity RollRarity(GachaData gacha)
    {
        float roll = Random.Range(
            0f,
            gacha.RRate + gacha.SRRate + gacha.SSRRate
        );

        if (roll < gacha.SSRRate)
            return RewardRarity.SSR;

        roll -= gacha.SSRRate;

        if (roll < gacha.SRRate)
            return RewardRarity.SR;

        return RewardRarity.R;
    }

    private RewardData PickRewardByRarity(
        GachaData gacha,
        RewardRarity rarity)
    {
        List<RewardData> candidates = new List<RewardData>();

        foreach (RewardData reward in gacha.RewardPool)
        {
            if (reward != null && reward.Rarity == rarity)
                candidates.Add(reward);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private RewardData PickFallbackReward(GachaData gacha)
    {
        RewardData best = null;

        foreach (RewardData reward in gacha.RewardPool)
        {
            if (reward == null)
                continue;

            if (best == null || reward.Rarity > best.Rarity)
                best = reward;
        }

        return best;
    }

    private bool AllRewardsGranted(RewardGrantResult[] results)
    {
        if (results == null || results.Length == 0)
            return false;

        foreach (RewardGrantResult result in results)
        {
            if (result == null || !result.Success)
                return false;
        }

        return true;
    }

    private BannerPityData FindBannerPity(string gachaId)
    {
        if (saveData.banners == null)
            saveData.banners = new List<BannerPityData>();

        return saveData.banners.Find(
            banner => banner != null &&
                      banner.gachaId == gachaId
        );
    }

    private void SetPityCount(string gachaId, int pityCount)
    {
        if (saveData.banners == null)
            saveData.banners = new List<BannerPityData>();

        BannerPityData banner = FindBannerPity(gachaId);

        if (banner == null)
        {
            banner = new BannerPityData
            {
                gachaId = gachaId,
                pityCount = pityCount
            };

            saveData.banners.Add(banner);
        }
        else
        {
            banner.pityCount = pityCount;
        }
    }

    private void LoadGachaData()
    {
        saveData = Save.From(
            SaveFileName,
            new GachaSaveData()
        );

        if (saveData == null)
            saveData = new GachaSaveData();

        if (saveData.banners == null)
            saveData.banners = new List<BannerPityData>();
    }

    private void SaveGachaData()
    {
        Save.To(saveData, SaveFileName);
    }

}