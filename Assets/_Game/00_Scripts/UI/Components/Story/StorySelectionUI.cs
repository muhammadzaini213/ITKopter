using System;
using UnityEngine;
using Slafurry.Systems.Scene;

public class StorySelectionUI : MonoBehaviour
{
    [Header("Stories")]
    [SerializeField] private StoryData[] stories;
    [SerializeField] private Transform storyListRoot;
    [SerializeField] private StoryListItemUI storyItemPrefab;

    [Header("Confirmation")]
    [SerializeField] private StoryConfirmPopupUI confirmPopup;

    [Header("Scene")]
    [SerializeField] private SceneReference storyScene;

    public event Action<StoryData> OnStoryStartRequested;

    private PlayerSystem subscribedPlayer;

    private void OnEnable()
    {
        SubscribeToPlayerSystem();
    }

    private void Start()
    {
        SubscribeToPlayerSystem();
        RefreshStoryList();
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
        subscribedPlayer.OnLevelProgressChanged += RefreshStoryList;

        if (subscribedPlayer.IsPlayerDataLoaded)
            RefreshStoryList();
    }

    private void UnsubscribeFromPlayerSystem()
    {
        if (subscribedPlayer == null)
            return;

        subscribedPlayer.OnPlayerDataReady -= HandlePlayerDataReady;
        subscribedPlayer.OnLevelProgressChanged -= RefreshStoryList;

        subscribedPlayer = null;
    }

    private void HandlePlayerDataReady()
    {
        RefreshStoryList();
    }

    public void RefreshStoryList()
    {
        if (storyListRoot == null || storyItemPrefab == null)
        {
            Debug.LogWarning(
                "[StorySelectionUI] Story list references are missing.");
            return;
        }

        foreach (Transform child in storyListRoot)
            Destroy(child.gameObject);

        if (stories == null)
            return;

        PlayerSystem player = PlayerSystem.Instance;

        foreach (StoryData story in stories)
        {
            if (story == null)
                continue;

            bool unlocked = IsStoryUnlocked(story, player);
            bool completed = player != null &&
                             player.IsPlayerDataLoaded &&
                             player.IsLevelCompleted(story.StoryId);

            StoryListItemUI item = Instantiate(
                storyItemPrefab,
                storyListRoot);

            item.Setup(
                story,
                unlocked,
                completed,
                HandleStorySelected);
        }
    }

    private static bool IsStoryUnlocked(StoryData story, PlayerSystem player)
    {
        if (story == null)
            return false;

        if (story.PreviousStory == null)
            return true;

        if (player == null ||
            !player.IsPlayerDataLoaded ||
            string.IsNullOrWhiteSpace(story.PreviousStory.StoryId))
        {
            return false;
        }

        return player.IsLevelCompleted(story.PreviousStory.StoryId);
    }

    private void HandleStorySelected(StoryData story)
    {
        if (story == null)
            return;

        PlayerSystem player = PlayerSystem.Instance;

        if (player == null ||
            !player.IsPlayerDataLoaded ||
            !IsStoryUnlocked(story, player))
        {
            RefreshStoryList();
            return;
        }

        if (confirmPopup == null)
        {
            Debug.LogWarning(
                "[StorySelectionUI] Confirmation popup is missing.");
            return;
        }

        confirmPopup.Show(story, HandleStoryConfirmed);
    }

    private void HandleStoryConfirmed(StoryData story)
    {
        if (story == null)
            return;

        if (storyScene == null)
        {
            Debug.LogWarning(
                "[StorySelectionUI] Story scene reference is missing.");
            return;
        }

        Debug.Log(
            $"[StorySelectionUI] Start requested for {story.StoryName}.");

        StorySession.Selected = story;
        OnStoryStartRequested?.Invoke(story);
        SceneSystem.Load(storyScene);
    }
}
