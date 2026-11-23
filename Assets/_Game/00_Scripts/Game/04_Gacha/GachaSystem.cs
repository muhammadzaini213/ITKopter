
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Slafurry.Core.Abstract;
using Slafurry.Systems.Save;

public class GachaSystem : GameSystem<GachaSystem>
{
    private const string SaveFileName = "gacha";

    private const int IntervalMin = 1;
    private const int IntervalMax = 99999;

    private const float FeaturedChance = 50f;

    private GachaSaveData saveData = new GachaSaveData();

    #region Initialization

    public override IEnumerator Initialize()
    {
        LoadGachaData();
        yield return null;
    }

    public override void PostInitialize()
    {
    }

    #endregion

    #region Pity

    public int GetPityCount(string gachaId)
    {
        if (string.IsNullOrWhiteSpace(gachaId))
            return 0;

        BannerPityData banner = FindBannerPity(gachaId);

        return banner != null
            ? Mathf.Max(0, banner.pityCount)
            : 0;
    }

    private BannerPityData FindBannerPity(string gachaId)
    {
        if (saveData.banners == null)
            saveData.banners = new List<BannerPityData>();

        return saveData.banners.Find(
            banner =>
                banner != null &&
                banner.gachaId == gachaId
        );
    }

    private void SetPityCount(
        string gachaId,
        int pityCount)
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

    #endregion

    #region Pull API

    /// <summary>Raised after a successful pull (gems spent, rewards granted).</summary>
    public event System.Action<GachaData, GachaPullResult[]> OnPullCompleted;

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

        return Pull(
            gacha,
            gacha.MultiPullCount,
            out results
        );
    }

    public bool Pull(
        GachaData gacha,
        int pullCount,
        out GachaPullResult[] results)
    {
        return Pull(gacha, pullCount, true, out results);
    }

    /// <param name="showPopup">False when the caller shows the results itself (e.g. the reveal overlay).</param>
    public bool Pull(
        GachaData gacha,
        int pullCount,
        bool showPopup,
        out GachaPullResult[] results)
    {
        results = new GachaPullResult[0];

        if (!ValidateBanner(
            gacha,
            pullCount,
            out string error))
        {
            Debug.LogWarning($"Gacha failed: {error}");
            return false;
        }

        // Calculate cost.
        int cost = CalculateCost(gacha, pullCount);

        if (CurrencySystem.Instance.Gems < cost)
        {
            Debug.LogWarning("Not enough Gems.");
            return false;
        }

        // Temporary pity.
        int currentPity = GetPityCount(gacha.GachaId);

        List<GachaPullResult> rolledResults =
            new List<GachaPullResult>(pullCount);

        List<RewardGrantRequest> grantRequests =
            new List<RewardGrantRequest>(pullCount);

        // Characters already owned, or pulled earlier in this batch, become duplicates.
        HashSet<string> pulledCharacterIds = new HashSet<string>();

        // Roll rewards.
        for (int i = 0; i < pullCount; i++)
        {
            GachaPullResult rolled = RollReward(
                gacha,
                ref currentPity
            );

            if (rolled == null || rolled.Reward == null)
            {
                Debug.LogError(
                    "Gacha produced an invalid reward."
                );

                return false;
            }

            if (rolled.Reward is CharacterData character)
            {
                bool owned = PlayerSystem.Instance != null &&
                             PlayerSystem.Instance.IsCharacterOwned(character);

                if (owned || !pulledCharacterIds.Add(character.CharacterId))
                    rolled.MarkDuplicate(GetDuplicateConversion(character));
            }

            rolledResults.Add(rolled);

            grantRequests.Add(
                new RewardGrantRequest(
                    rolled.Reward,
                    rolled.Amount
                )
            );
        }

        // Validate all rewards before spending currency.
        if (!RewardSystem.Instance.CanGrantRewards(
            grantRequests,
            out error))
        {
            Debug.LogWarning(
                $"Gacha batch rejected: {error}"
            );

            return false;
        }

        // Spend Gems.
        if (!CurrencySystem.Instance.SpendGems(cost))
            return false;

        // Grant rewards.
        RewardGrantResult[] grantResults =
            RewardSystem.Instance
                .GrantRewards(grantRequests)
                .ToArray();

        if (!AllRewardsGranted(grantResults))
        {
            Debug.LogError(
                "Gacha reward commit failed. " +
                "Currency refund/rollback is not implemented."
            );

            return false;
        }

        // Commit pity only after successful granting.
        SetPityCount(
            gacha.GachaId,
            currentPity
        );

        SaveGachaData();

        results = rolledResults.ToArray();

        OnPullCompleted?.Invoke(gacha, results);

        if (!showPopup)
            return true;

        // Prepare popup.
        List<RewardData> popupRewards =
            new List<RewardData>(results.Length);
        List<int> popupAmounts =
            new List<int>(results.Length);

        foreach (GachaPullResult result in results)
        {
            if (result != null && result.Reward != null)
            {
                popupRewards.Add(result.Reward);
                popupAmounts.Add(result.Amount);
            }
        }

        RewardSystem.Instance.ShowRewardPopup(
            popupRewards,
            popupAmounts
        );

        return true;
    }

    #endregion

    // Same materials RewardSystem.ConvertDuplicateCharacter grants.
    private static List<RewardAmount> GetDuplicateConversion(CharacterData character)
    {
        List<RewardAmount> converted = new List<RewardAmount>();

        if (character.DuplicateDust != null && character.DuplicateDustAmount > 0)
            converted.Add(new RewardAmount(character.DuplicateDust, character.DuplicateDustAmount));

        if (character.DuplicateEngram != null && character.DuplicateEngramAmount > 0)
            converted.Add(new RewardAmount(character.DuplicateEngram, character.DuplicateEngramAmount));

        return converted;
    }

    #region Validation

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

        if (gacha.GachaCost < 0)
        {
            error = "Gacha cost cannot be negative.";
            return false;
        }

        if (gacha.PityThreshold <= 0)
        {
            error = "Pity threshold must be greater than zero.";
            return false;
        }

        if (gacha.GachaEntries == null ||
            gacha.GachaEntries.Length == 0)
        {
            error = "Gacha entries are empty.";
            return false;
        }

        // Validate rarity rates.
        if (gacha.NRate < 0f ||
            gacha.RRate < 0f ||
            gacha.SRRate < 0f ||
            gacha.URRate < 0f)
        {
            error = "Rarity rates cannot be negative.";
            return false;
        }

        float totalRate =
            gacha.NRate +
            gacha.RRate +
            gacha.SRRate +
            gacha.URRate;

        if (!Mathf.Approximately(totalRate, 100f))
        {
            error =
                $"Rarity rates must total 100%. Current: {totalRate}%";

            return false;
        }

        // Validate reward entries.
        foreach (GachaEntry entry in gacha.GachaEntries)
        {
            if (entry.RewardData == null)
            {
                error = "Gacha contains a null reward entry.";
                return false;
            }

            if (entry.UseInterval)
            {
                if (entry.LowInterval < IntervalMin ||
                    entry.HighInterval > IntervalMax ||
                    entry.LowInterval > entry.HighInterval)
                {
                    error =
                        $"Invalid amount range: {entry.RewardData.name}";

                    return false;
                }

                // RewardSystem grants characters one at a time.
                if (entry.RewardData is CharacterData &&
                    entry.MaxAmount > 1)
                {
                    error =
                        $"Character entries cannot give more than 1: {entry.RewardData.name}";

                    return false;
                }
            }
        }

        // Validate rarity availability.
        if (gacha.NRate > 0f &&
            !HasRewardByRarity(gacha, RewardRarity.N))
        {
            error = "N rarity has no available rewards.";
            return false;
        }

        if (gacha.RRate > 0f &&
            !HasRewardByRarity(gacha, RewardRarity.R))
        {
            error = "R rarity has no available rewards.";
            return false;
        }

        if (gacha.SRRate > 0f &&
            !HasRewardByRarity(gacha, RewardRarity.SR))
        {
            error = "SR rarity has no available rewards.";
            return false;
        }

        if (gacha.URRate > 0f &&
            !HasRewardByRarity(gacha, RewardRarity.UR))
        {
            error = "UR rarity has no available rewards.";
            return false;
        }

        // Validate pity rarity.
        if (!HasRewardByRarity(gacha, gacha.PityRarity))
        {
            error =
                $"Pity rarity {gacha.PityRarity} has no rewards.";

            return false;
        }

        // Featured reward must belong to the reward entries.
        if (gacha.FeaturedReward != null)
        {
            bool featuredExists = false;

            foreach (GachaEntry entry in gacha.GachaEntries)
            {
                if (entry.RewardData == gacha.FeaturedReward &&
                    GetEffectiveRarity(entry) == RewardRarity.UR)
                {
                    featuredExists = true;
                    break;
                }
            }

            if (!featuredExists)
            {
                error =
                    "Featured reward must exist in GachaEntries " +
                    "and have effective UR rarity.";

                return false;
            }
        }

        return true;
    }

    private bool HasRewardByRarity(
        GachaData gacha,
        RewardRarity rarity)
    {
        foreach (GachaEntry entry in gacha.GachaEntries)
        {
            if (entry.RewardData == null)
                continue;

            if (GetEffectiveRarity(entry) == rarity)
                return true;
        }

        return false;
    }

    #endregion

    #region Effective Rarity

    private RewardRarity GetEffectiveRarity(
        GachaEntry entry)
    {
        if (entry.OverrideRarity)
            return entry.OverrideRarityValue;

        if (entry.RewardData == null)
            return default;

        return entry.RewardData.Rarity;
    }

    #endregion

    #region Cost

    private int CalculateCost(
        GachaData gacha,
        int pullCount)
    {
        double discountMultiplier =
            1.0 -
            Mathf.Clamp(
                gacha.GachaDiscount,
                0f,
                100f
            ) / 100.0;

        double rawCost =
            (double)gacha.GachaCost * pullCount;

        double discountedCost =
            System.Math.Round(
                rawCost * discountMultiplier,
                System.MidpointRounding.AwayFromZero
            );

        if (discountedCost > int.MaxValue)
            return int.MaxValue;

        return Mathf.Max(
            0,
            (int)discountedCost
        );
    }

    #endregion

    #region Rarity Rolling

    private RewardRarity RollRarity(
        GachaData gacha)
    {
        float roll = Random.Range(0f, 100f);

        if (roll < gacha.NRate)
            return RewardRarity.N;

        roll -= gacha.NRate;

        if (roll < gacha.RRate)
            return RewardRarity.R;

        roll -= gacha.RRate;

        if (roll < gacha.SRRate)
            return RewardRarity.SR;

        return RewardRarity.UR;
    }

    #endregion

    #region Reward Rolling

    private GachaPullResult RollReward(
        GachaData gacha,
        ref int pityCount)
    {
        pityCount++;

        // Hard pity.
        if (pityCount >= gacha.PityThreshold)
        {
            GachaEntry pityEntry = PickEntryByRarity(
                gacha,
                gacha.PityRarity
            );

            if (pityEntry == null)
                return null;

            pityCount = 0;

            return new GachaPullResult(
                pityEntry.RewardData,
                true,
                RollAmount(pityEntry),
                GetEffectiveRarity(pityEntry)
            );
        }

        // Roll rarity.
        RewardRarity rarity = RollRarity(gacha);

        GachaEntry entry = PickEntryByRarity(
            gacha,
            rarity
        );

        if (entry == null)
        {
            Debug.LogError(
                $"No reward found for rarity {rarity}."
            );

            return null;
        }

        // Reset pity when the picked entry reaches pity rarity.
        if (GetEffectiveRarity(entry) == gacha.PityRarity)
            pityCount = 0;

        return new GachaPullResult(
            entry.RewardData,
            false,
            RollAmount(entry),
            GetEffectiveRarity(entry)
        );
    }

    // Amount range is inclusive on both ends.
    private static int RollAmount(GachaEntry entry)
    {
        return Random.Range(entry.MinAmount, entry.MaxAmount + 1);
    }

    #endregion

    #region Reward Selection

    private List<GachaEntry> GetCandidates(
        GachaData gacha,
        RewardRarity rarity)
    {
        List<GachaEntry> candidates =
            new List<GachaEntry>();

        foreach (GachaEntry entry in gacha.GachaEntries)
        {
            if (entry == null || entry.RewardData == null)
                continue;

            if (GetEffectiveRarity(entry) == rarity)
                candidates.Add(entry);
        }

        return candidates;
    }

    private GachaEntry FindFeaturedEntry(
        GachaData gacha,
        List<GachaEntry> urCandidates)
    {
        if (gacha.FeaturedReward == null)
            return null;

        return urCandidates.Find(
            entry => entry.RewardData == gacha.FeaturedReward
        );
    }

    private GachaEntry PickEntryByRarity(
        GachaData gacha,
        RewardRarity rarity)
    {
        List<GachaEntry> candidates = GetCandidates(gacha, rarity);

        if (candidates.Count == 0)
            return null;

        // Featured reward selection for UR.
        if (rarity == RewardRarity.UR)
        {
            GachaEntry featured = FindFeaturedEntry(gacha, candidates);

            if (featured != null &&
                Random.Range(0f, 100f) < FeaturedChance)
            {
                return featured;
            }
        }

        // Every entry of the rolled rarity is equally likely.
        return candidates[
            Random.Range(0, candidates.Count)
        ];
    }

    #endregion

    #region Displayed Rates

    /// <summary>
    /// Chance of each reward entry on a single (non-pity) pull, in percent.
    /// Mirrors RollRarity + PickEntryByRarity: rarity rate, then the featured
    /// 50% on UR, then an even split between the rarity's entries.
    /// </summary>
    public List<GachaRateEntry> GetRewardRates(GachaData gacha)
    {
        List<GachaRateEntry> rates = new List<GachaRateEntry>();

        if (gacha == null || gacha.GachaEntries == null)
            return rates;

        RewardRarity[] order = { RewardRarity.UR, RewardRarity.SR, RewardRarity.R, RewardRarity.N };

        foreach (RewardRarity rarity in order)
        {
            List<GachaEntry> candidates = GetCandidates(gacha, rarity);

            if (candidates.Count == 0)
                continue;

            float rarityRate = GetRarityRate(gacha, rarity);
            GachaEntry featured = rarity == RewardRarity.UR
                ? FindFeaturedEntry(gacha, candidates)
                : null;

            double featuredDirect = featured != null ? FeaturedChance / 100.0 : 0.0;
            double evenShare = (1.0 - featuredDirect) / candidates.Count;

            foreach (GachaEntry entry in candidates)
            {
                double withinRarity = evenShare + (entry == featured ? featuredDirect : 0.0);

                rates.Add(new GachaRateEntry(
                    entry.RewardData,
                    rarity,
                    (float)(rarityRate * withinRarity),
                    entry == featured,
                    entry.MinAmount,
                    entry.MaxAmount));
            }
        }

        return rates;
    }

    public static float GetRarityRate(GachaData gacha, RewardRarity rarity)
    {
        switch (rarity)
        {
            case RewardRarity.N: return gacha.NRate;
            case RewardRarity.R: return gacha.RRate;
            case RewardRarity.SR: return gacha.SRRate;
            default: return gacha.URRate;
        }
    }

    #endregion

    #region Reward Granting

    private bool AllRewardsGranted(
        RewardGrantResult[] results)
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

    #endregion

    #region Save / Load

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
        Save.To(
            saveData,
            SaveFileName
        );
    }

    #endregion
}
