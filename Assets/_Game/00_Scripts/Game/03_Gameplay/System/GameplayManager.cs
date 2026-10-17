
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

    private void OnEnable()
    {
        if (enemyWaveManager != null)
        {
            enemyWaveManager.OnLevelWon += HandleLevelWon;
        }
    }

    private void OnDisable()
    {
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

        LevelData completedLevel = levelSystem.CurrentLevel;
        RewardData[] levelRewards = completedLevel.LevelRewards;

        Debug.Log(
            $"[GameplayManager] Level '{completedLevel.LevelName}' completed.",
            this);

        GrantLevelRewards(levelRewards);

        returnToMenuCoroutine = StartCoroutine(ReturnToMainMenu());
    }

    private void GrantLevelRewards(RewardData[] levelRewards)
    {
        if (levelRewards == null || levelRewards.Length == 0)
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

        foreach (RewardData reward in levelRewards)
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
        }

        if (grantedRewards.Count > 0)
        {
            rewardSystem.ShowRewardPopup(grantedRewards);
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
