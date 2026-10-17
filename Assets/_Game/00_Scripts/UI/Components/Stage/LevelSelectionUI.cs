
using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelSelectionUI : MonoBehaviour
{
    [Header("Levels")]
    [SerializeField] private LevelData[] levels;
    [SerializeField] private Transform levelListRoot;
    [SerializeField] private LevelListItemUI levelItemPrefab;

    [Header("Team Selection")]
    [SerializeField] private TeamSelectionPopupUI teamSelectionPopup;

    public event Action<LevelData, IReadOnlyList<CharacterData>>
        OnLevelStartRequested;

    private PlayerSystem subscribedPlayer;

    private void OnEnable()
    {
        SubscribeToPlayerSystem();
    }

    private void Start()
    {
        SubscribeToPlayerSystem();
        RefreshLevelList();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerSystem();
    }

    private void SubscribeToPlayerSystem()
    {
        PlayerSystem player = PlayerSystem.Instance;

        if (player == null || subscribedPlayer == player)
            return;

        UnsubscribeFromPlayerSystem();

        subscribedPlayer = player;
        subscribedPlayer.OnPlayerDataReady += HandlePlayerDataReady;
        subscribedPlayer.OnLevelProgressChanged += RefreshLevelList;

        if (subscribedPlayer.IsPlayerDataLoaded)
            RefreshLevelList();
    }

    private void UnsubscribeFromPlayerSystem()
    {
        if (subscribedPlayer == null)
            return;

        subscribedPlayer.OnPlayerDataReady -= HandlePlayerDataReady;
        subscribedPlayer.OnLevelProgressChanged -= RefreshLevelList;

        subscribedPlayer = null;
    }

    private void HandlePlayerDataReady()
    {
        RefreshLevelList();
    }

    public void RefreshLevelList()
    {
        if (levelListRoot == null || levelItemPrefab == null)
        {
            Debug.LogWarning(
                "[LevelSelectionUI] Level list references are missing.");
            return;
        }

        PlayerSystem player = PlayerSystem.Instance;

        if (player == null || !player.IsPlayerDataLoaded)
            return;

        foreach (Transform child in levelListRoot)
            Destroy(child.gameObject);

        if (levels == null)
            return;

        foreach (LevelData level in levels)
        {
            if (level == null)
                continue;

            bool unlocked = player.IsLevelUnlocked(level);
            bool completed = player.IsLevelCompleted(level.LevelId);

            LevelListItemUI item = Instantiate(
                levelItemPrefab,
                levelListRoot);

            item.Setup(
                level,
                unlocked,
                completed,
                HandleLevelSelected);
        }
    }

    private void HandleLevelSelected(LevelData level)
    {
        if (level == null)
            return;

        PlayerSystem player = PlayerSystem.Instance;

        if (player == null || !player.IsLevelUnlocked(level))
        {
            RefreshLevelList();
            return;
        }

        if (teamSelectionPopup == null)
        {
            Debug.LogWarning(
                "[LevelSelectionUI] Team selection popup is missing.");
            return;
        }

        teamSelectionPopup.Show(level, HandleLevelStartRequested);
    }

    private void HandleLevelStartRequested(
        LevelData level,
        IReadOnlyList<CharacterData> team)
    {
        if (level == null || team == null)
            return;

        Debug.Log(
            $"[LevelSelectionUI] Start requested for {level.LevelName}. " +
            $"Team size: {team.Count}");

        OnLevelStartRequested?.Invoke(level, team);
    }
}
