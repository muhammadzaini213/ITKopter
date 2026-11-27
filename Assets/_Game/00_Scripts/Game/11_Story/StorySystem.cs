using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Slafurry.Core.Abstract;
using Slafurry.Systems.Audio;
using Slafurry.Systems.Pause;
using Slafurry.Systems.Scene;

/// <summary>
/// Runs story chapters across scenes: dialogue segments play in the story scene,
/// battle segments in the gameplay scene with a fixed party. Also drives the
/// first-time onboarding (the opening chapters, in order) and the toasts that
/// guide the player after a chapter sends them to the Shop or Gacha.
/// </summary>
public class StorySystem : GameSystem<StorySystem>
{
    [Header("Scenes")]
    [SerializeField] private SceneReference storyScene;
    [SerializeField] private SceneReference gameplayScene;
    [SerializeField] private SceneReference mainMenuScene;
    [SerializeField] private SceneReference storyMenuScene;

    [Header("Onboarding")]
    [Tooltip("Opening chapters in order. New players play these automatically until all are completed.")]
    [SerializeField] private StoryData[] onboardingChapters = new StoryData[0];

    [Header("UI")]
    [SerializeField] private StoryToastUI toastUI;

    [Header("Named Speakers")]
    [Tooltip("Non-playable speakers (e.g. Fajar, the King of All Concepts). Lines with only a speaker name " +
             "matching one of these use its portrait (_0) and face icon (_1). Not part of the CharacterDatabase.")]
    [SerializeField] private CharacterData[] namedSpeakers = new CharacterData[0];

    [Tooltip("The player's avatar in battle (talks when the Annihilator manifests).")]
    [SerializeField] private CharacterData playerAvatar;

    public CharacterData PlayerAvatar => playerAvatar;

    /// <summary>The speaker for a line: its CharacterData, or a named speaker matched by name.</summary>
    public CharacterData ResolveSpeaker(CharacterData speaker, string speakerName)
    {
        if (speaker != null || string.IsNullOrWhiteSpace(speakerName) || namedSpeakers == null)
            return speaker;

        foreach (CharacterData named in namedSpeakers)
        {
            if (named != null &&
                string.Equals(named.CharacterName, speakerName.Trim(), System.StringComparison.OrdinalIgnoreCase))
            {
                return named;
            }
        }

        return null;
    }

    private bool battleInProgress;
    private bool completing;
    private string pendingToastScene;
    private StoryToastLine[] pendingSceneToasts;
    private StoryToastLine[] pendingGachaToasts;

    public StoryData CurrentStory { get; private set; }
    public int SegmentIndex { get; private set; }
    public bool IsOnboardingRun { get; private set; }

    public StorySegment CurrentSegment =>
        CurrentStory != null && CurrentStory.HasSegments &&
        SegmentIndex >= 0 && SegmentIndex < CurrentStory.Segments.Length
            ? CurrentStory.Segments[SegmentIndex]
            : null;

    /// <summary>True while the gameplay scene is running a story battle.</summary>
    public bool IsInStoryBattle =>
        battleInProgress && CurrentSegment != null &&
        CurrentSegment.type == StorySegmentType.Battle;

    public StoryToastUI Toasts => toastUI;
    public IReadOnlyList<StoryData> OnboardingChapters => onboardingChapters;

    /// <summary>Raised when the next dialogue segment is ready while the story scene is open.</summary>
    public event Action OnDialogueSegmentReady;
    public event Action<StoryData> OnStoryCompleted;

    public override IEnumerator Initialize()
    {
        yield return null;
    }

    public override void PostInitialize()
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.OnSceneLoadCompleted += HandleSceneLoaded;

        if (GachaSystem.Instance != null)
            GachaSystem.Instance.OnPullCompleted += HandleGachaPull;
    }

    protected override void OnSingletonDestroyed()
    {
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.OnSceneLoadCompleted -= HandleSceneLoaded;

        if (GachaSystem.Instance != null)
            GachaSystem.Instance.OnPullCompleted -= HandleGachaPull;
    }

    #region Onboarding

    public bool IsChapterCompleted(StoryData story)
    {
        PlayerSystem player = PlayerSystem.Instance;

        return story != null && player != null && player.IsPlayerDataLoaded &&
               player.IsLevelCompleted(story.StoryId);
    }

    /// <summary>First onboarding chapter the player hasn't completed, or null when onboarding is done.</summary>
    public StoryData NextOnboardingChapter
    {
        get
        {
            if (onboardingChapters == null)
                return null;

            foreach (StoryData chapter in onboardingChapters)
            {
                if (chapter != null && !IsChapterCompleted(chapter))
                    return chapter;
            }

            return null;
        }
    }

    public bool IsOnboardingComplete => NextOnboardingChapter == null;

    #endregion

    #region Flow

    public void StartStory(StoryData story, bool onboarding)
    {
        if (story == null)
        {
            Debug.LogWarning("[StorySystem] Cannot start a null story.");
            return;
        }

        CurrentStory = story;
        SegmentIndex = 0;
        IsOnboardingRun = onboarding;
        battleInProgress = false;
        completing = false;
        StorySession.Selected = story;

        ResetTime();

        Debug.Log($"[StorySystem] Starting '{story.StoryName}' (onboarding: {onboarding}).");

        EnterCurrentSegment();
    }

    /// <summary>Called by the story scene when the current dialogue segment has finished.</summary>
    public void CompleteDialogueSegment()
    {
        StorySegment segment = CurrentSegment;

        if (segment == null || segment.type != StorySegmentType.Dialogue)
        {
            // Legacy prefab stories (no segments) complete through here too.
            if (CurrentStory != null && !CurrentStory.HasSegments)
                CompleteCurrentStory();

            return;
        }

        SegmentIndex++;
        EnterCurrentSegment();
    }

    /// <summary>Called by the gameplay scene when a story battle ends (win or scripted loss).</summary>
    public void ReportBattleResult(bool won)
    {
        if (!battleInProgress)
            return;

        battleInProgress = false;

        if (LevelSystem.Instance != null)
            LevelSystem.Instance.ClearSession();

        ResetTime();

        Debug.Log($"[StorySystem] Battle {(won ? "won" : "lost")}; continuing '{CurrentStory?.StoryName}'.");

        SegmentIndex++;
        EnterCurrentSegment();
    }

    /// <summary>Leaves the current story without completing it.</summary>
    public void AbortStory()
    {
        if (CurrentStory != null)
            Debug.Log($"[StorySystem] Story '{CurrentStory.StoryName}' left before completion.");

        ClearCurrentStory();
    }

    private void ClearCurrentStory()
    {

        CurrentStory = null;
        SegmentIndex = 0;
        battleInProgress = false;
        completing = false;
        StorySession.Clear();
    }

    public bool IsStoryScene(string sceneName) =>
        storyScene != null && sceneName == storyScene.SceneName;

    private void EnterCurrentSegment()
    {
        if (CurrentStory == null)
            return;

        if (!CurrentStory.HasSegments)
        {
            // Legacy: the story prefab handles itself in the story scene.
            LoadScene(storyScene);
            return;
        }

        StorySegment segment = CurrentSegment;

        if (segment == null)
        {
            CompleteCurrentStory();
            return;
        }

        if (segment.type == StorySegmentType.Battle)
        {
            StartBattle(segment);
            return;
        }

        // Dialogue: continue in place when the story scene is already open.
        if (IsStoryScene(SceneManager.GetActiveScene().name) && OnDialogueSegmentReady != null)
            OnDialogueSegmentReady.Invoke();
        else
            LoadScene(storyScene);
    }

    private void StartBattle(StorySegment segment)
    {
        LevelSystem levelSystem = LevelSystem.Instance;

        if (segment.level == null || levelSystem == null ||
            !levelSystem.PrepareLevel(segment.level, segment.team))
        {
            Debug.LogError(
                $"[StorySystem] Battle segment {SegmentIndex} of '{CurrentStory.StoryName}' " +
                "could not start (missing level, team or LevelSystem). Skipping it.");

            SegmentIndex++;
            EnterCurrentSegment();
            return;
        }

        battleInProgress = true;
        LoadScene(gameplayScene);
    }

    private void CompleteCurrentStory()
    {
        if (completing || CurrentStory == null)
            return;

        completing = true;
        StartCoroutine(CompleteRoutine(CurrentStory));
    }

    private IEnumerator CompleteRoutine(StoryData story)
    {
        bool firstClear = !IsChapterCompleted(story);

        if (firstClear)
            GrantRewards(story);

        if (PlayerSystem.Instance != null && !string.IsNullOrWhiteSpace(story.StoryId))
            PlayerSystem.Instance.CompleteLevel(story.StoryId);

        OnStoryCompleted?.Invoke(story);

        // Let the player read the reward popup before moving on.
        yield return null;

        while (RewardSystem.Instance != null && RewardSystem.Instance.IsPopupOpen)
            yield return null;

        bool onboarding = IsOnboardingRun;
        completing = false;

        if (onboarding && story.AfterScene != null)
        {
            pendingToastScene = story.AfterScene.SceneName;
            pendingSceneToasts = story.AfterSceneToasts;

            if (story.AfterGachaPullToasts != null && story.AfterGachaPullToasts.Length > 0)
                pendingGachaToasts = story.AfterGachaPullToasts;

            ClearCurrentStory();
            LoadScene(story.AfterScene);
            yield break;
        }

        if (onboarding)
        {
            StoryData next = NextOnboardingChapter;

            if (next != null)
            {
                StartStory(next, true);
                yield break;
            }

            ClearCurrentStory();
            LoadScene(mainMenuScene);
            yield break;
        }

        ClearCurrentStory();
        LoadScene(storyMenuScene != null ? storyMenuScene : mainMenuScene);
    }

    private static void GrantRewards(StoryData story)
    {
        RewardSystem rewardSystem = RewardSystem.Instance;

        if (rewardSystem == null)
        {
            Debug.LogError("[StorySystem] RewardSystem is unavailable; story rewards not granted.");
            return;
        }

        List<RewardGrantRequest> requests = new();

        if (story.RewardEntries != null)
        {
            foreach (StoryRewardEntry entry in story.RewardEntries)
            {
                if (entry != null && entry.reward != null)
                    requests.Add(new RewardGrantRequest(entry.reward, Mathf.Max(1, entry.amount)));
            }
        }

        if (story.StoryRewards != null)
        {
            foreach (RewardData reward in story.StoryRewards)
            {
                if (reward != null)
                    requests.Add(new RewardGrantRequest(reward, 1));
            }
        }

        if (requests.Count == 0)
            return;

        List<RewardData> granted = new();
        List<int> amounts = new();

        foreach (RewardGrantResult result in rewardSystem.GrantRewards(requests))
        {
            if (result == null || !result.Success)
            {
                Debug.LogWarning($"[StorySystem] Story reward failed: {result?.Message}");
                continue;
            }

            granted.Add(result.Reward);
            amounts.Add(result.GrantedAmount);
        }

        if (granted.Count > 0)
            rewardSystem.ShowRewardPopup(granted, amounts);
    }

    private void HandleSceneLoaded(string sceneName)
    {
        // Leaving a story battle any other way (pause menu, game over exit) abandons the story.
        if (battleInProgress && (gameplayScene == null || sceneName != gameplayScene.SceneName))
            AbortStory();
        else if (CurrentStory != null && !battleInProgress && !completing && !IsStoryScene(sceneName))
            AbortStory();

        if (!string.IsNullOrEmpty(pendingToastScene) && sceneName == pendingToastScene)
        {
            StoryToastLine[] toasts = pendingSceneToasts;
            pendingToastScene = null;
            pendingSceneToasts = null;

            if (toastUI != null && toasts != null)
                toastUI.PlaySequence(toasts);
        }
    }

    private void HandleGachaPull(GachaData gacha, GachaPullResult[] results)
    {
        if (pendingGachaToasts == null)
            return;

        StoryToastLine[] toasts = pendingGachaToasts;
        pendingGachaToasts = null;

        if (toastUI != null)
            toastUI.PlaySequence(toasts);
    }

    private static void LoadScene(SceneReference scene)
    {
        if (scene == null)
        {
            Debug.LogError("[StorySystem] A scene reference is missing.");
            return;
        }

        SceneSystem.Load(scene);
    }

    private static void ResetTime()
    {
        Time.timeScale = 1f;

        if (PauseSystem.Instance != null)
            Pause.ForceResume();
    }

    #endregion
}
