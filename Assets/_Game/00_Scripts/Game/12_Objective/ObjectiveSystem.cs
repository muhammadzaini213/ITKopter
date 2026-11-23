using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Slafurry.Core.Abstract;
using Slafurry.Systems.Save;

/// <summary>Static entry point so gameplay systems can report progress without a hard reference.</summary>
public static class Objectives
{
    public static void Report(ObjectiveType type, int amount = 1)
    {
        if (ObjectiveSystem.Instance != null)
            ObjectiveSystem.Instance.Report(type, amount);
    }
}

[Serializable]
public class ObjectiveProgressSaveData
{
    public string id;
    public int progress;
    public bool claimed;
}

[Serializable]
public class ObjectiveSaveData
{
    public string dayKey;
    public string weekKey;
    public string monthKey;
    public string lastLoginDay;
    public List<ObjectiveProgressSaveData> entries = new();
}

/// <summary>
/// Daily / weekly / monthly objectives and one-time achievements. Periods reset at
/// local midnight, Monday and the 1st of the month. Progress is saved in objectives.json.
/// </summary>
public class ObjectiveSystem : GameSystem<ObjectiveSystem>
{
    private const string SaveFileName = "objectives";

    [SerializeField] private ObjectiveDatabase database;

    private readonly Dictionary<string, ObjectiveProgressSaveData> entries = new();
    private ObjectiveSaveData data = new();
    private bool isLoaded;
    private float rolloverTimer;

    public ObjectiveDatabase Database => database;
    public bool IsLoaded => isLoaded;

    /// <summary>Any progress or claim changed.</summary>
    public event Action OnObjectivesChanged;

    public override IEnumerator Initialize()
    {
        Load();
        yield return null;
    }

    public override void PostInitialize()
    {
        if (GachaSystem.Instance != null)
            GachaSystem.Instance.OnPullCompleted += HandleGachaPull;

        PlayerSystem player = PlayerSystem.Instance;

        if (player != null)
        {
            player.OnPlayerDataReady += HandleStateChanged;
            player.OnCharacterOwnedChanged += HandleStateChanged;
            player.OnCharacterProgressionChanged += HandleCharacterChanged;
            player.OnLevelProgressChanged += HandleStateChanged;
        }

        CheckRollover();
        RefreshStateObjectives();
    }

    protected override void OnSingletonDestroyed()
    {
        if (GachaSystem.Instance != null)
            GachaSystem.Instance.OnPullCompleted -= HandleGachaPull;

        PlayerSystem player = PlayerSystem.Instance;

        if (player != null)
        {
            player.OnPlayerDataReady -= HandleStateChanged;
            player.OnCharacterOwnedChanged -= HandleStateChanged;
            player.OnCharacterProgressionChanged -= HandleCharacterChanged;
            player.OnLevelProgressChanged -= HandleStateChanged;
        }
    }

    private void Update()
    {
        rolloverTimer -= Time.unscaledDeltaTime;

        if (rolloverTimer > 0f)
            return;

        rolloverTimer = 1f;
        CheckRollover();
    }

    #region Queries

    public IEnumerable<ObjectiveDefinition> GetObjectives(ObjectivePeriod period)
    {
        if (database == null || database.Objectives == null)
            yield break;

        foreach (ObjectiveDefinition def in database.Objectives)
        {
            if (def != null && def.period == period)
                yield return def;
        }
    }

    public int GetProgress(ObjectiveDefinition def) =>
        def != null && entries.TryGetValue(def.id, out var e) ? Mathf.Min(e.progress, def.target) : 0;

    public bool IsComplete(ObjectiveDefinition def) => def != null && GetProgress(def) >= def.target;

    public bool IsClaimed(ObjectiveDefinition def) =>
        def != null && entries.TryGetValue(def.id, out var e) && e.claimed;

    public bool CanClaim(ObjectiveDefinition def) => IsComplete(def) && !IsClaimed(def);

    public int GetClaimableCount(ObjectivePeriod period)
    {
        int count = 0;

        foreach (ObjectiveDefinition def in GetObjectives(period))
        {
            if (CanClaim(def))
                count++;
        }

        return count;
    }

    public int TotalClaimableCount =>
        GetClaimableCount(ObjectivePeriod.Daily) + GetClaimableCount(ObjectivePeriod.Weekly) +
        GetClaimableCount(ObjectivePeriod.Monthly) + GetClaimableCount(ObjectivePeriod.Achievement);

    /// <summary>Time until the period resets; zero for achievements.</summary>
    public TimeSpan TimeUntilReset(ObjectivePeriod period)
    {
        DateTime now = DateTime.Now;
        DateTime next;

        switch (period)
        {
            case ObjectivePeriod.Daily:
                next = now.Date.AddDays(1);
                break;
            case ObjectivePeriod.Weekly:
                next = WeekStart(now).AddDays(7);
                break;
            case ObjectivePeriod.Monthly:
                next = new DateTime(now.Year, now.Month, 1).AddMonths(1);
                break;
            default:
                return TimeSpan.Zero;
        }

        return next - now;
    }

    #endregion

    #region Progress

    public void Report(ObjectiveType type, int amount)
    {
        if (!isLoaded || amount <= 0 || database == null)
            return;

        CheckRollover();

        bool changed = false;

        foreach (ObjectiveDefinition def in database.Objectives)
        {
            if (def == null || def.type != type || def.IsStateBased)
                continue;

            ObjectiveProgressSaveData e = GetEntry(def);

            if (e.claimed || e.progress >= def.target)
                continue;

            e.progress = Mathf.Min(def.target, e.progress + amount);
            changed = true;
        }

        if (changed)
            SaveAndNotify();
    }

    public bool Claim(ObjectiveDefinition def, bool showPopup = true)
    {
        if (!CanClaim(def))
            return false;

        List<RewardData> granted = new();
        List<int> amounts = new();

        if (!GrantRewards(def, granted, amounts))
            return false;

        GetEntry(def).claimed = true;
        UpdateCompleteObjectives(def.period);
        SaveAndNotify();

        if (showPopup && granted.Count > 0 && RewardSystem.Instance != null)
            RewardSystem.Instance.ShowRewardPopup(granted, amounts);

        return true;
    }

    /// <summary>Claims everything claimable in a period, in one reward popup.</summary>
    public int ClaimAll(ObjectivePeriod period)
    {
        List<RewardData> granted = new();
        List<int> amounts = new();
        int claimed = 0;

        // Claiming can complete "claim N objectives", so loop until nothing new opens up.
        bool progress = true;

        while (progress)
        {
            progress = false;

            foreach (ObjectiveDefinition def in new List<ObjectiveDefinition>(GetObjectives(period)))
            {
                if (!CanClaim(def) || !GrantRewards(def, granted, amounts))
                    continue;

                GetEntry(def).claimed = true;
                UpdateCompleteObjectives(period);
                claimed++;
                progress = true;
            }
        }

        if (claimed > 0)
        {
            SaveAndNotify();

            if (granted.Count > 0 && RewardSystem.Instance != null)
                RewardSystem.Instance.ShowRewardPopup(granted, amounts);
        }

        return claimed;
    }

    private static bool GrantRewards(ObjectiveDefinition def, List<RewardData> granted, List<int> amounts)
    {
        RewardSystem rewardSystem = RewardSystem.Instance;

        if (rewardSystem == null)
            return false;

        List<RewardGrantRequest> requests = new();

        foreach (StoryRewardEntry entry in def.rewards ?? new StoryRewardEntry[0])
        {
            if (entry != null && entry.reward != null)
                requests.Add(new RewardGrantRequest(entry.reward, Mathf.Max(1, entry.amount)));
        }

        if (requests.Count == 0)
            return true;

        if (!rewardSystem.CanGrantRewards(requests, out string error))
        {
            Debug.LogWarning($"[ObjectiveSystem] Cannot claim '{def.id}': {error}");
            return false;
        }

        foreach (RewardGrantResult result in rewardSystem.GrantRewards(requests))
        {
            if (result != null && result.Success)
            {
                granted.Add(result.Reward);
                amounts.Add(result.GrantedAmount);
            }
        }

        return true;
    }

    private void UpdateCompleteObjectives(ObjectivePeriod period)
    {
        int claimedCount = 0;

        foreach (ObjectiveDefinition def in GetObjectives(period))
        {
            if (def.type != ObjectiveType.CompleteObjectives && IsClaimed(def))
                claimedCount++;
        }

        foreach (ObjectiveDefinition def in GetObjectives(period))
        {
            if (def.type == ObjectiveType.CompleteObjectives)
                GetEntry(def).progress = Mathf.Min(def.target, claimedCount);
        }
    }

    private void HandleGachaPull(GachaData gacha, GachaPullResult[] results)
    {
        Report(ObjectiveType.GachaPulls, results != null ? results.Length : 0);
    }

    private void HandleCharacterChanged(string _) => HandleStateChanged();

    private void HandleStateChanged()
    {
        if (RefreshStateObjectives())
            SaveAndNotify();
    }

    /// <summary>Re-reads ownership, character levels and cleared stages / chapters.</summary>
    public bool RefreshStateObjectives()
    {
        PlayerSystem player = PlayerSystem.Instance;

        if (!isLoaded || database == null || player == null || !player.IsPlayerDataLoaded)
            return false;

        int owned = 0;
        int maxLevel = 0;

        if (player.CharacterDatabase != null)
        {
            foreach (CharacterData character in player.CharacterDatabase.Characters)
            {
                if (character == null || !player.IsCharacterOwned(character))
                    continue;

                owned++;
                maxLevel = Mathf.Max(maxLevel, player.GetCharacterLevel(character.CharacterId));
            }
        }

        bool changed = false;

        foreach (ObjectiveDefinition def in database.Objectives)
        {
            if (def == null || !def.IsStateBased)
                continue;

            int value = def.type switch
            {
                ObjectiveType.OwnCharacters => owned,
                ObjectiveType.CharacterLevel => maxLevel,
                ObjectiveType.ClearStage or ObjectiveType.CompleteStory =>
                    player.IsLevelCompleted(def.targetId) ? def.target : 0,
                _ => 0
            };

            ObjectiveProgressSaveData e = GetEntry(def);
            int clamped = Mathf.Min(def.target, value);

            if (!e.claimed && clamped > e.progress)
            {
                e.progress = clamped;
                changed = true;
            }
        }

        return changed;
    }

    #endregion

    #region Periods & Save

    private static DateTime WeekStart(DateTime date)
    {
        int offset = ((int)date.DayOfWeek + 6) % 7; // Monday = 0
        return date.Date.AddDays(-offset);
    }

    private void CheckRollover()
    {
        if (!isLoaded)
            return;

        DateTime now = DateTime.Now;
        string day = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string week = WeekStart(now).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string month = now.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        bool changed = false;

        if (data.dayKey != day) { ResetPeriod(ObjectivePeriod.Daily); data.dayKey = day; changed = true; }
        if (data.weekKey != week) { ResetPeriod(ObjectivePeriod.Weekly); data.weekKey = week; changed = true; }
        if (data.monthKey != month) { ResetPeriod(ObjectivePeriod.Monthly); data.monthKey = month; changed = true; }

        if (data.lastLoginDay != day)
        {
            data.lastLoginDay = day;
            changed = true;

            // Counts once per calendar day for every Login objective (daily, weekly, monthly, achievements).
            foreach (ObjectiveDefinition def in database != null ? database.Objectives : new ObjectiveDefinition[0])
            {
                if (def == null || def.type != ObjectiveType.Login)
                    continue;

                ObjectiveProgressSaveData e = GetEntry(def);

                if (!e.claimed)
                    e.progress = Mathf.Min(def.target, e.progress + 1);
            }
        }

        if (changed)
            SaveAndNotify();
    }

    private void ResetPeriod(ObjectivePeriod period)
    {
        foreach (ObjectiveDefinition def in GetObjectives(period))
        {
            ObjectiveProgressSaveData e = GetEntry(def);
            e.progress = 0;
            e.claimed = false;
        }
    }

    private ObjectiveProgressSaveData GetEntry(ObjectiveDefinition def)
    {
        if (!entries.TryGetValue(def.id, out ObjectiveProgressSaveData e))
        {
            e = new ObjectiveProgressSaveData { id = def.id };
            entries[def.id] = e;
        }

        return e;
    }

    private void Load()
    {
        data = Save.From(SaveFileName, new ObjectiveSaveData()) ?? new ObjectiveSaveData();
        entries.Clear();

        if (data.entries != null)
        {
            foreach (ObjectiveProgressSaveData e in data.entries)
            {
                if (e != null && !string.IsNullOrEmpty(e.id))
                    entries[e.id] = e;
            }
        }

        isLoaded = true;
    }

    private void SaveAndNotify()
    {
        data.entries = new List<ObjectiveProgressSaveData>(entries.Values);
        Save.To(data, SaveFileName);
        OnObjectivesChanged?.Invoke();
    }

    #endregion
}
