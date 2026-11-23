using UnityEngine;

[CreateAssetMenu(fileName = "StoryData", menuName = "Scriptable Objects/StoryData")]
public class StoryData : ScriptableObject
{
    [Header("Story Info")]
    [SerializeField] private string storyId;
    [SerializeField] private string storyName;
    [SerializeField, TextArea] private string storyDescription;
    [Tooltip("Small label on the title card and story list, e.g. CHAPTER 3 or PROLOGUE.")]
    [SerializeField] private string chapterLabel;

    [Tooltip("Leave empty if this story is unlocked from the beginning.")]
    [SerializeField] private StoryData previousStory;

    [Header("Content")]
    [Tooltip("Prefab instantiated by StorySceneController inside 11_StoryScene.")]
    [SerializeField] private GameObject storyPrefab;

    [Tooltip("Dialogue and battle segments, played in order. Used instead of the prefab when set.")]
    [SerializeField] private StorySegment[] segments = new StorySegment[0];

    [Tooltip("Story scene background. Empty = black.")]
    [SerializeField] private Sprite background;

    [Header("Rewards")]
    [Tooltip("Granted when the story ends. Not shown in the confirm popup.")]
    [SerializeField] private RewardData[] storyRewards;

    [Tooltip("Rewards with amounts, granted on the first completion only.")]
    [SerializeField] private StoryRewardEntry[] rewardEntries = new StoryRewardEntry[0];

    [Header("Flow")]
    [Tooltip("Chapter that follows this one (onboarding continues into it automatically).")]
    [SerializeField] private StoryData nextStory;

    [Tooltip("Onboarding: scene opened after this chapter instead of continuing (e.g. Shop, Gacha).")]
    [SerializeField] private SceneReference afterScene;

    [Tooltip("Onboarding: toasts shown in the After Scene.")]
    [SerializeField] private StoryToastLine[] afterSceneToasts = new StoryToastLine[0];

    [Tooltip("Onboarding: toasts shown after the player's first gacha pull (reactions).")]
    [SerializeField] private StoryToastLine[] afterGachaPullToasts = new StoryToastLine[0];

    [Header("Time Limit")]
    [Tooltip("Maximum time allowed to complete this story, in seconds.")]
    [SerializeField, Min(1)] private int timeLimitSeconds = 180;

    [Header("Music")]
    [SerializeField] private MusicReference storyMusic;

    public string StoryId => storyId;
    public string StoryName => storyName;
    public string StoryDescription => storyDescription;
    public string ChapterLabel => chapterLabel;
    public StorySegment[] Segments => segments;
    public bool HasSegments => segments != null && segments.Length > 0;
    public Sprite Background => background;
    public StoryRewardEntry[] RewardEntries => rewardEntries;
    public StoryData NextStory => nextStory;
    public SceneReference AfterScene => afterScene;
    public StoryToastLine[] AfterSceneToasts => afterSceneToasts;
    public StoryToastLine[] AfterGachaPullToasts => afterGachaPullToasts;
    public StoryData PreviousStory => previousStory;
    public GameObject StoryPrefab => storyPrefab;
    public RewardData[] StoryRewards => storyRewards;
    public int TimeLimitSeconds => timeLimitSeconds;
    public MusicReference StoryMusic => storyMusic;
}
