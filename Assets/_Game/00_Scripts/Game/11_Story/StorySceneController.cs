using System.Collections;
using UnityEngine;
using Slafurry.Core.Abstract;
using Slafurry.Systems.Audio;

/// <summary>
/// 11_StoryScene: plays the current story's dialogue segment through
/// <see cref="StoryDialogueUI"/> (segment-based stories), or spawns the legacy
/// story prefab for stories without segments.
/// </summary>
public class StorySceneController : LocalSingleton<StorySceneController>
{
    [Tooltip("Where a legacy story prefab spawns. Null = this transform.")]
    [SerializeField] private Transform contentRoot;

    [SerializeField] private StoryDialogueUI dialogueUI;

    private GameObject spawnedStory;
    private StorySystem storySystem;

    public override IEnumerator Initialize() { yield return null; }
    public override void PostInitialize() { }

    protected override void OnSingletonAwake()
    {
        base.OnSingletonAwake();
    }

    private void Start()
    {
        storySystem = StorySystem.Instance;

        // Onboarding follow-up dialogue (after the Shop / Gacha / Objectives step).
        if (storySystem != null && storySystem.FollowUpStory != null && dialogueUI != null)
        {
            StoryData source = storySystem.FollowUpStory;

            if (source.StoryMusic != null && AudioSystem.Instance != null)
                Audio.PlayMusic(source.StoryMusic);

            dialogueUI.Play(source, source.FollowUpLines, null, storySystem.CompleteFollowUp);
            return;
        }

        StoryData story = storySystem != null && storySystem.CurrentStory != null
            ? storySystem.CurrentStory
            : StorySession.Selected;

        if (story == null)
        {
            Debug.LogWarning(
                "[StorySceneController] No story selected. " +
                "Start one through StorySystem.StartStory.");
            return;
        }

        if (story.StoryMusic != null && AudioSystem.Instance != null)
            Audio.PlayMusic(story.StoryMusic);

        if (story.HasSegments && storySystem != null && dialogueUI != null)
        {
            storySystem.OnDialogueSegmentReady += PlayCurrentSegment;
            PlayCurrentSegment();
            return;
        }

        SpawnSelectedStory();
    }

    protected override void OnSingletonDestroyed()
    {
        if (storySystem != null)
            storySystem.OnDialogueSegmentReady -= PlayCurrentSegment;
    }

    private void PlayCurrentSegment()
    {
        StorySegment segment = storySystem.CurrentSegment;

        if (segment == null || segment.type != StorySegmentType.Dialogue)
            return;

        StoryData story = storySystem.CurrentStory;

        // The chapter title card opens the story's first segment.
        StoryData titleStory = storySystem.SegmentIndex == 0 ? story : null;

        dialogueUI.Play(story, segment.lines, titleStory, storySystem.CompleteDialogueSegment);
    }

    public void SpawnSelectedStory()
    {
        if (spawnedStory != null)
        {
            Destroy(spawnedStory);
            spawnedStory = null;
        }

        StoryData story = StorySession.Selected;

        if (story == null || story.StoryPrefab == null)
        {
            Debug.LogWarning(
                $"[StorySceneController] '{story?.StoryName}' has no segments or prefab.");
            return;
        }

        Transform parent = contentRoot != null ? contentRoot : transform;
        spawnedStory = Instantiate(story.StoryPrefab, parent);
    }
}
