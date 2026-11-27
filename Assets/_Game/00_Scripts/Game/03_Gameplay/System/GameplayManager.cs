
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Slafurry.Core.Abstract;
using Slafurry.Systems.Scene;

public class GameplayManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyWaveManager enemyWaveManager;

    [Header("Level Completion")]
    [SerializeField] private SceneReference stageScene;
    [SerializeField, Min(0f)] private float returnToMenuDelay = 2f;

    public bool IsLevelCompleted { get; private set; }

    private Coroutine returnToMenuCoroutine;

    private void Awake()
    {
        // Story battles: StoryBattleController handles toasts, scripted losses and the result.
        if (StorySystem.Instance != null && StorySystem.Instance.IsInStoryBattle &&
            GetComponent<StoryBattleController>() == null)
        {
            gameObject.AddComponent<StoryBattleController>();
        }

        // Line the playable column up with the gap between the HUD side
        // panels at any screen aspect (WebGL / itch.io window sizes).
        PlayAreaCameraFit.Ensure();
    }

    private bool bossIntroShown;

    private void Start()
    {
        // Regular stages: optional speaker tips during the battle.
        LevelData level = LevelSystem.Instance != null ? LevelSystem.Instance.CurrentLevel : null;

        if (StoryBattleController.Active == null && level != null &&
            level.BattleToasts != null && level.BattleToasts.Length > 0)
        {
            StartCoroutine(PlayBattleToasts(level.BattleToasts));
        }
    }

    private IEnumerator PlayBattleToasts(StoryToastLine[] toasts)
    {
        StoryToastLine[] ordered = (StoryToastLine[])toasts.Clone();
        System.Array.Sort(ordered, (a, b) => a.time.CompareTo(b.time));

        float clock = 0f;

        foreach (StoryToastLine toast in ordered)
        {
            if (toast == null)
                continue;

            while (clock < toast.time && !IsLevelCompleted)
            {
                clock += Time.deltaTime;
                yield return null;
            }

            if (IsLevelCompleted)
                yield break;

            StorySystem.Instance?.Toasts?.Show(toast);
        }
    }

    private void HandleBossAppeared(EnemyBoss boss)
    {
        if (bossIntroShown || IsLevelCompleted)
            return;

        LevelData level = LevelSystem.Instance != null ? LevelSystem.Instance.CurrentLevel : null;

        if (level == null || level.BossIntroToasts == null || level.BossIntroToasts.Length == 0)
            return;

        bossIntroShown = true;
        StorySystem.Instance?.Toasts?.PlaySequence(level.BossIntroToasts);
    }

    private void OnEnable()
    {
        EnemyBoss.OnBossAppeared += HandleBossAppeared;

        if (enemyWaveManager != null)
        {
            enemyWaveManager.OnLevelWon += HandleLevelWon;
        }
    }

    private void OnDisable()
    {
        EnemyBoss.OnBossAppeared -= HandleBossAppeared;

        if (enemyWaveManager != null)
        {
            enemyWaveManager.OnLevelWon -= HandleLevelWon;
        }

        if (returnToMenuCoroutine != null)
        {
            StopCoroutine(returnToMenuCoroutine);
            returnToMenuCoroutine = null;
        }
    }

    private void HandleLevelWon()
    {
        if (IsLevelCompleted)
            return;

        LevelSystem levelSystem = LevelSystem.Instance;

        if (levelSystem == null || levelSystem.CurrentLevel == null)
        {
            Debug.LogError(
                "[GameplayManager] Current level data is unavailable.",
                this);
            return;
        }

        IsLevelCompleted = true;

        // Player EXP (first clear doubles) — granted before CompleteLevel so
        // the first-clear check still sees the level as uncleared.
        GrantPlayerExp(levelSystem.CurrentLevel);

        if (StoryBattleController.Active != null)
        {
            StoryBattleController.Active.HandleVictory();
            return;
        }

        LevelData completedLevel = levelSystem.CurrentLevel;
        RewardData[] levelRewards = completedLevel.LevelRewards;

        Debug.Log(
            $"[GameplayManager] Level '{completedLevel.LevelName}' completed.",
            this);

        // Unlocks the next stage (LevelData.previousLevel chain).
        if (PlayerSystem.Instance != null)
            PlayerSystem.Instance.CompleteLevel(completedLevel.LevelId);

        Objectives.Report(ObjectiveType.ClearStages, 1);

        GrantLevelRewards(levelRewards, completedLevel.RewardEntries);

        // A level with no item rewards still shows the popup for its EXP bar.
        if (RewardSystem.Instance != null && !RewardSystem.Instance.IsPopupOpen &&
            PlayerSystem.Instance != null && PlayerSystem.Instance.HasPendingExpGain)
        {
            RewardSystem.Instance.ShowRewardPopup(System.Array.Empty<RewardData>(), System.Array.Empty<int>());
        }

        returnToMenuCoroutine = StartCoroutine(ReturnToMainMenu());
    }

    // Grants the clear EXP. The reward popup's profile EXP bar
    // (ExpGainBarUI) animates the gain, level-ups included.
    private void GrantPlayerExp(LevelData level)
    {
        if (PlayerSystem.Instance != null && level != null)
            PlayerSystem.Instance.GrantLevelExp(level);
    }

    private void GrantLevelRewards(RewardData[] levelRewards, StoryRewardEntry[] rewardEntries)
    {
        bool hasEntries = rewardEntries != null && rewardEntries.Length > 0;

        if ((levelRewards == null || levelRewards.Length == 0) && !hasEntries)
        {
            Debug.Log(
                "[GameplayManager] This level has no rewards.",
                this);
            return;
        }

        RewardSystem rewardSystem = RewardSystem.Instance;

        if (rewardSystem == null)
        {
            Debug.LogError(
                "[GameplayManager] RewardSystem is unavailable. " +
                "Level rewards could not be granted.",
                this);
            return;
        }

        List<RewardGrantRequest> requests = new();

        if (hasEntries)
        {
            foreach (StoryRewardEntry entry in rewardEntries)
            {
                if (entry != null && entry.reward != null)
                    requests.Add(new RewardGrantRequest(entry.reward, Mathf.Max(1, entry.amount)));
            }
        }

        foreach (RewardData reward in levelRewards ?? new RewardData[0])
        {
            if (reward == null)
            {
                Debug.LogWarning(
                    "[GameplayManager] A level reward is null.",
                    this);
                continue;
            }

            requests.Add(new RewardGrantRequest(reward, 1));
        }

        if (requests.Count == 0)
        {
            Debug.LogWarning(
                "[GameplayManager] No valid level rewards were found.",
                this);
            return;
        }

        List<RewardGrantResult> results = rewardSystem.GrantRewards(requests);

        bool allGranted = true;
        List<RewardData> grantedRewards = new();
        List<int> grantedAmounts = new();

        foreach (RewardGrantResult result in results)
        {
            if (result == null)
            {
                allGranted = false;
                continue;
            }

            if (!result.Success)
            {
                allGranted = false;

                Debug.LogWarning(
                    $"[GameplayManager] Failed to grant reward: " +
                    $"{result.Message}",
                    this);

                continue;
            }

            Debug.Log(
                $"[GameplayManager] Granted reward: " +
                $"{result.Reward.name} x{result.GrantedAmount}.",
                this);

            grantedRewards.Add(result.Reward);
            grantedAmounts.Add(result.GrantedAmount);
        }

        if (grantedRewards.Count > 0)
        {
            rewardSystem.ShowRewardPopup(grantedRewards, grantedAmounts);
        }

        if (!allGranted)
        {
            Debug.LogWarning(
                "[GameplayManager] Some level rewards could not be granted.",
                this);
        }
    }

    private IEnumerator ReturnToMainMenu()
    {
        if (returnToMenuDelay > 0f)
        {
            yield return new WaitForSeconds(returnToMenuDelay);
        }

        if (stageScene == null)
        {
            Debug.LogError(
                "[GameplayManager] Main Menu scene name is empty.",
                this);

            returnToMenuCoroutine = null;
            yield break;
        }

        Debug.Log(
            "[GameplayManager] Returning to Main Menu.",
            this);

        SceneSystem.Load(stageScene);
        returnToMenuCoroutine = null;
    }
}
