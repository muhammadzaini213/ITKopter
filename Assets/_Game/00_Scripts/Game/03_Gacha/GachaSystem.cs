
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Slafurry.Core.Abstract;
using Slafurry.Systems.Save;

public class GachaSystem : GameSystem<GachaSystem>
{
    private const string SaveFileName = "gacha";

    private readonly Dictionary<string, int> pityCounters = new();

    public override IEnumerator Initialize()
    {
        LoadPityData();
        yield return null;
    }

    public override void PostInitialize()
    {
    }

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();
    }

    public int GetPityCount(GachaData banner)
    {
        if (banner == null || string.IsNullOrWhiteSpace(banner.GachaId))
            return 0;

        return pityCounters.TryGetValue(banner.GachaId, out int count)
            ? count
            : 0;
    }

    public int GetPullCost(GachaData banner, bool multi)
    {
        if (banner == null)
            return 0;

        int count = multi
            ? banner.MultiPullCount
            : banner.SinglePullCount;

        float discount = Mathf.Clamp(banner.GachaDiscount, 0f, 100f);

        return Mathf.Max(0, Mathf.RoundToInt(
            banner.GachaCost * count * (1f - discount / 100f)
        ));
    }

    public bool PullSingle(
        GachaData banner,
        out List<GachaPullResult> results)
    {
        return Pull(banner, false, out results);
    }

    public bool PullMulti(
        GachaData banner,
        out List<GachaPullResult> results)
    {
        return Pull(banner, true, out results);
    }

    private bool Pull(
        GachaData banner,
        bool multi,
        out List<GachaPullResult> results)
    {
        results = new List<GachaPullResult>();

        if (!ValidateBanner(banner))
            return false;

        int pullCount = multi
            ? banner.MultiPullCount
            : banner.SinglePullCount;

        int cost = GetPullCost(banner, multi);

        // Pastikan Gems cukup sebelum menjalankan RNG.
        if (CurrencySystem.Instance.Gems < cost)
            return false;

        // Semua konfigurasi sudah divalidasi sebelum Gems dipotong.
        if (!CurrencySystem.Instance.SpendGems(cost))
            return false;

        string id = banner.GachaId;
        int pity = GetPityCount(banner);

        for (int i = 0; i < pullCount; i++)
        {
            bool isPity = pity + 1 >= banner.PityThreshold;

            RewardData reward = isPity
                ? GetRandomRewardOfRarity(
                    banner, banner.PityRarity)
                : RollReward(banner);

            if (reward == null)
            {
                // Kondisi ini seharusnya tidak terjadi setelah validasi.
                Debug.LogError("[GachaSystem] Failed to select reward.");
                break;
            }

            if (reward.Rarity == banner.PityRarity)
                pity = 0;
            else
                pity++;

            results.Add(new GachaPullResult(reward, isPity));
        }

        pityCounters[id] = pity;
        SavePityData();

        return results.Count == pullCount;
    }

    private bool ValidateBanner(GachaData banner)
    {
        if (banner == null)
        {
            Debug.LogWarning("[GachaSystem] Banner is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(banner.GachaId))
        {
            Debug.LogWarning("[GachaSystem] Banner ID is missing.");
            return false;
        }

        if (banner.GachaCost <= 0 ||
            banner.PityThreshold <= 0 ||
            banner.SinglePullCount <= 0 ||
            banner.MultiPullCount <= 0)
        {
            Debug.LogWarning("[GachaSystem] Invalid banner settings.");
            return false;
        }

        if (banner.RewardPool == null ||
            banner.RewardPool.Length == 0)
        {
            Debug.LogWarning("[GachaSystem] Reward pool is empty.");
            return false;
        }

        // Hard pity wajib bisa memberikan rarity yang dijanjikan.
        if (GetRandomRewardOfRarity(
                banner, banner.PityRarity) == null)
        {
            Debug.LogWarning(
                $"[GachaSystem] No {banner.PityRarity} reward " +
                $"exists in banner '{banner.GachaName}'."
            );
            return false;
        }

        if (GetAvailableRateTotal(banner) <= 0f)
        {
            Debug.LogWarning("[GachaSystem] No valid rarity rates.");
            return false;
        }

        return true;
    }

    private RewardData RollReward(GachaData banner)
    {
        List<RewardRarity> rarities = new();
        List<float> weights = new();

        AddRarityIfAvailable(
            banner, RewardRarity.R, banner.RRate, rarities, weights);

        AddRarityIfAvailable(
            banner, RewardRarity.SR, banner.SRRate, rarities, weights);

        AddRarityIfAvailable(
            banner, RewardRarity.SSR, banner.SSRRate, rarities, weights);

        float totalWeight = 0f;

        foreach (float weight in weights)
            totalWeight += weight;

        if (totalWeight <= 0f)
            return null;

        float roll = Random.value * totalWeight;

        for (int i = 0; i < rarities.Count; i++)
        {
            roll -= weights[i];

            if (roll <= 0f)
                return GetRandomRewardOfRarity(banner, rarities[i]);
        }

        return GetRandomRewardOfRarity(
            banner, rarities[rarities.Count - 1]);
    }

    private void AddRarityIfAvailable(
        GachaData banner,
        RewardRarity rarity,
        float rate,
        List<RewardRarity> rarities,
        List<float> weights)
    {
        if (rate <= 0f)
            return;

        if (GetRandomRewardOfRarity(banner, rarity) == null)
            return;

        rarities.Add(rarity);
        weights.Add(rate);
    }

    private float GetAvailableRateTotal(GachaData banner)
    {
        float total = 0f;

        if (GetRandomRewardOfRarity(banner, RewardRarity.R) != null)
            total += banner.RRate;

        if (GetRandomRewardOfRarity(banner, RewardRarity.SR) != null)
            total += banner.SRRate;

        if (GetRandomRewardOfRarity(banner, RewardRarity.SSR) != null)
            total += banner.SSRRate;

        return total;
    }

    private RewardData GetRandomRewardOfRarity(
        GachaData banner,
        RewardRarity rarity)
    {
        List<RewardData> matchingRewards = new();

        foreach (RewardData reward in banner.RewardPool)
        {
            if (reward != null && reward.Rarity == rarity)
                matchingRewards.Add(reward);
        }

        if (matchingRewards.Count == 0)
            return null;

        return matchingRewards[Random.Range(0, matchingRewards.Count)];
    }

    private void LoadPityData()
    {
        GachaSaveData data = Save.From<GachaSaveData>(
            SaveFileName,
            new GachaSaveData()
        );

        pityCounters.Clear();

        if (data == null || data.banners == null)
            return;

        foreach (BannerPityData banner in data.banners)
        {
            if (banner == null ||
                string.IsNullOrWhiteSpace(banner.gachaId))
                continue;

            pityCounters[banner.gachaId] = Mathf.Max(0, banner.pityCount);
        }
    }

    private void SavePityData()
    {
        GachaSaveData data = new GachaSaveData();

        foreach (KeyValuePair<string, int> entry in pityCounters)
        {
            data.banners.Add(new BannerPityData
            {
                gachaId = entry.Key,
                pityCount = entry.Value
            });
        }

        Save.To(data, SaveFileName);
    }
}